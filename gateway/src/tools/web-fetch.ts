/**
 * Web Fetch - Content extraction with Readability
 * Week 9-10: Web Tools - Phase A
 */

import axios, { AxiosError } from 'axios';
import { JSDOM } from 'jsdom';
import { Readability } from '@mozilla/readability';
import * as cheerio from 'cheerio';

export interface FetchOptions {
  timeout?: number;
  maxRetries?: number;
  userAgent?: string;
  followRedirects?: boolean;
  extractMode?: 'readability' | 'raw' | 'text';
}

export interface FetchResult {
  url: string;
  title: string;
  content: string;
  textContent?: string;
  excerpt?: string;
  byline?: string;
  length?: number;
  siteName?: string;
  publishedTime?: string;
  success: boolean;
  error?: string;
}

export class WebFetcher {
  private defaultTimeout: number = 30000; // 30 seconds
  private defaultRetries: number = 3;
  private defaultUserAgent: string = 'Mozilla/5.0 (compatible; ZIMA-Bot/1.0)';

  /**
   * Fetch and extract content from a URL
   */
  async fetch(url: string, options: FetchOptions = {}): Promise<FetchResult> {
    const timeout = options.timeout || this.defaultTimeout;
    const maxRetries = options.maxRetries || this.defaultRetries;
    const userAgent = options.userAgent || this.defaultUserAgent;
    const extractMode = options.extractMode || 'readability';

    let lastError: Error | null = null;

    // Retry logic with exponential backoff
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        console.log(`🌐 Fetching: ${url} (attempt ${attempt}/${maxRetries})`);

        // Fetch HTML content
        const response = await axios.get(url, {
          timeout,
          headers: {
            'User-Agent': userAgent,
            'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
            'Accept-Language': 'en-US,en;q=0.9',
            'Accept-Encoding': 'gzip, deflate',
          },
          maxRedirects: options.followRedirects !== false ? 5 : 0,
          validateStatus: (status) => status >= 200 && status < 400,
        });

        const html = response.data;
        const finalUrl = response.request?.res?.responseUrl || url;

        // Extract content based on mode
        if (extractMode === 'readability') {
          return this.extractWithReadability(html, finalUrl);
        } else if (extractMode === 'text') {
          return this.extractText(html, finalUrl);
        } else {
          return this.extractRaw(html, finalUrl);
        }

      } catch (error: any) {
        lastError = error;

        // Don't retry on client errors (4xx)
        if (error.response && error.response.status >= 400 && error.response.status < 500) {
          console.error(`❌ Client error (${error.response.status}): ${url}`);
          break;
        }

        // Wait before retry (exponential backoff)
        if (attempt < maxRetries) {
          const delay = Math.min(1000 * Math.pow(2, attempt - 1), 10000);
          console.log(`   Retrying in ${delay}ms...`);
          await new Promise(resolve => setTimeout(resolve, delay));
        }
      }
    }

    // All retries failed
    return {
      url,
      title: '',
      content: '',
      success: false,
      error: this.formatError(lastError)
    };
  }

  /**
   * Extract content using Readability
   */
  private extractWithReadability(html: string, url: string): FetchResult {
    try {
      // Parse HTML with JSDOM
      const dom = new JSDOM(html, { url });
      const document = dom.window.document;

      // Use Readability to extract clean content
      const reader = new Readability(document);
      const article = reader.parse();

      if (!article) {
        throw new Error('Readability failed to extract article content');
      }

      // Convert HTML to markdown-like format
      const content = this.htmlToMarkdown(article.content);

      console.log(`✓ Extracted: "${article.title}" (${article.length} chars)`);

      return {
        url,
        title: article.title || '',
        content,
        textContent: article.textContent,
        excerpt: article.excerpt,
        byline: article.byline || undefined,
        length: article.length,
        siteName: article.siteName || undefined,
        success: true
      };
    } catch (error: any) {
      throw new Error(`Readability extraction failed: ${error.message}`);
    }
  }

  /**
   * Extract plain text content
   */
  private extractText(html: string, url: string): FetchResult {
    try {
      const $ = cheerio.load(html);

      // Remove script, style, and navigation elements
      $('script, style, nav, header, footer, aside').remove();

      const title = $('title').text().trim() || $('h1').first().text().trim() || '';
      const textContent = $('body').text().trim();

      // Clean up whitespace
      const content = textContent
        .replace(/\n\s*\n\s*\n/g, '\n\n')
        .replace(/[ \t]+/g, ' ')
        .trim();

      console.log(`✓ Extracted text: "${title}" (${content.length} chars)`);

      return {
        url,
        title,
        content,
        textContent: content,
        success: true
      };
    } catch (error: any) {
      throw new Error(`Text extraction failed: ${error.message}`);
    }
  }

  /**
   * Extract raw HTML
   */
  private extractRaw(html: string, url: string): FetchResult {
    try {
      const $ = cheerio.load(html);
      const title = $('title').text().trim() || '';

      console.log(`✓ Extracted raw HTML: "${title}" (${html.length} chars)`);

      return {
        url,
        title,
        content: html,
        success: true
      };
    } catch (error: any) {
      throw new Error(`Raw extraction failed: ${error.message}`);
    }
  }

  /**
   * Convert HTML to simplified markdown
   */
  private htmlToMarkdown(html: string): string {
    const $ = cheerio.load(html);

    // Convert headings
    $('h1').replaceWith((i, el) => `\n\n# ${$(el).text()}\n\n`);
    $('h2').replaceWith((i, el) => `\n\n## ${$(el).text()}\n\n`);
    $('h3').replaceWith((i, el) => `\n\n### ${$(el).text()}\n\n`);
    $('h4').replaceWith((i, el) => `\n\n#### ${$(el).text()}\n\n`);

    // Convert links
    $('a').replaceWith((i, el) => {
      const text = $(el).text();
      const href = $(el).attr('href');
      return href ? `[${text}](${href})` : text;
    });

    // Convert lists
    $('ul li').replaceWith((i, el) => `\n- ${$(el).text()}`);
    $('ol li').replaceWith((i, el) => `\n${i + 1}. ${$(el).text()}`);

    // Convert emphasis
    $('strong, b').replaceWith((i, el) => `**${$(el).text()}**`);
    $('em, i').replaceWith((i, el) => `*${$(el).text()}*`);

    // Convert code
    $('code').replaceWith((i, el) => `\`${$(el).text()}\``);
    $('pre').replaceWith((i, el) => `\n\`\`\`\n${$(el).text()}\n\`\`\`\n`);

    // Convert blockquotes
    $('blockquote').replaceWith((i, el) => {
      const text = $(el).text().split('\n').map(line => `> ${line}`).join('\n');
      return `\n${text}\n`;
    });

    // Convert paragraphs
    $('p').replaceWith((i, el) => `\n\n${$(el).text()}\n\n`);

    // Get text and clean up whitespace
    let markdown = $('body').text();
    markdown = markdown
      .replace(/\n\s*\n\s*\n/g, '\n\n')
      .replace(/[ \t]+/g, ' ')
      .trim();

    return markdown;
  }

  /**
   * Format error message
   */
  private formatError(error: any): string {
    if (!error) {
      return 'Unknown error';
    }

    if (error.code === 'ECONNABORTED') {
      return 'Request timeout';
    } else if (error.code === 'ENOTFOUND') {
      return 'Domain not found';
    } else if (error.code === 'ECONNREFUSED') {
      return 'Connection refused';
    } else if (error.response) {
      const status = error.response.status;
      const statusText = error.response.statusText || '';
      return `HTTP ${status} ${statusText}`.trim();
    } else {
      return error.message || 'Fetch failed';
    }
  }

  /**
   * Batch fetch multiple URLs
   */
  async fetchBatch(
    urls: string[],
    options: FetchOptions = {}
  ): Promise<FetchResult[]> {
    console.log(`🌐 Batch fetching ${urls.length} URLs...`);

    const results = await Promise.allSettled(
      urls.map(url => this.fetch(url, options))
    );

    return results.map((result, i) => {
      if (result.status === 'fulfilled') {
        return result.value;
      } else {
        return {
          url: urls[i],
          title: '',
          content: '',
          success: false,
          error: result.reason?.message || 'Batch fetch failed'
        };
      }
    });
  }

  /**
   * Extract metadata from HTML
   */
  async extractMetadata(url: string): Promise<{
    title?: string;
    description?: string;
    image?: string;
    siteName?: string;
    author?: string;
    publishedTime?: string;
    modifiedTime?: string;
  }> {
    try {
      const response = await axios.get(url, {
        timeout: 10000,
        headers: { 'User-Agent': this.defaultUserAgent }
      });

      const $ = cheerio.load(response.data);

      return {
        title: $('meta[property="og:title"]').attr('content') || $('title').text(),
        description: $('meta[property="og:description"]').attr('content') || $('meta[name="description"]').attr('content'),
        image: $('meta[property="og:image"]').attr('content'),
        siteName: $('meta[property="og:site_name"]').attr('content'),
        author: $('meta[name="author"]').attr('content'),
        publishedTime: $('meta[property="article:published_time"]').attr('content'),
        modifiedTime: $('meta[property="article:modified_time"]').attr('content')
      };
    } catch (error: any) {
      throw new Error(`Metadata extraction failed: ${error.message}`);
    }
  }
}

// Singleton instance
let fetcher: WebFetcher | null = null;

/**
 * Get global web fetcher instance
 */
export function getWebFetcher(): WebFetcher {
  if (!fetcher) {
    fetcher = new WebFetcher();
  }
  return fetcher;
}
