/**
 * Gateway Test Script
 * Tests HTTP and WebSocket connectivity
 */

const http = require('http');
const WebSocket = require('ws');

const GATEWAY_URL = 'http://localhost:18790';
const GATEWAY_WS = 'ws://localhost:18790';

console.log('🧪 ZIMA Gateway Test Suite\n');

// Test 1: Health Check
async function testHealthCheck() {
  console.log('Test 1: Health Check');
  return new Promise((resolve, reject) => {
    http.get(`${GATEWAY_URL}/health`, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        const result = JSON.parse(data);
        if (result.status === 'healthy') {
          console.log('  ✓ Gateway is healthy');
          console.log('  Version:', result.version);
          console.log('  Uptime:', result.uptime.toFixed(2), 'seconds\n');
          resolve();
        } else {
          reject(new Error('Health check failed'));
        }
      });
    }).on('error', reject);
  });
}

// Test 2: HTTP Chat API
async function testHttpChat() {
  console.log('Test 2: HTTP Chat API');
  return new Promise((resolve, reject) => {
    const postData = JSON.stringify({
      message: 'Test message from HTTP API',
      channel: 'webchat',
      senderId: 'test-user-123'
    });

    const options = {
      hostname: 'localhost',
      port: 18789,
      path: '/api/chat',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(postData)
      }
    };

    const req = http.request(options, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        const result = JSON.parse(data);
        console.log('  ✓ Chat message processed');
        console.log('  Session Key:', result.output.match(/Session: (.*)/)?.[1] || 'N/A');
        console.log('  Model:', result.model);
        console.log('  From Cache:', result.fromCache || false);
        console.log('\n');
        resolve();
      });
    });

    req.on('error', reject);
    req.write(postData);
    req.end();
  });
}

// Test 3: WebSocket Connection
async function testWebSocket() {
  console.log('Test 3: WebSocket Connection');
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(GATEWAY_WS);
    let received = false;

    ws.on('open', () => {
      console.log('  ✓ WebSocket connected');

      // Send chat message
      ws.send(JSON.stringify({
        type: 'chat',
        payload: {
          message: 'Test message from WebSocket',
          channel: 'webchat',
          senderId: 'ws-test-user'
        }
      }));
    });

    ws.on('message', (data) => {
      const msg = JSON.parse(data.toString());
      console.log('  ✓ Received:', msg.type);

      if (msg.type === 'chat:complete') {
        received = true;
        console.log('  ✓ Chat response received');
        console.log('\n');
        ws.close();
        resolve();
      }
    });

    ws.on('error', (error) => {
      console.log('  ✗ WebSocket error:', error.message);
      reject(error);
    });

    ws.on('close', () => {
      if (!received) {
        reject(new Error('WebSocket closed before receiving response'));
      }
    });

    // Timeout after 10 seconds
    setTimeout(() => {
      if (!received) {
        ws.close();
        reject(new Error('WebSocket test timeout'));
      }
    }, 10000);
  });
}

// Test 4: Stats Endpoint
async function testStats() {
  console.log('Test 4: Stats Endpoint');
  return new Promise((resolve, reject) => {
    http.get(`${GATEWAY_URL}/api/stats`, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        const result = JSON.parse(data);
        console.log('  ✓ Stats retrieved');
        console.log('  Connected Clients:', result.clients);
        console.log('  Cache Size:', result.cache.size);
        console.log('  Memory Usage:', (result.memory.heapUsed / 1024 / 1024).toFixed(2), 'MB\n');
        resolve();
      });
    }).on('error', reject);
  });
}

// Run all tests
async function runTests() {
  try {
    await testHealthCheck();
    await testHttpChat();
    await testWebSocket();
    await testStats();

    console.log('╔════════════════════════════════════════╗');
    console.log('║  ✅ All tests passed!                  ║');
    console.log('╚════════════════════════════════════════╝\n');
    process.exit(0);
  } catch (error) {
    console.error('\n❌ Test failed:', error.message);
    console.error('\nMake sure the gateway is running:');
    console.error('  npm run dev\n');
    process.exit(1);
  }
}

// Give gateway time to start if just launched
setTimeout(runTests, 2000);
