/**
 * Batch Processor - Optimize embedding generation with batching and retry logic
 * Week 7-8: Memory System - Phase C
 */

import { EmbeddingProvider } from './embedding-provider';
import { getPerformanceMonitor } from './performance-monitor';

export interface BatchJob<T, R> {
  id: string;
  data: T;
  resolve: (result: R) => void;
  reject: (error: Error) => void;
  timestamp: number;
  retries: number;
}

export interface BatchProcessorOptions {
  maxBatchSize: number;
  batchTimeoutMs: number;
  maxRetries: number;
  retryDelayMs: number;
}

export class BatchProcessor {
  private embeddingProvider: EmbeddingProvider;
  private queue: BatchJob<string, Float32Array>[] = [];
  private processing: boolean = false;
  private batchTimer: NodeJS.Timeout | null = null;
  private options: BatchProcessorOptions;
  private monitor = getPerformanceMonitor();

  constructor(embeddingProvider: EmbeddingProvider, options?: Partial<BatchProcessorOptions>) {
    this.embeddingProvider = embeddingProvider;

    this.options = {
      maxBatchSize: options?.maxBatchSize || 2048, // OpenAI limit
      batchTimeoutMs: options?.batchTimeoutMs || 1000, // 1 second
      maxRetries: options?.maxRetries || 3,
      retryDelayMs: options?.retryDelayMs || 1000
    };

    console.log(`📦 Batch processor initialized (batch size: ${this.options.maxBatchSize}, timeout: ${this.options.batchTimeoutMs}ms)`);
  }

  /**
   * Add text to embedding queue
   */
  async generateEmbedding(text: string): Promise<Float32Array> {
    return new Promise<Float32Array>((resolve, reject) => {
      const job: BatchJob<string, Float32Array> = {
        id: this.generateJobId(),
        data: text,
        resolve,
        reject,
        timestamp: Date.now(),
        retries: 0
      };

      this.queue.push(job);

      // Start batch timer if not already running
      if (!this.batchTimer) {
        this.startBatchTimer();
      }

      // Process immediately if batch is full
      if (this.queue.length >= this.options.maxBatchSize) {
        this.processBatch();
      }
    });
  }

  /**
   * Start batch timer
   */
  private startBatchTimer(): void {
    this.batchTimer = setTimeout(() => {
      this.processBatch();
    }, this.options.batchTimeoutMs);
  }

  /**
   * Process current batch
   */
  private async processBatch(): Promise<void> {
    // Clear timer
    if (this.batchTimer) {
      clearTimeout(this.batchTimer);
      this.batchTimer = null;
    }

    // Check if already processing
    if (this.processing) {
      return;
    }

    // Check if queue is empty
    if (this.queue.length === 0) {
      return;
    }

    this.processing = true;

    // Take batch from queue
    const batchSize = Math.min(this.queue.length, this.options.maxBatchSize);
    const batch = this.queue.splice(0, batchSize);

    console.log(`📦 Processing batch: ${batch.length} texts`);

    try {
      await this.processBatchWithRetry(batch);
    } catch (error: any) {
      console.error('❌ Batch processing failed:', error.message);
      // Reject all jobs in batch
      for (const job of batch) {
        job.reject(error);
      }
    } finally {
      this.processing = false;

      // Continue processing if more jobs in queue
      if (this.queue.length > 0) {
        this.startBatchTimer();
      }
    }
  }

  /**
   * Process batch with retry logic
   */
  private async processBatchWithRetry(
    batch: BatchJob<string, Float32Array>[],
    retryCount: number = 0
  ): Promise<void> {
    const startTime = Date.now();

    try {
      // Extract text from jobs
      const texts = batch.map(job => job.data);

      // Generate embeddings
      const result = await this.embeddingProvider.generateBatchEmbeddings(texts);

      const duration = Date.now() - startTime;

      // Record performance metric
      this.monitor.recordEmbedding({
        textCount: texts.length,
        totalTokens: result.totalTokens,
        latencyMs: duration,
        cost: this.embeddingProvider.estimateCost(result.totalTokens),
        model: result.model,
        timestamp: Date.now()
      });

      // Resolve all jobs with their embeddings
      for (let i = 0; i < batch.length; i++) {
        batch[i].resolve(result.embeddings[i]);
      }

      console.log(`✓ Batch processed: ${batch.length} embeddings in ${duration}ms`);
    } catch (error: any) {
      // Retry logic
      if (retryCount < this.options.maxRetries) {
        const delay = this.options.retryDelayMs * Math.pow(2, retryCount); // Exponential backoff
        console.warn(`⚠️  Batch failed (attempt ${retryCount + 1}/${this.options.maxRetries}), retrying in ${delay}ms...`);

        await new Promise(resolve => setTimeout(resolve, delay));

        // Increment retry count for all jobs
        for (const job of batch) {
          job.retries++;
        }

        return await this.processBatchWithRetry(batch, retryCount + 1);
      } else {
        // Max retries exceeded
        throw error;
      }
    }
  }

  /**
   * Generate unique job ID
   */
  private generateJobId(): string {
    return `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
  }

  /**
   * Get queue status
   */
  getStatus(): {
    queueSize: number;
    processing: boolean;
    batchTimerActive: boolean;
  } {
    return {
      queueSize: this.queue.length,
      processing: this.processing,
      batchTimerActive: this.batchTimer !== null
    };
  }

  /**
   * Flush queue (process all pending jobs immediately)
   */
  async flush(): Promise<void> {
    console.log(`🔄 Flushing batch queue (${this.queue.length} pending jobs)...`);

    while (this.queue.length > 0) {
      await this.processBatch();
      // Wait a bit to avoid rate limiting
      if (this.queue.length > 0) {
        await new Promise(resolve => setTimeout(resolve, 100));
      }
    }

    console.log('✓ Batch queue flushed');
  }

  /**
   * Clear queue (reject all pending jobs)
   */
  clear(): void {
    const count = this.queue.length;

    for (const job of this.queue) {
      job.reject(new Error('Batch processor cleared'));
    }

    this.queue = [];

    if (this.batchTimer) {
      clearTimeout(this.batchTimer);
      this.batchTimer = null;
    }

    console.log(`🗑️  Cleared ${count} pending jobs from batch queue`);
  }

  /**
   * Clean up resources
   */
  async close(): Promise<void> {
    // Flush remaining jobs
    if (this.queue.length > 0) {
      await this.flush();
    }

    // Clear timer
    if (this.batchTimer) {
      clearTimeout(this.batchTimer);
      this.batchTimer = null;
    }

    console.log('✓ Batch processor closed');
  }
}
