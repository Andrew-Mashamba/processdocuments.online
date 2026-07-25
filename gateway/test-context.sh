#!/bin/bash

echo "Testing Hybrid Context Manager..."
echo ""

# Test 1: Simple query
echo "Test 1: Simple query (should use Haiku model)"
curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What is AI?", "channel": "webchat", "senderId": "test-user-1"}' \
  -s | python3 -m json.tool

echo ""
echo "---"
echo ""

# Test 2: Standard document request
echo "Test 2: Standard complexity (should use Sonnet model)"
curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Create an Excel file with 10 random product names and prices", "channel": "webchat", "senderId": "test-user-2"}' \
  -s | python3 -m json.tool

echo ""
echo "---"
echo ""

# Test 3: Cache test (same message as Test 1)
echo "Test 3: Cache test (should return cached response)"
curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What is AI?", "channel": "webchat", "senderId": "test-user-1"}' \
  -s | python3 -m json.tool

echo ""
echo "---"
echo ""

# Test 4: Check stats
echo "Test 4: Gateway stats"
curl -s http://localhost:18790/api/stats | python3 -m json.tool
