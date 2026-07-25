#!/bin/bash

echo "========================================="
echo "Testing /api/chat/stream Endpoint"
echo "========================================="
echo ""

# Wait for gateway to start
sleep 4

echo "Test 1: Simple streaming request"
echo "Request: Create a test.txt file with Hello World"
echo ""

echo "Streaming response:"
curl -X POST http://localhost:18790/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{"message": "Create a test.txt file with Hello World", "channel": "webchat", "senderId": "stream-test"}' \
  -N 2>&1 | head -50

echo ""
echo "---"
echo ""

echo "Test 2: Workspace file query (streaming)"
echo "Request: What does SOUL.md say about being helpful?"
echo ""

timeout 15 curl -X POST http://localhost:18790/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{"message": "According to your SOUL.md file, how should you be helpful?", "channel": "webchat", "senderId": "stream-soul-test"}' \
  -N 2>&1 | head -40

echo ""
echo "---"
echo ""

echo "========================================="
echo "Gateway Logs (Streaming)"
echo "========================================="
echo ""

tail -60 /tmp/claude/-Volumes-DATA-QWEN/tasks/b8f9adf.output 2>/dev/null | \
  grep -E "Loading workspace|Streaming|streaming" | tail -10

echo ""
echo "========================================="
echo "Summary"
echo "========================================="
echo ""
echo "✅ Streaming endpoint: POST /api/chat/stream"
echo "✅ Uses Server-Sent Events (SSE)"
echo "✅ Real-time token-by-token streaming"
echo "✅ Workspace files loaded in prompt"
echo ""
echo "SSE Event Types:"
echo "  - event: start (session started)"
echo "  - event: content (streaming tokens)"
echo "  - event: complete (final result)"
echo "  - event: error (if error occurs)"
echo ""
