import * as fs from 'fs';
import * as path from 'path';

export enum FailureType {
  TIMEOUT = 'timeout',
  API_ERROR = 'api_error',
  MISSING_DEPENDENCY = 'missing_dependency',
  INVALID_INPUT = 'invalid_input',
  EXECUTION_ERROR = 'execution_error',
  UNKNOWN = 'unknown'
}

export interface ToolFailure {
  timestamp: string;
  toolName: string;
  failureType: FailureType;
  errorMessage: string;
  stackTrace?: string;
  input: any;
  executionTimeMs?: number;
  attemptNumber: number;
}

export interface ToolSuccess {
  timestamp: string;
  toolName: string;
  executionTimeMs: number;
  input: any;
}

export class ToolFailureLogger {
  private logDir: string;
  private failureLog: string;
  private successLog: string;
  private failureHistory: Map<string, ToolFailure[]>;

  constructor(logDir: string = './logs/tools') {
    this.logDir = logDir;
    this.failureLog = path.join(logDir, 'failures.jsonl');
    this.successLog = path.join(logDir, 'successes.jsonl');
    this.failureHistory = new Map();

    this.ensureLogDirectory();
    this.loadFailureHistory();
  }

  private ensureLogDirectory(): void {
    if (!fs.existsSync(this.logDir)) {
      fs.mkdirSync(this.logDir, { recursive: true });
    }
  }

  private loadFailureHistory(): void {
    if (fs.existsSync(this.failureLog)) {
      const lines = fs.readFileSync(this.failureLog, 'utf-8').split('\n').filter(l => l.trim());
      lines.forEach(line => {
        try {
          const failure: ToolFailure = JSON.parse(line);
          if (!this.failureHistory.has(failure.toolName)) {
            this.failureHistory.set(failure.toolName, []);
          }
          this.failureHistory.get(failure.toolName)!.push(failure);
        } catch (err) {
          // Skip invalid lines
        }
      });
    }
  }

  logFailure(failure: ToolFailure): void {
    // Add to in-memory history
    if (!this.failureHistory.has(failure.toolName)) {
      this.failureHistory.set(failure.toolName, []);
    }
    this.failureHistory.get(failure.toolName)!.push(failure);

    // Append to log file
    fs.appendFileSync(this.failureLog, JSON.stringify(failure) + '\n');
  }

  logSuccess(success: ToolSuccess): void {
    fs.appendFileSync(this.successLog, JSON.stringify(success) + '\n');
  }

  getFailureHistory(toolName: string): ToolFailure[] {
    return this.failureHistory.get(toolName) || [];
  }

  getRecentFailures(toolName: string, limit: number = 5): ToolFailure[] {
    const history = this.getFailureHistory(toolName);
    return history.slice(-limit);
  }

  getFailurePattern(toolName: string): {
    totalFailures: number;
    failureTypes: Record<FailureType, number>;
    averageExecutionTime?: number;
    lastFailure?: ToolFailure;
  } {
    const history = this.getFailureHistory(toolName);

    if (history.length === 0) {
      return {
        totalFailures: 0,
        failureTypes: {} as Record<FailureType, number>
      };
    }

    const failureTypes: Record<FailureType, number> = {
      [FailureType.TIMEOUT]: 0,
      [FailureType.API_ERROR]: 0,
      [FailureType.MISSING_DEPENDENCY]: 0,
      [FailureType.INVALID_INPUT]: 0,
      [FailureType.EXECUTION_ERROR]: 0,
      [FailureType.UNKNOWN]: 0
    };

    let totalExecutionTime = 0;
    let executionTimeCount = 0;

    history.forEach(failure => {
      failureTypes[failure.failureType]++;
      if (failure.executionTimeMs) {
        totalExecutionTime += failure.executionTimeMs;
        executionTimeCount++;
      }
    });

    return {
      totalFailures: history.length,
      failureTypes,
      averageExecutionTime: executionTimeCount > 0 ? totalExecutionTime / executionTimeCount : undefined,
      lastFailure: history[history.length - 1]
    };
  }

  categorizeError(error: Error | string, executionTimeMs?: number): FailureType {
    const errorMsg = typeof error === 'string' ? error : error.message;
    const errorMsgLower = errorMsg.toLowerCase();

    // Timeout detection
    if (executionTimeMs && executionTimeMs > 25000) {
      return FailureType.TIMEOUT;
    }
    if (errorMsgLower.includes('timeout') || errorMsgLower.includes('timed out')) {
      return FailureType.TIMEOUT;
    }

    // API errors
    if (errorMsgLower.includes('api') ||
        errorMsgLower.includes('fetch') ||
        errorMsgLower.includes('econnrefused') ||
        errorMsgLower.includes('network') ||
        errorMsgLower.includes('connection refused')) {
      return FailureType.API_ERROR;
    }

    // Missing dependencies
    if (errorMsgLower.includes('cannot find module') ||
        errorMsgLower.includes('not found') ||
        errorMsgLower.includes('enoent')) {
      return FailureType.MISSING_DEPENDENCY;
    }

    // Invalid input
    if (errorMsgLower.includes('invalid') ||
        errorMsgLower.includes('validation') ||
        errorMsgLower.includes('required parameter')) {
      return FailureType.INVALID_INPUT;
    }

    // Execution errors
    if (errorMsgLower.includes('error executing') ||
        errorMsgLower.includes('execution failed')) {
      return FailureType.EXECUTION_ERROR;
    }

    return FailureType.UNKNOWN;
  }

  createFailureReport(toolName: string): string {
    const pattern = this.getFailurePattern(toolName);
    const recentFailures = this.getRecentFailures(toolName, 3);

    let report = `Tool Failure Report: ${toolName}\n`;
    report += `${'='.repeat(60)}\n\n`;
    report += `Total Failures: ${pattern.totalFailures}\n`;

    if (pattern.averageExecutionTime) {
      report += `Average Execution Time: ${pattern.averageExecutionTime.toFixed(2)}ms\n`;
    }

    report += `\nFailure Types:\n`;
    Object.entries(pattern.failureTypes).forEach(([type, count]) => {
      if (count > 0) {
        report += `  - ${type}: ${count} (${((count / pattern.totalFailures) * 100).toFixed(1)}%)\n`;
      }
    });

    if (recentFailures.length > 0) {
      report += `\nRecent Failures:\n`;
      recentFailures.forEach((failure, idx) => {
        report += `\n  ${idx + 1}. ${failure.timestamp}\n`;
        report += `     Type: ${failure.failureType}\n`;
        report += `     Error: ${failure.errorMessage}\n`;
        if (failure.executionTimeMs) {
          report += `     Duration: ${failure.executionTimeMs}ms\n`;
        }
      });
    }

    return report;
  }

  clearHistory(toolName?: string): void {
    if (toolName) {
      this.failureHistory.delete(toolName);
    } else {
      this.failureHistory.clear();
      if (fs.existsSync(this.failureLog)) {
        fs.unlinkSync(this.failureLog);
      }
      if (fs.existsSync(this.successLog)) {
        fs.unlinkSync(this.successLog);
      }
    }
  }
}
