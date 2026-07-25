/**
 * Conversation Summarizer
 * Week 11: Advanced Features - LLM-Powered Summaries
 */

import Anthropic from '@anthropic-ai/sdk';
import { TranscriptMessage } from '../types';

export interface SummaryResult {
  summary: string;
  keyPoints: string[];
  messageCount: number;
  timestamp: string;
}

export class ConversationSummarizer {
  private anthropic: Anthropic;
  private model: string = 'claude-3-haiku-20240307'; // Fast and cheap for summaries

  constructor(apiKey?: string) {
    this.anthropic = new Anthropic({
      apiKey: apiKey || process.env.ANTHROPIC_API_KEY
    });
  }

  /**
   * Summarize a conversation
   */
  async summarize(
    messages: TranscriptMessage[],
    options: {
      maxLength?: number;
      includeKeyPoints?: boolean;
      focus?: string;
    } = {}
  ): Promise<SummaryResult> {
    const {
      maxLength = 500,
      includeKeyPoints = true,
      focus
    } = options;

    // Filter out non-message types
    const conversationMessages = messages.filter(m => m.type === 'message');

    if (conversationMessages.length === 0) {
      return {
        summary: 'No messages to summarize',
        keyPoints: [],
        messageCount: 0,
        timestamp: new Date().toISOString()
      };
    }

    // Build conversation text
    const conversationText = conversationMessages.map(m => {
      const role = m.role === 'user' ? 'User' : 'Assistant';
      const content = typeof m.content === 'string'
        ? m.content
        : JSON.stringify(m.content);
      return `${role}: ${content}`;
    }).join('\n\n');

    // Create summarization prompt
    const prompt = this.buildSummarizationPrompt(
      conversationText,
      maxLength,
      includeKeyPoints,
      focus
    );

    try {
      const response = await this.anthropic.messages.create({
        model: this.model,
        max_tokens: 1000,
        messages: [{
          role: 'user',
          content: prompt
        }]
      });

      const responseText = response.content[0].type === 'text'
        ? response.content[0].text
        : '';

      // Parse response
      const result = this.parseResponse(responseText, conversationMessages.length);

      return result;

    } catch (error) {
      console.error('❌ Summarization failed:', error);

      // Fallback to simple summarization
      return this.fallbackSummarize(conversationMessages);
    }
  }

  /**
   * Build summarization prompt
   */
  private buildSummarizationPrompt(
    conversationText: string,
    maxLength: number,
    includeKeyPoints: boolean,
    focus?: string
  ): string {
    let prompt = `Please summarize the following conversation in ${maxLength} characters or less.\n\n`;

    if (focus) {
      prompt += `Focus on: ${focus}\n\n`;
    }

    if (includeKeyPoints) {
      prompt += `Provide:\n`;
      prompt += `1. A concise summary\n`;
      prompt += `2. 3-5 key points (one per line, starting with "- ")\n\n`;
      prompt += `Format your response as:\n`;
      prompt += `SUMMARY: [your summary here]\n\n`;
      prompt += `KEY POINTS:\n`;
      prompt += `- Point 1\n`;
      prompt += `- Point 2\n`;
      prompt += `etc.\n\n`;
    } else {
      prompt += `Provide only a concise summary.\n\n`;
    }

    prompt += `Conversation:\n\n${conversationText}`;

    return prompt;
  }

  /**
   * Parse Claude's response
   */
  private parseResponse(responseText: string, messageCount: number): SummaryResult {
    let summary = '';
    const keyPoints: string[] = [];

    // Try to extract structured response
    const summaryMatch = responseText.match(/SUMMARY:\s*(.+?)(?=\n\nKEY POINTS:|$)/s);
    if (summaryMatch) {
      summary = summaryMatch[1].trim();
    } else {
      // If no structured format, use first paragraph
      summary = responseText.split('\n\n')[0].trim();
    }

    // Extract key points
    const keyPointsMatch = responseText.match(/KEY POINTS:\s*\n((?:- .+\n?)+)/);
    if (keyPointsMatch) {
      const points = keyPointsMatch[1].match(/- (.+)/g);
      if (points) {
        keyPoints.push(...points.map(p => p.replace(/^- /, '').trim()));
      }
    }

    return {
      summary,
      keyPoints,
      messageCount,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * Fallback summarization (without LLM)
   */
  private fallbackSummarize(messages: TranscriptMessage[]): SummaryResult {
    const userMessages = messages.filter(m => m.role === 'user');
    const assistantMessages = messages.filter(m => m.role === 'assistant');

    const summary = `Conversation with ${messages.length} messages (${userMessages.length} from user, ${assistantMessages.length} from assistant)`;

    const keyPoints: string[] = [];

    // Extract first user message topic
    if (userMessages.length > 0) {
      const firstMessage = typeof userMessages[0].content === 'string'
        ? userMessages[0].content
        : JSON.stringify(userMessages[0].content);
      keyPoints.push(`Initial topic: ${firstMessage.substring(0, 100)}...`);
    }

    // Count tool uses
    const toolUses = messages.filter(m =>
      m.content && typeof m.content === 'object' && Array.isArray(m.content)
    ).length;

    if (toolUses > 0) {
      keyPoints.push(`Used ${toolUses} tools during conversation`);
    }

    return {
      summary,
      keyPoints,
      messageCount: messages.length,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * Summarize with batching for long conversations
   */
  async summarizeBatched(
    messages: TranscriptMessage[],
    batchSize: number = 20
  ): Promise<SummaryResult[]> {
    const batches: TranscriptMessage[][] = [];

    for (let i = 0; i < messages.length; i += batchSize) {
      batches.push(messages.slice(i, i + batchSize));
    }

    const summaries: SummaryResult[] = [];

    for (const batch of batches) {
      const summary = await this.summarize(batch, {
        maxLength: 300,
        includeKeyPoints: true
      });
      summaries.push(summary);
    }

    return summaries;
  }

  /**
   * Create hierarchical summary (summary of summaries)
   */
  async hierarchicalSummary(summaries: SummaryResult[]): Promise<SummaryResult> {
    if (summaries.length === 0) {
      return {
        summary: 'No summaries to consolidate',
        keyPoints: [],
        messageCount: 0,
        timestamp: new Date().toISOString()
      };
    }

    if (summaries.length === 1) {
      return summaries[0];
    }

    // Combine summaries
    const combinedText = summaries.map((s, idx) =>
      `Part ${idx + 1}: ${s.summary}`
    ).join('\n\n');

    const prompt = `Consolidate these conversation summaries into one coherent summary:\n\n${combinedText}\n\nProvide a unified summary that captures the main topics and progression.`;

    try {
      const response = await this.anthropic.messages.create({
        model: this.model,
        max_tokens: 1000,
        messages: [{
          role: 'user',
          content: prompt
        }]
      });

      const responseText = response.content[0].type === 'text'
        ? response.content[0].text
        : '';

      return {
        summary: responseText.trim(),
        keyPoints: summaries.flatMap(s => s.keyPoints).slice(0, 5),
        messageCount: summaries.reduce((sum, s) => sum + s.messageCount, 0),
        timestamp: new Date().toISOString()
      };

    } catch (error) {
      console.error('❌ Hierarchical summarization failed:', error);

      // Fallback
      return {
        summary: `Combined summary of ${summaries.length} conversation parts with ${summaries.reduce((sum, s) => sum + s.messageCount, 0)} total messages`,
        keyPoints: summaries.flatMap(s => s.keyPoints).slice(0, 5),
        messageCount: summaries.reduce((sum, s) => sum + s.messageCount, 0),
        timestamp: new Date().toISOString()
      };
    }
  }
}
