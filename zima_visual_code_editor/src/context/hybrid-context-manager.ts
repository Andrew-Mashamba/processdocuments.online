import { Message } from '../types';
import { TranscriptManager } from './transcript-manager';
import { MemoryService } from '../memory/memory-service';

export interface ContextOptions {
  sessionKey: string;
  optimization: 'none' | 'adaptive' | 'aggressive';
  memoryEnabled: boolean;
  maxContextMessages?: number;
}

export class HybridContextManager {
  private transcriptManager: TranscriptManager;
  private memoryService?: MemoryService;

  constructor(
    transcriptManager: TranscriptManager,
    memoryService?: MemoryService
  ) {
    this.transcriptManager = transcriptManager;
    this.memoryService = memoryService;
  }

  /**
   * Get optimized context for Claude
   */
  async getContext(options: ContextOptions): Promise<Message[]> {
    const allMessages = await this.transcriptManager.load(options.sessionKey);
    const messageCount = allMessages.length;

    if (options.optimization === 'none') {
      return allMessages;
    }

    // Determine tier based on message count
    const tier = this.determineTier(messageCount);

    switch (tier) {
      case 0:
        // Full context (< 10 messages)
        return allMessages;

      case 1:
        // Recent context (10-50 messages)
        return this.optimizeTier1(allMessages);

      case 2:
        // Optimized context (50-100 messages)
        return this.optimizeTier2(allMessages);

      case 3:
        // Minimal context (> 100 messages)
        return this.optimizeTier3(allMessages);

      default:
        return allMessages;
    }
  }

  /**
   * Determine context tier
   */
  private determineTier(messageCount: number): number {
    if (messageCount < 10) return 0;
    if (messageCount < 50) return 1;
    if (messageCount < 100) return 2;
    return 3;
  }

  /**
   * Tier 1: Keep last 30 messages + summary of older
   */
  private optimizeTier1(messages: Message[]): Message[] {
    if (messages.length <= 30) {
      return messages;
    }

    const recentMessages = messages.slice(-30);
    const olderMessages = messages.slice(0, -30);

    const summary = this.generateSummary(olderMessages);
    const summaryMessage: Message = {
      role: 'assistant',
      content: `[Context Summary of ${olderMessages.length} earlier messages: ${summary}]`,
      timestamp: new Date().toISOString(),
      metadata: { type: 'summary', tier: 1 },
    };

    return [summaryMessage, ...recentMessages];
  }

  /**
   * Tier 2: Keep last 20 messages + stronger summary
   */
  private optimizeTier2(messages: Message[]): Message[] {
    if (messages.length <= 20) {
      return messages;
    }

    const recentMessages = messages.slice(-20);
    const olderMessages = messages.slice(0, -20);

    const summary = this.generateStrongSummary(olderMessages);
    const summaryMessage: Message = {
      role: 'assistant',
      content: `[Condensed Context Summary: ${summary}]`,
      timestamp: new Date().toISOString(),
      metadata: { type: 'summary', tier: 2 },
    };

    return [summaryMessage, ...recentMessages];
  }

  /**
   * Tier 3: Keep last 10 messages + minimal summary
   */
  private optimizeTier3(messages: Message[]): Message[] {
    if (messages.length <= 10) {
      return messages;
    }

    const recentMessages = messages.slice(-10);
    const olderMessages = messages.slice(0, -10);

    const summary = this.generateMinimalSummary(olderMessages);
    const summaryMessage: Message = {
      role: 'assistant',
      content: `[Minimal Context: ${summary}]`,
      timestamp: new Date().toISOString(),
      metadata: { type: 'summary', tier: 3 },
    };

    return [summaryMessage, ...recentMessages];
  }

  /**
   * Generate summary of messages
   */
  private generateSummary(messages: Message[]): string {
    const userMessages = messages.filter(m => m.role === 'user');
    const topics = userMessages
      .map(m => this.extractTopic(m.content))
      .filter(Boolean)
      .slice(0, 5);

    if (topics.length === 0) {
      return 'Earlier conversation covered general topics.';
    }

    return `Earlier we discussed: ${topics.join(', ')}.`;
  }

  /**
   * Generate stronger summary
   */
  private generateStrongSummary(messages: Message[]): string {
    const userMessages = messages.filter(m => m.role === 'user');
    const topics = userMessages
      .map(m => this.extractTopic(m.content))
      .filter(Boolean)
      .slice(0, 3);

    if (topics.length === 0) {
      return 'Previous conversation history available.';
    }

    return `Key topics: ${topics.join(', ')}.`;
  }

  /**
   * Generate minimal summary
   */
  private generateMinimalSummary(messages: Message[]): string {
    return `${messages.length} earlier messages in conversation history.`;
  }

  /**
   * Extract topic from message content
   */
  private extractTopic(content: string | any[]): string | null {
    if (typeof content !== 'string') {
      return null;
    }

    // Simple topic extraction (first 50 chars)
    const cleaned = content.trim().replace(/\n/g, ' ').slice(0, 50);
    return cleaned || null;
  }

  /**
   * Search memory for relevant context
   */
  async searchMemory(query: string, limit: number = 3): Promise<string[]> {
    if (!this.memoryService) {
      return [];
    }

    const results = await this.memoryService.search({ query, limit });
    return results.map(r => r.content);
  }

  /**
   * Store message in memory
   */
  async storeInMemory(
    content: string,
    metadata: {
      type: 'conversation' | 'file' | 'workspace' | 'custom';
      sessionKey: string;
      timestamp: string;
    }
  ): Promise<void> {
    if (!this.memoryService) {
      return;
    }

    await this.memoryService.store(content, metadata);
  }

  /**
   * Classify task complexity
   */
  classifyTaskComplexity(message: string): 'simple' | 'standard' | 'complex' {
    const length = message.length;
    const hasAttachments = message.includes('attachment') || message.includes('file');
    const hasMultipleSteps = message.includes('and') || message.includes('then');

    if (length < 50 && !hasAttachments && !hasMultipleSteps) {
      return 'simple';
    }

    if (length > 1000 || hasMultipleSteps) {
      return 'complex';
    }

    return 'standard';
  }

  /**
   * Select model based on task complexity
   */
  selectModel(complexity: 'simple' | 'standard' | 'complex'): string {
    switch (complexity) {
      case 'simple':
        return 'claude-3-5-haiku-20241022';
      case 'standard':
        return 'claude-sonnet-4-5-20250514';
      case 'complex':
        return 'claude-opus-4-5-20251101';
      default:
        return 'claude-sonnet-4-5-20250514';
    }
  }

  /**
   * Calculate cost estimate
   */
  calculateCost(
    inputTokens: number,
    outputTokens: number,
    model: string,
    cacheHit: boolean = false
  ): number {
    const costs: Record<string, { input: number; output: number; cacheRead: number }> = {
      'claude-3-5-haiku-20241022': { input: 0.25, output: 1.25, cacheRead: 0.03 },
      'claude-sonnet-4-5-20250514': { input: 3.0, output: 15.0, cacheRead: 0.3 },
      'claude-opus-4-5-20251101': { input: 15.0, output: 75.0, cacheRead: 1.5 },
    };

    const cost = costs[model] || costs['claude-sonnet-4-5-20250514'];

    const inputCost = cacheHit
      ? (inputTokens * cost.cacheRead) / 1_000_000
      : (inputTokens * cost.input) / 1_000_000;

    const outputCost = (outputTokens * cost.output) / 1_000_000;

    return inputCost + outputCost;
  }
}
