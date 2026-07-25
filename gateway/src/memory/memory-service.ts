/**
 * Memory Service - High-level API for memory operations
 * Week 7-8: Memory System - Service Layer
 */

import { MemoryDatabase } from './memory-database';
import { EmbeddingProvider } from './embedding-provider';
import { HybridSearch } from './hybrid-search';
import { Indexer } from './indexer';
import { GatewayConfig, SearchResult, SearchOptions, IndexStats, MemoryStats } from '../types';

export class MemoryService {
  private db: MemoryDatabase;
  private embeddings: EmbeddingProvider;
  private searchEngine: HybridSearch;
  private indexer: Indexer;
  private config: GatewayConfig;
  private initialized: boolean = false;

  constructor(config: GatewayConfig) {
    this.config = config;
    this.db = new MemoryDatabase(config);
    this.embeddings = new EmbeddingProvider(config);
    this.searchEngine = new HybridSearch(
      this.db,
      this.embeddings,
      config.memory?.vectorWeight || 0.7
    );
    this.indexer = new Indexer(config);
  }

  /**
   * Initialize memory system
   */
  async initialize(): Promise<void> {
    if (this.initialized) {
      return;
    }

    console.log('\n📚 Initializing Memory System...');

    // Try to load vector extension
    await this.db.loadVectorExtension();

    // Check if workspace needs indexing
    const stats = this.db.getStats();
    const lastSync = this.db.getLastSync();
    const timeSinceSync = Date.now() - lastSync;

    // Auto-index if never synced or synced more than 1 hour ago
    const autoIndexThreshold = 3600000; // 1 hour

    if (stats.totalFiles === 0 || timeSinceSync > autoIndexThreshold) {
      console.log(`⚡ Auto-indexing workspace (${timeSinceSync > 0 ? `${Math.round(timeSinceSync / 60000)}m` : 'never'} since last sync)...`);
      await this.indexWorkspace({ skipEmbeddings: !this.embeddings.isAvailable() });
    } else {
      console.log(`✓ Memory system ready (${stats.totalFiles} files, ${stats.totalChunks} chunks)`);
    }

    this.initialized = true;
  }

  /**
   * Hybrid search (vector + BM25)
   */
  async hybridSearch(query: string, limit: number = 5): Promise<SearchResult[]> {
    if (!this.initialized) {
      await this.initialize();
    }

    return await this.searchEngine.search(query, { limit });
  }

  /**
   * Search with custom options
   */
  async search(query: string, options: SearchOptions = {}): Promise<SearchResult[]> {
    if (!this.initialized) {
      await this.initialize();
    }

    return await this.searchEngine.search(query, options);
  }

  /**
   * BM25-only search (fallback)
   */
  async keywordSearch(query: string, limit: number = 5): Promise<SearchResult[]> {
    if (!this.initialized) {
      await this.initialize();
    }

    return await this.searchEngine.bm25OnlySearch(query, limit);
  }

  /**
   * Index workspace directory
   */
  async indexWorkspace(options: {
    forceReindex?: boolean;
    skipEmbeddings?: boolean;
    filePattern?: string;
  } = {}): Promise<IndexStats> {
    return await this.indexer.indexWorkspace(options);
  }

  /**
   * Index a single file
   */
  async indexFile(filePath: string, options: {
    forceReindex?: boolean;
    skipEmbeddings?: boolean;
  } = {}): Promise<void> {
    await this.indexer.indexSingleFile(filePath, options);
  }

  /**
   * Remove file from index
   */
  async removeFile(filePath: string): Promise<void> {
    await this.indexer.removeFile(filePath);
  }

  /**
   * Get memory statistics
   */
  async getMemoryStats(): Promise<MemoryStats> {
    const indexStats = this.indexer.getStats();

    return {
      indexStats,
      embeddingCoverage: indexStats.totalChunks > 0
        ? indexStats.totalEmbeddings / indexStats.totalChunks
        : 0
    };
  }

  /**
   * Get index statistics
   */
  getIndexStats(): IndexStats {
    return this.indexer.getStats();
  }

  /**
   * Check if memory system is enabled
   */
  isEnabled(): boolean {
    return this.config.memory?.enabled || false;
  }

  /**
   * Check if embeddings are available
   */
  hasEmbeddings(): boolean {
    return this.embeddings.isAvailable();
  }

  /**
   * Close and cleanup resources
   */
  close(): void {
    this.indexer.close();
    console.log('✓ Memory service closed');
  }

  /**
   * Force sync workspace (reindex all files)
   */
  async syncWorkspace(): Promise<IndexStats> {
    console.log('🔄 Force syncing workspace...');
    return await this.indexWorkspace({ forceReindex: true });
  }

  /**
   * Get search capabilities
   */
  getCapabilities(): {
    vectorSearch: boolean;
    bm25Search: boolean;
    hybridSearch: boolean;
  } {
    const hasEmbeddings = this.embeddings.isAvailable();

    return {
      vectorSearch: hasEmbeddings,
      bm25Search: true, // Always available via FTS5
      hybridSearch: hasEmbeddings
    };
  }
}
