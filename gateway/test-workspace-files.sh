#!/bin/bash

echo "========================================="
echo "Testing Workspace Files Integration"
echo "========================================="
echo ""

# Test 1: Check if SOUL.md personality is being used
echo "Test 1: SOUL.md Personality Check"
echo "SOUL.md says: 'Skip the Great question! and I'd be happy to help!'"
echo "Let's see if ZIMA follows this..."
echo ""

curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Can you help me create an Excel file?", "channel": "webchat", "senderId": "test-soul"}' \
  -s | python3 -c "import sys, json; data = json.load(sys.stdin); print('Response:', data['output'][:200])"

echo ""
echo "---"
echo ""

# Test 2: Check if AGENTS.md operational guidelines are being used
echo "Test 2: AGENTS.md Operational Guidelines"
echo "AGENTS.md says: 'Don't narrate routine tool calls - just call the tool'"
echo "Testing with a document creation request..."
echo ""

curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Create a simple hello.txt file with the text Hello World", "channel": "webchat", "senderId": "test-agents"}' \
  -s | python3 -c "import sys, json; data = json.load(sys.stdin); print('Response:', data['output'][:200])"

echo ""
echo "---"
echo ""

# Test 3: Check if TOOLS.md environment notes are accessible
echo "Test 3: TOOLS.md Environment Context"
echo "TOOLS.md lists server URLs and infrastructure"
echo "Ask about the infrastructure setup..."
echo ""

curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What is the ZIMA Core URL you are using?", "channel": "webchat", "senderId": "test-tools"}' \
  -s | python3 -c "import sys, json; data = json.load(sys.stdin); print('Response:', data['output'][:300])"

echo ""
echo "---"
echo ""

# Test 4: Check system prompt token usage (indicates workspace files loaded)
echo "Test 4: System Prompt Size Check"
echo "Checking if workspace files are contributing to prompt size..."
echo ""

curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Hi", "channel": "webchat", "senderId": "test-prompt-size"}' \
  -s | python3 -c "import sys, json; data = json.load(sys.stdin); print('Cache Creation Tokens:', data['usage'].get('cacheCreationTokens', 0), '(includes workspace files in system prompt)')"

echo ""
echo "========================================="
echo "Summary"
echo "========================================="
echo ""
echo "Workspace files location: /Volumes/DATA/QWEN/gateway/workspace/"
echo "  - SOUL.md: Agent personality and tone"
echo "  - AGENTS.md: Operational guidelines and tool usage"
echo "  - TOOLS.md: Environment-specific context"
echo ""
echo "Expected behaviors:"
echo "  ✓ Concise, helpful responses (no filler phrases)"
echo "  ✓ Direct tool usage without narration"
echo "  ✓ Environment awareness (knows about ZIMA Core URL)"
echo "  ✓ Large cache creation tokens (~28K+ with workspace files)"
echo ""
