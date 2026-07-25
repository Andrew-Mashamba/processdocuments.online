/**
 * Fact Extractor
 * Week 11: Advanced Features - Important Fact Extraction
 */

import Anthropic from '@anthropic-ai/sdk';
import { TranscriptMessage } from '../types';

export interface ExtractedFact {
  id: string;
  type: 'person' | 'place' | 'date' | 'event' | 'preference' | 'skill' | 'goal' | 'other';
  content: string;
  context: string;
  confidence: number;
  timestamp: string;
  sessionKey: string;
  sourceMessageIndex?: number;
}

export interface FactExtractionResult {
  facts: ExtractedFact[];
  messageCount: number;
  timestamp: string;
}

export class FactExtractor {
  private anthropic: Anthropic;
  private model: string = 'claude-3-haiku-20240307'; // Fast and cheap for extraction

  constructor(apiKey?: string) {
    this.anthropic = new Anthropic({
      apiKey: apiKey || process.env.ANTHROPIC_API_KEY
    });
  }

  /**
   * Extract facts from conversation messages
   */
  async extractFacts(
    messages: TranscriptMessage[],
    sessionKey: string,
    options: {
      extractPeople?: boolean;
      extractPlaces?: boolean;
      extractDates?: boolean;
      extractPreferences?: boolean;
      minConfidence?: number;
    } = {}
  ): Promise<FactExtractionResult> {
    const {
      extractPeople = true,
      extractPlaces = true,
      extractDates = true,
      extractPreferences = true,
      minConfidence = 0.7
    } = options;

    // Filter out non-message types
    const conversationMessages = messages.filter(m => m.type === 'message');

    if (conversationMessages.length === 0) {
      return {
        facts: [],
        messageCount: 0,
        timestamp: new Date().toISOString()
      };
    }

    // Build conversation text with message indices
    const conversationText = conversationMessages.map((m, idx) => {
      const role = m.role === 'user' ? 'User' : 'Assistant';
      const content = typeof m.content === 'string'
        ? m.content
        : JSON.stringify(m.content);
      return `[Message ${idx}] ${role}: ${content}`;
    }).join('\n\n');

    // Create extraction prompt
    const prompt = this.buildExtractionPrompt(
      conversationText,
      { extractPeople, extractPlaces, extractDates, extractPreferences }
    );

    try {
      const response = await this.anthropic.messages.create({
        model: this.model,
        max_tokens: 2000,
        messages: [{
          role: 'user',
          content: prompt
        }]
      });

      const responseText = response.content[0].type === 'text'
        ? response.content[0].text
        : '';

      // Parse response
      const facts = this.parseFactsResponse(
        responseText,
        sessionKey,
        minConfidence
      );

      return {
        facts,
        messageCount: conversationMessages.length,
        timestamp: new Date().toISOString()
      };

    } catch (error) {
      console.error('❌ Fact extraction failed:', error);

      // Fallback to simple extraction
      return this.fallbackExtraction(conversationMessages, sessionKey);
    }
  }

  /**
   * Build fact extraction prompt
   */
  private buildExtractionPrompt(
    conversationText: string,
    options: {
      extractPeople: boolean;
      extractPlaces: boolean;
      extractDates: boolean;
      extractPreferences: boolean;
    }
  ): string {
    let prompt = `Extract important facts from the following conversation.\n\n`;

    prompt += `Please identify:\n`;

    if (options.extractPeople) {
      prompt += `- PERSON: Names of people mentioned\n`;
    }
    if (options.extractPlaces) {
      prompt += `- PLACE: Locations, cities, countries, addresses\n`;
    }
    if (options.extractDates) {
      prompt += `- DATE: Specific dates, times, deadlines\n`;
    }
    if (options.extractPreferences) {
      prompt += `- PREFERENCE: User preferences, likes, dislikes\n`;
      prompt += `- SKILL: User skills or expertise mentioned\n`;
      prompt += `- GOAL: User goals or objectives\n`;
    }
    prompt += `- EVENT: Significant events or milestones\n`;
    prompt += `- OTHER: Any other important factual information\n\n`;

    prompt += `Format your response as a JSON array of facts:\n`;
    prompt += `[\n`;
    prompt += `  {\n`;
    prompt += `    "type": "person|place|date|event|preference|skill|goal|other",\n`;
    prompt += `    "content": "The actual fact",\n`;
    prompt += `    "context": "Brief context or explanation",\n`;
    prompt += `    "confidence": 0.0-1.0,\n`;
    prompt += `    "sourceMessageIndex": 0\n`;
    prompt += `  }\n`;
    prompt += `]\n\n`;

    prompt += `Only include facts that are:\n`;
    prompt += `1. Explicitly stated or clearly implied\n`;
    prompt += `2. Likely to be useful for future reference\n`;
    prompt += `3. Not trivial or obvious\n\n`;

    prompt += `Conversation:\n\n${conversationText}\n\n`;
    prompt += `Respond with ONLY the JSON array, no other text.`;

    return prompt;
  }

  /**
   * Parse Claude's response into facts
   */
  private parseFactsResponse(
    responseText: string,
    sessionKey: string,
    minConfidence: number
  ): ExtractedFact[] {
    const facts: ExtractedFact[] = [];

    try {
      // Try to extract JSON array from response
      const jsonMatch = responseText.match(/\[[\s\S]*\]/);
      if (!jsonMatch) {
        console.log('⚠️  No JSON array found in response');
        return facts;
      }

      const parsedFacts = JSON.parse(jsonMatch[0]);

      if (!Array.isArray(parsedFacts)) {
        console.log('⚠️  Response is not an array');
        return facts;
      }

      parsedFacts.forEach((fact: any) => {
        // Validate fact structure
        if (!fact.type || !fact.content || !fact.context) {
          return;
        }

        // Filter by confidence
        const confidence = fact.confidence || 0.5;
        if (confidence < minConfidence) {
          return;
        }

        facts.push({
          id: this.generateFactId(),
          type: fact.type,
          content: fact.content,
          context: fact.context,
          confidence,
          timestamp: new Date().toISOString(),
          sessionKey,
          sourceMessageIndex: fact.sourceMessageIndex
        });
      });

    } catch (error) {
      console.error('❌ Error parsing facts:', error);
    }

    return facts;
  }

  /**
   * Fallback extraction (without LLM)
   */
  private fallbackExtraction(
    messages: TranscriptMessage[],
    sessionKey: string
  ): FactExtractionResult {
    const facts: ExtractedFact[] = [];

    // Simple pattern-based extraction
    messages.forEach((msg, idx) => {
      if (msg.role !== 'user' || !msg.content || typeof msg.content !== 'string') {
        return;
      }

      const content = msg.content;

      // Extract names (simple pattern: capitalized words)
      const namePattern = /\b([A-Z][a-z]+ [A-Z][a-z]+)\b/g;
      let match;
      while ((match = namePattern.exec(content)) !== null) {
        facts.push({
          id: this.generateFactId(),
          type: 'person',
          content: match[1],
          context: `Mentioned in conversation`,
          confidence: 0.5,
          timestamp: new Date().toISOString(),
          sessionKey,
          sourceMessageIndex: idx
        });
      }

      // Extract dates (simple pattern)
      const datePattern = /\b(\d{1,2}\/\d{1,2}\/\d{2,4}|\d{4}-\d{2}-\d{2})\b/g;
      while ((match = datePattern.exec(content)) !== null) {
        facts.push({
          id: this.generateFactId(),
          type: 'date',
          content: match[1],
          context: `Date mentioned`,
          confidence: 0.6,
          timestamp: new Date().toISOString(),
          sessionKey,
          sourceMessageIndex: idx
        });
      }

      // Extract preferences (pattern: "I like/prefer/want")
      const prefPattern = /I (?:like|prefer|want|need|love)\s+(.+?)(?:\.|,|$)/gi;
      while ((match = prefPattern.exec(content)) !== null) {
        facts.push({
          id: this.generateFactId(),
          type: 'preference',
          content: match[1].trim(),
          context: `User preference`,
          confidence: 0.7,
          timestamp: new Date().toISOString(),
          sessionKey,
          sourceMessageIndex: idx
        });
      }
    });

    return {
      facts,
      messageCount: messages.length,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * Generate unique fact ID
   */
  private generateFactId(): string {
    return `fact_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Extract facts from a single message
   */
  async extractFromMessage(
    message: TranscriptMessage,
    sessionKey: string
  ): Promise<ExtractedFact[]> {
    const result = await this.extractFacts([message], sessionKey);
    return result.facts;
  }

  /**
   * Batch extract facts from multiple conversations
   */
  async extractFromSessions(
    sessions: { sessionKey: string; messages: TranscriptMessage[] }[]
  ): Promise<Map<string, ExtractedFact[]>> {
    const results = new Map<string, ExtractedFact[]>();

    for (const session of sessions) {
      const result = await this.extractFacts(session.messages, session.sessionKey);
      results.set(session.sessionKey, result.facts);
    }

    return results;
  }
}
