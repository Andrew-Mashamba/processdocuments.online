#!/bin/bash

echo "Testing Hybrid Agent Runtime Integration..."
echo ""

# Test 1: Simple query (should work with ZIMA Core fallback)
echo "Test 1: Simple query (fallback mode - no ANTHROPIC_API_KEY)"
curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello, how are you?", "channel": "webchat", "senderId": "test-user-runtime"}' \
  -s | python3 -m json.tool | head -30

echo ""
echo "---"
echo ""

# Test 2: Check stats (should show tool registry disabled)
echo "Test 2: Gateway stats"
curl -s http://localhost:18790/api/stats | python3 -m json.tool

echo ""
echo "---"
echo ""

echo "Note: To enable full agent runtime with tool calling,"
echo "set ANTHROPIC_API_KEY environment variable and restart the gateway."
