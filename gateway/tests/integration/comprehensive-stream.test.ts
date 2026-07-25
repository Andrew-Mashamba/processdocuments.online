/**
 * Comprehensive Streaming Test
 * Tests all major system functionalities via the /stream endpoint
 */

import axios from 'axios';
import * as fs from 'fs-extra';
import * as path from 'path';

const GATEWAY_URL = process.env.GATEWAY_URL || 'http://localhost:5555';
const TIMEOUT = 60000; // 60 seconds

interface TestResult {
  name: string;
  category: string;
  passed: boolean;
  duration: number;
  response?: string;
  toolsUsed?: string[];
  model?: string;
  error?: string;
  cached?: boolean;
}

interface StreamEvent {
  type: string;
  content?: string;
  delta?: string;
  output?: string;
  tool_use?: any;
  model?: string;
  usage?: any;
  fromCache?: boolean;
  error?: string;
}

/**
 * Send streaming request and parse response
 */
async function streamRequest(
  prompt: string,
  sessionKey?: string
): Promise<{
  success: boolean;
  output: string;
  toolsUsed: string[];
  model?: string;
  cached?: boolean;
  error?: string;
}> {
  try {
    const response = await axios.post(
      `${GATEWAY_URL}/api/chat/stream`,
      {
        message: prompt,
        channel: 'webchat',
        senderId: 'comprehensive-test',
        sessionKey: sessionKey || `test-${Date.now()}`
      },
      {
        timeout: TIMEOUT,
        responseType: 'text'
      }
    );

    let fullOutput = '';
    const toolsUsed: string[] = [];
    let model: string | undefined;
    let cached = false;

    // Parse streaming response (SSE format)
    const lines = response.data.split('\n');

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        try {
          const event: StreamEvent = JSON.parse(line.substring(6));

          // Accumulate content
          if (event.type === 'content' && event.content) {
            fullOutput += event.content;
          }

          // Handle delta streaming
          if (event.type === 'delta' && event.delta) {
            fullOutput += event.delta;
          }

          // Complete event has full output
          if (event.type === 'complete' && event.output) {
            fullOutput = event.output;
            model = event.model;
            cached = event.fromCache || false;
          }

          // Track tool usage
          if (event.type === 'tool_use' && event.tool_use) {
            toolsUsed.push(event.tool_use.name || 'unknown');
          }

          // Handle errors
          if (event.type === 'error') {
            return {
              success: false,
              output: '',
              toolsUsed: [],
              error: event.error || 'Unknown error'
            };
          }
        } catch (parseError) {
          // Skip invalid JSON
        }
      }
    }

    return {
      success: true,
      output: fullOutput,
      toolsUsed,
      model,
      cached
    };
  } catch (error: any) {
    return {
      success: false,
      output: '',
      toolsUsed: [],
      error: error.message
    };
  }
}

/**
 * Run a single test
 */
async function runTest(
  name: string,
  category: string,
  prompt: string,
  sessionKey?: string,
  validator?: (result: any) => boolean
): Promise<TestResult> {
  const startTime = Date.now();

  console.log(`\n${'='.repeat(60)}`);
  console.log(`🧪 ${name}`);
  console.log(`📂 Category: ${category}`);
  console.log(`${'='.repeat(60)}`);
  console.log(`Prompt: ${prompt.substring(0, 100)}${prompt.length > 100 ? '...' : ''}`);
  console.log('');

  try {
    const result = await streamRequest(prompt, sessionKey);
    const duration = Date.now() - startTime;

    if (!result.success) {
      console.log(`❌ FAILED: ${result.error}`);
      return {
        name,
        category,
        passed: false,
        duration,
        error: result.error
      };
    }

    // Run custom validator if provided
    const validationPassed = validator ? validator(result) : true;

    console.log(`✅ SUCCESS (${duration}ms)`);
    console.log(`Model: ${result.model || 'unknown'}`);
    console.log(`Cached: ${result.cached ? 'Yes' : 'No'}`);
    console.log(`Tools Used: ${result.toolsUsed.length > 0 ? result.toolsUsed.join(', ') : 'None'}`);
    console.log(`Response Length: ${result.output.length} chars`);
    console.log(`Response Preview: ${result.output.substring(0, 200)}${result.output.length > 200 ? '...' : ''}`);

    return {
      name,
      category,
      passed: validationPassed,
      duration,
      response: result.output,
      toolsUsed: result.toolsUsed,
      model: result.model,
      cached: result.cached
    };
  } catch (error: any) {
    const duration = Date.now() - startTime;
    console.log(`❌ FAILED: ${error.message}`);
    return {
      name,
      category,
      passed: false,
      duration,
      error: error.message
    };
  }
}

/**
 * Main test suite
 */
async function runComprehensiveTests() {
  console.log('\n╔════════════════════════════════════════════════════════╗');
  console.log('║   ZIMA Gateway - Comprehensive Streaming Test Suite   ║');
  console.log('║   Testing All Major System Functionalities            ║');
  console.log('╚════════════════════════════════════════════════════════╝\n');

  console.log(`Gateway URL: ${GATEWAY_URL}`);
  console.log(`Timeout: ${TIMEOUT}ms\n`);

  // Check gateway health
  try {
    await axios.get(`${GATEWAY_URL}/health`, { timeout: 5000 });
    console.log('✅ Gateway is running\n');
  } catch (error) {
    console.log('❌ Gateway is not accessible. Please start it first.\n');
    process.exit(1);
  }

  const results: TestResult[] = [];

  // ========================================================================
  // CATEGORY 1: Task Classification & Model Selection
  // ========================================================================
  results.push(await runTest(
    'Simple Greeting',
    'Task Classification',
    'Hello! How are you today?',
    undefined,
    (r) => r.model?.includes('haiku')
  ));

  results.push(await runTest(
    'Standard Complexity Query',
    'Task Classification',
    'Explain the concept of vector embeddings in machine learning and their applications',
    undefined,
    (r) => r.model?.includes('sonnet')
  ));

  results.push(await runTest(
    'Complex Multi-Step Task',
    'Task Classification',
    'Create a comprehensive business plan outline with executive summary, market analysis, financial projections, implementation timeline, risk assessment, and competitive analysis. Make each section detailed with at least 5 sub-points.',
    undefined,
    (r) => r.model?.includes('opus') || r.model?.includes('sonnet')
  ));

  // ========================================================================
  // CATEGORY 2: Memory System (OpenClaw)
  // ========================================================================
  results.push(await runTest(
    'Memory Search - Workspace Files',
    'Memory System',
    'Search the workspace for any information about configuration or setup instructions',
    undefined,
    (r) => r.toolsUsed.includes('memory_search') || r.output.length > 0
  ));

  results.push(await runTest(
    'Memory Get - Specific File',
    'Memory System',
    'Get the contents of the README.md file from the workspace',
    undefined,
    (r) => r.toolsUsed.includes('memory_get') || r.output.toLowerCase().includes('readme')
  ));

  // ========================================================================
  // CATEGORY 3: Web Tools
  // ========================================================================
  results.push(await runTest(
    'Web Search',
    'Web Tools',
    'Search the web for "Claude AI assistant capabilities" and summarize the top 3 results',
    undefined,
    (r) => r.toolsUsed.includes('web_search') || r.output.toLowerCase().includes('claude')
  ));

  results.push(await runTest(
    'Web Fetch - Content Extraction',
    'Web Tools',
    'Fetch the content from https://example.com and tell me what the main heading says',
    undefined,
    (r) => r.toolsUsed.includes('web_fetch') || r.output.toLowerCase().includes('example')
  ));

  results.push(await runTest(
    'Browser Automation',
    'Web Tools',
    'Open a browser, navigate to https://example.com, and extract the page title',
    undefined,
    (r) => r.toolsUsed.includes('browser') || r.output.toLowerCase().includes('example')
  ));

  // ========================================================================
  // CATEGORY 4: File Operations
  // ========================================================================
  const testFilePath = path.join(process.cwd(), 'storage', 'test-stream-output.txt');

  results.push(await runTest(
    'File Write Operation',
    'File Operations',
    `Create a text file at "${testFilePath}" with the content "Hello from comprehensive streaming test at ${new Date().toISOString()}"`,
    undefined,
    (r) => r.toolsUsed.includes('write') || r.output.toLowerCase().includes('created')
  ));

  results.push(await runTest(
    'File Read Operation',
    'File Operations',
    `Read the file at "${testFilePath}" and tell me what it contains`,
    undefined,
    (r) => r.toolsUsed.includes('read') || r.output.toLowerCase().includes('hello')
  ));

  // ========================================================================
  // CATEGORY 5: Command Execution
  // ========================================================================
  results.push(await runTest(
    'Safe Command Execution',
    'Exec Tools',
    'Execute the command "echo Hello from ZIMA Gateway" and show me the output',
    undefined,
    (r) => r.toolsUsed.includes('exec') || r.output.toLowerCase().includes('hello')
  ));

  results.push(await runTest(
    'Security - Dangerous Command Block',
    'Exec Tools',
    'Execute the command "rm -rf /" and see what happens',
    undefined,
    (r) => r.output.toLowerCase().includes('blocked') || r.output.toLowerCase().includes('not allowed') || r.output.toLowerCase().includes('security')
  ));

  results.push(await runTest(
    'Background Process Spawn',
    'Exec Tools',
    'Spawn a background process that runs "sleep 2 && echo Process completed"',
    undefined,
    (r) => r.toolsUsed.includes('process') || r.output.toLowerCase().includes('spawned') || r.output.toLowerCase().includes('background')
  ));

  // ========================================================================
  // CATEGORY 6: Document Generation (ZIMA Tools)
  // ========================================================================
  results.push(await runTest(
    'Excel Generation',
    'ZIMA Tools',
    'Create an Excel file called "test-products-stream.xlsx" with 5 sample products including columns: Product Name, Price, Quantity, Category',
    undefined,
    (r) => r.toolsUsed.some(t => t.toLowerCase().includes('excel')) || r.output.toLowerCase().includes('excel')
  ));

  results.push(await runTest(
    'PDF Generation',
    'ZIMA Tools',
    'Create a PDF document called "test-report-stream.pdf" with title "Streaming Test Report" and some content about AI assistants',
    undefined,
    (r) => r.toolsUsed.some(t => t.toLowerCase().includes('pdf')) || r.output.toLowerCase().includes('pdf')
  ));

  // ========================================================================
  // CATEGORY 7: Multi-Turn Conversation (Context Management)
  // ========================================================================
  const multiTurnSession = `test-multiturn-${Date.now()}`;

  results.push(await runTest(
    'Multi-Turn: Setup Context',
    'Context Management',
    'My name is Alice, I work at TechCorp as a software engineer, and my favorite programming language is TypeScript. Remember all of this.',
    multiTurnSession
  ));

  results.push(await runTest(
    'Multi-Turn: Recall Context',
    'Context Management',
    'What is my name, where do I work, and what is my favorite programming language?',
    multiTurnSession,
    (r) => r.output.toLowerCase().includes('alice') && r.output.toLowerCase().includes('techcorp') && r.output.toLowerCase().includes('typescript')
  ));

  results.push(await runTest(
    'Multi-Turn: Extended Context',
    'Context Management',
    'Based on what you know about me, what kind of projects do you think I might be working on?',
    multiTurnSession
  ));

  // ========================================================================
  // CATEGORY 8: Response Caching
  // ========================================================================
  const cacheSession = `test-cache-${Date.now()}`;
  const cachePrompt = 'What is the capital of France?';

  results.push(await runTest(
    'Cache Test: First Request',
    'Response Caching',
    cachePrompt,
    cacheSession,
    (r) => !r.cached
  ));

  // Wait a moment
  await new Promise(resolve => setTimeout(resolve, 1000));

  results.push(await runTest(
    'Cache Test: Second Request (Should Cache)',
    'Response Caching',
    cachePrompt,
    cacheSession,
    (r) => r.cached === true
  ));

  // ========================================================================
  // CATEGORY 9: Tool Chaining
  // ========================================================================
  results.push(await runTest(
    'Tool Chaining: Search + Fetch',
    'Tool Chaining',
    'Search for "Anthropic Claude" on the web, then fetch the content of the first result and give me a brief summary',
    undefined,
    (r) => r.toolsUsed.includes('web_search') && r.toolsUsed.includes('web_fetch')
  ));

  // ========================================================================
  // CATEGORY 10: Session Management
  // ========================================================================
  results.push(await runTest(
    'Session List',
    'Session Management',
    'List all my active sessions',
    undefined,
    (r) => r.toolsUsed.includes('sessions_list') || r.output.toLowerCase().includes('session')
  ));

  // ========================================================================
  // Generate Report
  // ========================================================================
  console.log('\n╔════════════════════════════════════════════════════════╗');
  console.log('║                    TEST SUMMARY                        ║');
  console.log('╚════════════════════════════════════════════════════════╝\n');

  const passed = results.filter(r => r.passed).length;
  const failed = results.filter(r => !r.passed).length;
  const total = results.length;
  const successRate = ((passed / total) * 100).toFixed(1);

  console.log(`Total Tests: ${total}`);
  console.log(`✅ Passed: ${passed}`);
  console.log(`❌ Failed: ${failed}`);
  console.log(`Success Rate: ${successRate}%\n`);

  // Group by category
  const categories = [...new Set(results.map(r => r.category))];

  console.log('Results by Category:\n');
  categories.forEach(category => {
    const categoryResults = results.filter(r => r.category === category);
    const categoryPassed = categoryResults.filter(r => r.passed).length;
    const categoryTotal = categoryResults.length;
    console.log(`  ${category}: ${categoryPassed}/${categoryTotal}`);
  });

  console.log('\nDetailed Results:\n');
  results.forEach((result, index) => {
    const status = result.passed ? '✅' : '❌';
    const cached = result.cached ? '💾' : '';
    console.log(`  ${index + 1}. ${status} ${cached} ${result.name} (${result.duration}ms)`);
    if (result.toolsUsed && result.toolsUsed.length > 0) {
      console.log(`     Tools: ${result.toolsUsed.join(', ')}`);
    }
    if (result.error) {
      console.log(`     Error: ${result.error}`);
    }
  });

  // Save report
  const reportPath = path.join(__dirname, '../../COMPREHENSIVE_STREAM_TEST_REPORT.md');
  const report = generateMarkdownReport(results, passed, failed);
  await fs.writeFile(reportPath, report);
  console.log(`\n📄 Detailed report saved to: ${reportPath}\n`);

  return {
    results,
    passed,
    failed,
    total,
    successRate: parseFloat(successRate)
  };
}

/**
 * Generate markdown report
 */
function generateMarkdownReport(results: TestResult[], passed: number, failed: number): string {
  const timestamp = new Date().toISOString();
  const total = results.length;
  const successRate = ((passed / total) * 100).toFixed(1);

  let md = `# ZIMA Gateway - Comprehensive Streaming Test Report\n\n`;
  md += `**Generated:** ${timestamp}\n`;
  md += `**Status:** ${failed === 0 ? '✅ ALL PASSED' : '⚠️ SOME FAILED'}\n\n`;

  md += `## Summary\n\n`;
  md += `- **Total Tests:** ${total}\n`;
  md += `- **Passed:** ${passed} ✅\n`;
  md += `- **Failed:** ${failed} ❌\n`;
  md += `- **Success Rate:** ${successRate}%\n\n`;

  md += `## Test Categories\n\n`;
  const categories = [...new Set(results.map(r => r.category))];
  categories.forEach(category => {
    const categoryResults = results.filter(r => r.category === category);
    const categoryPassed = categoryResults.filter(r => r.passed).length;
    const categoryTotal = categoryResults.length;
    const categoryRate = ((categoryPassed / categoryTotal) * 100).toFixed(1);

    md += `### ${category}\n\n`;
    md += `**Success Rate:** ${categoryRate}% (${categoryPassed}/${categoryTotal})\n\n`;

    md += `| Test | Status | Duration | Model | Tools | Cached |\n`;
    md += `|------|--------|----------|-------|-------|--------|\n`;

    categoryResults.forEach(result => {
      const status = result.passed ? '✅' : '❌';
      const tools = result.toolsUsed?.join(', ') || 'None';
      const cached = result.cached ? '💾 Yes' : 'No';
      md += `| ${result.name} | ${status} | ${result.duration}ms | ${result.model || 'N/A'} | ${tools} | ${cached} |\n`;
    });

    md += `\n`;
  });

  md += `## Failed Tests\n\n`;
  const failedTests = results.filter(r => !r.passed);
  if (failedTests.length === 0) {
    md += `✅ No failed tests!\n\n`;
  } else {
    failedTests.forEach(result => {
      md += `### ❌ ${result.name}\n\n`;
      md += `- **Category:** ${result.category}\n`;
      md += `- **Duration:** ${result.duration}ms\n`;
      md += `- **Error:** ${result.error || 'Validation failed'}\n\n`;
    });
  }

  md += `---\n\n`;
  md += `*Generated by ZIMA Gateway Comprehensive Test Suite*\n`;

  return md;
}

// Run tests
if (require.main === module) {
  runComprehensiveTests()
    .then((summary) => {
      process.exit(summary.failed === 0 ? 0 : 1);
    })
    .catch((error) => {
      console.error('Test suite crashed:', error);
      process.exit(1);
    });
}

export { runComprehensiveTests };
