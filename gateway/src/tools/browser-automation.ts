/**
 * Browser Automation - Playwright integration
 * Week 9-10: Web Tools - Phase A
 */

import { chromium, Browser, BrowserContext, Page } from 'playwright';
import * as fs from 'fs-extra';
import * as path from 'path';
import { v4 as uuidv4 } from 'uuid';

export interface BrowserOptions {
  headless?: boolean;
  timeout?: number;
  viewport?: { width: number; height: number };
  userAgent?: string;
}

export interface NavigateOptions {
  waitUntil?: 'load' | 'domcontentloaded' | 'networkidle';
  timeout?: number;
}

export interface ScreenshotOptions {
  fullPage?: boolean;
  type?: 'png' | 'jpeg';
  quality?: number;
  path?: string;
}

export interface EvaluateOptions {
  timeout?: number;
}

export interface TabInfo {
  id: string;
  url: string;
  title: string;
  createdAt: number;
}

export interface NavigateResult {
  success: boolean;
  url: string;
  title: string;
  statusCode?: number;
  error?: string;
}

export interface ScreenshotResult {
  success: boolean;
  path?: string;
  base64?: string;
  error?: string;
}

export interface EvaluateResult {
  success: boolean;
  result?: any;
  error?: string;
}

interface Tab {
  id: string;
  page: Page;
  url: string;
  createdAt: number;
}

export class BrowserManager {
  private browser: Browser | null = null;
  private context: BrowserContext | null = null;
  private tabs: Map<string, Tab> = new Map();
  private options: BrowserOptions;
  private screenshotsDir: string;

  constructor(options: BrowserOptions = {}) {
    this.options = {
      headless: options.headless !== false,
      timeout: options.timeout || 30000,
      viewport: options.viewport || { width: 1920, height: 1080 },
      userAgent: options.userAgent || 'Mozilla/5.0 (compatible; ZIMA-Bot/1.0)'
    };
    this.screenshotsDir = path.join(process.cwd(), 'screenshots');
  }

  /**
   * Initialize browser instance
   */
  async initialize(): Promise<void> {
    if (this.browser) {
      return;
    }

    console.log('🌐 Initializing Playwright browser...');

    this.browser = await chromium.launch({
      headless: this.options.headless,
      args: [
        '--no-sandbox',
        '--disable-setuid-sandbox',
        '--disable-dev-shm-usage',
        '--disable-accelerated-2d-canvas',
        '--no-first-run',
        '--no-zygote',
        '--disable-gpu'
      ]
    });

    this.context = await this.browser.newContext({
      viewport: this.options.viewport,
      userAgent: this.options.userAgent
    });

    // Ensure screenshots directory exists
    await fs.ensureDir(this.screenshotsDir);

    console.log('✓ Browser initialized');
  }

  /**
   * Open a new browser tab
   */
  async open(): Promise<string> {
    await this.initialize();

    if (!this.context) {
      throw new Error('Browser context not initialized');
    }

    const page = await this.context.newPage();
    page.setDefaultTimeout(this.options.timeout!);

    const tabId = uuidv4();
    const tab: Tab = {
      id: tabId,
      page,
      url: 'about:blank',
      createdAt: Date.now()
    };

    this.tabs.set(tabId, tab);

    console.log(`✓ Opened new tab: ${tabId}`);

    return tabId;
  }

  /**
   * Navigate to a URL
   */
  async navigate(
    tabId: string,
    url: string,
    options: NavigateOptions = {}
  ): Promise<NavigateResult> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      return {
        success: false,
        url,
        title: '',
        error: `Tab ${tabId} not found`
      };
    }

    try {
      console.log(`🌐 Navigating to: ${url}`);

      const response = await tab.page.goto(url, {
        waitUntil: options.waitUntil || 'load',
        timeout: options.timeout || this.options.timeout
      });

      const title = await tab.page.title();
      const finalUrl = tab.page.url();

      tab.url = finalUrl;

      console.log(`✓ Loaded: "${title}"`);

      return {
        success: true,
        url: finalUrl,
        title,
        statusCode: response?.status()
      };
    } catch (error: any) {
      console.error(`❌ Navigation failed: ${error.message}`);
      return {
        success: false,
        url,
        title: '',
        error: error.message
      };
    }
  }

  /**
   * Take a screenshot
   */
  async screenshot(
    tabId: string,
    options: ScreenshotOptions = {}
  ): Promise<ScreenshotResult> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      return {
        success: false,
        error: `Tab ${tabId} not found`
      };
    }

    try {
      const filename = options.path || `screenshot-${Date.now()}.${options.type || 'png'}`;
      const filepath = path.isAbsolute(filename)
        ? filename
        : path.join(this.screenshotsDir, filename);

      console.log(`📸 Taking screenshot: ${filename}`);

      const buffer = await tab.page.screenshot({
        path: filepath,
        fullPage: options.fullPage !== false,
        type: options.type || 'png',
        quality: options.quality
      });

      console.log(`✓ Screenshot saved: ${filepath}`);

      return {
        success: true,
        path: filepath,
        base64: buffer.toString('base64')
      };
    } catch (error: any) {
      console.error(`❌ Screenshot failed: ${error.message}`);
      return {
        success: false,
        error: error.message
      };
    }
  }

  /**
   * Evaluate JavaScript in the page context
   */
  async evaluate(
    tabId: string,
    script: string,
    options: EvaluateOptions = {}
  ): Promise<EvaluateResult> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      return {
        success: false,
        error: `Tab ${tabId} not found`
      };
    }

    try {
      console.log(`🔧 Evaluating script in tab ${tabId}`);

      // Wrap in async function to support await
      const wrappedScript = `(async () => { ${script} })()`;

      const result = await tab.page.evaluate(wrappedScript);

      console.log(`✓ Script executed successfully`);

      return {
        success: true,
        result
      };
    } catch (error: any) {
      console.error(`❌ Evaluation failed: ${error.message}`);
      return {
        success: false,
        error: error.message
      };
    }
  }

  /**
   * Click an element
   */
  async click(tabId: string, selector: string): Promise<boolean> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      throw new Error(`Tab ${tabId} not found`);
    }

    try {
      await tab.page.click(selector);
      console.log(`✓ Clicked: ${selector}`);
      return true;
    } catch (error: any) {
      console.error(`❌ Click failed: ${error.message}`);
      return false;
    }
  }

  /**
   * Fill a form field
   */
  async fill(tabId: string, selector: string, value: string): Promise<boolean> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      throw new Error(`Tab ${tabId} not found`);
    }

    try {
      await tab.page.fill(selector, value);
      console.log(`✓ Filled: ${selector}`);
      return true;
    } catch (error: any) {
      console.error(`❌ Fill failed: ${error.message}`);
      return false;
    }
  }

  /**
   * Wait for selector
   */
  async waitForSelector(
    tabId: string,
    selector: string,
    timeout?: number
  ): Promise<boolean> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      throw new Error(`Tab ${tabId} not found`);
    }

    try {
      await tab.page.waitForSelector(selector, {
        timeout: timeout || this.options.timeout
      });
      console.log(`✓ Found: ${selector}`);
      return true;
    } catch (error: any) {
      console.error(`❌ Wait failed: ${error.message}`);
      return false;
    }
  }

  /**
   * Get page content
   */
  async getContent(tabId: string): Promise<string> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      throw new Error(`Tab ${tabId} not found`);
    }

    return await tab.page.content();
  }

  /**
   * Get page HTML
   */
  async getHTML(tabId: string, selector?: string): Promise<string> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      throw new Error(`Tab ${tabId} not found`);
    }

    if (selector) {
      const element = await tab.page.$(selector);
      if (!element) {
        throw new Error(`Element not found: ${selector}`);
      }
      return await element.innerHTML();
    }

    return await tab.page.content();
  }

  /**
   * Get element text
   */
  async getText(tabId: string, selector: string): Promise<string> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      throw new Error(`Tab ${tabId} not found`);
    }

    const element = await tab.page.$(selector);
    if (!element) {
      throw new Error(`Element not found: ${selector}`);
    }

    return await element.textContent() || '';
  }

  /**
   * List all open tabs
   */
  listTabs(): TabInfo[] {
    return Array.from(this.tabs.values()).map(tab => ({
      id: tab.id,
      url: tab.url,
      title: '', // Would need async call to get current title
      createdAt: tab.createdAt
    }));
  }

  /**
   * Close a specific tab
   */
  async close(tabId: string): Promise<boolean> {
    const tab = this.tabs.get(tabId);
    if (!tab) {
      return false;
    }

    try {
      await tab.page.close();
      this.tabs.delete(tabId);
      console.log(`✓ Closed tab: ${tabId}`);
      return true;
    } catch (error: any) {
      console.error(`❌ Close failed: ${error.message}`);
      return false;
    }
  }

  /**
   * Close all tabs and browser
   */
  async closeAll(): Promise<void> {
    console.log('🔒 Closing all browser tabs...');

    // Close all tabs
    for (const [tabId, tab] of this.tabs.entries()) {
      try {
        await tab.page.close();
      } catch (error) {
        console.error(`Failed to close tab ${tabId}:`, error);
      }
    }
    this.tabs.clear();

    // Close browser
    if (this.context) {
      await this.context.close();
      this.context = null;
    }

    if (this.browser) {
      await this.browser.close();
      this.browser = null;
    }

    console.log('✓ Browser closed');
  }

  /**
   * Get browser status
   */
  getStatus(): {
    initialized: boolean;
    tabCount: number;
    headless: boolean;
  } {
    return {
      initialized: this.browser !== null,
      tabCount: this.tabs.size,
      headless: this.options.headless || true
    };
  }
}

// Global instance
let browserManager: BrowserManager | null = null;

/**
 * Get global browser manager instance
 */
export function getBrowserManager(options?: BrowserOptions): BrowserManager {
  if (!browserManager) {
    browserManager = new BrowserManager(options);
  }
  return browserManager;
}

/**
 * Cleanup on process exit
 */
process.on('SIGINT', async () => {
  if (browserManager) {
    await browserManager.closeAll();
  }
  process.exit(0);
});

process.on('SIGTERM', async () => {
  if (browserManager) {
    await browserManager.closeAll();
  }
  process.exit(0);
});
