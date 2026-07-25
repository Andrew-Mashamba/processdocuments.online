/**
 * Integration Test: /stream Endpoint
 * Tests the actual streaming endpoint with various prompts
 */

import axios from 'axios';
import { spawn, ChildProcess } from 'child_process';
import * as path from 'path';

const GATEWAY_URL = 'http://localhost:18790';
const SERVER_STARTUP_DELAY = 5000; // Wait 5s for server to start

interface StreamResponse {
  type: string;
  message?: any;
  content?: any;
  delta?: any;
  error?: string;
}

async function waitForServer(maxAttempts = 10): Promise<boolean> {
  for (let i = 0; i < maxAttempts; i++) {
    try {
      await axios.get(`${GATEWAY_URL}/health`, { timeout: 1000 });
      return true;
    } catch (error) {
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
  }
  return false;
}

async function streamRequest(prompt: string, sessionKey?: string): Promise<{
  success: boolean;
  response: string;
  toolCalls: any[];
  error?: string;
}> {
  try {
    const response = await axios.post(
      `${GATEWAY_URL}/api/chat/stream`,
      {
        prompt,
        session_key: sessionKey || `test-session-${Date.now()}`,
        model: 'claude-3-5-haiku-20241022', // Use Haiku for faster/cheaper tests
        max_tokens: 1024
      },
      {
        timeout: 30000,
        responseType: 'text'
      }
    );

    // Parse Server-Sent Events (SSE) format
    const lines = response.data.split('\n').filter((line: string) => line.trim());
    let fullResponse = '';
    const toolCalls: any[] = [];

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];

      // SSE format: "event: content" followed by "data: {...}"
      if (line.startsWith('data: ')) {
        try {
          const jsonData = line.substring(6); // Remove "data: " prefix
          const event = JSON.parse(jsonData);

          // Handle content events
          if (event.type === 'content' && event.content) {
            fullResponse += event.content;
          }

          // Handle complete event with full output
          if (event.type === 'complete' && event.output) {
            fullResponse = event.output; // Use complete output
          }

          // Handle tool use events
          if (event.type === 'tool_use') {
            toolCalls.push(event);
          }

          // Handle error events
          if (event.type === 'error') {
            return {
              success: false,
              response: '',
              toolCalls: [],
              error: event.error
            };
          }
        } catch (parseError) {
          // Skip invalid JSON lines
        }
      }
    }

    return {
      success: true,
      response: fullResponse,
      toolCalls
    };
  } catch (error: any) {
    return {
      success: false,
      response: '',
      toolCalls: [],
      error: error.message
    };
  }
}

async function testStreamEndpoint() {
  console.log('========================================');
  console.log('Integration Test: /stream Endpoint');
  console.log('========================================\n');

  let passed = 0;
  let failed = 0;
  let serverProcess: ChildProcess | null = null;

  try {
    // Start the gateway server
    console.log('Starting ZIMA Gateway server...');
    serverProcess = spawn('npm', ['start'], {
      cwd: path.join(__dirname, '../..'),
      stdio: 'pipe',
      env: {
        ...process.env,
        ANTHROPIC_API_KEY: process.env.ANTHROPIC_API_KEY,
        NODE_ENV: 'test'
      }
    });

    serverProcess.stdout?.on('data', (data) => {
      const output = data.toString();
      if (output.includes('error') || output.includes('Error')) {
        console.log('  [server]:', output.trim());
      }
    });

    serverProcess.stderr?.on('data', (data) => {
      console.log('  [server error]:', data.toString().trim());
    });

    console.log('  Waiting for server to start...');
    await new Promise(resolve => setTimeout(resolve, SERVER_STARTUP_DELAY));

    const serverReady = await waitForServer();
    if (!serverReady) {
      console.log('✗ Server failed to start');
      failed++;
      return { passed, failed };
    }
    console.log('✓ Server started successfully\n');

    // Test 1: Simple prompt
    try {
      console.log('Test 1: Simple greeting...');
      const result = await streamRequest('Hello! Just say hi back in one sentence.');

      if (result.success && result.response.length > 0) {
        console.log(`✓ Got response (${result.response.length} chars)`);
        console.log(`  Response: "${result.response.substring(0, 100)}..."`);
        passed++;
      } else {
        console.log(`✗ No response: ${result.error}`);
        failed++;
      }
    } catch (error: any) {
      console.log(`✗ Test failed: ${error.message}`);
      failed++;
    }

    // Test 2: Memory search query
    try {
      console.log('\nTest 2: Memory search...');
      const result = await streamRequest('Search the workspace for "gateway" and tell me what you find.');

      if (result.success) {
        const usedMemorySearch = result.toolCalls.some((tc: any) => tc.name === 'memory_search');
        if (usedMemorySearch) {
          console.log(`✓ Memory search tool used`);
          console.log(`  Response length: ${result.response.length} chars`);
          passed++;
        } else {
          console.log(`✗ Memory search tool not used`);
          console.log(`  Tools called: ${result.toolCalls.map((tc: any) => tc.name).join(', ')}`);
          failed++;
        }
      } else {
        console.log(`✗ Request failed: ${result.error}`);
        failed++;
      }
    } catch (error: any) {
      console.log(`✗ Test failed: ${error.message}`);
      failed++;
    }

    // Test 3: Web fetch
    try {
      console.log('\nTest 3: Web fetch...');
      const result = await streamRequest('Fetch the content from https://example.com and summarize it.');

      if (result.success) {
        const usedWebFetch = result.toolCalls.some((tc: any) => tc.name === 'web_fetch');
        if (usedWebFetch) {
          console.log(`✓ Web fetch tool used`);
          console.log(`  Response includes content: ${result.response.includes('Example') || result.response.includes('domain')}`);
          passed++;
        } else {
          console.log(`✗ Web fetch tool not used`);
          console.log(`  Tools called: ${result.toolCalls.map((tc: any) => tc.name).join(', ')}`);
          failed++;
        }
      } else {
        console.log(`✗ Request failed: ${result.error}`);
        failed++;
      }
    } catch (error: any) {
      console.log(`✗ Test failed: ${error.message}`);
      failed++;
    }

    // Test 4: File operations
    try {
      console.log('\nTest 4: File operations...');
      const result = await streamRequest('List the files in the workspace directory.');

      if (result.success) {
        const usedFileOps = result.toolCalls.some((tc: any) =>
          tc.name === 'file_list' || tc.name === 'memory_search'
        );
        if (usedFileOps || result.response.length > 0) {
          console.log(`✓ File operation handled`);
          console.log(`  Tools used: ${result.toolCalls.length}`);
          passed++;
        } else {
          console.log(`✗ No file operations performed`);
          failed++;
        }
      } else {
        console.log(`✗ Request failed: ${result.error}`);
        failed++;
      }
    } catch (error: any) {
      console.log(`✗ Test failed: ${error.message}`);
      failed++;
    }

    // Test 5: Exec command (safe)
    try {
      console.log('\nTest 5: Execute safe command...');
      const result = await streamRequest('Run the command "echo Hello from ZIMA" and show me the output.');

      if (result.success) {
        const usedExec = result.toolCalls.some((tc: any) => tc.name === 'exec');
        if (usedExec) {
          console.log(`✓ Exec tool used`);
          console.log(`  Response contains output: ${result.response.includes('Hello') || result.response.includes('ZIMA')}`);
          passed++;
        } else {
          console.log(`✗ Exec tool not used`);
          console.log(`  Tools called: ${result.toolCalls.map((tc: any) => tc.name).join(', ')}`);
          failed++;
        }
      } else {
        console.log(`✗ Request failed: ${result.error}`);
        failed++;
      }
    } catch (error: any) {
      console.log(`✗ Test failed: ${error.message}`);
      failed++;
    }

    // Test 6: Multi-turn conversation
    try {
      console.log('\nTest 6: Multi-turn conversation...');
      const sessionKey = `multi-turn-${Date.now()}`;

      const result1 = await streamRequest('My name is Alice.', sessionKey);
      await new Promise(resolve => setTimeout(resolve, 500)); // Small delay
      const result2 = await streamRequest('What is my name?', sessionKey);

      if (result1.success && result2.success) {
        const remembersName = result2.response.toLowerCase().includes('alice');
        if (remembersName) {
          console.log(`✓ Multi-turn conversation working`);
          console.log(`  Remembered name: Alice`);
          passed++;
        } else {
          console.log(`✗ Did not remember name`);
          console.log(`  Response: "${result2.response.substring(0, 100)}..."`);
          failed++;
        }
      } else {
        console.log(`✗ Conversation failed`);
        failed++;
      }
    } catch (error: any) {
      console.log(`✗ Test failed: ${error.message}`);
      failed++;
    }

    // Test 7: Complex reasoning
    try {
      console.log('\nTest 7: Complex reasoning...');
      const result = await streamRequest(
        'Search the workspace for configuration files and tell me what features are enabled.'
      );

      if (result.success) {
        const usedTools = result.toolCalls.length > 0;
        const hasResponse = result.response.length > 50;

        if (usedTools && hasResponse) {
          console.log(`✓ Complex reasoning working`);
          console.log(`  Tools used: ${result.toolCalls.length}`);
          console.log(`  Response length: ${result.response.length} chars`);
          passed++;
        } else {
          console.log(`✗ Incomplete reasoning`);
          console.log(`  Tools: ${usedTools}, Response: ${hasResponse}`);
          failed++;
        }
      } else {
        console.log(`✗ Request failed: ${result.error}`);
        failed++;
      }
    } catch (error: any) {
      console.log(`✗ Test failed: ${error.message}`);
      failed++;
    }

  } finally {
    // Cleanup: Kill server
    if (serverProcess) {
      console.log('\n🔪 Stopping server...');
      serverProcess.kill('SIGTERM');
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
  }

  // Results
  console.log('\n========================================');
  console.log(`Stream Endpoint Tests: ${passed} passed, ${failed} failed`);
  console.log('========================================\n');

  return { passed, failed };
}

// Run if executed directly
if (require.main === module) {
  testStreamEndpoint()
    .then(({ passed, failed }) => {
      process.exit(failed > 0 ? 1 : 0);
    })
    .catch((error) => {
      console.error('Test suite failed:', error);
      process.exit(1);
    });
}

export { testStreamEndpoint };
