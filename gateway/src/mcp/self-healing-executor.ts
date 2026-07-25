import { ToolExecutor, ToolCall, ToolResult } from '../agent/tool-executor';
import { ToolFailureLogger, FailureType } from './tool-failure-logger';
import { ToolFixer } from './tool-fixer';
import { ToolGenerator } from './tool-generator';
import { UnifiedToolRegistry } from '../agent/tool-registry';
import { GatewayConfig } from '../types';
import { GatewayMcpServer } from './gateway-mcp-server';

export interface SelfHealingConfig {
  maxRetries: number;
  enableAutoFix: boolean;
  enableAutoGenerate: boolean;
  logDir?: string;
  configDir?: string;
  generatedToolsDir?: string;
}

/**
 * Self-Healing Tool Executor
 *
 * Wraps the standard ToolExecutor with automatic failure detection,
 * diagnosis, fixing, and tool generation capabilities.
 *
 * Flow:
 * 1. Execute tool
 * 2. If failure → Log failure
 * 3. If failure → Diagnose and attempt fix
 * 4. Retry with fixed configuration
 * 5. If still fails → Generate new tool implementation
 * 6. Register new tool to MCP
 * 7. Retry with new tool
 */
export class SelfHealingExecutor {
  private executor: ToolExecutor;
  private registry: UnifiedToolRegistry;
  private logger: ToolFailureLogger;
  private fixer: ToolFixer;
  private generator: ToolGenerator;
  private config: GatewayConfig;
  private healingConfig: SelfHealingConfig;
  private mcpServer?: GatewayMcpServer;

  constructor(
    config: GatewayConfig,
    registry: UnifiedToolRegistry,
    executor: ToolExecutor,
    healingConfig: Partial<SelfHealingConfig> = {}
  ) {
    this.config = config;
    this.registry = registry;
    this.executor = executor;

    // Default healing config
    this.healingConfig = {
      maxRetries: 3,
      enableAutoFix: true,
      enableAutoGenerate: true,
      logDir: healingConfig.logDir || './logs/tools',
      configDir: healingConfig.configDir || './config/tools',
      generatedToolsDir: healingConfig.generatedToolsDir || './src/tools/generated',
      ...healingConfig
    };

    // Initialize self-healing components
    this.logger = new ToolFailureLogger(this.healingConfig.logDir);
    this.fixer = new ToolFixer(this.logger, this.registry, this.healingConfig.configDir);
    this.generator = new ToolGenerator(this.healingConfig.generatedToolsDir);

    console.log('🔧 [Self-Healing] Initialized with config:', {
      maxRetries: this.healingConfig.maxRetries,
      autoFix: this.healingConfig.enableAutoFix,
      autoGenerate: this.healingConfig.enableAutoGenerate
    });
  }

  /**
   * Set MCP server for dynamic tool registration
   */
  setMcpServer(server: GatewayMcpServer): void {
    this.mcpServer = server;
  }

  /**
   * Execute tool with self-healing capabilities
   */
  async execute(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const startTime = Date.now();
    let attemptNumber = 1;
    let lastError: Error | null = null;
    let lastResult: ToolResult | null = null;

    console.log(`\n🔧 [Self-Healing] Executing tool: ${toolCall.name} (attempt ${attemptNumber}/${this.healingConfig.maxRetries})`);

    // Try executing the tool with retries
    while (attemptNumber <= this.healingConfig.maxRetries) {
      try {
        const result = await this.executor.execute(toolCall, sessionKey);

        // Check if execution succeeded
        if (!result.is_error) {
          const executionTime = Date.now() - startTime;

          // Log success
          this.logger.logSuccess({
            timestamp: new Date().toISOString(),
            toolName: toolCall.name,
            executionTimeMs: executionTime,
            input: toolCall.input
          });

          console.log(`✅ [Self-Healing] Tool executed successfully: ${toolCall.name} (${executionTime}ms)`);

          return result;
        }

        // Tool returned error
        lastResult = result;
        lastError = new Error(typeof result.content === 'string' ? result.content : JSON.stringify(result.content));

        console.log(`⚠️  [Self-Healing] Tool returned error (attempt ${attemptNumber}): ${lastError.message}`);

      } catch (error) {
        lastError = error instanceof Error ? error : new Error(String(error));
        console.log(`❌ [Self-Healing] Tool execution threw error (attempt ${attemptNumber}): ${lastError.message}`);
      }

      // Log the failure
      const executionTime = Date.now() - startTime;
      const failureType = this.logger.categorizeError(lastError, executionTime);

      const failure = {
        timestamp: new Date().toISOString(),
        toolName: toolCall.name,
        failureType,
        errorMessage: lastError.message,
        stackTrace: lastError.stack,
        input: toolCall.input,
        executionTimeMs: executionTime,
        attemptNumber
      };

      this.logger.logFailure(failure);

      // If we've exhausted retries, try to fix or generate
      if (attemptNumber === this.healingConfig.maxRetries) {
        console.log(`\n🔍 [Self-Healing] Max retries reached. Attempting self-healing...`);

        // Try to fix the tool
        if (this.healingConfig.enableAutoFix) {
          const diagnosis = await this.fixer.diagnoseAndFix(toolCall.name);

          console.log(`\n📋 [Self-Healing] Diagnosis:`);
          console.log(diagnosis.diagnosis);

          // If fix succeeded, retry once more
          if (diagnosis.shouldRetry) {
            console.log(`\n🔄 [Self-Healing] Fix applied. Retrying tool execution...`);

            try {
              const retryResult = await this.executor.execute(toolCall, sessionKey);

              if (!retryResult.is_error) {
                console.log(`✅ [Self-Healing] Tool succeeded after fix!`);

                // Log the successful recovery
                this.logger.logSuccess({
                  timestamp: new Date().toISOString(),
                  toolName: toolCall.name,
                  executionTimeMs: Date.now() - startTime,
                  input: toolCall.input
                });

                return retryResult;
              }

              console.log(`⚠️  [Self-Healing] Tool still failing after fix`);
            } catch (retryError) {
              console.log(`❌ [Self-Healing] Retry after fix failed:`, retryError);
            }
          }

          // If we should generate a new tool
          if (diagnosis.shouldCreateNewTool && this.healingConfig.enableAutoGenerate) {
            console.log(`\n🔨 [Self-Healing] Generating new tool implementation...`);

            try {
              // Get original tool definition
              const tools = await this.registry.getTools();
              const originalTool = tools.find(t => t.name === toolCall.name);

              if (!originalTool) {
                console.log(`❌ [Self-Healing] Cannot find original tool definition for ${toolCall.name}`);
              } else {
                // Generate new tool
                const generatedTool = await this.generator.generateTool(
                  toolCall.name,
                  originalTool,
                  undefined, // Auto-select template
                  failure
                );

                console.log(`✅ [Self-Healing] Generated new tool: ${generatedTool.name}`);
                console.log(`   Implementation: ${generatedTool.implementationType}`);
                console.log(`   File: ${generatedTool.filePath}`);

                // Register to MCP server if available
                if (this.mcpServer) {
                  await this.mcpServer.registerNewTool(generatedTool.definition);
                  console.log(`✅ [Self-Healing] Tool registered to MCP server`);
                } else {
                  console.log(`⚠️  [Self-Healing] MCP server not set - tool not auto-registered`);
                }

                // Refresh registry
                await this.registry.refresh();

                // Try executing with new tool
                console.log(`\n🔄 [Self-Healing] Retrying with generated tool...`);

                try {
                  const generatedResult = await this.executor.execute(
                    {
                      ...toolCall,
                      name: generatedTool.name // Use generated tool name
                    },
                    sessionKey
                  );

                  if (!generatedResult.is_error) {
                    console.log(`✅ [Self-Healing] Generated tool succeeded!`);

                    // Log recovery
                    this.logger.logSuccess({
                      timestamp: new Date().toISOString(),
                      toolName: generatedTool.name,
                      executionTimeMs: Date.now() - startTime,
                      input: toolCall.input
                    });

                    return generatedResult;
                  }

                  console.log(`⚠️  [Self-Healing] Generated tool also failed`);
                } catch (genError) {
                  console.log(`❌ [Self-Healing] Generated tool execution failed:`, genError);
                }
              }
            } catch (genError) {
              console.log(`❌ [Self-Healing] Tool generation failed:`, genError);
            }
          }
        }

        // All recovery attempts failed
        console.log(`\n❌ [Self-Healing] All recovery attempts exhausted for ${toolCall.name}`);

        // Generate comprehensive failure report
        const failureReport = this.logger.createFailureReport(toolCall.name);
        console.log(`\n📊 [Self-Healing] Failure Report:\n${failureReport}`);

        // Return the last error result
        return lastResult || {
          tool_use_id: toolCall.id,
          content: `Tool ${toolCall.name} failed after ${attemptNumber} attempts and self-healing. Error: ${lastError?.message}`,
          is_error: true
        };
      }

      // Wait before retry (exponential backoff)
      const delayMs = Math.min(1000 * Math.pow(2, attemptNumber - 1), 5000);
      console.log(`⏳ [Self-Healing] Waiting ${delayMs}ms before retry...`);
      await new Promise(resolve => setTimeout(resolve, delayMs));

      attemptNumber++;
    }

    // Should never reach here, but just in case
    return {
      tool_use_id: toolCall.id,
      content: `Tool ${toolCall.name} failed unexpectedly`,
      is_error: true
    };
  }

  /**
   * Get self-healing statistics
   */
  getStats(): {
    totalTools: number;
    failedTools: string[];
    fixedTools: string[];
    generatedTools: number;
  } {
    // TODO: Implement comprehensive stats
    return {
      totalTools: 0,
      failedTools: [],
      fixedTools: [],
      generatedTools: 0
    };
  }

  /**
   * Clear all failure history
   */
  clearHistory(): void {
    this.logger.clearHistory();
    console.log('✓ [Self-Healing] Failure history cleared');
  }
}
