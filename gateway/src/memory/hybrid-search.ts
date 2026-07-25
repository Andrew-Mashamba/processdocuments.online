/**
 * Hybrid Search - Combining vector similarity and BM25 keyword search
 * Week 7-8: Memory System - Search Implementation
 */

import { MemoryDatabase } from './memory-database';
import { EmbeddingProvider } from './embedding-provider';
import { SearchResult, SearchOptions } from '../types';
import { getPerformanceMonitor } from './performance-monitor';

export interface RankedResult {
  chunk_id: number;
  score: number;
  source: 'vector' | 'bm25' | 'hybrid';
}

export class HybridSearch {
  private db: MemoryDatabase;
  private embeddings: EmbeddingProvider;
  private vectorWeight: number;

  constructor(db: MemoryDatabase, embeddings: EmbeddingProvider, vectorWeight: number = 0.7) {
    this.db = db;
    this.embeddings = embeddings;
    this.vectorWeight = vectorWeight; // Default: 70% vector, 30% BM25
  }

  /**
   * Perform hybrid search combining vector similarity and BM25
   */
  async search(query: string, options: SearchOptions = {}): Promise<SearchResult[]> {
    const limit = options.limit || 5;
    const vectorWeight = options.vectorWeight || this.vectorWeight;
    const bm25Weight = 1 - vectorWeight;

    const startTime = Date.now();

    // Perform both searches in parallel
    const [vectorResults, bm25Results] = await Promise.all([
      this.vectorSearch(query, limit * 2), // Get more results for better RRF
      this.bm25Search(query, limit * 2)
    ]);

    // Combine using Reciprocal Rank Fusion (RRF)
    const combined = this.reciprocalRankFusion(
      vectorResults,
      bm25Results,
      vectorWeight,
      bm25Weight
    );

    // Get top results
    const topResults = combined.slice(0, limit);

    // Enrich with chunk details
    const enriched = await this.enrichResults(topResults);

    const duration = Date.now() - startTime;
    console.log(`🔍 Hybrid search completed in ${duration}ms (${enriched.length} results)`);

    // Record performance metric
    const monitor = getPerformanceMonitor();
    monitor.recordSearch({
      query,
      resultCount: enriched.length,
      latencyMs: duration,
      searchType: 'hybrid',
      timestamp: Date.now()
    });

    return enriched;
  }

  /**
   * Vector similarity search
   */
  private async vectorSearch(query: string, limit: number): Promise<RankedResult[]> {
    if (!this.embeddings.isAvailable()) {
      return [];
    }

    try {
      // Generate query embedding
      const queryEmbedding = await this.embeddings.generateEmbedding(query);

      // Get all embeddings from database
      // TODO: Use sqlite-vec for efficient vector search when available
      // For now, we'll do brute-force similarity calculation
      const allChunks = this.db.getDb()
        .prepare('SELECT chunk_id FROM embedding_cache')
        .all() as Array<{ chunk_id: number }>;

      const similarities: Array<{ chunk_id: number; similarity: number }> = [];

      for (const chunk of allChunks) {
        const embedding = this.db.getEmbedding(chunk.chunk_id);
        if (embedding) {
          const similarity = EmbeddingProvider.cosineSimilarity(
            queryEmbedding.embedding,
            embedding.embedding
          );
          similarities.push({
            chunk_id: chunk.chunk_id,
            similarity
          });
        }
      }

      // Sort by similarity (descending)
      similarities.sort((a, b) => b.similarity - a.similarity);

      // Take top N and convert to RankedResult
      return similarities.slice(0, limit).map((result, index) => ({
        chunk_id: result.chunk_id,
        score: result.similarity,
        source: 'vector' as const
      }));
    } catch (error: any) {
      console.warn('⚠️  Vector search failed:', error.message);
      return [];
    }
  }

  /**
   * BM25 keyword search using FTS5
   */
  private async bm25Search(query: string, limit: number): Promise<RankedResult[]> {
    try {
      const results = this.db.keywordSearch(query, limit);

      // Convert to RankedResult and normalize scores
      // FTS5 rank is negative (lower is better), so we negate it
      const maxRank = Math.max(...results.map(r => Math.abs(r.rank)), 1);

      return results.map(result => ({
        chunk_id: result.chunk_id,
        score: Math.abs(result.rank) / maxRank, // Normalize to 0-1
        source: 'bm25' as const
      }));
    } catch (error: any) {
      console.warn('⚠️  BM25 search failed:', error.message);
      return [];
    }
  }

  /**
   * Reciprocal Rank Fusion (RRF) algorithm
   * Combines ranked lists from different sources
   */
  private reciprocalRankFusion(
    vectorResults: RankedResult[],
    bm25Results: RankedResult[],
    vectorWeight: number,
    bm25Weight: number
  ): RankedResult[] {
    const k = 60; // RRF constant
    const scores = new Map<number, number>();

    // Calculate RRF scores for vector results
    vectorResults.forEach((result, rank) => {
      const rrfScore = vectorWeight / (k + rank + 1);
      scores.set(result.chunk_id, (scores.get(result.chunk_id) || 0) + rrfScore);
    });

    // Calculate RRF scores for BM25 results
    bm25Results.forEach((result, rank) => {
      const rrfScore = bm25Weight / (k + rank + 1);
      scores.set(result.chunk_id, (scores.get(result.chunk_id) || 0) + rrfScore);
    });

    // Convert to array and sort by combined score
    const combined: RankedResult[] = Array.from(scores.entries()).map(
      ([chunk_id, score]) => ({
        chunk_id,
        score,
        source: 'hybrid' as const
      })
    );

    combined.sort((a, b) => b.score - a.score);

    return combined;
  }

  /**
   * Enrich ranked results with chunk details
   */
  private async enrichResults(results: RankedResult[]): Promise<SearchResult[]> {
    const enriched: SearchResult[] = [];

    for (let i = 0; i < results.length; i++) {
      const result = results[i];
      const chunk = this.db.getChunk(result.chunk_id);

      if (chunk) {
        enriched.push({
          chunk_id: result.chunk_id,
          file_path: chunk.file_path,
          content: chunk.content,
          score: result.score,
          rank: i + 1,
          start_line: chunk.start_line,
          end_line: chunk.end_line,
          tokens: chunk.tokens
        });
      }
    }

    return enriched;
  }

  /**
   * Search with only BM25 (fallback when embeddings unavailable)
   */
  async bm25OnlySearch(query: string, limit: number = 5): Promise<SearchResult[]> {
    const startTime = Date.now();
    console.log('🔍 BM25-only search (embeddings unavailable)');

    const results = await this.bm25Search(query, limit);
    const enriched = await this.enrichResults(results);

    const duration = Date.now() - startTime;

    // Record performance metric
    const monitor = getPerformanceMonitor();
    monitor.recordSearch({
      query,
      resultCount: enriched.length,
      latencyMs: duration,
      searchType: 'bm25',
      timestamp: Date.now()
    });

    return enriched;
  }

  /**
   * Update vector weight for future searches
   */
  setVectorWeight(weight: number): void {
    if (weight < 0 || weight > 1) {
      throw new Error('Vector weight must be between 0 and 1');
    }
    this.vectorWeight = weight;
  }
}
