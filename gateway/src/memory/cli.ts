/**
 * Memory CLI - Command-line interface for memory operations
 * Week 7-8: Memory System - Phase B
 */

import { GatewayConfig } from '../types';
import { MemoryService } from './memory-service';
import { SyncManager } from './sync-manager';
import { MemoryDatabase } from './memory-database';
import { getPerformanceMonitor } from './performance-monitor';

export class MemoryCLI {
  private config: GatewayConfig;

  constructor(config: GatewayConfig) {
    this.config = config;
  }

  /**
   * Execute CLI command
   */
  async execute(command: string, args: string[] = []): Promise<void> {
    switch (command) {
      case 'memory:status':
        await this.showStatus();
        break;

      case 'memory:index':
        await this.forceIndex(args);
        break;

      case 'memory:search':
        await this.testSearch(args);
        break;

      case 'memory:sync':
        await this.manualSync();
        break;

      case 'memory:stats':
        await this.showStats();
        break;

      case 'memory:performance':
      case 'memory:perf':
        this.showPerformance();
        break;

      case 'memory:help':
        this.showHelp();
        break;

      default:
        console.error(`Unknown command: ${command}`);
        this.showHelp();
    }
  }

  /**
   * Show memory system status
   */
  private async showStatus(): Promise<void> {
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║         ZIMA Memory System - Status                   ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');

    try {
      const db = new MemoryDatabase(this.config);
      const stats = db.getStats();
      const lastSync = db.getLastSync();

      console.log('📊 Index Statistics:');
      console.log(`   Files:      ${stats.totalFiles}`);
      console.log(`   Chunks:     ${stats.totalChunks}`);
      console.log(`   Embeddings: ${stats.totalEmbeddings}`);
      console.log(`   DB Size:    ${(stats.databaseSize / 1024 / 1024).toFixed(2)} MB`);

      if (lastSync > 0) {
        const timeSinceSync = Date.now() - lastSync;
        const minutes = Math.floor(timeSinceSync / 60000);
        const hours = Math.floor(minutes / 60);

        console.log(`\n⏱️  Last Sync:`);
        if (hours > 0) {
          console.log(`   ${hours}h ${minutes % 60}m ago`);
        } else {
          console.log(`   ${minutes}m ago`);
        }
        console.log(`   ${new Date(lastSync).toLocaleString()}`);
      } else {
        console.log(`\n⏱️  Last Sync: Never`);
      }

      // Coverage
      const coverage = stats.totalChunks > 0
        ? (stats.totalEmbeddings / stats.totalChunks * 100).toFixed(1)
        : 0;

      console.log(`\n📈 Embedding Coverage: ${coverage}%`);

      if (stats.totalEmbeddings === 0) {
        console.log('   ⚠️  No embeddings found. Set OPENAI_API_KEY to enable vector search.');
      }

      db.close();
    } catch (error: any) {
      console.error('❌ Error getting status:', error.message);
    }

    console.log('');
  }

  /**
   * Force full reindex
   */
  private async forceIndex(args: string[]): Promise<void> {
    console.log('\n🔨 Force Reindexing Workspace...\n');

    const forceReindex = !args.includes('--incremental');
    const skipEmbeddings = args.includes('--no-embeddings');

    try {
      const memoryService = new MemoryService(this.config);

      const stats = await memoryService.indexWorkspace({
        forceReindex,
        skipEmbeddings
      });

      console.log('\n✓ Indexing complete!');
      console.log(`   Files:      ${stats.totalFiles}`);
      console.log(`   Chunks:     ${stats.totalChunks}`);
      console.log(`   Embeddings: ${stats.totalEmbeddings}`);

      memoryService.close();
    } catch (error: any) {
      console.error('❌ Indexing failed:', error.message);
    }
  }

  /**
   * Test search functionality
   */
  private async testSearch(args: string[]): Promise<void> {
    if (args.length === 0) {
      console.error('❌ Usage: memory:search <query>');
      console.error('   Example: memory:search "how to configure the gateway"');
      return;
    }

    const query = args.join(' ');
    const limit = 5;

    console.log(`\n🔍 Searching: "${query}"\n`);

    try {
      const memoryService = new MemoryService(this.config);
      await memoryService.initialize();

      const results = await memoryService.hybridSearch(query, limit);

      if (results.length === 0) {
        console.log('   No results found.');
      } else {
        console.log(`   Found ${results.length} results:\n`);

        results.forEach((result, i) => {
          console.log(`   ${i + 1}. ${result.file_path} (score: ${result.score.toFixed(3)})`);
          console.log(`      Lines ${result.start_line}-${result.end_line} (${result.tokens} tokens)`);
          console.log(`      ${result.content.substring(0, 100).replace(/\n/g, ' ')}...`);
          console.log('');
        });
      }

      memoryService.close();
    } catch (error: any) {
      console.error('❌ Search failed:', error.message);
    }
  }

  /**
   * Manual sync
   */
  private async manualSync(): Promise<void> {
    console.log('\n🔄 Manual Sync Starting...\n');

    try {
      const syncManager = new SyncManager(this.config);

      const result = await syncManager.syncNow();

      if (result.success && result.stats) {
        console.log('\n✓ Sync completed!');
        console.log(`   Duration:   ${(result.duration / 1000).toFixed(1)}s`);
        console.log(`   Files:      ${result.stats.totalFiles}`);
        console.log(`   Chunks:     ${result.stats.totalChunks}`);
        console.log(`   Embeddings: ${result.stats.totalEmbeddings}`);
      } else {
        console.error(`❌ Sync failed: ${result.error}`);
      }

      syncManager.close();
    } catch (error: any) {
      console.error('❌ Sync error:', error.message);
    }

    console.log('');
  }

  /**
   * Show detailed statistics
   */
  private async showStats(): Promise<void> {
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║         ZIMA Memory System - Statistics               ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');

    try {
      const memoryService = new MemoryService(this.config);
      const stats = await memoryService.getMemoryStats();

      console.log('📊 Index Statistics:');
      console.log(`   Total Files:      ${stats.indexStats.totalFiles}`);
      console.log(`   Total Chunks:     ${stats.indexStats.totalChunks}`);
      console.log(`   Total Embeddings: ${stats.indexStats.totalEmbeddings}`);
      console.log(`   Database Size:    ${(stats.indexStats.databaseSize / 1024 / 1024).toFixed(2)} MB`);

      if (stats.indexStats.totalChunks > 0) {
        const avgChunksPerFile = (stats.indexStats.totalChunks / stats.indexStats.totalFiles).toFixed(1);
        console.log(`   Avg Chunks/File:  ${avgChunksPerFile}`);
      }

      console.log(`\n📈 Coverage:`);
      console.log(`   Embedding Coverage: ${(stats.embeddingCoverage * 100).toFixed(1)}%`);

      console.log(`\n🔧 Capabilities:`);
      const capabilities = memoryService.getCapabilities();
      console.log(`   Vector Search:  ${capabilities.vectorSearch ? '✓' : '✗'}`);
      console.log(`   BM25 Search:    ${capabilities.bm25Search ? '✓' : '✗'}`);
      console.log(`   Hybrid Search:  ${capabilities.hybridSearch ? '✓' : '✗'}`);

      memoryService.close();
    } catch (error: any) {
      console.error('❌ Error getting stats:', error.message);
    }

    console.log('');
  }

  /**
   * Show performance report
   */
  private showPerformance(): void {
    const monitor = getPerformanceMonitor();
    monitor.printReport();
  }

  /**
   * Show help
   */
  private showHelp(): void {
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║         ZIMA Memory System - CLI Commands             ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');

    console.log('Available Commands:\n');

    console.log('  memory:status');
    console.log('    Show memory system status and index statistics\n');

    console.log('  memory:index [--incremental] [--no-embeddings]');
    console.log('    Force reindex workspace files');
    console.log('    --incremental    Only reindex changed files (default: full reindex)');
    console.log('    --no-embeddings  Skip embedding generation\n');

    console.log('  memory:search <query>');
    console.log('    Test search functionality with a query');
    console.log('    Example: memory:search "how to configure"');
    console.log('');

    console.log('  memory:sync');
    console.log('    Manually trigger workspace sync\n');

    console.log('  memory:stats');
    console.log('    Show detailed statistics and capabilities\n');

    console.log('  memory:performance | memory:perf');
    console.log('    Show performance metrics and analysis\n');

    console.log('  memory:help');
    console.log('    Show this help message\n');

    console.log('Environment Variables:\n');
    console.log('  OPENAI_API_KEY      OpenAI API key for embeddings');
    console.log('  MEMORY_ENABLED      Enable/disable memory system (default: true)');
    console.log('  MEMORY_DATABASE     Database file path');
    console.log('  EMBEDDING_MODEL     Embedding model name (default: text-embedding-3-small)');
    console.log('');
  }
}

/**
 * Execute memory CLI command
 */
export async function executeMemoryCommand(
  command: string,
  args: string[],
  config: GatewayConfig
): Promise<void> {
  const cli = new MemoryCLI(config);
  await cli.execute(command, args);
}
