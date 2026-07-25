/**
 * Session Lock Manager
 * Prevents concurrent message processing for the same session (OpenClaw approach)
 */

import * as lockfile from 'lockfile';
import * as path from 'path';
import * as fs from 'fs-extra';
import { Lock } from '../types';

export class SessionLockManager {
  private lockDir: string;
  private activeLocks: Map<string, boolean>;

  constructor(lockDir?: string) {
    this.lockDir = lockDir || '/tmp/zima-locks';
    this.activeLocks = new Map();
    this.ensureLockDir();
  }

  /**
   * Acquire a write lock for a session
   */
  async acquireSessionWriteLock(sessionKey: string): Promise<Lock> {
    const lockPath = this.getLockPath(sessionKey);

    console.log(`🔒 Acquiring lock for session: ${sessionKey}`);

    return new Promise((resolve, reject) => {
      lockfile.lock(lockPath, {
        wait: 30000,      // Wait up to 30 seconds
        retries: 5,       // Retry 5 times
        retryWait: 1000   // Wait 1 second between retries
      }, (err) => {
        if (err) {
          console.error(`❌ Failed to acquire lock for ${sessionKey}:`, err.message);
          reject(new Error(`Failed to acquire session lock: ${err.message}`));
        } else {
          console.log(`✓ Lock acquired for session: ${sessionKey}`);
          this.activeLocks.set(sessionKey, true);

          resolve({
            release: async () => {
              return new Promise((res, rej) => {
                lockfile.unlock(lockPath, (unlockErr) => {
                  if (unlockErr) {
                    console.error(`❌ Failed to release lock for ${sessionKey}:`, unlockErr.message);
                    rej(unlockErr);
                  } else {
                    console.log(`🔓 Lock released for session: ${sessionKey}`);
                    this.activeLocks.delete(sessionKey);
                    res();
                  }
                });
              });
            }
          });
        }
      });
    });
  }

  /**
   * Check if a session is currently locked
   */
  isLocked(sessionKey: string): boolean {
    return this.activeLocks.has(sessionKey);
  }

  /**
   * Get active lock count
   */
  getActiveLockCount(): number {
    return this.activeLocks.size;
  }

  /**
   * Get lock path for session
   */
  private getLockPath(sessionKey: string): string {
    const safeName = sessionKey.replace(/:/g, '-').replace(/\//g, '_');
    return path.join(this.lockDir, `${safeName}.lock`);
  }

  /**
   * Ensure lock directory exists
   */
  private ensureLockDir(): void {
    fs.ensureDirSync(this.lockDir);
  }

  /**
   * Clean up stale locks (optional maintenance)
   */
  async cleanupStaleLocks(): Promise<void> {
    console.log('🧹 Cleaning up stale locks...');

    const files = await fs.readdir(this.lockDir);
    const now = Date.now();
    let cleaned = 0;

    for (const file of files) {
      if (!file.endsWith('.lock')) continue;

      const lockPath = path.join(this.lockDir, file);
      try {
        const stats = await fs.stat(lockPath);
        const age = now - stats.mtimeMs;

        // Remove locks older than 5 minutes
        if (age > 300000) {
          await fs.unlink(lockPath);
          cleaned++;
        }
      } catch (err) {
        // Ignore errors (file might have been removed)
      }
    }

    if (cleaned > 0) {
      console.log(`✓ Cleaned up ${cleaned} stale locks`);
    }
  }
}

// Singleton instance
let lockManager: SessionLockManager | null = null;

/**
 * Get global lock manager instance
 */
export function getLockManager(): SessionLockManager {
  if (!lockManager) {
    lockManager = new SessionLockManager();

    // Clean up stale locks every 10 minutes
    setInterval(() => {
      lockManager?.cleanupStaleLocks();
    }, 600000);
  }
  return lockManager;
}
