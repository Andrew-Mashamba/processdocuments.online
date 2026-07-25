/**
 * Context Tier Optimizer
 * Optimizes context window based on conversation length (ZIMA approach)
 */

import { ContextTier, TaskComplexity, TranscriptMessage } from '../types';
import { ConversationSummarizer } from '../memory/conversation-summarizer';

export class ContextTierOptimizer {
  private summarizer: ConversationSummarizer;
  private useLLMSummarization: boolean;

  constructor(options: { useLLMSummarization?: boolean; apiKey?: string } = {}) {
    this.useLLMSummarization = options.useLLMSummarization ?? true;
    this.summarizer = new ConversationSummarizer(options.apiKey);
  }

  /**
   * Determine context tier based on message count and complexity
   */
  determineContextTier(messageCount: number, complexity: TaskComplexity): ContextTier {
    // Fresh conversations get full context
    if (messageCount < 5) {
      return 0; // Tier 0: Full context
    }

    // Complex tasks need more context
    if (complexity === 'complex') {
      if (messageCount < 15) return 0;
      if (messageCount < 30) return 1;
      if (messageCount < 60) return 2;
      return 3;
    }

    // Standard complexity
    if (complexity === 'standard') {
      if (messageCount < 20) return 1;
      if (messageCount < 50) return 2;
      return 3;
    }

    // Simple tasks don't need much context
    if (messageCount < 10) return 1;
    if (messageCount < 25) return 2;
    return 3;
  }

  /**
   * Filter messages based on context tier
   */
  async filterMessagesByTier(messages: TranscriptMessage[], tier: ContextTier): Promise<TranscriptMessage[]> {
    switch (tier) {
      case 0:
        // Tier 0: Full context - return all messages
        return messages;

      case 1:
        // Tier 1: Summarized old messages + recent full
        return await this.applySummarization(messages, 20);

      case 2:
        // Tier 2: Recent messages only (last 10)
        return this.getRecentMessages(messages, 10);

      case 3:
        // Tier 3: Minimal context (last 3 messages)
        return this.getRecentMessages(messages, 3);

      default:
        return messages;
    }
  }

  /**
   * Get only recent messages
   */
  private getRecentMessages(messages: TranscriptMessage[], count: number): TranscriptMessage[] {
    const messageOnly = messages.filter(m => m.type === 'message');
    const recent = messageOnly.slice(-count);

    // Also include any summaries
    const summaries = messages.filter(m => m.type === 'summary');

    return [...summaries, ...recent];
  }

  /**
   * Apply summarization (keep recent full, old messages as summary)
   */
  private async applySummarization(
    messages: TranscriptMessage[],
    keepRecentCount: number
  ): Promise<TranscriptMessage[]> {
    const messageOnly = messages.filter(m => m.type === 'message');

    if (messageOnly.length <= keepRecentCount) {
      return messages;
    }

    const recent = messageOnly.slice(-keepRecentCount);
    const old = messageOnly.slice(0, -keepRecentCount);

    // Check if we already have a summary
    const existingSummary = messages.find(m => m.type === 'summary');

    if (existingSummary) {
      return [existingSummary, ...recent];
    }

    // Create new summary using LLM or fallback
    const summaryContent = await this.createSummaryText(old);

    const summary: TranscriptMessage = {
      type: 'summary',
      timestamp: Date.now(),
      content: summaryContent,
      messageCount: old.length
    };

    return [summary, ...recent];
  }

  /**
   * Create summary text from old messages using LLM or fallback
   */
  private async createSummaryText(messages: TranscriptMessage[]): Promise<string> {
    // Try LLM-powered summarization if enabled
    if (this.useLLMSummarization) {
      try {
        const summaryResult = await this.summarizer.summarize(messages, {
          maxLength: 500,
          includeKeyPoints: true
        });

        // Format summary with key points
        let formattedSummary = `Earlier conversation summary (${summaryResult.messageCount} messages):\n\n`;
        formattedSummary += summaryResult.summary;

        if (summaryResult.keyPoints && summaryResult.keyPoints.length > 0) {
          formattedSummary += '\n\nKey points:\n';
          formattedSummary += summaryResult.keyPoints.map(point => `• ${point}`).join('\n');
        }

        return formattedSummary;
      } catch (error) {
        console.log('⚠️  LLM summarization failed, using fallback:', error instanceof Error ? error.message : 'Unknown error');
        // Fall through to manual summarization
      }
    }

    // Fallback to manual summarization
    return this.createManualSummary(messages);
  }

  /**
   * Create manual summary (fallback when LLM is unavailable)
   */
  private createManualSummary(messages: TranscriptMessage[]): string {
    const userMessages = messages.filter(m => m.role === 'user').length;
    const assistantMessages = messages.filter(m => m.role === 'assistant').length;

    const topics: string[] = [];

    // Extract topics from first few messages
    const firstMessages = messages.slice(0, 3);
    for (const msg of firstMessages) {
      if (msg.content && typeof msg.content === 'string' && msg.content.length > 20) {
        const preview = msg.content.substring(0, 50).trim();
        if (!topics.includes(preview)) {
          topics.push(preview);
        }
      }
    }

    let summary = `Earlier conversation summary (${messages.length} messages): `;
    summary += `${userMessages} user messages, ${assistantMessages} assistant responses.`;

    if (topics.length > 0) {
      summary += ` Topics discussed: ${topics.join('; ')}.`;
    }

    return summary;
  }

  /**
   * Estimate token count for messages (rough approximation)
   */
  estimateTokenCount(messages: TranscriptMessage[]): number {
    let total = 0;

    for (const msg of messages) {
      if (msg.content) {
        // Rough estimate: 1 token ≈ 4 characters
        total += msg.content.length / 4;
      }

      if (msg.type === 'tool_call' && msg.input) {
        total += JSON.stringify(msg.input).length / 4;
      }

      if (msg.type === 'tool_result' && msg.result) {
        total += JSON.stringify(msg.result).length / 4;
      }
    }

    return Math.ceil(total);
  }

  /**
   * Check if context optimization is needed
   */
  needsOptimization(messageCount: number): boolean {
    return messageCount >= 5;
  }

  /**
   * Get tier description
   */
  getTierDescription(tier: ContextTier): string {
    const descriptions = {
      0: 'Full context (all messages)',
      1: 'Optimized (summarized old + recent full)',
      2: 'Recent only (last 10 messages)',
      3: 'Minimal (last 3 messages)'
    };

    return descriptions[tier] || 'Unknown';
  }

  /**
   * Get tier statistics
   */
  getTierStats(tier: ContextTier, originalCount: number, filteredCount: number): {
    tier: ContextTier;
    description: string;
    originalCount: number;
    filteredCount: number;
    reduction: string;
  } {
    const reduction = originalCount > 0
      ? ((1 - filteredCount / originalCount) * 100).toFixed(1) + '%'
      : '0%';

    return {
      tier,
      description: this.getTierDescription(tier),
      originalCount,
      filteredCount,
      reduction
    };
  }
}
