/**
 * Embedding Provider - OpenAI text-embedding-3-small
 * Week 7-8: Memory System Foundation
 */

import OpenAI from 'openai';
import { GatewayConfig } from '../types';

export interface EmbeddingResult {
  embedding: Float32Array;
  tokens: number;
  model: string;
}

export interface BatchEmbeddingResult {
  embeddings: Float32Array[];
  totalTokens: number;
  model: string;
}

export class EmbeddingProvider {
  private client: OpenAI | null = null;
  private model: string;
  private enabled: boolean;
  private rateLimit: {
    requestsPerMinute: number;
    lastRequest: number;
    requestCount: number;
  };

  constructor(config: GatewayConfig) {
    this.model = config.memory?.embeddingModel || 'text-embedding-3-small';
    this.enabled = config.memory?.enabled || false;

    // Initialize rate limiting (OpenAI: 3000 RPM for tier 1)
    this.rateLimit = {
      requestsPerMinute: 3000,
      lastRequest: 0,
      requestCount: 0
    };

    // Initialize OpenAI client if API key is available
    const apiKey = process.env.OPENAI_API_KEY;
    if (apiKey && this.enabled) {
      this.client = new OpenAI({ apiKey });
      console.log(`✓ OpenAI embedding provider initialized (model: ${this.model})`);
    } else {
      console.warn('⚠️  OPENAI_API_KEY not set or memory disabled, embeddings unavailable');
    }
  }

  /**
   * Check if embeddings are available
   */
  isAvailable(): boolean {
    return this.client !== null && this.enabled;
  }

  /**
   * Generate embedding for single text
   */
  async generateEmbedding(text: string): Promise<EmbeddingResult> {
    if (!this.client) {
      throw new Error('OpenAI client not initialized. Set OPENAI_API_KEY environment variable.');
    }

    // Rate limiting
    await this.rateLimit_check();

    try {
      const response = await this.client.embeddings.create({
        model: this.model,
        input: text,
        encoding_format: 'float'
      });

      const embedding = response.data[0].embedding;

      return {
        embedding: new Float32Array(embedding),
        tokens: response.usage.total_tokens,
        model: this.model
      };
    } catch (error: any) {
      console.error('❌ Error generating embedding:', error.message);
      throw error;
    }
  }

  /**
   * Generate embeddings for multiple texts (batch API)
   * Max 2048 texts per batch
   */
  async generateBatchEmbeddings(texts: string[]): Promise<BatchEmbeddingResult> {
    if (!this.client) {
      throw new Error('OpenAI client not initialized. Set OPENAI_API_KEY environment variable.');
    }

    if (texts.length === 0) {
      return {
        embeddings: [],
        totalTokens: 0,
        model: this.model
      };
    }

    // OpenAI supports up to 2048 texts in a single batch
    const batchSize = 2048;
    const allEmbeddings: Float32Array[] = [];
    let totalTokens = 0;

    // Process in batches
    for (let i = 0; i < texts.length; i += batchSize) {
      const batch = texts.slice(i, i + batchSize);

      // Rate limiting
      await this.rateLimit_check();

      try {
        const response = await this.client.embeddings.create({
          model: this.model,
          input: batch,
          encoding_format: 'float'
        });

        // Convert to Float32Array
        for (const item of response.data) {
          allEmbeddings.push(new Float32Array(item.embedding));
        }

        totalTokens += response.usage.total_tokens;

        console.log(`✓ Generated ${batch.length} embeddings (${totalTokens} tokens total)`);
      } catch (error: any) {
        console.error('❌ Error generating batch embeddings:', error.message);
        throw error;
      }
    }

    return {
      embeddings: allEmbeddings,
      totalTokens,
      model: this.model
    };
  }

  /**
   * Rate limiting check with exponential backoff
   */
  private async rateLimit_check(): Promise<void> {
    const now = Date.now();
    const minuteAgo = now - 60000;

    // Reset counter if more than a minute has passed
    if (this.rateLimit.lastRequest < minuteAgo) {
      this.rateLimit.requestCount = 0;
    }

    // Check if we've exceeded rate limit
    if (this.rateLimit.requestCount >= this.rateLimit.requestsPerMinute) {
      const waitTime = 60000 - (now - this.rateLimit.lastRequest);
      if (waitTime > 0) {
        console.log(`⏱️  Rate limit reached, waiting ${Math.ceil(waitTime / 1000)}s...`);
        await new Promise(resolve => setTimeout(resolve, waitTime));
        this.rateLimit.requestCount = 0;
      }
    }

    this.rateLimit.lastRequest = now;
    this.rateLimit.requestCount++;
  }

  /**
   * Calculate cosine similarity between two embeddings
   */
  static cosineSimilarity(a: Float32Array, b: Float32Array): number {
    if (a.length !== b.length) {
      throw new Error('Embeddings must have same dimensions');
    }

    let dotProduct = 0;
    let normA = 0;
    let normB = 0;

    for (let i = 0; i < a.length; i++) {
      dotProduct += a[i] * b[i];
      normA += a[i] * a[i];
      normB += b[i] * b[i];
    }

    return dotProduct / (Math.sqrt(normA) * Math.sqrt(normB));
  }

  /**
   * Get embedding dimensions
   */
  getDimensions(): number {
    // text-embedding-3-small: 1536 dimensions
    // text-embedding-3-large: 3072 dimensions
    return this.model.includes('large') ? 3072 : 1536;
  }

  /**
   * Estimate cost for number of tokens
   * text-embedding-3-small: $0.02 per 1M tokens
   * text-embedding-3-large: $0.13 per 1M tokens
   */
  estimateCost(tokens: number): number {
    const pricePerMillion = this.model.includes('large') ? 0.13 : 0.02;
    return (tokens / 1000000) * pricePerMillion;
  }
}
