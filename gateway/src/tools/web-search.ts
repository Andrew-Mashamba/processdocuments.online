/**
 * Web Search - Brave Search API integration
 * Week 9-10: Web Tools - Phase A
 */

import axios from 'axios';

export interface SearchResult {
  title: string;
  url: string;
  description: string;
  snippet?: string;
  favicon?: string;
  age?: string;
}

export interface SearchOptions {
  count?: number;
  offset?: number;
  safesearch?: 'off' | 'moderate' | 'strict';
  freshness?: 'pd' | 'pw' | 'pm' | 'py' | '2024' | '2023';
  country?: string;
}

export interface SearchResponse {
  query: string;
  results: SearchResult[];
  totalResults: number;
  hasMore: boolean;
}

export class BraveSearchProvider {
  private apiKey: string | null;
  private baseUrl: string = 'https://api.search.brave.com/res/v1/web/search';
  private enabled: boolean;

  constructor() {
    this.apiKey = process.env.BRAVE_API_KEY || null;
    this.enabled = this.apiKey !== null;

    if (!this.enabled) {
      console.warn('⚠️  BRAVE_API_KEY not set, web search disabled');
    } else {
      console.log('✓ Brave Search provider initialized');
    }
  }

  /**
   * Check if web search is available
   */
  isAvailable(): boolean {
    return this.enabled;
  }

  /**
   * Perform web search
   */
  async search(query: string, options: SearchOptions = {}): Promise<SearchResponse> {
    if (!this.apiKey) {
      throw new Error('Brave Search API key not configured. Set BRAVE_API_KEY environment variable.');
    }

    const count = options.count || 10;
    const offset = options.offset || 0;

    try {
      console.log(`🔍 Brave Search: "${query}" (count: ${count}, offset: ${offset})`);

      const response = await axios.get(this.baseUrl, {
        params: {
          q: query,
          count,
          offset,
          safesearch: options.safesearch || 'moderate',
          freshness: options.freshness,
          country: options.country,
          search_lang: 'en',
          text_decorations: false
        },
        headers: {
          'Accept': 'application/json',
          'Accept-Encoding': 'gzip',
          'X-Subscription-Token': this.apiKey
        },
        timeout: 10000
      });

      // Parse results
      const web = response.data.web || {};
      const results: SearchResult[] = (web.results || []).map((item: any) => ({
        title: item.title || '',
        url: item.url || '',
        description: item.description || '',
        snippet: item.extra_snippets?.join(' ') || undefined,
        favicon: item.profile?.img || undefined,
        age: item.age || undefined
      }));

      const totalResults = web.total_results || results.length;

      console.log(`✓ Found ${results.length} results (total: ${totalResults})`);

      return {
        query,
        results,
        totalResults,
        hasMore: offset + count < totalResults
      };
    } catch (error: any) {
      if (error.response) {
        const status = error.response.status;
        const message = error.response.data?.message || error.message;

        if (status === 401) {
          throw new Error('Brave Search API authentication failed. Check BRAVE_API_KEY.');
        } else if (status === 429) {
          throw new Error('Brave Search rate limit exceeded. Try again later.');
        } else {
          throw new Error(`Brave Search API error (${status}): ${message}`);
        }
      }

      throw new Error(`Brave Search failed: ${error.message}`);
    }
  }

  /**
   * Format search results as text
   */
  formatResults(response: SearchResponse): string {
    if (response.results.length === 0) {
      return `No results found for: "${response.query}"`;
    }

    let output = `Search results for: "${response.query}"\n`;
    output += `Total results: ${response.totalResults}\n\n`;

    response.results.forEach((result, i) => {
      output += `${i + 1}. ${result.title}\n`;
      output += `   URL: ${result.url}\n`;
      output += `   ${result.description}\n`;
      if (result.snippet) {
        output += `   Snippet: ${result.snippet.substring(0, 200)}...\n`;
      }
      output += '\n';
    });

    if (response.hasMore) {
      output += '(More results available)';
    }

    return output;
  }

  /**
   * Get API usage information
   */
  getUsageInfo(): {
    enabled: boolean;
    apiKeySet: boolean;
  } {
    return {
      enabled: this.enabled,
      apiKeySet: this.apiKey !== null
    };
  }
}

// Singleton instance
let searchProvider: BraveSearchProvider | null = null;

/**
 * Get global search provider instance
 */
export function getSearchProvider(): BraveSearchProvider {
  if (!searchProvider) {
    searchProvider = new BraveSearchProvider();
  }
  return searchProvider;
}
