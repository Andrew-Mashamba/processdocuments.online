/**
 * Performance Monitor - Track memory system metrics
 * Week 7-8: Memory System - Phase C
 */

import { EventEmitter } from 'events';

export interface SearchMetric {
  query: string;
  resultCount: number;
  latencyMs: number;
  searchType: 'hybrid' | 'vector' | 'bm25';
  timestamp: number;
}

export interface EmbeddingMetric {
  textCount: number;
  totalTokens: number;
  latencyMs: number;
  cost: number;
  model: string;
  timestamp: number;
}

export interface DatabaseMetric {
  operation: string;
  latencyMs: number;
  rowCount?: number;
  timestamp: number;
}

export interface PerformanceStats {
  search: {
    totalQueries: number;
    avgLatencyMs: number;
    p50LatencyMs: number;
    p95LatencyMs: number;
    p99LatencyMs: number;
    slowestQuery: SearchMetric | null;
  };
  embedding: {
    totalRequests: number;
    totalTokens: number;
    totalCost: number;
    avgLatencyMs: number;
    avgTokensPerRequest: number;
  };
  database: {
    totalOperations: number;
    avgLatencyMs: number;
    operationCounts: Record<string, number>;
  };
}

export class PerformanceMonitor extends EventEmitter {
  private searchMetrics: SearchMetric[] = [];
  private embeddingMetrics: EmbeddingMetric[] = [];
  private databaseMetrics: DatabaseMetric[] = [];
  private maxMetricsRetention: number;
  private cleanupInterval: NodeJS.Timeout | null = null;

  constructor(maxMetricsRetention: number = 1000) {
    super();
    this.maxMetricsRetention = maxMetricsRetention;

    // Clean up old metrics every 5 minutes
    this.cleanupInterval = setInterval(() => {
      this.cleanupOldMetrics();
    }, 300000);
  }

  /**
   * Record search metric
   */
  recordSearch(metric: SearchMetric): void {
    this.searchMetrics.push(metric);

    // Emit event for real-time monitoring
    this.emit('search', metric);

    // Trim if needed
    if (this.searchMetrics.length > this.maxMetricsRetention) {
      this.searchMetrics.shift();
    }

    // Log slow queries (>500ms)
    if (metric.latencyMs > 500) {
      console.warn(`⚠️  Slow search query (${metric.latencyMs}ms): "${metric.query}"`);
    }
  }

  /**
   * Record embedding metric
   */
  recordEmbedding(metric: EmbeddingMetric): void {
    this.embeddingMetrics.push(metric);

    // Emit event
    this.emit('embedding', metric);

    // Trim if needed
    if (this.embeddingMetrics.length > this.maxMetricsRetention) {
      this.embeddingMetrics.shift();
    }

    // Log expensive operations (>$0.01)
    if (metric.cost > 0.01) {
      console.warn(`💰 High embedding cost: $${metric.cost.toFixed(4)} (${metric.totalTokens} tokens)`);
    }
  }

  /**
   * Record database metric
   */
  recordDatabase(metric: DatabaseMetric): void {
    this.databaseMetrics.push(metric);

    // Emit event
    this.emit('database', metric);

    // Trim if needed
    if (this.databaseMetrics.length > this.maxMetricsRetention) {
      this.databaseMetrics.shift();
    }

    // Log slow queries (>100ms)
    if (metric.latencyMs > 100) {
      console.warn(`⚠️  Slow database operation (${metric.latencyMs}ms): ${metric.operation}`);
    }
  }

  /**
   * Get performance statistics
   */
  getStats(): PerformanceStats {
    return {
      search: this.calculateSearchStats(),
      embedding: this.calculateEmbeddingStats(),
      database: this.calculateDatabaseStats()
    };
  }

  /**
   * Calculate search statistics
   */
  private calculateSearchStats(): PerformanceStats['search'] {
    if (this.searchMetrics.length === 0) {
      return {
        totalQueries: 0,
        avgLatencyMs: 0,
        p50LatencyMs: 0,
        p95LatencyMs: 0,
        p99LatencyMs: 0,
        slowestQuery: null
      };
    }

    const latencies = this.searchMetrics.map(m => m.latencyMs).sort((a, b) => a - b);
    const total = latencies.reduce((sum, l) => sum + l, 0);

    return {
      totalQueries: this.searchMetrics.length,
      avgLatencyMs: total / this.searchMetrics.length,
      p50LatencyMs: this.percentile(latencies, 0.5),
      p95LatencyMs: this.percentile(latencies, 0.95),
      p99LatencyMs: this.percentile(latencies, 0.99),
      slowestQuery: this.searchMetrics.reduce((slowest, current) =>
        current.latencyMs > (slowest?.latencyMs || 0) ? current : slowest
      )
    };
  }

  /**
   * Calculate embedding statistics
   */
  private calculateEmbeddingStats(): PerformanceStats['embedding'] {
    if (this.embeddingMetrics.length === 0) {
      return {
        totalRequests: 0,
        totalTokens: 0,
        totalCost: 0,
        avgLatencyMs: 0,
        avgTokensPerRequest: 0
      };
    }

    const totalTokens = this.embeddingMetrics.reduce((sum, m) => sum + m.totalTokens, 0);
    const totalCost = this.embeddingMetrics.reduce((sum, m) => sum + m.cost, 0);
    const totalLatency = this.embeddingMetrics.reduce((sum, m) => sum + m.latencyMs, 0);

    return {
      totalRequests: this.embeddingMetrics.length,
      totalTokens,
      totalCost,
      avgLatencyMs: totalLatency / this.embeddingMetrics.length,
      avgTokensPerRequest: totalTokens / this.embeddingMetrics.length
    };
  }

  /**
   * Calculate database statistics
   */
  private calculateDatabaseStats(): PerformanceStats['database'] {
    if (this.databaseMetrics.length === 0) {
      return {
        totalOperations: 0,
        avgLatencyMs: 0,
        operationCounts: {}
      };
    }

    const totalLatency = this.databaseMetrics.reduce((sum, m) => sum + m.latencyMs, 0);
    const operationCounts: Record<string, number> = {};

    for (const metric of this.databaseMetrics) {
      operationCounts[metric.operation] = (operationCounts[metric.operation] || 0) + 1;
    }

    return {
      totalOperations: this.databaseMetrics.length,
      avgLatencyMs: totalLatency / this.databaseMetrics.length,
      operationCounts
    };
  }

  /**
   * Calculate percentile
   */
  private percentile(sorted: number[], p: number): number {
    const index = Math.ceil(sorted.length * p) - 1;
    return sorted[Math.max(0, index)];
  }

  /**
   * Get recent search metrics
   */
  getRecentSearches(limit: number = 10): SearchMetric[] {
    return this.searchMetrics.slice(-limit);
  }

  /**
   * Get recent embedding metrics
   */
  getRecentEmbeddings(limit: number = 10): EmbeddingMetric[] {
    return this.embeddingMetrics.slice(-limit);
  }

  /**
   * Get recent database metrics
   */
  getRecentDatabaseOps(limit: number = 10): DatabaseMetric[] {
    return this.databaseMetrics.slice(-limit);
  }

  /**
   * Clean up old metrics (older than 1 hour)
   */
  private cleanupOldMetrics(): void {
    const oneHourAgo = Date.now() - 3600000;

    const beforeCount = this.searchMetrics.length + this.embeddingMetrics.length + this.databaseMetrics.length;

    this.searchMetrics = this.searchMetrics.filter(m => m.timestamp > oneHourAgo);
    this.embeddingMetrics = this.embeddingMetrics.filter(m => m.timestamp > oneHourAgo);
    this.databaseMetrics = this.databaseMetrics.filter(m => m.timestamp > oneHourAgo);

    const afterCount = this.searchMetrics.length + this.embeddingMetrics.length + this.databaseMetrics.length;
    const removed = beforeCount - afterCount;

    if (removed > 0) {
      console.log(`🧹 Cleaned up ${removed} old metrics (>1 hour)`);
    }
  }

  /**
   * Reset all metrics
   */
  reset(): void {
    this.searchMetrics = [];
    this.embeddingMetrics = [];
    this.databaseMetrics = [];
    console.log('🔄 Performance metrics reset');
  }

  /**
   * Export metrics for analysis
   */
  exportMetrics(): {
    search: SearchMetric[];
    embedding: EmbeddingMetric[];
    database: DatabaseMetric[];
  } {
    return {
      search: [...this.searchMetrics],
      embedding: [...this.embeddingMetrics],
      database: [...this.databaseMetrics]
    };
  }

  /**
   * Print performance report
   */
  printReport(): void {
    const stats = this.getStats();

    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║         Memory System - Performance Report            ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');

    console.log('🔍 Search Performance:');
    console.log(`   Total Queries:  ${stats.search.totalQueries}`);
    console.log(`   Avg Latency:    ${stats.search.avgLatencyMs.toFixed(1)}ms`);
    console.log(`   P50 Latency:    ${stats.search.p50LatencyMs.toFixed(1)}ms`);
    console.log(`   P95 Latency:    ${stats.search.p95LatencyMs.toFixed(1)}ms`);
    console.log(`   P99 Latency:    ${stats.search.p99LatencyMs.toFixed(1)}ms`);

    if (stats.search.slowestQuery) {
      console.log(`   Slowest Query:  ${stats.search.slowestQuery.latencyMs.toFixed(1)}ms`);
      console.log(`                   "${stats.search.slowestQuery.query.substring(0, 50)}..."`);
    }

    console.log('\n🤖 Embedding Performance:');
    console.log(`   Total Requests: ${stats.embedding.totalRequests}`);
    console.log(`   Total Tokens:   ${stats.embedding.totalTokens.toLocaleString()}`);
    console.log(`   Total Cost:     $${stats.embedding.totalCost.toFixed(4)}`);
    console.log(`   Avg Latency:    ${stats.embedding.avgLatencyMs.toFixed(1)}ms`);
    console.log(`   Avg Tokens:     ${stats.embedding.avgTokensPerRequest.toFixed(0)} per request`);

    console.log('\n💾 Database Performance:');
    console.log(`   Total Ops:      ${stats.database.totalOperations}`);
    console.log(`   Avg Latency:    ${stats.database.avgLatencyMs.toFixed(1)}ms`);

    if (Object.keys(stats.database.operationCounts).length > 0) {
      console.log('   Operation Breakdown:');
      for (const [op, count] of Object.entries(stats.database.operationCounts)) {
        console.log(`     - ${op}: ${count}`);
      }
    }

    console.log('');
  }

  /**
   * Clean up resources
   */
  close(): void {
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
      this.cleanupInterval = null;
    }
  }
}

// Singleton instance
let performanceMonitor: PerformanceMonitor | null = null;

/**
 * Get global performance monitor instance
 */
export function getPerformanceMonitor(): PerformanceMonitor {
  if (!performanceMonitor) {
    performanceMonitor = new PerformanceMonitor();
  }
  return performanceMonitor;
}
