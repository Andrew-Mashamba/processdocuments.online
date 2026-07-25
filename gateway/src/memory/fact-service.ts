/**
 * Fact Service
 * Week 11: Advanced Features - Integrated Fact Management
 */

import { FactExtractor, ExtractedFact, FactExtractionResult } from './fact-extractor';
import { FactStore, FactQuery } from './fact-store';
import { TranscriptMessage } from '../types';

export interface FactServiceConfig {
  storageDir?: string;
  apiKey?: string;
  autoExtract?: boolean;
  extractionThreshold?: number; // Extract facts after N new messages
  minConfidence?: number;
}

export class FactService {
  private extractor: FactExtractor;
  private store: FactStore;
  private config: FactServiceConfig;
  private sessionMessageCounts: Map<string, number> = new Map();

  constructor(config: FactServiceConfig = {}) {
    this.config = {
      storageDir: config.storageDir || './storage/memory/facts',
      autoExtract: config.autoExtract ?? true,
      extractionThreshold: config.extractionThreshold || 10, // Extract every 10 messages
      minConfidence: config.minConfidence || 0.7,
      ...config
    };

    this.extractor = new FactExtractor(config.apiKey);
    this.store = new FactStore(this.config.storageDir);
  }

  /**
   * Initialize the service
   */
  async initialize(): Promise<void> {
    await this.store.load();
    console.log('✓ Fact Service initialized');
  }

  /**
   * Extract and store facts from messages
   */
  async extractAndStoreFacts(
    messages: TranscriptMessage[],
    sessionKey: string
  ): Promise<FactExtractionResult> {
    console.log(`🔍 Extracting facts from ${messages.length} messages (session: ${sessionKey})`);

    // Extract facts
    const result = await this.extractor.extractFacts(messages, sessionKey, {
      extractPeople: true,
      extractPlaces: true,
      extractDates: true,
      extractPreferences: true,
      minConfidence: this.config.minConfidence
    });

    if (result.facts.length > 0) {
      // Store extracted facts
      await this.store.saveFacts(result.facts);
      console.log(`✓ Extracted and stored ${result.facts.length} facts`);
    } else {
      console.log('⚠️  No facts extracted');
    }

    return result;
  }

  /**
   * Process new message and auto-extract facts if threshold reached
   */
  async processMessage(
    message: TranscriptMessage,
    sessionKey: string,
    allMessages?: TranscriptMessage[]
  ): Promise<void> {
    if (!this.config.autoExtract) {
      return;
    }

    // Track message count for this session
    const currentCount = this.sessionMessageCounts.get(sessionKey) || 0;
    const newCount = currentCount + 1;
    this.sessionMessageCounts.set(sessionKey, newCount);

    // Check if we should extract facts
    if (newCount % this.config.extractionThreshold! === 0) {
      console.log(`📊 Fact extraction threshold reached for session ${sessionKey}`);

      // If we have all messages, use them; otherwise just extract from this one
      if (allMessages && allMessages.length > 0) {
        // Extract from recent messages only (last extractionThreshold messages)
        const recentMessages = allMessages.slice(-this.config.extractionThreshold!);
        await this.extractAndStoreFacts(recentMessages, sessionKey);
      } else {
        // Extract from single message
        const facts = await this.extractor.extractFromMessage(message, sessionKey);
        if (facts.length > 0) {
          await this.store.saveFacts(facts);
        }
      }
    }
  }

  /**
   * Get facts for a session
   */
  async getSessionFacts(sessionKey: string, limit?: number): Promise<ExtractedFact[]> {
    return this.store.getFactsForSession(sessionKey, limit);
  }

  /**
   * Search facts
   */
  async searchFacts(query: FactQuery): Promise<ExtractedFact[]> {
    return this.store.query(query);
  }

  /**
   * Get facts by type
   */
  async getFactsByType(type: string, limit?: number): Promise<ExtractedFact[]> {
    return this.store.getFactsByType(type, limit);
  }

  /**
   * Get recent facts
   */
  async getRecentFacts(count: number = 10): Promise<ExtractedFact[]> {
    return this.store.getRecentFacts(count);
  }

  /**
   * Get high-confidence facts
   */
  async getHighConfidenceFacts(minConfidence?: number, limit?: number): Promise<ExtractedFact[]> {
    return this.store.getHighConfidenceFacts(
      minConfidence || this.config.minConfidence,
      limit
    );
  }

  /**
   * Build context string from relevant facts
   */
  async buildFactContext(
    sessionKey: string,
    options: {
      maxFacts?: number;
      includeTypes?: string[];
      minConfidence?: number;
    } = {}
  ): Promise<string> {
    const {
      maxFacts = 10,
      includeTypes,
      minConfidence = this.config.minConfidence
    } = options;

    // Query facts for this session
    const query: FactQuery = {
      sessionKey,
      minConfidence,
      limit: maxFacts
    };

    const facts = await this.store.query(query);

    if (facts.length === 0) {
      return '';
    }

    // Filter by type if specified
    let filteredFacts = facts;
    if (includeTypes && includeTypes.length > 0) {
      filteredFacts = facts.filter(f => includeTypes.includes(f.type));
    }

    if (filteredFacts.length === 0) {
      return '';
    }

    // Build context string
    let context = '<relevant_facts>\n';
    context += `The following facts have been extracted from previous conversations:\n\n`;

    // Group by type
    const byType = new Map<string, ExtractedFact[]>();
    filteredFacts.forEach(fact => {
      if (!byType.has(fact.type)) {
        byType.set(fact.type, []);
      }
      byType.get(fact.type)!.push(fact);
    });

    byType.forEach((facts, type) => {
      context += `${type.toUpperCase()}:\n`;
      facts.forEach(fact => {
        context += `  • ${fact.content}`;
        if (fact.context) {
          context += ` (${fact.context})`;
        }
        context += `\n`;
      });
      context += `\n`;
    });

    context += '</relevant_facts>\n';

    return context;
  }

  /**
   * Get statistics
   */
  async getStats() {
    return this.store.getStats();
  }

  /**
   * Clear old facts
   */
  async clearOldFacts(daysToKeep: number = 90): Promise<number> {
    return this.store.clearOldFacts(daysToKeep);
  }

  /**
   * Delete facts for a session
   */
  async deleteSessionFacts(sessionKey: string): Promise<number> {
    this.sessionMessageCounts.delete(sessionKey);
    return this.store.deleteSessionFacts(sessionKey);
  }

  /**
   * Export facts
   */
  async exportFacts(outputPath: string): Promise<void> {
    return this.store.exportFacts(outputPath);
  }

  /**
   * Batch process multiple sessions
   */
  async batchProcessSessions(
    sessions: { sessionKey: string; messages: TranscriptMessage[] }[]
  ): Promise<Map<string, number>> {
    console.log(`🔄 Batch processing ${sessions.length} sessions for fact extraction`);

    const results = new Map<string, number>();

    for (const session of sessions) {
      try {
        const result = await this.extractAndStoreFacts(
          session.messages,
          session.sessionKey
        );
        results.set(session.sessionKey, result.facts.length);
      } catch (error) {
        console.error(`❌ Error processing session ${session.sessionKey}:`, error);
        results.set(session.sessionKey, 0);
      }
    }

    return results;
  }

  /**
   * Get fact summary for a session
   */
  async getSessionFactSummary(sessionKey: string): Promise<{
    totalFacts: number;
    byType: { [type: string]: number };
    highConfidence: number;
    recentFacts: ExtractedFact[];
  }> {
    const facts = await this.store.getFactsForSession(sessionKey);

    const byType: { [type: string]: number } = {};
    let highConfidence = 0;

    facts.forEach(fact => {
      byType[fact.type] = (byType[fact.type] || 0) + 1;
      if (fact.confidence >= 0.8) {
        highConfidence++;
      }
    });

    const recentFacts = facts
      .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
      .slice(0, 5);

    return {
      totalFacts: facts.length,
      byType,
      highConfidence,
      recentFacts
    };
  }
}
