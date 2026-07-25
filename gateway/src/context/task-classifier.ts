/**
 * Task Classifier
 * Intelligently classifies tasks and selects appropriate model (ZIMA approach)
 */

import { TaskComplexity, GatewayConfig } from '../types';

export class TaskClassifier {
  private config: GatewayConfig;

  constructor(config: GatewayConfig) {
    this.config = config;
  }

  /**
   * Classify task complexity based on message content and context
   */
  classifyTask(
    message: string,
    messageCount: number,
    attachmentCount: number = 0
  ): TaskComplexity {
    const lower = message.toLowerCase();
    const length = message.length;

    // Simple task indicators
    const simplePatterns = [
      /^(what|who|when|where|why|how)\s+(is|are|was|were|does|do|did)/i,
      /^(explain|define|describe|list|show|tell)\s+/i,
      /^(yes|no|ok|thanks|thank you|hello|hi|hey)/i
    ];

    const simpleKeywords = [
      'what is', 'explain', 'define', 'list', 'show me',
      'tell me about', 'can you', 'please'
    ];

    // Complex task indicators
    const complexPatterns = [
      /create\s+multiple/i,
      /build\s+a\s+(system|platform|application)/i,
      /implement\s+(full|complete|entire)/i,
      /design\s+(architecture|database|schema)/i,
      /refactor\s+(entire|whole|complete)/i
    ];

    const complexKeywords = [
      'architecture', 'design system', 'implement',
      'build a platform', 'comprehensive', 'detailed analysis',
      'full implementation', 'end-to-end', 'complete solution'
    ];

    // Document generation keywords (usually standard complexity)
    const documentPatterns = [
      /create\s+(an?\s+)?(excel|pdf|word|powerpoint|ppt|document|spreadsheet|presentation)/i,
      /generate\s+(a\s+)?(report|document|file|spreadsheet)/i,
      /make\s+(a\s+)?(spreadsheet|document|presentation|report)/i,
      /build\s+(a\s+)?(document|spreadsheet|presentation)/i
    ];

    const documentKeywords = [
      'create excel', 'create pdf', 'create word', 'create powerpoint',
      'generate report', 'make a spreadsheet', 'build a document',
      'create a document', 'create file', 'generate document'
    ];

    // CHECK DOCUMENT GENERATION FIRST (always standard complexity)
    if (
      documentPatterns.some(pattern => pattern.test(message)) ||
      documentKeywords.some(keyword => lower.includes(keyword))
    ) {
      return 'standard';
    }

    // SIMPLE: Basic questions, greetings, short messages
    if (
      length < 50 ||
      (messageCount < 3 && !lower.includes('create') && !lower.includes('generate')) ||
      simplePatterns.some(pattern => pattern.test(message)) ||
      simpleKeywords.some(keyword => lower.startsWith(keyword))
    ) {
      return 'simple';
    }

    // COMPLEX: Multi-step tasks, architecture, long messages
    if (
      length > 1000 ||
      attachmentCount > 5 ||
      complexPatterns.some(pattern => pattern.test(message)) ||
      complexKeywords.some(keyword => lower.includes(keyword))
    ) {
      return 'complex';
    }

    // STANDARD: Everything else (including most document generation)
    return 'standard';
  }

  /**
   * Select model based on task complexity
   */
  selectModel(complexity: TaskComplexity): string {
    const models = this.config.models || {
      simple: 'claude-3-5-haiku-20241022',
      standard: 'claude-sonnet-4-20250514',
      complex: 'claude-opus-4-20250514'
    };

    return models[complexity];
  }

  /**
   * Get model cost per million tokens (input)
   */
  getModelCost(model: string): { input: number; output: number } {
    const costs: Record<string, { input: number; output: number }> = {
      'claude-3-5-haiku-20241022': { input: 0.25, output: 1.25 },
      'claude-sonnet-4-20250514': { input: 3.0, output: 15.0 },
      'claude-opus-4-20250514': { input: 15.0, output: 75.0 }
    };

    return costs[model] || { input: 3.0, output: 15.0 };
  }

  /**
   * Calculate estimated cost for a request
   */
  estimateCost(
    inputTokens: number,
    outputTokens: number,
    model: string
  ): number {
    const costs = this.getModelCost(model);
    const inputCost = (inputTokens / 1000000) * costs.input;
    const outputCost = (outputTokens / 1000000) * costs.output;
    return inputCost + outputCost;
  }

  /**
   * Get complexity statistics (for logging)
   */
  getComplexityStats(complexity: TaskComplexity): {
    name: string;
    model: string;
    costSavings: string;
  } {
    const model = this.selectModel(complexity);
    const baseCost = this.getModelCost(this.selectModel('standard'));
    const actualCost = this.getModelCost(model);

    const savings = ((baseCost.input - actualCost.input) / baseCost.input) * 100;

    return {
      name: complexity,
      model,
      costSavings: savings > 0 ? `${savings.toFixed(0)}% cheaper` : savings < 0 ? `${Math.abs(savings).toFixed(0)}% more expensive` : 'same cost'
    };
  }
}
