/**
 * File Watcher - Auto-sync workspace changes
 * Week 7-8: Memory System - Phase B
 */

import * as fs from 'fs';
import * as path from 'path';
import { EventEmitter } from 'events';
import { GatewayConfig } from '../types';
import { Indexer } from './indexer';

export interface FileChangeEvent {
  type: 'add' | 'change' | 'unlink';
  path: string;
  timestamp: number;
}

export interface WatcherOptions {
  debounceMs?: number;
  ignorePatterns?: string[];
  fileExtensions?: string[];
}

export class FileWatcher extends EventEmitter {
  private config: GatewayConfig;
  private indexer: Indexer;
  private workspaceDir: string;
  private watcher: fs.FSWatcher | null = null;
  private debounceTimers: Map<string, NodeJS.Timeout>;
  private debounceMs: number;
  private ignorePatterns: RegExp[];
  private fileExtensions: string[];
  private running: boolean = false;

  constructor(config: GatewayConfig, indexer: Indexer, options: WatcherOptions = {}) {
    super();
    this.config = config;
    this.indexer = indexer;
    this.workspaceDir = config.storage.workspace || path.join(process.cwd(), 'workspace');
    this.debounceTimers = new Map();
    this.debounceMs = options.debounceMs || 2000; // 2 second debounce

    // Default ignore patterns
    this.ignorePatterns = [
      /node_modules/,
      /\.git/,
      /memory\//,
      /\.DS_Store/,
      /.*~$/,
      /\.swp$/
    ];

    // Add custom ignore patterns
    if (options.ignorePatterns) {
      this.ignorePatterns.push(...options.ignorePatterns.map(p => new RegExp(p)));
    }

    // Default file extensions to watch
    this.fileExtensions = options.fileExtensions || ['.md', '.txt', '.json', '.yaml', '.yml'];
  }

  /**
   * Start watching workspace directory
   */
  start(): void {
    if (this.running) {
      console.warn('⚠️  File watcher already running');
      return;
    }

    try {
      console.log(`👁️  Starting file watcher: ${this.workspaceDir}`);
      console.log(`   Debounce: ${this.debounceMs}ms`);
      console.log(`   Extensions: ${this.fileExtensions.join(', ')}`);

      this.watcher = fs.watch(
        this.workspaceDir,
        { recursive: true },
        (eventType, filename) => {
          if (filename) {
            this.handleFileChange(eventType, filename);
          }
        }
      );

      this.running = true;
      console.log('✓ File watcher started');
    } catch (error: any) {
      console.error('❌ Failed to start file watcher:', error.message);
      throw error;
    }
  }

  /**
   * Stop watching
   */
  stop(): void {
    if (!this.running) {
      return;
    }

    console.log('⏹️  Stopping file watcher...');

    // Clear all pending debounce timers
    for (const timer of this.debounceTimers.values()) {
      clearTimeout(timer);
    }
    this.debounceTimers.clear();

    // Close watcher
    if (this.watcher) {
      this.watcher.close();
      this.watcher = null;
    }

    this.running = false;
    console.log('✓ File watcher stopped');
  }

  /**
   * Handle file system change event
   */
  private handleFileChange(eventType: string, filename: string): void {
    const fullPath = path.join(this.workspaceDir, filename);

    // Check if file should be ignored
    if (this.shouldIgnore(filename)) {
      return;
    }

    // Check if file extension is watched
    const ext = path.extname(filename);
    if (!this.fileExtensions.includes(ext)) {
      return;
    }

    // Debounce the change
    this.debounceChange(fullPath, filename, eventType);
  }

  /**
   * Check if file should be ignored
   */
  private shouldIgnore(filename: string): boolean {
    for (const pattern of this.ignorePatterns) {
      if (pattern.test(filename)) {
        return true;
      }
    }
    return false;
  }

  /**
   * Debounce file changes
   */
  private debounceChange(fullPath: string, filename: string, eventType: string): void {
    // Clear existing timer for this file
    const existingTimer = this.debounceTimers.get(fullPath);
    if (existingTimer) {
      clearTimeout(existingTimer);
    }

    // Set new timer
    const timer = setTimeout(() => {
      this.debounceTimers.delete(fullPath);
      this.processFileChange(fullPath, filename, eventType);
    }, this.debounceMs);

    this.debounceTimers.set(fullPath, timer);
  }

  /**
   * Process file change after debounce
   */
  private async processFileChange(fullPath: string, filename: string, eventType: string): Promise<void> {
    try {
      // Check if file still exists (for change events)
      const exists = fs.existsSync(fullPath);

      if (!exists) {
        // File deleted
        console.log(`🗑️  File deleted: ${filename}`);
        await this.indexer.removeFile(filename);
        this.emit('change', {
          type: 'unlink',
          path: filename,
          timestamp: Date.now()
        } as FileChangeEvent);
      } else {
        // File added or changed
        const stats = fs.statSync(fullPath);

        if (stats.isFile()) {
          console.log(`📝 File ${eventType === 'rename' ? 'added' : 'changed'}: ${filename}`);
          await this.indexer.indexSingleFile(filename, { forceReindex: true });
          this.emit('change', {
            type: eventType === 'rename' ? 'add' : 'change',
            path: filename,
            timestamp: Date.now()
          } as FileChangeEvent);
        }
      }
    } catch (error: any) {
      console.error(`❌ Error processing file change (${filename}):`, error.message);
      this.emit('error', error);
    }
  }

  /**
   * Get watcher status
   */
  isRunning(): boolean {
    return this.running;
  }

  /**
   * Get pending changes count
   */
  getPendingChanges(): number {
    return this.debounceTimers.size;
  }

  /**
   * Manually trigger reindex for a file
   */
  async reindexFile(filename: string): Promise<void> {
    const fullPath = path.join(this.workspaceDir, filename);
    await this.processFileChange(fullPath, filename, 'change');
  }

  /**
   * Get watched directory
   */
  getWatchedDirectory(): string {
    return this.workspaceDir;
  }
}
