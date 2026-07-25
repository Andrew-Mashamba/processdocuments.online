/**
 * Process Manager - Background process control
 * Week 9-10: Exec & Queue - Phase B
 */

import { spawn, ChildProcess } from 'child_process';
import { EventEmitter } from 'events';
import { v4 as uuidv4 } from 'uuid';
import * as path from 'path';
import * as fs from 'fs-extra';

export interface SpawnOptions {
  cwd?: string;
  env?: Record<string, string>;
  shell?: boolean;
  detached?: boolean;
  captureOutput?: boolean;
  logFile?: string;
}

export interface ProcessInfo {
  id: string;
  command: string;
  args: string[];
  pid?: number;
  status: 'running' | 'stopped' | 'failed';
  startedAt: number;
  exitCode?: number;
  signal?: string;
}

export interface ProcessOutput {
  stdout: string;
  stderr: string;
  combined: string;
}

interface ManagedProcess {
  id: string;
  command: string;
  args: string[];
  process: ChildProcess;
  startedAt: number;
  output: ProcessOutput;
  logStream?: fs.WriteStream;
}

export class ProcessManager extends EventEmitter {
  private processes: Map<string, ManagedProcess> = new Map();
  private logsDir: string;

  constructor(logsDir?: string) {
    super();
    this.logsDir = logsDir || path.join(process.cwd(), 'logs');
    fs.ensureDirSync(this.logsDir);
  }

  /**
   * Spawn a new background process
   */
  async spawn(
    command: string,
    args: string[] = [],
    options: SpawnOptions = {}
  ): Promise<string> {
    const processId = uuidv4();

    console.log(`🚀 Spawning process: ${command} ${args.join(' ')}`);

    // Prepare spawn options
    const spawnOptions: any = {
      cwd: options.cwd || process.cwd(),
      env: { ...process.env, ...options.env },
      shell: options.shell !== false,
      detached: options.detached || false
    };

    // Spawn process
    const proc = spawn(command, args, spawnOptions);

    // Setup log file if requested
    let logStream: fs.WriteStream | undefined;
    if (options.logFile) {
      const logPath = path.isAbsolute(options.logFile)
        ? options.logFile
        : path.join(this.logsDir, options.logFile);
      logStream = fs.createWriteStream(logPath, { flags: 'a' });
      logStream.write(`\n=== Process started: ${new Date().toISOString()} ===\n`);
      logStream.write(`Command: ${command} ${args.join(' ')}\n`);
      logStream.write(`PID: ${proc.pid}\n\n`);
    }

    // Create managed process record
    const managedProc: ManagedProcess = {
      id: processId,
      command,
      args,
      process: proc,
      startedAt: Date.now(),
      output: {
        stdout: '',
        stderr: '',
        combined: ''
      },
      logStream
    };

    this.processes.set(processId, managedProc);

    // Capture output if requested
    if (options.captureOutput !== false) {
      proc.stdout?.on('data', (data: Buffer) => {
        const text = data.toString();
        managedProc.output.stdout += text;
        managedProc.output.combined += text;

        if (logStream) {
          logStream.write(text);
        }

        this.emit('process-stdout', { id: processId, data: text });
      });

      proc.stderr?.on('data', (data: Buffer) => {
        const text = data.toString();
        managedProc.output.stderr += text;
        managedProc.output.combined += text;

        if (logStream) {
          logStream.write(text);
        }

        this.emit('process-stderr', { id: processId, data: text });
      });
    }

    // Handle process exit
    proc.on('exit', (code, signal) => {
      console.log(`✓ Process exited: ${processId} (code: ${code}, signal: ${signal})`);

      if (logStream) {
        logStream.write(`\n=== Process exited: ${new Date().toISOString()} ===\n`);
        logStream.write(`Exit code: ${code}\n`);
        logStream.write(`Signal: ${signal}\n\n`);
        logStream.end();
      }

      this.emit('process-exit', { id: processId, code, signal });
    });

    // Handle process error
    proc.on('error', (error: Error) => {
      console.error(`❌ Process error: ${processId} - ${error.message}`);

      if (logStream) {
        logStream.write(`\nERROR: ${error.message}\n`);
        logStream.end();
      }

      this.emit('process-error', { id: processId, error: error.message });
    });

    console.log(`✓ Process spawned: ${processId} (PID: ${proc.pid})`);
    this.emit('process-spawn', { id: processId, pid: proc.pid });

    return processId;
  }

  /**
   * Get process information
   */
  getProcess(processId: string): ProcessInfo | null {
    const proc = this.processes.get(processId);
    if (!proc) {
      return null;
    }

    return {
      id: proc.id,
      command: proc.command,
      args: proc.args,
      pid: proc.process.pid,
      status: proc.process.killed ? 'stopped' :
              proc.process.exitCode !== null ? 'stopped' : 'running',
      startedAt: proc.startedAt,
      exitCode: proc.process.exitCode || undefined,
      signal: proc.process.signalCode || undefined
    };
  }

  /**
   * List all managed processes
   */
  list(): ProcessInfo[] {
    return Array.from(this.processes.values()).map(proc => ({
      id: proc.id,
      command: proc.command,
      args: proc.args,
      pid: proc.process.pid,
      status: proc.process.killed ? 'stopped' :
              proc.process.exitCode !== null ? 'stopped' : 'running',
      startedAt: proc.startedAt,
      exitCode: proc.process.exitCode || undefined,
      signal: proc.process.signalCode || undefined
    }));
  }

  /**
   * Get process output
   */
  getOutput(processId: string): ProcessOutput | null {
    const proc = this.processes.get(processId);
    if (!proc) {
      return null;
    }

    return { ...proc.output };
  }

  /**
   * Kill a specific process
   */
  async kill(processId: string, signal: NodeJS.Signals = 'SIGTERM'): Promise<boolean> {
    const proc = this.processes.get(processId);
    if (!proc) {
      console.warn(`⚠️  Process not found: ${processId}`);
      return false;
    }

    if (proc.process.killed || proc.process.exitCode !== null) {
      console.log(`Process already stopped: ${processId}`);
      return true;
    }

    console.log(`🔪 Killing process: ${processId} (signal: ${signal})`);

    return new Promise((resolve) => {
      proc.process.once('exit', () => {
        console.log(`✓ Process killed: ${processId}`);
        resolve(true);
      });

      // Timeout after 5 seconds
      const timeout = setTimeout(() => {
        if (!proc.process.killed) {
          console.warn(`⚠️  Process did not exit, sending SIGKILL: ${processId}`);
          proc.process.kill('SIGKILL');
        }
        resolve(true);
      }, 5000);

      proc.process.once('exit', () => {
        clearTimeout(timeout);
      });

      proc.process.kill(signal);
    });
  }

  /**
   * Kill all managed processes
   */
  async killAll(signal: NodeJS.Signals = 'SIGTERM'): Promise<void> {
    console.log(`🔪 Killing all ${this.processes.size} processes...`);

    const kills = Array.from(this.processes.keys()).map(id =>
      this.kill(id, signal)
    );

    await Promise.all(kills);

    console.log('✓ All processes killed');
  }

  /**
   * Clean up stopped processes from memory
   */
  cleanup(): number {
    let count = 0;

    for (const [id, proc] of this.processes.entries()) {
      if (proc.process.killed || proc.process.exitCode !== null) {
        this.processes.delete(id);
        count++;
      }
    }

    if (count > 0) {
      console.log(`✓ Cleaned up ${count} stopped processes`);
    }

    return count;
  }

  /**
   * Send signal to process
   */
  signal(processId: string, signal: NodeJS.Signals): boolean {
    const proc = this.processes.get(processId);
    if (!proc) {
      return false;
    }

    if (proc.process.killed || proc.process.exitCode !== null) {
      return false;
    }

    proc.process.kill(signal);
    return true;
  }

  /**
   * Check if process is running
   */
  isRunning(processId: string): boolean {
    const proc = this.processes.get(processId);
    if (!proc) {
      return false;
    }

    return !proc.process.killed && proc.process.exitCode === null;
  }

  /**
   * Wait for process to exit
   */
  async waitForExit(processId: string, timeout?: number): Promise<{
    exitCode: number | null;
    signal: string | null;
  }> {
    const proc = this.processes.get(processId);
    if (!proc) {
      throw new Error(`Process not found: ${processId}`);
    }

    // Already exited
    if (proc.process.exitCode !== null || proc.process.killed) {
      return {
        exitCode: proc.process.exitCode,
        signal: proc.process.signalCode
      };
    }

    // Wait for exit
    return new Promise((resolve, reject) => {
      const handler = (code: number | null, signal: string | null) => {
        if (timer) clearTimeout(timer);
        resolve({ exitCode: code, signal });
      };

      proc.process.once('exit', handler);

      // Timeout
      let timer: NodeJS.Timeout | null = null;
      if (timeout) {
        timer = setTimeout(() => {
          proc.process.off('exit', handler);
          reject(new Error(`Wait timeout after ${timeout}ms`));
        }, timeout);
      }
    });
  }

  /**
   * Get manager statistics
   */
  getStats(): {
    total: number;
    running: number;
    stopped: number;
  } {
    let running = 0;
    let stopped = 0;

    for (const proc of this.processes.values()) {
      if (proc.process.killed || proc.process.exitCode !== null) {
        stopped++;
      } else {
        running++;
      }
    }

    return {
      total: this.processes.size,
      running,
      stopped
    };
  }
}

// Global instance
let processManager: ProcessManager | null = null;

/**
 * Get global process manager instance
 */
export function getProcessManager(logsDir?: string): ProcessManager {
  if (!processManager) {
    processManager = new ProcessManager(logsDir);
  }
  return processManager;
}

/**
 * Cleanup on process exit
 */
process.on('SIGINT', async () => {
  if (processManager) {
    await processManager.killAll('SIGINT');
  }
  process.exit(0);
});

process.on('SIGTERM', async () => {
  if (processManager) {
    await processManager.killAll('SIGTERM');
  }
  process.exit(0);
});
