/**
 * Response Cache Manager
 * Caches responses to avoid redundant API calls (ZIMA approach)
 */

import { createHash } from 'crypto';
import { MessageResponse, CachedResponse } from '../types';

export class ResponseCache {
  private cache: Map<string, CachedResponse>;
  private ttl: number;
  private maxSize: number;
  private hits: number;
  private misses: number;

  constructor(ttl: number = 3600000, maxSize: number = 1000) {
    this.cache = new Map();
    this.ttl = ttl; // Default: 1 hour
    this.maxSize = maxSize;
    this.hits = 0;
    this.misses = 0;

    // Clean up expired entries every 5 minutes
    setInterval(() => this.cleanup(), 300000);
  }

  /**
   * Generate cache key from request parameters
   */
  generateCacheKey(
    message: string,
    sessionKey: string,
    messageCount: number,
    attachmentCount: number = 0
  ): string {
    // Create hash from key components
    const keyData = `${message}:${sessionKey}:${messageCount}:${attachmentCount}`;
    return createHash('sha256').update(keyData).digest('hex');
  }

  /**
   * Get cached response
   */
  get(cacheKey: string): MessageResponse | null {
    const cached = this.cache.get(cacheKey);

    if (!cached) {
      this.misses++;
      return null;
    }

    // Check if expired
    const age = Date.now() - cached.timestamp;
    if (age > this.ttl) {
      this.cache.delete(cacheKey);
      this.misses++;
      return null;
    }

    this.hits++;
    console.log(`💾 Cache HIT (${this.getHitRate().toFixed(1)}% hit rate)`);

    return {
      ...cached.response,
      fromCache: true
    };
  }

  /**
   * Set cached response
   */
  set(cacheKey: string, response: MessageResponse): void {
    // Check cache size limit
    if (this.cache.size >= this.maxSize) {
      this.evictOldest();
    }

    this.cache.set(cacheKey, {
      response,
      timestamp: Date.now(),
      cacheKey
    });

    console.log(`💾 Cached response (cache size: ${this.cache.size}/${this.maxSize})`);
  }

  /**
   * Check if key exists in cache
   */
  has(cacheKey: string): boolean {
    const cached = this.cache.get(cacheKey);
    if (!cached) return false;

    const age = Date.now() - cached.timestamp;
    return age <= this.ttl;
  }

  /**
   * Clear cache
   */
  clear(): void {
    this.cache.clear();
    this.hits = 0;
    this.misses = 0;
    console.log('💾 Cache cleared');
  }

  /**
   * Get cache statistics
   */
  getStats(): {
    size: number;
    maxSize: number;
    hits: number;
    misses: number;
    hitRate: number;
    oldestEntry: number;
  } {
    let oldestTimestamp = Date.now();

    for (const cached of this.cache.values()) {
      if (cached.timestamp < oldestTimestamp) {
        oldestTimestamp = cached.timestamp;
      }
    }

    return {
      size: this.cache.size,
      maxSize: this.maxSize,
      hits: this.hits,
      misses: this.misses,
      hitRate: this.getHitRate(),
      oldestEntry: Date.now() - oldestTimestamp
    };
  }

  /**
   * Get cache hit rate percentage
   */
  private getHitRate(): number {
    const total = this.hits + this.misses;
    return total === 0 ? 0 : (this.hits / total) * 100;
  }

  /**
   * Clean up expired entries
   */
  private cleanup(): void {
    const now = Date.now();
    let removed = 0;

    for (const [key, cached] of this.cache.entries()) {
      const age = now - cached.timestamp;
      if (age > this.ttl) {
        this.cache.delete(key);
        removed++;
      }
    }

    if (removed > 0) {
      console.log(`💾 Cache cleanup: removed ${removed} expired entries`);
    }
  }

  /**
   * Evict oldest entry when cache is full
   */
  private evictOldest(): void {
    let oldestKey: string | null = null;
    let oldestTimestamp = Date.now();

    for (const [key, cached] of this.cache.entries()) {
      if (cached.timestamp < oldestTimestamp) {
        oldestTimestamp = cached.timestamp;
        oldestKey = key;
      }
    }

    if (oldestKey) {
      this.cache.delete(oldestKey);
      console.log('💾 Evicted oldest cache entry');
    }
  }
}

// Singleton instance
let responseCache: ResponseCache | null = null;

/**
 * Get global response cache instance
 */
export function getResponseCache(): ResponseCache {
  if (!responseCache) {
    responseCache = new ResponseCache();
  }
  return responseCache;
}
