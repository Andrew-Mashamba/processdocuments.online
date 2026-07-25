import Database from 'better-sqlite3';
import { Memory, SearchOptions } from '../types';
import * as path from 'path';
import * as fs from 'fs';

export class MemoryService {
  private db: Database.Database;
  private embeddings: EmbeddingProvider;

  constructor(dbPath: string) {
    // Ensure directory exists
    const dir = path.dirname(dbPath);
    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
    }

    this.db = new Database(dbPath);
    this.embeddings = new EmbeddingProvider();
    this.initializeDatabase();
  }

  /**
   * Initialize database schema
   */
  private initializeDatabase(): void {
    this.db.exec(`
      CREATE TABLE IF NOT EXISTS memories (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        content TEXT NOT NULL,
        embedding BLOB,
        metadata TEXT NOT NULL,
        created_at TEXT NOT NULL DEFAULT (datetime('now'))
      );

      CREATE VIRTUAL TABLE IF NOT EXISTS memories_fts
      USING fts5(content, tokenize='porter unicode61');

      CREATE INDEX IF NOT EXISTS idx_memories_created_at ON memories(created_at);
      CREATE INDEX IF NOT EXISTS idx_memories_metadata ON memories(json_extract(metadata, '$.type'));
    `);
  }

  /**
   * Store a memory
   */
  async store(content: string, metadata: Memory['metadata']): Promise<number> {
    const embedding = await this.embeddings.embed(content);

    const stmt = this.db.prepare(`
      INSERT INTO memories (content, embedding, metadata)
      VALUES (?, ?, ?)
    `);

    const result = stmt.run(
      content,
      embedding ? Buffer.from(new Float32Array(embedding).buffer) : null,
      JSON.stringify(metadata)
    );

    // Also insert into FTS table for keyword search
    if (result.lastInsertRowid) {
      this.db.prepare(`
        INSERT INTO memories_fts (rowid, content)
        VALUES (?, ?)
      `).run(result.lastInsertRowid, content);
    }

    return result.lastInsertRowid as number;
  }

  /**
   * Hybrid search (vector + keyword)
   */
  async search(options: SearchOptions): Promise<Memory[]> {
    const limit = options.limit || 5;
    const threshold = options.threshold || 0.7;

    // Vector search
    const queryEmbedding = await this.embeddings.embed(options.query);
    const vectorResults = queryEmbedding
      ? this.vectorSearch(queryEmbedding, limit * 2, threshold)
      : [];

    // Keyword search (BM25 via FTS5)
    const keywordResults = this.keywordSearch(options.query, limit * 2);

    // Hybrid: 70% vector + 30% keyword
    const combined = this.combineResults(vectorResults, keywordResults, 0.7, 0.3);

    // Filter by type if specified
    let results = combined;
    if (options.type) {
      results = results.filter((m) => m.metadata.type === options.type);
    }

    return results.slice(0, limit);
  }

  /**
   * Vector similarity search
   */
  private vectorSearch(
    queryEmbedding: number[],
    limit: number,
    threshold: number
  ): Array<Memory & { score: number }> {
    const stmt = this.db.prepare(`
      SELECT id, content, metadata, created_at, embedding
      FROM memories
      WHERE embedding IS NOT NULL
    `);

    const rows = stmt.all() as any[];
    const results: Array<Memory & { score: number }> = [];

    for (const row of rows) {
      if (!row.embedding) continue;

      const embedding = Array.from(new Float32Array(
        row.embedding.buffer,
        row.embedding.byteOffset,
        row.embedding.byteLength / 4
      ));

      const similarity = this.cosineSimilarity(queryEmbedding, embedding);

      if (similarity >= threshold) {
        results.push({
          id: row.id,
          content: row.content,
          metadata: JSON.parse(row.metadata),
          createdAt: row.created_at,
          score: similarity,
        });
      }
    }

    return results.sort((a, b) => b.score - a.score).slice(0, limit);
  }

  /**
   * Keyword search using FTS5
   */
  private keywordSearch(query: string, limit: number): Array<Memory & { score: number }> {
    try {
      const stmt = this.db.prepare(`
        SELECT m.id, m.content, m.metadata, m.created_at, fts.rank
        FROM memories_fts fts
        JOIN memories m ON fts.rowid = m.id
        WHERE memories_fts MATCH ?
        ORDER BY fts.rank
        LIMIT ?
      `);

      const rows = stmt.all(query, limit) as any[];

      return rows.map((row: any) => ({
        id: row.id,
        content: row.content,
        metadata: JSON.parse(row.metadata),
        createdAt: row.created_at,
        score: Math.abs(row.rank), // FTS5 rank is negative
      }));
    } catch (error) {
      // FTS query might fail with special characters
      return [];
    }
  }

  /**
   * Combine vector and keyword results
   */
  private combineResults(
    vectorResults: Array<Memory & { score: number }>,
    keywordResults: Array<Memory & { score: number }>,
    vectorWeight: number,
    keywordWeight: number
  ): Memory[] {
    const combined = new Map<number, Memory & { score: number }>();

    // Normalize scores to 0-1 range
    const maxVectorScore = Math.max(...vectorResults.map(r => r.score), 1);
    const maxKeywordScore = Math.max(...keywordResults.map(r => r.score), 1);

    for (const result of vectorResults) {
      combined.set(result.id, {
        ...result,
        score: (result.score / maxVectorScore) * vectorWeight,
      });
    }

    for (const result of keywordResults) {
      const normalizedScore = (result.score / maxKeywordScore) * keywordWeight;
      const existing = combined.get(result.id);
      if (existing) {
        existing.score += normalizedScore;
      } else {
        combined.set(result.id, {
          ...result,
          score: normalizedScore,
        });
      }
    }

    return Array.from(combined.values())
      .sort((a, b) => b.score - a.score)
      .map(({ score, ...memory }) => memory);
  }

  /**
   * Cosine similarity
   */
  private cosineSimilarity(a: number[], b: number[]): number {
    if (a.length !== b.length) return 0;

    const dotProduct = a.reduce((sum, val, i) => sum + val * b[i], 0);
    const magA = Math.sqrt(a.reduce((sum, val) => sum + val * val, 0));
    const magB = Math.sqrt(b.reduce((sum, val) => sum + val * val, 0));

    if (magA === 0 || magB === 0) return 0;
    return dotProduct / (magA * magB);
  }

  /**
   * Get memory by ID
   */
  get(id: number): Memory | null {
    const stmt = this.db.prepare(`
      SELECT id, content, metadata, created_at
      FROM memories
      WHERE id = ?
    `);

    const row = stmt.get(id) as any;
    if (!row) return null;

    return {
      id: row.id,
      content: row.content,
      metadata: JSON.parse(row.metadata),
      createdAt: row.created_at,
    };
  }

  /**
   * Delete memory
   */
  delete(id: number): boolean {
    const stmt = this.db.prepare('DELETE FROM memories WHERE id = ?');
    const result = stmt.run(id);

    // Also delete from FTS
    this.db.prepare('DELETE FROM memories_fts WHERE rowid = ?').run(id);

    return result.changes > 0;
  }

  /**
   * Clear all memories
   */
  clear(): void {
    this.db.exec('DELETE FROM memories');
    this.db.exec('DELETE FROM memories_fts');
  }

  /**
   * Get memory count
   */
  count(): number {
    const stmt = this.db.prepare('SELECT COUNT(*) as count FROM memories');
    const result = stmt.get() as any;
    return result.count;
  }

  /**
   * Close database connection
   */
  close(): void {
    this.db.close();
  }
}

/**
 * Simple embedding provider (placeholder)
 * In production, would use Voyage AI, OpenAI, or similar
 */
class EmbeddingProvider {
  async embed(text: string): Promise<number[] | null> {
    // Placeholder: Return null to skip vector search
    // In production, integrate with embedding API
    return null;
  }
}
