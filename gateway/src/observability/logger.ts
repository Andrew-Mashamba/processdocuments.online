/**
 * Structured Logger
 * Week 12: Production Polish - Observability
 */

import * as fs from 'fs-extra';
import * as path from 'path';

export enum LogLevel {
  DEBUG = 'debug',
  INFO = 'info',
  WARN = 'warn',
  ERROR = 'error',
  FATAL = 'fatal'
}

export interface LogEntry {
  level: LogLevel;
  message: string;
  timestamp: string;
  context?: Record<string, any>;
  error?: {
    message: string;
    stack?: string;
    code?: string;
  };
  trace_id?: string;
  span_id?: string;
  service: string;
  environment: string;
}

export class StructuredLogger {
  private logDir: string;
  private service: string;
  private environment: string;
  private minLevel: LogLevel;
  private console: boolean;

  constructor(options: {
    logDir?: string;
    service?: string;
    environment?: string;
    minLevel?: LogLevel;
    console?: boolean;
  } = {}) {
    this.logDir = options.logDir || './logs';
    this.service = options.service || 'zima-gateway';
    this.environment = options.environment || process.env.NODE_ENV || 'development';
    this.minLevel = options.minLevel || LogLevel.INFO;
    this.console = options.console ?? true;

    this.ensureLogDirectory();
  }

  private ensureLogDirectory(): void {
    if (!fs.existsSync(this.logDir)) {
      fs.mkdirSync(this.logDir, { recursive: true });
    }
  }

  private shouldLog(level: LogLevel): boolean {
    const levels = [LogLevel.DEBUG, LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL];
    const minIndex = levels.indexOf(this.minLevel);
    const currentIndex = levels.indexOf(level);
    return currentIndex >= minIndex;
  }

  private buildLogEntry(
    level: LogLevel,
    message: string,
    context?: Record<string, any>,
    error?: Error
  ): LogEntry {
    const entry: LogEntry = {
      level,
      message,
      timestamp: new Date().toISOString(),
      service: this.service,
      environment: this.environment
    };

    if (context) {
      entry.context = context;
      // Extract trace_id and span_id if present
      if (context.trace_id) entry.trace_id = context.trace_id;
      if (context.span_id) entry.span_id = context.span_id;
    }

    if (error) {
      entry.error = {
        message: error.message,
        stack: error.stack,
        code: (error as any).code
      };
    }

    return entry;
  }

  private writeLog(entry: LogEntry): void {
    const logFile = path.join(
      this.logDir,
      `${entry.level}-${new Date().toISOString().split('T')[0]}.jsonl`
    );

    fs.appendFileSync(logFile, JSON.stringify(entry) + '\n');
  }

  private consoleLog(entry: LogEntry): void {
    if (!this.console) return;

    const color = {
      [LogLevel.DEBUG]: '\x1b[36m',   // Cyan
      [LogLevel.INFO]: '\x1b[32m',    // Green
      [LogLevel.WARN]: '\x1b[33m',    // Yellow
      [LogLevel.ERROR]: '\x1b[31m',   // Red
      [LogLevel.FATAL]: '\x1b[35m'    // Magenta
    }[entry.level];

    const reset = '\x1b[0m';

    let msg = `${color}[${entry.level.toUpperCase()}]${reset} ${entry.timestamp} - ${entry.message}`;

    if (entry.context) {
      msg += ` ${JSON.stringify(entry.context)}`;
    }

    if (entry.error) {
      msg += `\n  Error: ${entry.error.message}`;
      if (entry.error.stack) {
        msg += `\n${entry.error.stack}`;
      }
    }

    console.log(msg);
  }

  debug(message: string, context?: Record<string, any>): void {
    if (!this.shouldLog(LogLevel.DEBUG)) return;

    const entry = this.buildLogEntry(LogLevel.DEBUG, message, context);
    this.writeLog(entry);
    this.consoleLog(entry);
  }

  info(message: string, context?: Record<string, any>): void {
    if (!this.shouldLog(LogLevel.INFO)) return;

    const entry = this.buildLogEntry(LogLevel.INFO, message, context);
    this.writeLog(entry);
    this.consoleLog(entry);
  }

  warn(message: string, context?: Record<string, any>): void {
    if (!this.shouldLog(LogLevel.WARN)) return;

    const entry = this.buildLogEntry(LogLevel.WARN, message, context);
    this.writeLog(entry);
    this.consoleLog(entry);
  }

  error(message: string, error?: Error, context?: Record<string, any>): void {
    if (!this.shouldLog(LogLevel.ERROR)) return;

    const entry = this.buildLogEntry(LogLevel.ERROR, message, context, error);
    this.writeLog(entry);
    this.consoleLog(entry);
  }

  fatal(message: string, error?: Error, context?: Record<string, any>): void {
    if (!this.shouldLog(LogLevel.FATAL)) return;

    const entry = this.buildLogEntry(LogLevel.FATAL, message, context, error);
    this.writeLog(entry);
    this.consoleLog(entry);
  }

  /**
   * Create child logger with additional context
   */
  child(context: Record<string, any>): ChildLogger {
    return new ChildLogger(this, context);
  }

  /**
   * Query logs
   */
  async queryLogs(options: {
    level?: LogLevel;
    since?: Date;
    until?: Date;
    limit?: number;
    service?: string;
  } = {}): Promise<LogEntry[]> {
    const { level, since, until, limit = 100, service } = options;

    const logs: LogEntry[] = [];
    const files = await fs.readdir(this.logDir);

    for (const file of files.filter(f => f.endsWith('.jsonl'))) {
      if (level && !file.startsWith(level)) continue;

      const filePath = path.join(this.logDir, file);
      const lines = (await fs.readFile(filePath, 'utf-8')).split('\n').filter(l => l.trim());

      for (const line of lines) {
        try {
          const entry: LogEntry = JSON.parse(line);

          // Apply filters
          if (service && entry.service !== service) continue;
          if (since && new Date(entry.timestamp) < since) continue;
          if (until && new Date(entry.timestamp) > until) continue;

          logs.push(entry);
        } catch (err) {
          // Skip invalid lines
        }
      }
    }

    // Sort by timestamp (newest first)
    logs.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());

    return logs.slice(0, limit);
  }
}

class ChildLogger {
  constructor(
    private parent: StructuredLogger,
    private childContext: Record<string, any>
  ) {}

  private mergeContext(context?: Record<string, any>): Record<string, any> {
    return { ...this.childContext, ...context };
  }

  debug(message: string, context?: Record<string, any>): void {
    this.parent.debug(message, this.mergeContext(context));
  }

  info(message: string, context?: Record<string, any>): void {
    this.parent.info(message, this.mergeContext(context));
  }

  warn(message: string, context?: Record<string, any>): void {
    this.parent.warn(message, this.mergeContext(context));
  }

  error(message: string, error?: Error, context?: Record<string, any>): void {
    this.parent.error(message, error, this.mergeContext(context));
  }

  fatal(message: string, error?: Error, context?: Record<string, any>): void {
    this.parent.fatal(message, error, this.mergeContext(context));
  }
}

// Global logger instance
let globalLogger: StructuredLogger;

export function getLogger(options?: {
  logDir?: string;
  service?: string;
  environment?: string;
  minLevel?: LogLevel;
  console?: boolean;
}): StructuredLogger {
  if (!globalLogger) {
    globalLogger = new StructuredLogger(options);
  }
  return globalLogger;
}
