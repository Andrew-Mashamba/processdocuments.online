/**
 * Sync Manager - Coordinate memory system synchronization
 * Week 7-8: Memory System - Phase B
 */

import { EventEmitter } from 'events';
import { GatewayConfig, IndexStats } from '../types';
import { Indexer } from './indexer';
import { FileWatcher } from './file-watcher';
import { MemoryDatabase } from './memory-database';
import { getLockManager } from '../context/session-lock-manager';

export interface SyncOptions {
  forceReindex?: boolean;
  skipEmbeddings?: boolean;
  filePattern?: string;
}

export interface SyncResult {
  success: boolean;
  stats?: IndexStats;
  duration: number;
  error?: string;
}

export type SyncMode = 'scheduled' | 'watch' | 'manual';

export class SyncManager extends EventEmitter {
  private config: GatewayConfig;
  private indexer: Indexer;
  private fileWatcher: FileWatcher | null = null;
  private db: MemoryDatabase;
  private scheduledSyncInterval: NodeJS.Timeout | null = null;
  private syncInterval: number;
  private watchEnabled: boolean = false;
  private lastSync: number = 0;
  private syncing: boolean = false;

  constructor(config: GatewayConfig) {
    super();
    this.config = config;
    this.indexer = new Indexer(config);
    this.db = new MemoryDatabase(config);
    this.syncInterval = config.memory?.syncInterval || 3600000; // 1 hour default

    console.log(`🔄 Sync manager initialized (interval: ${this.syncInterval / 60000}m)`);
  }

  /**
   * Start sync manager with all sync modes
   */
  async start(options: {
    enableScheduled?: boolean;
    enableWatch?: boolean;
  } = {}): Promise<void> {
    console.log('\n🚀 Starting sync manager...');

    // Start scheduled sync
    if (options.enableScheduled !== false) {
      this.startScheduledSync();
    }

    // Start file watcher
    if (options.enableWatch !== false) {
      this.startWatchSync();
    }

    // Perform initial sync if needed
    const lastSyncTime = this.db.getLastSync();
    const timeSinceSync = Date.now() - lastSyncTime;

    if (timeSinceSync > this.syncInterval || lastSyncTime === 0) {
      console.log(`⚡ Performing initial sync (${lastSyncTime === 0 ? 'never' : `${Math.round(timeSinceSync / 60000)}m`} since last sync)...`);
      await this.sync({ mode: 'manual' });
    } else {
      console.log(`✓ Sync manager ready (${Math.round((this.syncInterval - timeSinceSync) / 60000)}m until next scheduled sync)`);
    }
  }

  /**
   * Stop sync manager
   */
  stop(): void {
    console.log('⏹️  Stopping sync manager...');

    // Stop scheduled sync
    if (this.scheduledSyncInterval) {
      clearInterval(this.scheduledSyncInterval);
      this.scheduledSyncInterval = null;
    }

    // Stop file watcher
    if (this.fileWatcher) {
      this.fileWatcher.stop();
      this.fileWatcher = null;
    }

    this.watchEnabled = false;
    console.log('✓ Sync manager stopped');
  }

  /**
   * Start scheduled sync
   */
  private startScheduledSync(): void {
    if (this.scheduledSyncInterval) {
      console.warn('⚠️  Scheduled sync already running');
      return;
    }

    console.log(`⏰ Starting scheduled sync (every ${this.syncInterval / 60000}m)`);

    this.scheduledSyncInterval = setInterval(async () => {
      console.log('⏰ Scheduled sync triggered');
      await this.sync({ mode: 'scheduled' });
    }, this.syncInterval);
  }

  /**
   * Start watch-based sync
   */
  private startWatchSync(): void {
    if (this.fileWatcher) {
      console.warn('⚠️  File watcher already running');
      return;
    }

    this.fileWatcher = new FileWatcher(this.config, this.indexer);

    // Listen to file changes
    this.fileWatcher.on('change', (event) => {
      console.log(`👁️  File watcher: ${event.type} ${event.path}`);
      this.emit('file-change', event);
    });

    this.fileWatcher.on('error', (error) => {
      console.error('❌ File watcher error:', error);
      this.emit('error', error);
    });

    this.fileWatcher.start();
    this.watchEnabled = true;
  }

  /**
   * Perform sync (with lock to prevent concurrent syncs)
   */
  async sync(options: {
    mode: SyncMode;
    forceReindex?: boolean;
    skipEmbeddings?: boolean;
  }): Promise<SyncResult> {
    const startTime = Date.now();

    // Check if already syncing
    if (this.syncing) {
      console.log('⏭️  Sync already in progress, skipping...');
      return {
        success: false,
        duration: 0,
        error: 'Sync already in progress'
      };
    }

    // Acquire lock
    const lockManager = getLockManager();
    const lockKey = 'memory:sync:global';
    let lock;

    try {
      lock = await lockManager.acquireSessionWriteLock(lockKey);
    } catch (error: any) {
      console.error('❌ Failed to acquire sync lock:', error.message);
      return {
        success: false,
        duration: Date.now() - startTime,
        error: 'Failed to acquire lock'
      };
    }

    this.syncing = true;

    try {
      console.log(`\n🔄 [${options.mode}] Starting sync...`);

      // Perform indexing
      const stats = await this.indexer.indexWorkspace({
        forceReindex: options.forceReindex,
        skipEmbeddings: options.skipEmbeddings
      });

      const duration = Date.now() - startTime;
      this.lastSync = Date.now();

      console.log(`✓ [${options.mode}] Sync completed in ${(duration / 1000).toFixed(1)}s`);

      const result: SyncResult = {
        success: true,
        stats,
        duration
      };

      this.emit('sync-complete', result);

      return result;
    } catch (error: any) {
      const duration = Date.now() - startTime;
      console.error(`❌ [${options.mode}] Sync failed:`, error.message);

      const result: SyncResult = {
        success: false,
        duration,
        error: error.message
      };

      this.emit('sync-error', result);

      return result;
    } finally {
      this.syncing = false;
      await lock.release();
    }
  }

  /**
   * Trigger manual sync
   */
  async syncNow(options: SyncOptions = {}): Promise<SyncResult> {
    return await this.sync({
      mode: 'manual',
      ...options
    });
  }

  /**
   * Force full reindex
   */
  async reindexAll(): Promise<SyncResult> {
    return await this.sync({
      mode: 'manual',
      forceReindex: true
    });
  }

  /**
   * Get sync status
   */
  getStatus(): {
    syncing: boolean;
    lastSync: number;
    nextScheduledSync: number;
    watchEnabled: boolean;
    pendingChanges: number;
  } {
    const timeSinceSync = Date.now() - this.lastSync;
    const nextSync = this.lastSync > 0
      ? this.lastSync + this.syncInterval
      : Date.now() + this.syncInterval;

    return {
      syncing: this.syncing,
      lastSync: this.lastSync,
      nextScheduledSync: nextSync,
      watchEnabled: this.watchEnabled,
      pendingChanges: this.fileWatcher?.getPendingChanges() || 0
    };
  }

  /**
   * Get index statistics
   */
  getStats(): IndexStats {
    return this.indexer.getStats();
  }

  /**
   * Enable/disable watch mode
   */
  setWatchEnabled(enabled: boolean): void {
    if (enabled && !this.watchEnabled) {
      this.startWatchSync();
    } else if (!enabled && this.watchEnabled) {
      if (this.fileWatcher) {
        this.fileWatcher.stop();
        this.fileWatcher = null;
      }
      this.watchEnabled = false;
    }
  }

  /**
   * Update sync interval
   */
  setSyncInterval(intervalMs: number): void {
    this.syncInterval = intervalMs;

    // Restart scheduled sync with new interval
    if (this.scheduledSyncInterval) {
      clearInterval(this.scheduledSyncInterval);
      this.scheduledSyncInterval = null;
      this.startScheduledSync();
    }

    console.log(`✓ Sync interval updated: ${intervalMs / 60000}m`);
  }

  /**
   * Clean up resources
   */
  close(): void {
    this.stop();
    this.indexer.close();
    this.db.close();
  }
}
