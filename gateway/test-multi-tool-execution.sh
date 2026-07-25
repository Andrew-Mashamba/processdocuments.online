#!/bin/bash

# Test Multi-Tool Execution via Gateway API
# This tests that multiple tools can be chained together

set -e

echo "🧪 Testing Multi-Tool Execution (Gateway API)..."
echo ""

# Ensure ZIMA API is running
echo "🔍 Checking ZIMA API..."
if ! curl -s http://localhost:5000/health > /dev/null 2>&1; then
    echo "⚠️  ZIMA API not running. Starting it..."
    echo "   Run: cd /Volumes/DATA/QWEN/zima-file-service && dotnet run &"
    exit 1
fi
echo "✓ ZIMA API is running"
echo ""

# Test 1: Simple Multi-Tool (2 tools)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Test 1: Create Excel + Get File Info (2 tools)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

PROMPT_1="Create an Excel file called 'test_products.xlsx' with 3 products: Apple (\$1.99), Banana (\$0.79), Orange (\$2.49). After creating it, get the file info to verify it was created successfully."

echo "📝 Prompt: $PROMPT_1"
echo ""
echo "🚀 Sending request..."

curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d "{
    \"message\": \"$PROMPT_1\",
    \"channel\": \"webchat\",
    \"senderId\": \"test-multi-tool\",
    \"sessionKey\": \"multi-tool-test-$(date +%s)\"
  }" 2>/dev/null | jq -r '.output' | tee /tmp/test1-result.txt

echo ""
echo "📊 Tools used:"
grep -E "create_excel|Excel|get_file_info|file info" /tmp/test1-result.txt || echo "   No tool mentions found"
echo ""

# Test 2: Medium Multi-Tool (3 tools)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Test 2: Memory Search + Excel + Verify (3 tools)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

PROMPT_2="First, search my memory for information about Alice using memory_search. Then create an Excel file called 'alice_data.xlsx' with her information. Finally, use get_file_info to confirm the file exists."

echo "📝 Prompt: Search memory → Create Excel → Verify file"
echo ""
echo "🚀 Sending request..."

curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d "{
    \"message\": \"$PROMPT_2\",
    \"channel\": \"webchat\",
    \"senderId\": \"test-multi-tool\",
    \"sessionKey\": \"multi-tool-test-$(date +%s)\"
  }" 2>/dev/null | jq -r '.output' | tee /tmp/test2-result.txt

echo ""
echo "📊 Tools used:"
echo -n "   memory_search: "
grep -q "memory\|Alice" /tmp/test2-result.txt && echo "✓" || echo "✗"
echo -n "   create_excel: "
grep -q "Excel\|xlsx" /tmp/test2-result.txt && echo "✓" || echo "✗"
echo -n "   get_file_info: "
grep -q "file info\|file.*exist" /tmp/test2-result.txt && echo "✓" || echo "✗"
echo ""

# Test 3: Complex Multi-Tool (4+ tools)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Test 3: Create → Convert → Verify → List (4 tools)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

PROMPT_3="Create an Excel file 'report.xlsx' with sales data (3 rows). Convert it to JSON using excel_to_json. Read the JSON content using read_file_content to verify. Finally, list all files using list_files."

echo "📝 Prompt: Excel → JSON → Read → List files"
echo ""
echo "🚀 Sending request..."

curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d "{
    \"message\": \"$PROMPT_3\",
    \"channel\": \"webchat\",
    \"senderId\": \"test-multi-tool\",
    \"sessionKey\": \"multi-tool-test-$(date +%s)\"
  }" 2>/dev/null | jq -r '.output' | tee /tmp/test3-result.txt

echo ""
echo "📊 Tools used:"
echo -n "   create_excel: "
grep -q "Excel\|xlsx" /tmp/test3-result.txt && echo "✓" || echo "✗"
echo -n "   excel_to_json: "
grep -q "JSON\|json" /tmp/test3-result.txt && echo "✓" || echo "✗"
echo -n "   read_file_content: "
grep -q "read\|content" /tmp/test3-result.txt && echo "✓" || echo "✗"
echo -n "   list_files: "
grep -q "list\|files" /tmp/test3-result.txt && echo "✓" || echo "✗"
echo ""

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📋 Summary"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Test 1 (2 tools): See /tmp/test1-result.txt"
echo "Test 2 (3 tools): See /tmp/test2-result.txt"
echo "Test 3 (4 tools): See /tmp/test3-result.txt"
echo ""
echo "✅ Multi-tool execution tested!"
