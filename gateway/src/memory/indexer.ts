/**
 * File Indexer - Incremental indexing with embeddings
 * Week 7-8: Memory System Foundation
 */

import * as fs from 'fs-extra';
import * as path from 'path';
import * as crypto from 'crypto';
import { MemoryDatabase } from './memory-database';
import { Chunker, Chunk } from './chunker';
import { EmbeddingProvider } from './embedding-provider';
import { GatewayConfig, IndexStats } from '../types';
import * as glob from 'glob';

export interface IndexOptions {
  forceReindex?: boolean;
  skipEmbeddings?: boolean;
  filePattern?: string;
}

export interface IndexProgress {
  totalFiles: number;
  processedFiles: number;
  totalChunks: number;
  totalEmbeddings: number;
  startTime: number;
  currentFile?: string;
}

export class Indexer {
  private db: MemoryDatabase;
  private chunker: Chunker;
  private embeddings: EmbeddingProvider;
  private workspaceDir: string;
  private config: GatewayConfig;

  constructor(config: GatewayConfig) {
    this.config = config;
    this.db = new MemoryDatabase(config);
    this.chunker = new Chunker(config);
    this.embeddings = new EmbeddingProvider(config);
    this.workspaceDir = config.storage.workspace || path.join(process.cwd(), 'workspace');
  }

  /**
   * Index workspace directory
   */
  async indexWorkspace(options: IndexOptions = {}): Promise<IndexStats> {
    console.log(`\n📁 Indexing workspace: ${this.workspaceDir}`);

    const progress: IndexProgress = {
      totalFiles: 0,
      processedFiles: 0,
      totalChunks: 0,
      totalEmbeddings: 0,
      startTime: Date.now()
    };

    // Get all markdown and text files
    const pattern = options.filePattern || '**/*.{md,txt,json,yaml,yml}';
    const files = glob.sync(pattern, {
      cwd: this.workspaceDir,
      ignore: ['**/node_modules/**', '**/.git/**', '**/memory/**']
    });

    progress.totalFiles = files.length;
    console.log(`   Found ${files.length} files to index`);

    // Index each file
    for (const file of files) {
      const fullPath = path.join(this.workspaceDir, file);
      progress.currentFile = file;

      try {
        await this.indexFile(fullPath, file, options);
        progress.processedFiles++;

        // Show progress every 10 files
        if (progress.processedFiles % 10 === 0) {
          console.log(`   Progress: ${progress.processedFiles}/${progress.totalFiles} files`);
        }
      } catch (error: any) {
        console.error(`   ❌ Error indexing ${file}:`, error.message);
      }
    }

    // Update last sync time
    this.db.updateLastSync();

    // Get final stats
    const stats = this.db.getStats();
    const duration = Date.now() - progress.startTime;

    console.log(`\n✓ Indexing complete in ${(duration / 1000).toFixed(1)}s`);
    console.log(`   Files: ${stats.totalFiles}`);
    console.log(`   Chunks: ${stats.totalChunks}`);
    console.log(`   Embeddings: ${stats.totalEmbeddings}`);
    console.log(`   Database size: ${(stats.databaseSize / 1024 / 1024).toFixed(2)} MB`);

    return {
      totalFiles: stats.totalFiles,
      totalChunks: stats.totalChunks,
      totalEmbeddings: stats.totalEmbeddings,
      databaseSize: stats.databaseSize,
      lastSync: Date.now()
    };
  }

  /**
   * Index a single file
   */
  private async indexFile(
    fullPath: string,
    relativePath: string,
    options: IndexOptions
  ): Promise<void> {
    // Get file stats and hash
    const stats = await fs.stat(fullPath);
    const content = await fs.readFile(fullPath, 'utf-8');
    const hash = this.calculateHash(content);

    // Check if file needs reindexing
    const existingFile = this.db.getFile(relativePath);
    if (!options.forceReindex && existingFile && existingFile.hash === hash) {
      // File hasn't changed, skip
      return;
    }

    // Insert/update file record
    const fileId = this.db.insertFile({
      path: relativePath,
      hash,
      size: stats.size,
      mtime: stats.mtimeMs
    });

    // Delete old chunks if reindexing
    if (existingFile) {
      this.db.deleteChunksForFile(fileId);
    }

    // Split into chunks
    const chunks = this.chunker.splitIntoChunks(content);

    if (chunks.length === 0) {
      return;
    }

    // Insert chunks
    const chunkIds: number[] = [];
    for (const chunk of chunks) {
      const chunkId = this.db.insertChunk({
        file_id: fileId,
        chunk_index: chunk.index,
        content: chunk.content,
        tokens: chunk.tokens,
        start_line: chunk.startLine,
        end_line: chunk.endLine
      });
      chunkIds.push(chunkId);
    }

    // Generate embeddings if enabled
    if (!options.skipEmbeddings && this.embeddings.isAvailable()) {
      await this.generateEmbeddingsForChunks(chunkIds, chunks);
    }
  }

  /**
   * Generate embeddings for chunks
   */
  private async generateEmbeddingsForChunks(
    chunkIds: number[],
    chunks: Chunk[]
  ): Promise<void> {
    // Extract text content
    const texts = chunks.map(c => c.content);

    try {
      // Generate embeddings in batch
      const result = await this.embeddings.generateBatchEmbeddings(texts);

      // Store embeddings
      for (let i = 0; i < chunkIds.length; i++) {
        this.db.insertEmbedding({
          chunk_id: chunkIds[i],
          embedding: result.embeddings[i],
          model: result.model
        });
      }

      // Log cost estimate
      const cost = this.embeddings.estimateCost(result.totalTokens);
      if (cost > 0.001) {
        console.log(`   💰 Embedding cost: ~$${cost.toFixed(4)} (${result.totalTokens} tokens)`);
      }
    } catch (error: any) {
      console.warn(`   ⚠️  Failed to generate embeddings:`, error.message);
    }
  }

  /**
   * Calculate file content hash
   */
  private calculateHash(content: string): string {
    return crypto.createHash('sha256').update(content).digest('hex');
  }

  /**
   * Index a single file by path
   */
  async indexSingleFile(filePath: string, options: IndexOptions = {}): Promise<void> {
    const fullPath = path.isAbsolute(filePath)
      ? filePath
      : path.join(this.workspaceDir, filePath);

    const relativePath = path.relative(this.workspaceDir, fullPath);

    await this.indexFile(fullPath, relativePath, options);
  }

  /**
   * Remove file from index
   */
  async removeFile(filePath: string): Promise<void> {
    const relativePath = path.isAbsolute(filePath)
      ? path.relative(this.workspaceDir, filePath)
      : filePath;

    this.db.deleteFile(relativePath);
    console.log(`✓ Removed ${relativePath} from index`);
  }

  /**
   * Get index statistics
   */
  getStats(): IndexStats {
    const stats = this.db.getStats();
    const lastSync = this.db.getLastSync();

    return {
      totalFiles: stats.totalFiles,
      totalChunks: stats.totalChunks,
      totalEmbeddings: stats.totalEmbeddings,
      databaseSize: stats.databaseSize,
      lastSync
    };
  }

  /**
   * Close resources
   */
  close(): void {
    this.db.close();
    this.chunker.close();
  }
}
