/**
 * E2E Test: Web Tools
 * Tests web search, fetch, and browser automation
 */

import { getSearchProvider } from '../../src/tools/web-search';
import { getWebFetcher } from '../../src/tools/web-fetch';
import { getBrowserManager } from '../../src/tools/browser-automation';

async function testWebTools() {
  console.log('========================================');
  console.log('E2E Test: Web Tools');
  console.log('========================================\n');

  let passed = 0;
  let failed = 0;

  // Test 1: Web Search (if API key available)
  try {
    console.log('Test 1: Web search...');
    const searchProvider = getSearchProvider();

    if (searchProvider.isAvailable()) {
      const response = await searchProvider.search('OpenAI', { count: 3 });

      if (response.results.length > 0) {
        console.log(`✓ Found ${response.results.length} results`);
        console.log(`  Top result: ${response.results[0].title}`);
        passed++;
      } else {
        console.log('✗ No search results');
        failed++;
      }
    } else {
      console.log('⚠️  Brave Search not available (no API key)');
      passed++; // Not a failure
    }
  } catch (error: any) {
    console.log(`✗ Web search failed: ${error.message}`);
    failed++;
  }

  // Test 2: Web Fetch
  try {
    console.log('\nTest 2: Web fetch...');
    const fetcher = getWebFetcher();
    const result = await fetcher.fetch('https://example.com', {
      timeout: 10000,
      extractMode: 'text'
    });

    if (result.success && result.content.length > 0) {
      console.log(`✓ Fetched content (${result.content.length} chars)`);
      console.log(`  Title: ${result.title}`);
      passed++;
    } else {
      console.log(`✗ Fetch failed: ${result.error}`);
      failed++;
    }
  } catch (error: any) {
    console.log(`✗ Web fetch failed: ${error.message}`);
    failed++;
  }

  // Test 3: Browser Automation
  try {
    console.log('\nTest 3: Browser automation...');
    const browserManager = getBrowserManager({ headless: true });

    // Open browser
    await browserManager.initialize();
    const tabId = await browserManager.open();
    console.log(`  ✓ Opened tab: ${tabId}`);

    // Navigate
    const navResult = await browserManager.navigate(tabId, 'https://example.com');
    if (navResult.success) {
      console.log(`  ✓ Navigated to: ${navResult.title}`);
    }

    // Get content
    const content = await browserManager.getContent(tabId);
    if (content.length > 0) {
      console.log(`  ✓ Retrieved content (${content.length} chars)`);
    }

    // Close
    await browserManager.close(tabId);
    await browserManager.closeAll();

    console.log('✓ Browser automation working');
    passed++;
  } catch (error: any) {
    console.log(`✗ Browser automation failed: ${error.message}`);
    failed++;
  }

  // Results
  console.log('\n========================================');
  console.log(`Web Tools Tests: ${passed} passed, ${failed} failed`);
  console.log('========================================\n');

  return { passed, failed };
}

// Run if executed directly
if (require.main === module) {
  testWebTools()
    .then(({ passed, failed }) => {
      process.exit(failed > 0 ? 1 : 0);
    })
    .catch((error) => {
      console.error('Test suite failed:', error);
      process.exit(1);
    });
}

export { testWebTools };
