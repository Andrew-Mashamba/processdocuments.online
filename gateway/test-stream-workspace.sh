#!/bin/bash

echo "========================================="
echo "Testing Workspace Files in Prompting"
echo "========================================="
echo ""

# Test: Send a request and verify workspace files are in the system prompt
echo "Test: Verify workspace files are loaded during prompt generation"
echo ""

# Send request
response=$(curl -s -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What guidelines should you follow according to your SOUL.md and AGENTS.md files?", "channel": "webchat", "senderId": "workspace-prompt-test"}')

echo "Response:"
echo "$response" | python3 -m json.tool | head -50

echo ""
echo "---"
echo ""

# Check gateway logs for workspace loading
echo "Gateway Logs (last workspace load):"
tail -150 /tmp/claude/-Volumes-DATA-QWEN/tasks/bf51384.output 2>/dev/null | grep -A 3 "Loading workspace" | tail -4

echo ""
echo "---"
echo ""

echo "Checking for workspace file indicators in response..."
echo ""

# Extract response content
content=$(echo "$response" | python3 -c "import sys, json; print(json.load(sys.stdin).get('output', ''))" 2>/dev/null)

echo "Response preview (first 500 chars):"
echo "$content" | head -c 500
echo "..."
echo ""

# Check for SOUL.md guidelines
if echo "$content" | grep -iq "helpful\|performative\|opinion\|resourceful\|trust"; then
  echo "✅ Response mentions SOUL.md concepts (helpful, opinions, trust, etc.)"
else
  echo "⚠️  Response doesn't explicitly mention SOUL.md concepts"
fi

# Check for AGENTS.md guidelines
if echo "$content" | grep -iq "tool\|narrat\|session\|memory"; then
  echo "✅ Response mentions AGENTS.md concepts (tools, sessions, memory, etc.)"
else
  echo "⚠️  Response doesn't explicitly mention AGENTS.md concepts"
fi

echo ""
echo "========================================="
echo "System Prompt Token Usage"
echo "========================================="
echo ""

# Get cache creation tokens (indicates system prompt size)
cache_tokens=$(echo "$response" | python3 -c "import sys, json; print(json.load(sys.stdin).get('usage', {}).get('cacheCreationTokens', 0))" 2>/dev/null)

echo "Cache Creation Tokens: $cache_tokens"
echo ""

if [ "$cache_tokens" -gt 5000 ]; then
  echo "✅ Large system prompt ($cache_tokens tokens) - workspace files are included"
  echo "   Breakdown estimate:"
  echo "   - Base prompt + tools: ~4,000 tokens"
  echo "   - SOUL.md (1,683 chars): ~400 tokens"
  echo "   - AGENTS.md (4,432 chars): ~1,100 tokens"
  echo "   - TOOLS.md (updated): ~300 tokens"
  echo "   - Total: ~5,800+ tokens"
else
  echo "⚠️  Small system prompt ($cache_tokens tokens) - workspace files may not be included"
fi

echo ""
echo "========================================="
echo "Workspace Files Status"
echo "========================================="
echo ""

echo "Files in workspace directory:"
ls -lh workspace/*.md 2>/dev/null | awk '{printf "  %s (%s)\n", $9, $5}'

echo ""
echo "TOOLS.md storage paths (updated):"
grep -A 7 "Storage Paths" workspace/TOOLS.md | head -8

echo ""
echo "✅ Test complete"
echo ""
