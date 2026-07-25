/**
 * Fact Store
 * Week 11: Advanced Features - Fact Database and Retrieval
 */

import * as fs from 'fs-extra';
import * as path from 'path';
import { ExtractedFact } from './fact-extractor';

export interface FactQuery {
  sessionKey?: string;
  type?: string;
  contentPattern?: string;
  minConfidence?: number;
  since?: number;
  limit?: number;
}

export interface FactStats {
  totalFacts: number;
  factsByType: { [type: string]: number };
  factsBySession: { [sessionKey: string]: number };
  averageConfidence: number;
  oldestFact: string;
  newestFact: string;
}

export class FactStore {
  private storageDir: string;
  private factsFile: string;
  private indexFile: string;
  private facts: ExtractedFact[] = [];
  private typeIndex: Map<string, string[]> = new Map(); // type -> fact IDs
  private sessionIndex: Map<string, string[]> = new Map(); // sessionKey -> fact IDs
  private loaded: boolean = false;

  constructor(storageDir: string = './storage/memory/facts') {
    this.storageDir = storageDir;
    this.factsFile = path.join(storageDir, 'facts.jsonl');
    this.indexFile = path.join(storageDir, 'index.json');

    this.ensureStorageDirectory();
  }

  private ensureStorageDirectory(): void {
    if (!fs.existsSync(this.storageDir)) {
      fs.mkdirSync(this.storageDir, { recursive: true });
    }
  }

  /**
   * Load facts from storage
   */
  async load(): Promise<void> {
    if (this.loaded) {
      return;
    }

    if (!fs.existsSync(this.factsFile)) {
      this.loaded = true;
      return;
    }

    const lines = fs.readFileSync(this.factsFile, 'utf-8')
      .split('\n')
      .filter(l => l.trim());

    this.facts = [];

    lines.forEach(line => {
      try {
        const fact: ExtractedFact = JSON.parse(line);
        this.facts.push(fact);
        this.indexFact(fact);
      } catch (err) {
        // Skip invalid lines
      }
    });

    this.loaded = true;
    console.log(`✓ Loaded ${this.facts.length} facts from storage`);
  }

  /**
   * Save a fact to storage
   */
  async saveFact(fact: ExtractedFact): Promise<void> {
    await this.load();

    // Append to JSONL file
    fs.appendFileSync(this.factsFile, JSON.stringify(fact) + '\n');

    // Add to in-memory cache
    this.facts.push(fact);
    this.indexFact(fact);
  }

  /**
   * Save multiple facts
   */
  async saveFacts(facts: ExtractedFact[]): Promise<void> {
    await this.load();

    const lines = facts.map(f => JSON.stringify(f)).join('\n') + '\n';
    fs.appendFileSync(this.factsFile, lines);

    facts.forEach(fact => {
      this.facts.push(fact);
      this.indexFact(fact);
    });

    console.log(`✓ Saved ${facts.length} facts to storage`);
  }

  /**
   * Index a fact for fast retrieval
   */
  private indexFact(fact: ExtractedFact): void {
    // Index by type
    if (!this.typeIndex.has(fact.type)) {
      this.typeIndex.set(fact.type, []);
    }
    this.typeIndex.get(fact.type)!.push(fact.id);

    // Index by session
    if (!this.sessionIndex.has(fact.sessionKey)) {
      this.sessionIndex.set(fact.sessionKey, []);
    }
    this.sessionIndex.get(fact.sessionKey)!.push(fact.id);
  }

  /**
   * Query facts
   */
  async query(query: FactQuery = {}): Promise<ExtractedFact[]> {
    await this.load();

    let results = [...this.facts];

    // Filter by session
    if (query.sessionKey) {
      results = results.filter(f => f.sessionKey === query.sessionKey);
    }

    // Filter by type
    if (query.type) {
      results = results.filter(f => f.type === query.type);
    }

    // Filter by content pattern
    if (query.contentPattern) {
      const pattern = new RegExp(query.contentPattern, 'i');
      results = results.filter(f =>
        pattern.test(f.content) || pattern.test(f.context)
      );
    }

    // Filter by confidence
    if (query.minConfidence !== undefined) {
      results = results.filter(f => f.confidence >= query.minConfidence!);
    }

    // Filter by time
    if (query.since) {
      results = results.filter(f =>
        new Date(f.timestamp).getTime() >= query.since!
      );
    }

    // Sort by confidence (descending) and timestamp (newest first)
    results.sort((a, b) => {
      if (a.confidence !== b.confidence) {
        return b.confidence - a.confidence;
      }
      return new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime();
    });

    // Apply limit
    if (query.limit) {
      results = results.slice(0, query.limit);
    }

    return results;
  }

  /**
   * Get fact by ID
   */
  async getFactById(id: string): Promise<ExtractedFact | null> {
    await this.load();
    return this.facts.find(f => f.id === id) || null;
  }

  /**
   * Get facts by type
   */
  async getFactsByType(type: string, limit?: number): Promise<ExtractedFact[]> {
    return this.query({ type, limit });
  }

  /**
   * Get facts for a session
   */
  async getFactsForSession(sessionKey: string, limit?: number): Promise<ExtractedFact[]> {
    return this.query({ sessionKey, limit });
  }

  /**
   * Search facts by content
   */
  async searchFacts(searchTerm: string, limit: number = 10): Promise<ExtractedFact[]> {
    return this.query({
      contentPattern: searchTerm,
      limit
    });
  }

  /**
   * Get recent facts
   */
  async getRecentFacts(count: number = 10): Promise<ExtractedFact[]> {
    await this.load();

    return [...this.facts]
      .sort((a, b) =>
        new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
      )
      .slice(0, count);
  }

  /**
   * Get high-confidence facts
   */
  async getHighConfidenceFacts(minConfidence: number = 0.8, limit: number = 20): Promise<ExtractedFact[]> {
    return this.query({ minConfidence, limit });
  }

  /**
   * Delete fact by ID
   */
  async deleteFact(id: string): Promise<boolean> {
    await this.load();

    const index = this.facts.findIndex(f => f.id === id);
    if (index === -1) {
      return false;
    }

    const fact = this.facts[index];

    // Remove from memory
    this.facts.splice(index, 1);

    // Update indices
    this.typeIndex.get(fact.type)?.splice(
      this.typeIndex.get(fact.type)!.indexOf(fact.id),
      1
    );
    this.sessionIndex.get(fact.sessionKey)?.splice(
      this.sessionIndex.get(fact.sessionKey)!.indexOf(fact.id),
      1
    );

    // Rewrite file
    await this.rewriteFactsFile();

    return true;
  }

  /**
   * Delete facts for a session
   */
  async deleteSessionFacts(sessionKey: string): Promise<number> {
    await this.load();

    const factsToDelete = this.facts.filter(f => f.sessionKey === sessionKey);
    const count = factsToDelete.length;

    if (count === 0) {
      return 0;
    }

    // Remove from memory
    this.facts = this.facts.filter(f => f.sessionKey !== sessionKey);

    // Update indices
    factsToDelete.forEach(fact => {
      this.typeIndex.get(fact.type)?.splice(
        this.typeIndex.get(fact.type)!.indexOf(fact.id),
        1
      );
    });
    this.sessionIndex.delete(sessionKey);

    // Rewrite file
    await this.rewriteFactsFile();

    return count;
  }

  /**
   * Clear old facts
   */
  async clearOldFacts(daysToKeep: number = 90): Promise<number> {
    await this.load();

    const cutoffTime = Date.now() - (daysToKeep * 24 * 60 * 60 * 1000);
    const factsToKeep = this.facts.filter(f =>
      new Date(f.timestamp).getTime() >= cutoffTime
    );

    const removedCount = this.facts.length - factsToKeep.length;

    if (removedCount === 0) {
      return 0;
    }

    this.facts = factsToKeep;

    // Rebuild indices
    this.rebuildIndices();

    // Rewrite file
    await this.rewriteFactsFile();

    return removedCount;
  }

  /**
   * Rewrite facts file (after deletions)
   */
  private async rewriteFactsFile(): Promise<void> {
    const tempFile = this.factsFile + '.tmp';

    this.facts.forEach(fact => {
      fs.appendFileSync(tempFile, JSON.stringify(fact) + '\n');
    });

    fs.renameSync(tempFile, this.factsFile);
  }

  /**
   * Rebuild indices
   */
  private rebuildIndices(): void {
    this.typeIndex.clear();
    this.sessionIndex.clear();

    this.facts.forEach(fact => this.indexFact(fact));
  }

  /**
   * Get statistics
   */
  async getStats(): Promise<FactStats> {
    await this.load();

    const factsByType: { [type: string]: number } = {};
    const factsBySession: { [sessionKey: string]: number } = {};
    let totalConfidence = 0;

    this.facts.forEach(fact => {
      factsByType[fact.type] = (factsByType[fact.type] || 0) + 1;
      factsBySession[fact.sessionKey] = (factsBySession[fact.sessionKey] || 0) + 1;
      totalConfidence += fact.confidence;
    });

    const timestamps = this.facts.map(f => new Date(f.timestamp).getTime());
    const oldestFact = this.facts.length > 0
      ? new Date(Math.min(...timestamps)).toISOString()
      : '';
    const newestFact = this.facts.length > 0
      ? new Date(Math.max(...timestamps)).toISOString()
      : '';

    return {
      totalFacts: this.facts.length,
      factsByType,
      factsBySession,
      averageConfidence: this.facts.length > 0
        ? totalConfidence / this.facts.length
        : 0,
      oldestFact,
      newestFact
    };
  }

  /**
   * Export facts to JSON
   */
  async exportFacts(outputPath: string): Promise<void> {
    await this.load();

    const exportData = {
      exportedAt: new Date().toISOString(),
      totalFacts: this.facts.length,
      facts: this.facts
    };

    fs.writeFileSync(outputPath, JSON.stringify(exportData, null, 2));
    console.log(`✓ Exported ${this.facts.length} facts to ${outputPath}`);
  }

  /**
   * Get all facts (for debugging)
   */
  async getAllFacts(): Promise<ExtractedFact[]> {
    await this.load();
    return [...this.facts];
  }
}
