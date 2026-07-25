/**
 * Memory Database - SQLite with vector search support
 * Week 7-8: Memory System Foundation
 */

import Database from 'better-sqlite3';
import * as path from 'path';
import * as fs from 'fs-extra';
import { GatewayConfig, FileRecord, ChunkRecord, EmbeddingRecord } from '../types';

export class MemoryDatabase {
  private db: Database.Database;
  private dbPath: string;

  constructor(config: GatewayConfig) {
    this.dbPath = config.memory?.database || path.join(
      config.storage.root,
      'agents',
      'main',
      'memory.db'
    );

    // Ensure directory exists
    fs.ensureDirSync(path.dirname(this.dbPath));

    // Open database
    this.db = new Database(this.dbPath);
    this.db.pragma('journal_mode = WAL'); // Write-Ahead Logging for better concurrency
    this.db.pragma('foreign_keys = ON');

    console.log(`📊 Memory database initialized: ${this.dbPath}`);

    // Initialize schema
    this.initSchema();
  }

  /**
   * Initialize database schema
   */
  private initSchema(): void {
    // Check if database is already initialized
    const result = this.db.prepare(
      "SELECT name FROM sqlite_master WHERE type='table' AND name='meta'"
    ).get();

    if (result) {
      console.log('✓ Memory database schema already exists');
      return;
    }

    console.log('🔨 Creating memory database schema...');

    // Run schema creation in transaction
    const createSchema = this.db.transaction(() => {
      // Meta table - database version and sync state
      this.db.exec(`
        CREATE TABLE IF NOT EXISTS meta (
          key TEXT PRIMARY KEY,
          value TEXT NOT NULL,
          updated_at INTEGER NOT NULL DEFAULT (unixepoch())
        )
      `);

      // Insert initial meta
      this.db.prepare(
        `INSERT INTO meta (key, value) VALUES ('version', '1.0'), ('last_sync', '0')`
      ).run();

      // Files table - tracks workspace files
      this.db.exec(`
        CREATE TABLE IF NOT EXISTS files (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          path TEXT UNIQUE NOT NULL,
          hash TEXT NOT NULL,
          size INTEGER NOT NULL,
          mtime INTEGER NOT NULL,
          indexed_at INTEGER NOT NULL DEFAULT (unixepoch())
        )
      `);

      this.db.exec(`
        CREATE INDEX IF NOT EXISTS idx_files_path ON files(path);
        CREATE INDEX IF NOT EXISTS idx_files_hash ON files(hash);
      `);

      // Chunks table - text chunks from files
      this.db.exec(`
        CREATE TABLE IF NOT EXISTS chunks (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          file_id INTEGER NOT NULL,
          chunk_index INTEGER NOT NULL,
          content TEXT NOT NULL,
          tokens INTEGER NOT NULL,
          start_line INTEGER NOT NULL,
          end_line INTEGER NOT NULL,
          FOREIGN KEY (file_id) REFERENCES files(id) ON DELETE CASCADE,
          UNIQUE(file_id, chunk_index)
        )
      `);

      this.db.exec(`
        CREATE INDEX IF NOT EXISTS idx_chunks_file_id ON chunks(file_id);
      `);

      // Embedding cache table - stores vector embeddings
      this.db.exec(`
        CREATE TABLE IF NOT EXISTS embedding_cache (
          chunk_id INTEGER PRIMARY KEY,
          embedding BLOB NOT NULL,
          model TEXT NOT NULL,
          created_at INTEGER NOT NULL DEFAULT (unixepoch()),
          FOREIGN KEY (chunk_id) REFERENCES chunks(id) ON DELETE CASCADE
        )
      `);

      // FTS5 virtual table for keyword search
      this.db.exec(`
        CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts USING fts5(
          content,
          content='chunks',
          content_rowid='id'
        )
      `);

      // Triggers to keep FTS5 in sync
      this.db.exec(`
        CREATE TRIGGER IF NOT EXISTS chunks_ai AFTER INSERT ON chunks BEGIN
          INSERT INTO chunks_fts(rowid, content) VALUES (new.id, new.content);
        END;
      `);

      this.db.exec(`
        CREATE TRIGGER IF NOT EXISTS chunks_ad AFTER DELETE ON chunks BEGIN
          INSERT INTO chunks_fts(chunks_fts, rowid, content) VALUES('delete', old.id, old.content);
        END;
      `);

      this.db.exec(`
        CREATE TRIGGER IF NOT EXISTS chunks_au AFTER UPDATE ON chunks BEGIN
          INSERT INTO chunks_fts(chunks_fts, rowid, content) VALUES('delete', old.id, old.content);
          INSERT INTO chunks_fts(rowid, content) VALUES (new.id, new.content);
        END;
      `);

      console.log('✓ Memory database schema created');
    });

    createSchema();
  }

  /**
   * Load sqlite-vec extension for vector search
   */
  async loadVectorExtension(): Promise<void> {
    try {
      // Try to load sqlite-vec extension
      // Note: This requires sqlite-vec to be installed and accessible
      // For now, we'll implement BM25-only search if extension isn't available
      const vecPath = require.resolve('sqlite-vec');
      this.db.loadExtension(vecPath);

      // Create virtual table for vector search
      this.db.exec(`
        CREATE VIRTUAL TABLE IF NOT EXISTS vec_chunks USING vec0(
          chunk_id INTEGER PRIMARY KEY,
          embedding FLOAT[1536]
        )
      `);

      console.log('✓ sqlite-vec extension loaded');
    } catch (error) {
      console.warn('⚠️  sqlite-vec extension not available, using BM25-only search');
      console.warn('   Install sqlite-vec for hybrid vector+keyword search');
    }
  }

  /**
   * Insert or update file record
   */
  insertFile(file: Omit<FileRecord, 'id' | 'indexed_at'>): number {
    const stmt = this.db.prepare(`
      INSERT INTO files (path, hash, size, mtime)
      VALUES (?, ?, ?, ?)
      ON CONFLICT(path) DO UPDATE SET
        hash = excluded.hash,
        size = excluded.size,
        mtime = excluded.mtime,
        indexed_at = unixepoch()
      RETURNING id
    `);

    const result = stmt.get(file.path, file.hash, file.size, file.mtime) as { id: number };
    return result.id;
  }

  /**
   * Get file by path
   */
  getFile(filePath: string): FileRecord | undefined {
    const stmt = this.db.prepare('SELECT * FROM files WHERE path = ?');
    return stmt.get(filePath) as FileRecord | undefined;
  }

  /**
   * Get all files
   */
  getAllFiles(): FileRecord[] {
    const stmt = this.db.prepare('SELECT * FROM files ORDER BY path');
    return stmt.all() as FileRecord[];
  }

  /**
   * Insert chunk
   */
  insertChunk(chunk: Omit<ChunkRecord, 'id'>): number {
    const stmt = this.db.prepare(`
      INSERT INTO chunks (file_id, chunk_index, content, tokens, start_line, end_line)
      VALUES (?, ?, ?, ?, ?, ?)
      ON CONFLICT(file_id, chunk_index) DO UPDATE SET
        content = excluded.content,
        tokens = excluded.tokens,
        start_line = excluded.start_line,
        end_line = excluded.end_line
      RETURNING id
    `);

    const result = stmt.get(
      chunk.file_id,
      chunk.chunk_index,
      chunk.content,
      chunk.tokens,
      chunk.start_line,
      chunk.end_line
    ) as { id: number };

    return result.id;
  }

  /**
   * Get chunks for file
   */
  getChunksForFile(fileId: number): ChunkRecord[] {
    const stmt = this.db.prepare(
      'SELECT * FROM chunks WHERE file_id = ? ORDER BY chunk_index'
    );
    return stmt.all(fileId) as ChunkRecord[];
  }

  /**
   * Insert or update embedding
   */
  insertEmbedding(embedding: Omit<EmbeddingRecord, 'created_at'>): void {
    const stmt = this.db.prepare(`
      INSERT INTO embedding_cache (chunk_id, embedding, model)
      VALUES (?, ?, ?)
      ON CONFLICT(chunk_id) DO UPDATE SET
        embedding = excluded.embedding,
        model = excluded.model,
        created_at = unixepoch()
    `);

    // Convert Float32Array to Buffer
    const buffer = Buffer.from(embedding.embedding.buffer);
    stmt.run(embedding.chunk_id, buffer, embedding.model);
  }

  /**
   * Get embedding for chunk
   */
  getEmbedding(chunkId: number): EmbeddingRecord | undefined {
    const stmt = this.db.prepare(
      'SELECT * FROM embedding_cache WHERE chunk_id = ?'
    );
    const result = stmt.get(chunkId) as any;

    if (!result) return undefined;

    // Convert Buffer back to Float32Array
    return {
      chunk_id: result.chunk_id,
      embedding: new Float32Array(result.embedding.buffer),
      model: result.model,
      created_at: result.created_at
    };
  }

  /**
   * BM25 keyword search using FTS5
   */
  keywordSearch(query: string, limit: number = 10): Array<{ chunk_id: number; rank: number }> {
    const stmt = this.db.prepare(`
      SELECT
        rowid as chunk_id,
        rank as rank
      FROM chunks_fts
      WHERE chunks_fts MATCH ?
      ORDER BY rank
      LIMIT ?
    `);

    return stmt.all(query, limit) as Array<{ chunk_id: number; rank: number }>;
  }

  /**
   * Get chunk by ID
   */
  getChunk(chunkId: number): (ChunkRecord & { file_path: string }) | undefined {
    const stmt = this.db.prepare(`
      SELECT
        c.*,
        f.path as file_path
      FROM chunks c
      JOIN files f ON c.file_id = f.id
      WHERE c.id = ?
    `);

    return stmt.get(chunkId) as (ChunkRecord & { file_path: string }) | undefined;
  }

  /**
   * Delete chunks for file
   */
  deleteChunksForFile(fileId: number): void {
    const stmt = this.db.prepare('DELETE FROM chunks WHERE file_id = ?');
    stmt.run(fileId);
  }

  /**
   * Delete file and its chunks
   */
  deleteFile(filePath: string): void {
    const stmt = this.db.prepare('DELETE FROM files WHERE path = ?');
    stmt.run(filePath);
  }

  /**
   * Get database statistics
   */
  getStats(): {
    totalFiles: number;
    totalChunks: number;
    totalEmbeddings: number;
    databaseSize: number;
  } {
    const files = this.db.prepare('SELECT COUNT(*) as count FROM files').get() as { count: number };
    const chunks = this.db.prepare('SELECT COUNT(*) as count FROM chunks').get() as { count: number };
    const embeddings = this.db.prepare('SELECT COUNT(*) as count FROM embedding_cache').get() as { count: number };

    // Get database file size
    const stats = fs.statSync(this.dbPath);

    return {
      totalFiles: files.count,
      totalChunks: chunks.count,
      totalEmbeddings: embeddings.count,
      databaseSize: stats.size
    };
  }

  /**
   * Update last sync time
   */
  updateLastSync(): void {
    const stmt = this.db.prepare(`
      UPDATE meta SET value = ?, updated_at = unixepoch() WHERE key = 'last_sync'
    `);
    stmt.run(Date.now().toString());
  }

  /**
   * Get last sync time
   */
  getLastSync(): number {
    const stmt = this.db.prepare("SELECT value FROM meta WHERE key = 'last_sync'");
    const result = stmt.get() as { value: string } | undefined;
    return result ? parseInt(result.value) : 0;
  }

  /**
   * Begin transaction
   */
  beginTransaction(): Database.Transaction {
    return this.db.transaction(() => {});
  }

  /**
   * Close database
   */
  close(): void {
    this.db.close();
    console.log('✓ Memory database closed');
  }

  /**
   * Get raw database instance (for advanced operations)
   */
  getDb(): Database.Database {
    return this.db;
  }
}
