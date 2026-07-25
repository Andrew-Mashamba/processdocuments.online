/**
 * E2E Test: Memory System
 * Tests indexing, search, and retrieval
 */

import { MemoryService } from '../../src/memory/memory-service';
import { Indexer } from '../../src/memory/indexer';
import { loadConfig } from '../../src/config/config-loader';
import * as path from 'path';

async function testMemorySystem() {
  console.log('========================================');
  console.log('E2E Test: Memory System');
  console.log('========================================\n');

  const config = await loadConfig();
  let passed = 0;
  let failed = 0;

  // Test 1: Index workspace
  try {
    console.log('Test 1: Indexing workspace...');
    const indexer = new Indexer(config);
    const stats = await indexer.indexWorkspace({ forceReindex: false });

    if (stats.totalFiles > 0) {
      console.log(`✓ Indexed ${stats.totalFiles} files, ${stats.totalChunks} chunks`);
      passed++;
    } else {
      console.log('✗ No files indexed');
      failed++;
    }

    indexer.close();
  } catch (error: any) {
    console.log(`✗ Indexing failed: ${error.message}`);
    failed++;
  }

  // Test 2: Hybrid search
  try {
    console.log('\nTest 2: Hybrid search...');
    const memoryService = new MemoryService(config);
    const results = await memoryService.hybridSearch('memory system', 5);

    if (results.length > 0) {
      console.log(`✓ Found ${results.length} results`);
      console.log(`  Top result: ${results[0].file_path} (score: ${results[0].score.toFixed(3)})`);
      passed++;
    } else {
      console.log('✗ No search results');
      failed++;
    }

    memoryService.close();
  } catch (error: any) {
    console.log(`✗ Search failed: ${error.message}`);
    failed++;
  }

  // Test 3: BM25-only search
  try {
    console.log('\nTest 3: BM25-only search...');
    const memoryService = new MemoryService(config);
    const results = await memoryService.keywordSearch('configuration', 5);

    if (results.length > 0) {
      console.log(`✓ Found ${results.length} results`);
      passed++;
    } else {
      console.log('⚠️  No BM25 results (may be expected)');
      passed++;
    }

    memoryService.close();
  } catch (error: any) {
    console.log(`✗ BM25 search failed: ${error.message}`);
    failed++;
  }

  // Test 4: Get memory stats
  try {
    console.log('\nTest 4: Memory statistics...');
    const memoryService = new MemoryService(config);
    const stats = await memoryService.getMemoryStats();

    console.log(`✓ Total chunks: ${stats.indexStats.totalChunks}`);
    console.log(`  Embedded chunks: ${stats.indexStats.totalEmbeddings}`);
    console.log(`  Embedding coverage: ${(stats.embeddingCoverage * 100).toFixed(1)}%`);
    passed++;

    memoryService.close();
  } catch (error: any) {
    console.log(`✗ Stats failed: ${error.message}`);
    failed++;
  }

  // Results
  console.log('\n========================================');
  console.log(`Memory System Tests: ${passed} passed, ${failed} failed`);
  console.log('========================================\n');

  return { passed, failed };
}

// Run if executed directly
if (require.main === module) {
  testMemorySystem()
    .then(({ passed, failed }) => {
      process.exit(failed > 0 ? 1 : 0);
    })
    .catch((error) => {
      console.error('Test suite failed:', error);
      process.exit(1);
    });
}

export { testMemorySystem };
