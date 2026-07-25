/**
 * Query Optimizer
 * Week 12: Production Polish - Database Query Optimization
 */

import Database from 'better-sqlite3';

export class QueryOptimizer {
  private db: Database.Database;
  private queryCache: Map<string, { result: any; timestamp: number }> = new Map();
  private cacheConfig: {
    enabled: boolean;
    ttl: number; // milliseconds
    maxSize: number;
  };

  constructor(db: Database.Database, cacheConfig?: {
    enabled?: boolean;
    ttl?: number;
    maxSize?: number;
  }) {
    this.db = db;
    this.cacheConfig = {
      enabled: cacheConfig?.enabled ?? true,
      ttl: cacheConfig?.ttl || 60000, // 1 minute default
      maxSize: cacheConfig?.maxSize || 1000
    };

    // Enable WAL mode for better concurrency
    this.db.pragma('journal_mode = WAL');

    // Optimize SQLite settings
    this.optimizeDatabase();
  }

  private optimizeDatabase(): void {
    // Increase cache size (in KB)
    this.db.pragma('cache_size = -64000'); // 64MB

    // Synchronous mode (faster but less safe - use for non-critical data)
    this.db.pragma('synchronous = NORMAL');

    // Increase temp store size
    this.db.pragma('temp_store = MEMORY');

    // Enable memory-mapped I/O (faster reads)
    this.db.pragma('mmap_size = 268435456'); // 256MB

    // Optimize for multi-threaded access
    this.db.pragma('busy_timeout = 5000');
  }

  /**
   * Execute query with caching
   */
  query<T = any>(
    sql: string,
    params?: any[],
    options?: {
      cache?: boolean;
      cacheTTL?: number;
      cacheKey?: string;
    }
  ): T[] {
    const cacheEnabled = options?.cache ?? this.cacheConfig.enabled;
    const cacheTTL = options?.cacheTTL || this.cacheConfig.ttl;
    const cacheKey = options?.cacheKey || this.generateCacheKey(sql, params);

    // Check cache
    if (cacheEnabled) {
      const cached = this.queryCache.get(cacheKey);
      if (cached && Date.now() - cached.timestamp < cacheTTL) {
        return cached.result;
      }
    }

    // Execute query
    const stmt = this.db.prepare(sql);
    const result = params ? stmt.all(...params) : stmt.all();

    // Cache result
    if (cacheEnabled) {
      this.cacheResult(cacheKey, result);
    }

    return result as T[];
  }

  /**
   * Execute single-row query
   */
  queryOne<T = any>(
    sql: string,
    params?: any[],
    options?: { cache?: boolean; cacheTTL?: number }
  ): T | undefined {
    const results = this.query<T>(sql, params, options);
    return results[0];
  }

  /**
   * Execute write query (no caching)
   */
  execute(sql: string, params?: any[]): Database.RunResult {
    const stmt = this.db.prepare(sql);
    const result = params ? stmt.run(...params) : stmt.run();

    // Invalidate related cache entries
    this.invalidateCache(sql);

    return result;
  }

  /**
   * Execute many queries in a transaction
   */
  executeMany(queries: { sql: string; params?: any[] }[]): void {
    const transaction = this.db.transaction((queries: typeof queries) => {
      for (const { sql, params } of queries) {
        const stmt = this.db.prepare(sql);
        if (params) {
          stmt.run(...params);
        } else {
          stmt.run();
        }
      }
    });

    transaction(queries);

    // Invalidate all cache
    this.clearCache();
  }

  /**
   * Prepare statement for reuse
   */
  prepare(sql: string): Database.Statement {
    return this.db.prepare(sql);
  }

  /**
   * Create index
   */
  createIndex(tableName: string, columns: string[], indexName?: string): void {
    const name = indexName || `idx_${tableName}_${columns.join('_')}`;
    const sql = `CREATE INDEX IF NOT EXISTS ${name} ON ${tableName} (${columns.join(', ')})`;
    this.db.exec(sql);
  }

  /**
   * Analyze query performance
   */
  analyzeQuery(sql: string, params?: any[]): {
    plan: any[];
    executionTime: number;
  } {
    const start = performance.now();

    // Get query plan
    const plan = this.db.prepare(`EXPLAIN QUERY PLAN ${sql}`).all();

    // Execute query
    const stmt = this.db.prepare(sql);
    if (params) {
      stmt.all(...params);
    } else {
      stmt.all();
    }

    const executionTime = performance.now() - start;

    return { plan, executionTime };
  }

  /**
   * Optimize table (vacuum and analyze)
   */
  optimizeTable(tableName: string): void {
    this.db.exec(`ANALYZE ${tableName}`);
  }

  /**
   * Get cache statistics
   */
  getCacheStats(): {
    size: number;
    maxSize: number;
    hitRate: number;
  } {
    return {
      size: this.queryCache.size,
      maxSize: this.cacheConfig.maxSize,
      hitRate: 0 // TODO: track hits/misses
    };
  }

  private generateCacheKey(sql: string, params?: any[]): string {
    return `${sql}:${JSON.stringify(params || [])}`;
  }

  private cacheResult(key: string, result: any): void {
    // Evict oldest entries if cache is full
    if (this.queryCache.size >= this.cacheConfig.maxSize) {
      const firstKey = this.queryCache.keys().next().value;
      this.queryCache.delete(firstKey);
    }

    this.queryCache.set(key, {
      result,
      timestamp: Date.now()
    });
  }

  private invalidateCache(sql: string): void {
    // Simple invalidation: clear entries related to modified tables
    const tableMatch = sql.match(/(?:FROM|INTO|UPDATE)\s+(\w+)/i);
    if (tableMatch) {
      const tableName = tableMatch[1];

      for (const [key] of this.queryCache.entries()) {
        if (key.includes(tableName)) {
          this.queryCache.delete(key);
        }
      }
    }
  }

  clearCache(): void {
    this.queryCache.clear();
  }

  /**
   * Enable query logging
   */
  enableQueryLogging(): void {
    this.db.function('log_query', (query: string) => {
      console.log('[SQL Query]', query);
      return null;
    });
  }
}
