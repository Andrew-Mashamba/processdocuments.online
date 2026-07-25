/**
 * Exec Runner - Secure command execution
 * Week 9-10: Exec & Queue - Phase B
 */

import * as shell from 'shelljs';
import * as path from 'path';
import { EventEmitter } from 'events';

export interface ExecOptions {
  cwd?: string;
  env?: Record<string, string>;
  timeout?: number;
  shell?: string;
  maxBuffer?: number;
  silent?: boolean;
}

export interface ExecResult {
  success: boolean;
  stdout: string;
  stderr: string;
  exitCode: number;
  duration: number;
  command: string;
  error?: string;
}

/**
 * Command blacklist - dangerous commands that should never be executed
 */
const COMMAND_BLACKLIST: RegExp[] = [
  /rm\s+-rf\s+\//i,          // Prevent recursive delete from root
  /rm\s+-rf\s+\*/i,          // Prevent recursive delete all
  /mkfs/i,                    // Prevent filesystem formatting
  /dd\s+if=/i,               // Prevent disk operations
  /:\(\)\{\s*:\|:\&\s*\}/,   // Fork bomb
  /\/dev\/sda/i,             // Direct disk access
  /\/dev\/hda/i,             // Direct disk access
  /fdisk/i,                   // Disk partitioning
  /parted/i,                  // Disk partitioning
  /chmod\s+-R\s+777/i,       // Dangerous permissions
  /chown\s+-R/i,             // Mass ownership change
  /shutdown/i,                // System shutdown
  /reboot/i,                  // System reboot
  /halt/i,                    // System halt
  /poweroff/i,                // System poweroff
  /init\s+0/i,               // Init shutdown
  /init\s+6/i,               // Init reboot
];

/**
 * Command whitelist mode - when enabled, only these commands are allowed
 */
const COMMAND_WHITELIST: string[] = [
  'ls', 'cat', 'echo', 'pwd', 'whoami', 'date', 'grep', 'find',
  'head', 'tail', 'wc', 'sort', 'uniq', 'cut', 'sed', 'awk',
  'git', 'npm', 'node', 'python', 'python3', 'pip', 'pip3',
  'docker', 'docker-compose', 'curl', 'wget', 'tar', 'gzip',
  'mkdir', 'touch', 'cp', 'mv', 'ln'
];

export class ExecRunner extends EventEmitter {
  private defaultTimeout: number = 60000; // 60 seconds
  private whitelistMode: boolean = false;
  private allowDangerous: boolean = false;

  constructor(options: {
    whitelistMode?: boolean;
    allowDangerous?: boolean;
    defaultTimeout?: number;
  } = {}) {
    super();
    this.whitelistMode = options.whitelistMode || false;
    this.allowDangerous = options.allowDangerous || false;
    this.defaultTimeout = options.defaultTimeout || 60000;

    // Configure shelljs
    shell.config.silent = true;
  }

  /**
   * Execute a command
   */
  async run(command: string, options: ExecOptions = {}): Promise<ExecResult> {
    const startTime = Date.now();

    // Security check
    const securityCheck = this.checkSecurity(command);
    if (!securityCheck.safe) {
      return {
        success: false,
        stdout: '',
        stderr: securityCheck.reason || 'Command blocked by security policy',
        exitCode: -1,
        duration: 0,
        command,
        error: securityCheck.reason
      };
    }

    // Prepare options
    const cwd = options.cwd || process.cwd();
    const timeout = options.timeout || this.defaultTimeout;
    const env = { ...process.env, ...options.env } as Record<string, string>;

    console.log(`🔧 Executing: ${command.substring(0, 100)}`);
    this.emit('exec-start', { command, cwd, timeout });

    try {
      // Execute with timeout
      const result = await this.executeWithTimeout(command, {
        cwd,
        env,
        timeout,
        maxBuffer: options.maxBuffer || 1024 * 1024, // 1MB
        silent: options.silent !== false
      });

      const duration = Date.now() - startTime;

      const execResult: ExecResult = {
        success: result.code === 0,
        stdout: result.stdout,
        stderr: result.stderr,
        exitCode: result.code,
        duration,
        command
      };

      console.log(`✓ Execution completed (${duration}ms, exit: ${result.code})`);
      this.emit('exec-complete', execResult);

      return execResult;

    } catch (error: any) {
      const duration = Date.now() - startTime;

      const execResult: ExecResult = {
        success: false,
        stdout: '',
        stderr: error.message,
        exitCode: -1,
        duration,
        command,
        error: error.message
      };

      console.error(`❌ Execution failed: ${error.message}`);
      this.emit('exec-error', execResult);

      return execResult;
    }
  }

  /**
   * Execute command with timeout
   */
  private executeWithTimeout(
    command: string,
    options: ExecOptions & { timeout: number }
  ): Promise<shell.ShellString> {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        reject(new Error(`Command timeout after ${options.timeout}ms`));
      }, options.timeout);

      try {
        const result = shell.exec(command, {
          cwd: options.cwd,
          env: options.env,
          silent: options.silent,
          async: false
        });

        clearTimeout(timer);
        resolve(result);
      } catch (error) {
        clearTimeout(timer);
        reject(error);
      }
    });
  }

  /**
   * Check if command is safe to execute
   */
  private checkSecurity(command: string): { safe: boolean; reason?: string } {
    // Trim and normalize
    const normalized = command.trim().toLowerCase();

    // Check blacklist (unless dangerous commands are allowed)
    if (!this.allowDangerous) {
      for (const pattern of COMMAND_BLACKLIST) {
        if (pattern.test(command)) {
          return {
            safe: false,
            reason: `Dangerous command blocked: matches blacklist pattern ${pattern}`
          };
        }
      }
    }

    // Check whitelist mode
    if (this.whitelistMode) {
      const firstWord = normalized.split(/\s+/)[0];
      if (!COMMAND_WHITELIST.includes(firstWord)) {
        return {
          safe: false,
          reason: `Command not in whitelist: ${firstWord}`
        };
      }
    }

    // Additional checks
    if (normalized.includes('sudo') && !this.allowDangerous) {
      return {
        safe: false,
        reason: 'sudo commands are not allowed'
      };
    }

    return { safe: true };
  }

  /**
   * Test if a command exists
   */
  commandExists(command: string): boolean {
    return shell.which(command) !== null;
  }

  /**
   * Get command path
   */
  getCommandPath(command: string): string | null {
    const result = shell.which(command);
    return result ? result.toString() : null;
  }

  /**
   * Execute multiple commands in sequence
   */
  async runSequence(
    commands: string[],
    options: ExecOptions = {}
  ): Promise<ExecResult[]> {
    const results: ExecResult[] = [];

    for (const command of commands) {
      const result = await this.run(command, options);
      results.push(result);

      // Stop on first failure
      if (!result.success) {
        console.log(`❌ Sequence stopped at command: ${command}`);
        break;
      }
    }

    return results;
  }

  /**
   * Execute multiple commands in parallel
   */
  async runParallel(
    commands: string[],
    options: ExecOptions = {}
  ): Promise<ExecResult[]> {
    const promises = commands.map(cmd => this.run(cmd, options));
    return await Promise.all(promises);
  }

  /**
   * Get security configuration
   */
  getConfig(): {
    whitelistMode: boolean;
    allowDangerous: boolean;
    defaultTimeout: number;
    blacklistPatterns: number;
    whitelistCommands: number;
  } {
    return {
      whitelistMode: this.whitelistMode,
      allowDangerous: this.allowDangerous,
      defaultTimeout: this.defaultTimeout,
      blacklistPatterns: COMMAND_BLACKLIST.length,
      whitelistCommands: COMMAND_WHITELIST.length
    };
  }
}

// Global instance
let execRunner: ExecRunner | null = null;

/**
 * Get global exec runner instance
 */
export function getExecRunner(options?: {
  whitelistMode?: boolean;
  allowDangerous?: boolean;
  defaultTimeout?: number;
}): ExecRunner {
  if (!execRunner || options) {
    execRunner = new ExecRunner(options);
  }
  return execRunner;
}
