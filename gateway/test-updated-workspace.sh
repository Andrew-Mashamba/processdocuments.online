#!/bin/bash

echo "========================================="
echo "Testing Updated Workspace Files"
echo "========================================="
echo ""

# Test 1: Check execution model from AGENTS.md
echo "Test 1: Execution Model (from migrated CLAUDE.md)"
echo "Question: What is your execution model?"
echo ""

response1=$(curl -s -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "According to your AGENTS.md, what is your execution model when you receive a task?", "channel": "webchat", "senderId": "exec-model-test"}')

echo "$response1" | python3 -c "import sys, json; print(json.load(sys.stdin)['output'][:600])" 2>/dev/null

echo ""
echo "---"
echo ""

# Test 2: Check ZIMA project structure
echo "Test 2: ZIMA Project Structure (from TOOLS.md)"
echo "Question: What is the project structure?"
echo ""

response2=$(curl -s -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What does TOOLS.md say about the ZIMA backend project structure?", "channel": "webchat", "senderId": "structure-test"}')

echo "$response2" | python3 -c "import sys, json; print(json.load(sys.stdin)['output'][:600])" 2>/dev/null

echo ""
echo "---"
echo ""

# Test 3: Check storage paths (updated to relative)
echo "Test 3: Storage Paths (updated to relative)"
echo "Question: What are the storage paths?"
echo ""

response3=$(curl -s -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What storage paths are configured in TOOLS.md?", "channel": "webchat", "senderId": "paths-test"}')

echo "$response3" | python3 -c "import sys, json; print(json.load(sys.stdin)['output'][:500])" 2>/dev/null

echo ""
echo "---"
echo ""

# Check gateway logs
echo "Gateway Loading Log:"
tail -100 /tmp/claude/-Volumes-DATA-QWEN/tasks/bec39b7.output 2>/dev/null | \
  grep -A 1 "Loading workspace" | tail -2

echo ""
echo "========================================="
echo "Workspace File Sizes"
echo "========================================="
ls -lh workspace/*.md | awk '{printf "%-25s %6s\n", $9, $5}'

echo ""
echo "Old CLAUDE.md Status:"
if [ -f "/Volumes/DATA/QWEN/zima-file-service/.claude/CLAUDE.md" ]; then
  echo "❌ Still exists (should be deleted)"
else
  echo "✅ Deleted successfully"
fi

echo ""
echo "✅ Test complete"
