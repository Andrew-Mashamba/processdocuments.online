import { ToolFailureLogger, FailureType, ToolFailure } from './tool-failure-logger';
import { UnifiedToolRegistry } from '../agent/tool-registry';
import * as fs from 'fs';
import * as path from 'path';

export interface FixAttempt {
  timestamp: string;
  toolName: string;
  failureType: FailureType;
  fixStrategy: string;
  success: boolean;
  details: string;
}

export interface FixStrategy {
  name: string;
  description: string;
  applicableToFailureTypes: FailureType[];
  apply: (toolName: string, failure: ToolFailure) => Promise<boolean>;
}

export class ToolFixer {
  private logger: ToolFailureLogger;
  private registry: UnifiedToolRegistry;
  private fixStrategies: Map<string, FixStrategy>;
  private fixHistory: Map<string, FixAttempt[]>;
  private configDir: string;

  constructor(
    logger: ToolFailureLogger,
    registry: UnifiedToolRegistry,
    configDir: string = './config/tools'
  ) {
    this.logger = logger;
    this.registry = registry;
    this.fixStrategies = new Map();
    this.fixHistory = new Map();
    this.configDir = configDir;

    this.ensureConfigDirectory();
    this.registerDefaultStrategies();
  }

  private ensureConfigDirectory(): void {
    if (!fs.existsSync(this.configDir)) {
      fs.mkdirSync(this.configDir, { recursive: true });
    }
  }

  private registerDefaultStrategies(): void {
    // Strategy 1: Increase timeout for timeout failures
    this.registerStrategy({
      name: 'increase_timeout',
      description: 'Increase tool timeout configuration',
      applicableToFailureTypes: [FailureType.TIMEOUT],
      apply: async (toolName: string, failure: ToolFailure) => {
        const configPath = path.join(this.configDir, `${toolName}.json`);
        let config: any = {};

        if (fs.existsSync(configPath)) {
          config = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
        }

        const currentTimeout = config.timeout || 30000;
        const newTimeout = Math.min(currentTimeout * 2, 120000); // Max 2 minutes

        config.timeout = newTimeout;
        fs.writeFileSync(configPath, JSON.stringify(config, null, 2));

        console.log(`✓ Increased timeout for ${toolName}: ${currentTimeout}ms → ${newTimeout}ms`);
        return true;
      }
    });

    // Strategy 2: Check and restart API for API errors
    this.registerStrategy({
      name: 'check_api_health',
      description: 'Check API health and suggest restart if needed',
      applicableToFailureTypes: [FailureType.API_ERROR],
      apply: async (toolName: string, failure: ToolFailure) => {
        // For ZIMA tools, check if ZIMA API is responsive
        if (toolName.startsWith('create_') ||
            toolName.startsWith('convert_') ||
            toolName.includes('excel') ||
            toolName.includes('pdf') ||
            toolName.includes('word')) {

          try {
            const response = await fetch('http://localhost:5000/health', {
              method: 'GET',
              signal: AbortSignal.timeout(5000)
            });

            if (!response.ok) {
              console.log(`⚠️  ZIMA API health check failed: ${response.status}`);
              console.log(`   Recommendation: Restart ZIMA API service`);
              return false;
            }

            console.log(`✓ ZIMA API is healthy`);
            return true;
          } catch (err) {
            console.log(`❌ ZIMA API is not responding`);
            console.log(`   Recommendation: Start ZIMA API with: cd /Volumes/DATA/QWEN/zima-file-service && dotnet run`);
            return false;
          }
        }

        return true;
      }
    });

    // Strategy 3: Retry with exponential backoff
    this.registerStrategy({
      name: 'exponential_backoff',
      description: 'Add retry delay for transient failures',
      applicableToFailureTypes: [FailureType.API_ERROR, FailureType.EXECUTION_ERROR],
      apply: async (toolName: string, failure: ToolFailure) => {
        const attempt = failure.attemptNumber || 1;
        const delayMs = Math.min(1000 * Math.pow(2, attempt - 1), 10000); // Max 10s

        console.log(`⏳ Applying exponential backoff: ${delayMs}ms before retry #${attempt + 1}`);
        await new Promise(resolve => setTimeout(resolve, delayMs));

        return true;
      }
    });

    // Strategy 4: Validate input parameters
    this.registerStrategy({
      name: 'validate_input',
      description: 'Check and fix invalid input parameters',
      applicableToFailureTypes: [FailureType.INVALID_INPUT],
      apply: async (toolName: string, failure: ToolFailure) => {
        const tools = await this.registry.getTools();
        const tool = tools.find((t: any) => t.name === toolName);

        if (!tool) {
          console.log(`❌ Tool ${toolName} not found in registry`);
          return false;
        }

        const schema = tool.input_schema;
        const required = schema.required || [];
        const input = failure.input;

        let hasIssues = false;
        const issues: string[] = [];

        // Check required fields
        required.forEach((field: string) => {
          if (!(field in input) || input[field] === null || input[field] === undefined) {
            issues.push(`Missing required field: ${field}`);
            hasIssues = true;
          }
        });

        if (hasIssues) {
          console.log(`⚠️  Input validation issues for ${toolName}:`);
          issues.forEach(issue => console.log(`   - ${issue}`));
          return false;
        }

        console.log(`✓ Input validation passed for ${toolName}`);
        return true;
      }
    });
  }

  registerStrategy(strategy: FixStrategy): void {
    this.fixStrategies.set(strategy.name, strategy);
  }

  async attemptFix(toolName: string, failure: ToolFailure): Promise<FixAttempt> {
    console.log(`\n🔧 Attempting to fix tool: ${toolName}`);
    console.log(`   Failure type: ${failure.failureType}`);
    console.log(`   Error: ${failure.errorMessage}`);

    // Find applicable strategies
    const applicableStrategies = Array.from(this.fixStrategies.values()).filter(
      strategy => strategy.applicableToFailureTypes.includes(failure.failureType)
    );

    if (applicableStrategies.length === 0) {
      console.log(`   No fix strategies available for ${failure.failureType}`);

      const attempt: FixAttempt = {
        timestamp: new Date().toISOString(),
        toolName,
        failureType: failure.failureType,
        fixStrategy: 'none',
        success: false,
        details: `No applicable fix strategies for ${failure.failureType}`
      };

      this.recordFixAttempt(attempt);
      return attempt;
    }

    // Try each strategy
    for (const strategy of applicableStrategies) {
      console.log(`\n   Applying strategy: ${strategy.name}`);
      console.log(`   ${strategy.description}`);

      try {
        const success = await strategy.apply(toolName, failure);

        const attempt: FixAttempt = {
          timestamp: new Date().toISOString(),
          toolName,
          failureType: failure.failureType,
          fixStrategy: strategy.name,
          success,
          details: success ? `Successfully applied ${strategy.name}` : `Failed to apply ${strategy.name}`
        };

        this.recordFixAttempt(attempt);

        if (success) {
          console.log(`   ✅ Fix successful: ${strategy.name}`);
          return attempt;
        } else {
          console.log(`   ⚠️  Fix unsuccessful: ${strategy.name}`);
        }
      } catch (err) {
        console.log(`   ❌ Fix error: ${err instanceof Error ? err.message : String(err)}`);

        const attempt: FixAttempt = {
          timestamp: new Date().toISOString(),
          toolName,
          failureType: failure.failureType,
          fixStrategy: strategy.name,
          success: false,
          details: `Error applying ${strategy.name}: ${err instanceof Error ? err.message : String(err)}`
        };

        this.recordFixAttempt(attempt);
      }
    }

    // All strategies failed
    const attempt: FixAttempt = {
      timestamp: new Date().toISOString(),
      toolName,
      failureType: failure.failureType,
      fixStrategy: 'all_strategies_failed',
      success: false,
      details: `Tried ${applicableStrategies.length} strategies, all failed`
    };

    this.recordFixAttempt(attempt);
    return attempt;
  }

  private recordFixAttempt(attempt: FixAttempt): void {
    if (!this.fixHistory.has(attempt.toolName)) {
      this.fixHistory.set(attempt.toolName, []);
    }
    this.fixHistory.get(attempt.toolName)!.push(attempt);

    // Also log to file
    const logPath = path.join(this.configDir, 'fix-attempts.jsonl');
    fs.appendFileSync(logPath, JSON.stringify(attempt) + '\n');
  }

  getFixHistory(toolName: string): FixAttempt[] {
    return this.fixHistory.get(toolName) || [];
  }

  async diagnoseAndFix(toolName: string): Promise<{
    shouldRetry: boolean;
    shouldCreateNewTool: boolean;
    diagnosis: string;
    fixAttempts: FixAttempt[];
  }> {
    console.log(`\n🔍 Diagnosing tool failures for: ${toolName}`);

    const pattern = this.logger.getFailurePattern(toolName);
    const recentFailures = this.logger.getRecentFailures(toolName, 3);

    if (recentFailures.length === 0) {
      return {
        shouldRetry: true,
        shouldCreateNewTool: false,
        diagnosis: 'No recent failures found',
        fixAttempts: []
      };
    }

    console.log(`\n📊 Failure Analysis:`);
    console.log(`   Total failures: ${pattern.totalFailures}`);
    console.log(`   Recent failures: ${recentFailures.length}`);

    const fixAttempts: FixAttempt[] = [];

    // Try to fix the most recent failure
    const latestFailure = recentFailures[recentFailures.length - 1];
    const fixAttempt = await this.attemptFix(toolName, latestFailure);
    fixAttempts.push(fixAttempt);

    // Decide whether to retry or create new tool
    const shouldRetry = fixAttempt.success;

    // If we've failed too many times, create new tool
    const recentFixAttempts = this.getFixHistory(toolName).slice(-3);
    const allRecentFixesFailed = recentFixAttempts.length >= 2 &&
                                  recentFixAttempts.every(a => !a.success);

    // Trigger tool generation after 3 failures, or if fix didn't help, or if all recent fixes failed
    const shouldCreateNewTool = allRecentFixesFailed ||
                                 pattern.totalFailures >= 3 ||
                                 (!fixAttempt.success && pattern.totalFailures >= 2);

    const diagnosis = this.generateDiagnosis(pattern, recentFailures, fixAttempts);

    return {
      shouldRetry,
      shouldCreateNewTool,
      diagnosis,
      fixAttempts
    };
  }

  private generateDiagnosis(
    pattern: any,
    recentFailures: ToolFailure[],
    fixAttempts: FixAttempt[]
  ): string {
    let diagnosis = '';

    // Most common failure type
    const failureEntries = Object.entries(pattern.failureTypes) as [FailureType, number][];
    const sortedFailures = failureEntries.sort((a, b) => b[1] - a[1]);
    const mostCommonFailure = sortedFailures[0];

    diagnosis += `Most common failure: ${mostCommonFailure[0]} (${mostCommonFailure[1]} occurrences)\n`;

    if (pattern.averageExecutionTime) {
      diagnosis += `Average execution time: ${pattern.averageExecutionTime.toFixed(2)}ms\n`;
    }

    // Fix success rate
    const successfulFixes = fixAttempts.filter(a => a.success).length;
    diagnosis += `Fix success rate: ${successfulFixes}/${fixAttempts.length}\n`;

    // Recommendations
    if (mostCommonFailure[0] === FailureType.TIMEOUT) {
      diagnosis += `\nRecommendation: Tool consistently times out. Consider:\n`;
      diagnosis += `  - Increasing timeout threshold\n`;
      diagnosis += `  - Optimizing tool implementation\n`;
      diagnosis += `  - Creating alternative implementation\n`;
    } else if (mostCommonFailure[0] === FailureType.API_ERROR) {
      diagnosis += `\nRecommendation: API connectivity issues. Consider:\n`;
      diagnosis += `  - Checking if API service is running\n`;
      diagnosis += `  - Verifying network connectivity\n`;
      diagnosis += `  - Creating direct implementation without API\n`;
    }

    return diagnosis;
  }
}
