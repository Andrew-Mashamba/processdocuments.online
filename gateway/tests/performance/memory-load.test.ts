/**
 * Performance Test: Memory System Load Testing
 * Tests search performance under load
 */

import { MemoryService } from '../../src/memory/memory-service';
import { loadConfig } from '../../src/config/config-loader';

async function testMemoryLoad() {
  console.log('========================================');
  console.log('Performance Test: Memory System Load');
  console.log('========================================\n');

  const config = await loadConfig();
  const memoryService = new MemoryService(config);

  // Test queries
  const queries = [
    'memory system',
    'configuration',
    'tools',
    'agent',
    'search',
    'database',
    'embedding',
    'hybrid',
    'vector',
    'index'
  ];

  // Warm up
  console.log('Warming up...');
  for (let i = 0; i < 5; i++) {
    await memoryService.hybridSearch(queries[i % queries.length], 5);
  }

  // Test 1: Sequential load (100 queries)
  console.log('\nTest 1: Sequential load (100 queries)...');
  const sequentialLatencies: number[] = [];
  const startSeq = Date.now();

  for (let i = 0; i < 100; i++) {
    const start = Date.now();
    await memoryService.hybridSearch(queries[i % queries.length], 5);
    const latency = Date.now() - start;
    sequentialLatencies.push(latency);
  }

  const totalSeq = Date.now() - startSeq;
  const avgSeq = sequentialLatencies.reduce((a, b) => a + b, 0) / sequentialLatencies.length;
  const p50Seq = percentile(sequentialLatencies, 50);
  const p95Seq = percentile(sequentialLatencies, 95);
  const p99Seq = percentile(sequentialLatencies, 99);

  console.log(`  Total time: ${totalSeq}ms`);
  console.log(`  Throughput: ${(100 / (totalSeq / 1000)).toFixed(2)} queries/sec`);
  console.log(`  Avg latency: ${avgSeq.toFixed(2)}ms`);
  console.log(`  P50 latency: ${p50Seq.toFixed(2)}ms`);
  console.log(`  P95 latency: ${p95Seq.toFixed(2)}ms`);
  console.log(`  P99 latency: ${p99Seq.toFixed(2)}ms`);

  // Test 2: Parallel load (100 queries, 10 concurrent)
  console.log('\nTest 2: Parallel load (100 queries, 10 concurrent)...');
  const parallelLatencies: number[] = [];
  const startPar = Date.now();

  const batches = [];
  for (let i = 0; i < 10; i++) {
    const batch = [];
    for (let j = 0; j < 10; j++) {
      const idx = i * 10 + j;
      batch.push((async () => {
        const start = Date.now();
        await memoryService.hybridSearch(queries[idx % queries.length], 5);
        const latency = Date.now() - start;
        parallelLatencies.push(latency);
      })());
    }
    batches.push(Promise.all(batch));
  }

  await Promise.all(batches);

  const totalPar = Date.now() - startPar;
  const avgPar = parallelLatencies.reduce((a, b) => a + b, 0) / parallelLatencies.length;
  const p50Par = percentile(parallelLatencies, 50);
  const p95Par = percentile(parallelLatencies, 95);
  const p99Par = percentile(parallelLatencies, 99);

  console.log(`  Total time: ${totalPar}ms`);
  console.log(`  Throughput: ${(100 / (totalPar / 1000)).toFixed(2)} queries/sec`);
  console.log(`  Avg latency: ${avgPar.toFixed(2)}ms`);
  console.log(`  P50 latency: ${p50Par.toFixed(2)}ms`);
  console.log(`  P95 latency: ${p95Par.toFixed(2)}ms`);
  console.log(`  P99 latency: ${p99Par.toFixed(2)}ms`);

  // Performance evaluation
  console.log('\n========================================');
  console.log('Performance Evaluation:');
  console.log('========================================');

  const p95Pass = p95Seq < 100;
  const throughputPass = (100 / (totalSeq / 1000)) > 10;

  console.log(`P95 latency < 100ms: ${p95Pass ? '✓ PASS' : '✗ FAIL'} (${p95Seq.toFixed(2)}ms)`);
  console.log(`Throughput > 10 q/s: ${throughputPass ? '✓ PASS' : '✗ FAIL'} (${(100 / (totalSeq / 1000)).toFixed(2)} q/s)`);

  memoryService.close();

  console.log('\n========================================\n');

  return {
    sequential: { total: totalSeq, avg: avgSeq, p50: p50Seq, p95: p95Seq, p99: p99Seq },
    parallel: { total: totalPar, avg: avgPar, p50: p50Par, p95: p95Par, p99: p99Par },
    passed: p95Pass && throughputPass
  };
}

function percentile(values: number[], p: number): number {
  const sorted = values.slice().sort((a, b) => a - b);
  const index = Math.ceil((p / 100) * sorted.length) - 1;
  return sorted[index];
}

// Run if executed directly
if (require.main === module) {
  testMemoryLoad()
    .then((results) => {
      process.exit(results.passed ? 0 : 1);
    })
    .catch((error) => {
      console.error('Test failed:', error);
      process.exit(1);
    });
}

export { testMemoryLoad };
