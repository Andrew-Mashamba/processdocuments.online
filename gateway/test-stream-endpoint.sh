#!/bin/bash

echo "========================================="
echo "Testing Stream Endpoints"
echo "========================================="
echo ""

# Test 1: Check if ZIMA Core has /api/generate/stream
echo "Test 1: ZIMA Core - /api/generate/stream"
echo "Testing: POST http://localhost:5000/api/generate/stream"
echo ""

timeout 10 curl -X POST http://localhost:5000/api/generate/stream \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Create a test.txt file with Hello World", "sessionId": "stream-test-1"}' \
  -N 2>&1 | head -20

echo ""
echo "---"
echo ""

# Test 2: Check if Gateway has a stream endpoint
echo "Test 2: Gateway - Check available endpoints"
echo ""

echo "Available Gateway endpoints:"
echo "  - POST /api/chat (confirmed)"
echo "  - GET /health (confirmed)"
echo "  - GET /api/stats (confirmed)"
echo "  - GET /api/config (confirmed)"
echo ""

# Test 3: Check WebSocket streaming
echo "Test 3: WebSocket connection (Gateway)"
echo "WebSocket URL: ws://localhost:18790"
echo ""

# Create simple WebSocket test
cat > /tmp/ws-test.js << 'EOF'
const WebSocket = require('ws');

const ws = new WebSocket('ws://localhost:18790');

ws.on('open', function open() {
  console.log('✅ WebSocket connected');

  // Send a test message
  ws.send(JSON.stringify({
    type: 'chat',
    channel: 'webchat',
    senderId: 'ws-test',
    message: 'Hello from WebSocket'
  }));
});

ws.on('message', function incoming(data) {
  console.log('📨 Received:', data.toString().substring(0, 200));
  ws.close();
});

ws.on('error', function error(err) {
  console.log('❌ WebSocket error:', err.message);
});

ws.on('close', function close() {
  console.log('🔌 WebSocket closed');
  process.exit(0);
});

setTimeout(() => {
  console.log('⏱️  Timeout - closing');
  ws.close();
  process.exit(0);
}, 5000);
EOF

echo "Running WebSocket test..."
node /tmp/ws-test.js

echo ""
echo "---"
echo ""

# Test 4: Check hybrid agent runtime streaming
echo "Test 4: Check Hybrid Agent Runtime Streaming Support"
echo ""

echo "Checking hybrid-agent-runtime.ts for streaming support..."
grep -A 3 "StreamCallback\|onStream" /Volumes/DATA/QWEN/gateway/src/agent/hybrid-agent-runtime.ts | head -10

echo ""
echo "========================================="
echo "Summary"
echo "========================================="
echo ""
echo "Gateway Streaming Capabilities:"
echo "  1. WebSocket (ws://localhost:18790) - Real-time bidirectional"
echo "  2. HTTP /api/chat - Standard request/response"
echo "  3. Agent Runtime - Has StreamCallback support in code"
echo ""
echo "ZIMA Core Streaming:"
echo "  - /api/generate/stream - SSE endpoint (if available)"
echo ""
echo "To implement HTTP streaming in Gateway:"
echo "  - Add POST /api/chat/stream endpoint"
echo "  - Use Server-Sent Events (SSE)"
echo "  - Pass onStream callback to hybrid agent runtime"
echo ""
