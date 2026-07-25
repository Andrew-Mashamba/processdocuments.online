/**
 * Text Chunker - Smart text splitting with token counting
 * Week 7-8: Memory System Foundation
 */

import { encoding_for_model } from 'tiktoken';
import { GatewayConfig } from '../types';

export interface Chunk {
  content: string;
  tokens: number;
  startLine: number;
  endLine: number;
  index: number;
}

export interface ChunkOptions {
  chunkSize?: number;
  chunkOverlap?: number;
  respectSentences?: boolean;
}

export class Chunker {
  private chunkSize: number;
  private chunkOverlap: number;
  private encoder: any;

  constructor(config: GatewayConfig) {
    this.chunkSize = config.memory?.chunkSize || 400;
    this.chunkOverlap = config.memory?.chunkOverlap || 80;

    // Initialize tiktoken encoder for GPT models
    // Using cl100k_base encoding (GPT-4, GPT-3.5-turbo)
    try {
      this.encoder = encoding_for_model('gpt-4');
      console.log(`✓ Tiktoken encoder initialized (chunk size: ${this.chunkSize}, overlap: ${this.chunkOverlap})`);
    } catch (error) {
      console.error('❌ Failed to initialize tiktoken encoder:', error);
      throw error;
    }
  }

  /**
   * Count tokens in text
   */
  countTokens(text: string): number {
    const tokens = this.encoder.encode(text);
    return tokens.length;
  }

  /**
   * Split text into chunks
   */
  splitIntoChunks(text: string, options: ChunkOptions = {}): Chunk[] {
    const chunkSize = options.chunkSize || this.chunkSize;
    const chunkOverlap = options.chunkOverlap || this.chunkOverlap;
    const respectSentences = options.respectSentences !== false;

    const lines = text.split('\n');
    const chunks: Chunk[] = [];

    let currentChunk: string[] = [];
    let currentTokens = 0;
    let currentStartLine = 0;
    let chunkIndex = 0;

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const lineTokens = this.countTokens(line + '\n');

      // If single line exceeds chunk size, split it
      if (lineTokens > chunkSize) {
        // Save current chunk if any
        if (currentChunk.length > 0) {
          chunks.push({
            content: currentChunk.join('\n'),
            tokens: currentTokens,
            startLine: currentStartLine,
            endLine: i - 1,
            index: chunkIndex++
          });
          currentChunk = [];
          currentTokens = 0;
        }

        // Split long line by sentences if requested
        if (respectSentences) {
          const sentenceChunks = this.splitLongLine(line, chunkSize);
          for (const sentenceChunk of sentenceChunks) {
            chunks.push({
              content: sentenceChunk,
              tokens: this.countTokens(sentenceChunk),
              startLine: i,
              endLine: i,
              index: chunkIndex++
            });
          }
        } else {
          // Just include the long line as-is
          chunks.push({
            content: line,
            tokens: lineTokens,
            startLine: i,
            endLine: i,
            index: chunkIndex++
          });
        }

        currentStartLine = i + 1;
        continue;
      }

      // Check if adding this line would exceed chunk size
      if (currentTokens + lineTokens > chunkSize && currentChunk.length > 0) {
        // Save current chunk
        chunks.push({
          content: currentChunk.join('\n'),
          tokens: currentTokens,
          startLine: currentStartLine,
          endLine: i - 1,
          index: chunkIndex++
        });

        // Start new chunk with overlap
        const overlapLines = this.getOverlapLines(currentChunk, chunkOverlap);
        currentChunk = overlapLines;
        currentTokens = this.countTokens(currentChunk.join('\n'));
        currentStartLine = i - overlapLines.length;
      }

      // Add line to current chunk
      currentChunk.push(line);
      currentTokens += lineTokens;
    }

    // Save final chunk
    if (currentChunk.length > 0) {
      chunks.push({
        content: currentChunk.join('\n'),
        tokens: currentTokens,
        startLine: currentStartLine,
        endLine: lines.length - 1,
        index: chunkIndex
      });
    }

    return chunks;
  }

  /**
   * Split a long line by sentences
   */
  private splitLongLine(line: string, maxTokens: number): string[] {
    // Split by common sentence boundaries
    const sentences = line.split(/(?<=[.!?])\s+/);
    const chunks: string[] = [];
    let currentChunk = '';

    for (const sentence of sentences) {
      const testChunk = currentChunk ? `${currentChunk} ${sentence}` : sentence;
      const tokens = this.countTokens(testChunk);

      if (tokens > maxTokens && currentChunk) {
        // Save current chunk and start new one
        chunks.push(currentChunk);
        currentChunk = sentence;
      } else {
        currentChunk = testChunk;
      }
    }

    if (currentChunk) {
      chunks.push(currentChunk);
    }

    return chunks.length > 0 ? chunks : [line];
  }

  /**
   * Get overlap lines from previous chunk
   */
  private getOverlapLines(lines: string[], targetTokens: number): string[] {
    const overlapLines: string[] = [];
    let tokenCount = 0;

    // Take lines from the end until we reach target overlap tokens
    for (let i = lines.length - 1; i >= 0; i--) {
      const lineTokens = this.countTokens(lines[i]);
      if (tokenCount + lineTokens > targetTokens) {
        break;
      }
      overlapLines.unshift(lines[i]);
      tokenCount += lineTokens;
    }

    return overlapLines;
  }

  /**
   * Clean up encoder resources
   */
  close(): void {
    if (this.encoder) {
      this.encoder.free();
    }
  }

  /**
   * Get chunk statistics
   */
  getChunkStats(chunks: Chunk[]): {
    totalChunks: number;
    totalTokens: number;
    avgTokensPerChunk: number;
    minTokens: number;
    maxTokens: number;
  } {
    if (chunks.length === 0) {
      return {
        totalChunks: 0,
        totalTokens: 0,
        avgTokensPerChunk: 0,
        minTokens: 0,
        maxTokens: 0
      };
    }

    const totalTokens = chunks.reduce((sum, chunk) => sum + chunk.tokens, 0);
    const tokens = chunks.map(c => c.tokens);

    return {
      totalChunks: chunks.length,
      totalTokens,
      avgTokensPerChunk: Math.round(totalTokens / chunks.length),
      minTokens: Math.min(...tokens),
      maxTokens: Math.max(...tokens)
    };
  }
}
