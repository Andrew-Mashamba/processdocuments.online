#!/bin/bash

echo "========================================="
echo "Direct Workspace File Content Test"
echo "========================================="
echo ""

echo "Test 1: Ask specifically about SOUL.md content"
echo "---"

response1=$(curl -s -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What does your SOUL.md file say about being helpful? Quote from it directly.", "channel": "webchat", "senderId": "direct-soul-test"}')

echo "Question: What does SOUL.md say about being helpful?"
echo ""
echo "Response:"
echo "$response1" | python3 -c "import sys, json; print(json.load(sys.stdin)['output'][:400])" 2>/dev/null || echo "Error parsing response"

echo ""
echo "---"
echo ""

echo "Test 2: Ask about storage paths from TOOLS.md"
echo "---"

response2=$(curl -s -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "According to your TOOLS.md file, what are the storage paths configured?", "channel": "webchat", "senderId": "direct-tools-test"}')

echo "Question: What storage paths are in TOOLS.md?"
echo ""
echo "Response:"
echo "$response2" | python3 -c "import sys, json; print(json.load(sys.stdin)['output'][:500])" 2>/dev/null || echo "Error parsing response"

echo ""
echo "---"
echo ""

echo "Test 3: Ask about tool usage from AGENTS.md"
echo "---"

response3=$(curl -s -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What does AGENTS.md say about tool call style and narration?", "channel": "webchat", "senderId": "direct-agents-test"}')

echo "Question: What does AGENTS.md say about tool usage?"
echo ""
echo "Response:"
echo "$response3" | python3 -c "import sys, json; print(json.load(sys.stdin)['output'][:400])" 2>/dev/null || echo "Error parsing response"

echo ""
echo "========================================="
echo "Gateway Workspace Loading Log"
echo "========================================="
echo ""

# Check last 3 workspace loads
tail -300 /tmp/claude/-Volumes-DATA-QWEN/tasks/bf51384.output 2>/dev/null | \
  grep -A 1 "Loading workspace" | tail -6

echo ""
echo "✅ Test complete"
echo ""
