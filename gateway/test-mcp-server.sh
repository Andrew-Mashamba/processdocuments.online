#!/bin/bash

# Test Gateway MCP Server
#
# This script tests the MCP server without Claude CLI
# to verify it's working correctly.

set -e

echo "🧪 Testing Gateway MCP Server..."
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

MCP_SERVER="/Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js"

# Check if server exists
if [ ! -f "$MCP_SERVER" ]; then
    echo -e "${RED}❌ MCP server not found!${NC}"
    echo "Run: npm run build"
    exit 1
fi

echo -e "${GREEN}✓${NC} MCP server found"
echo ""

# Test 1: List tools
echo "Test 1: List available tools..."
TOOLS_COUNT=$(echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | \
    node "$MCP_SERVER" 2>/dev/null | \
    jq -r '.result.tools | length' 2>/dev/null || echo "0")

if [ "$TOOLS_COUNT" -gt "0" ]; then
    echo -e "${GREEN}✓${NC} Found $TOOLS_COUNT tools"
else
    echo -e "${RED}❌${NC} No tools found (expected 216)"
fi
echo ""

# Test 2: Check specific tools exist
echo "Test 2: Verify memory_search tool..."
HAS_MEMORY_SEARCH=$(echo '{"jsonrpc":"2.0","id":2,"method":"tools/list"}' | \
    node "$MCP_SERVER" 2>/dev/null | \
    jq -r '.result.tools[] | select(.name=="memory_search") | .name' 2>/dev/null || echo "")

if [ "$HAS_MEMORY_SEARCH" == "memory_search" ]; then
    echo -e "${GREEN}✓${NC} memory_search tool available"
else
    echo -e "${RED}❌${NC} memory_search tool not found"
fi
echo ""

# Test 3: Check ZIMA tools exist
echo "Test 3: Verify ZIMA tools..."
HAS_CREATE_EXCEL=$(echo '{"jsonrpc":"2.0","id":3,"method":"tools/list"}' | \
    node "$MCP_SERVER" 2>/dev/null | \
    jq -r '.result.tools[] | select(.name=="create_excel") | .name' 2>/dev/null || echo "")

if [ "$HAS_CREATE_EXCEL" == "create_excel" ]; then
    echo -e "${GREEN}✓${NC} ZIMA tools available (create_excel found)"
else
    echo -e "${YELLOW}⚠${NC}  ZIMA tools not found (ZIMA API may not be running)"
fi
echo ""

# Test 4: Call a tool (if database exists)
if [ -f "/Volumes/DATA/QWEN/gateway/storage/memory.db" ]; then
    echo "Test 4: Execute memory_search tool..."

    # Create test request
    cat > /tmp/mcp-test-request.json <<EOF
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "memory_search",
    "arguments": {
      "query": "test",
      "limit": 1
    }
  }
}
EOF

    TOOL_RESULT=$(cat /tmp/mcp-test-request.json | node "$MCP_SERVER" 2>/dev/null | jq -r '.result' 2>/dev/null || echo "")

    if [ -n "$TOOL_RESULT" ]; then
        echo -e "${GREEN}✓${NC} memory_search executed successfully"
    else
        echo -e "${YELLOW}⚠${NC}  memory_search execution failed (check logs)"
    fi
    echo ""
else
    echo -e "${YELLOW}⚠${NC}  Skipping tool execution test (memory.db not found)"
    echo ""
fi

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Summary:"
echo "  Tools found: $TOOLS_COUNT"
echo "  memory_search: ${HAS_MEMORY_SEARCH:-not found}"
echo "  ZIMA tools: ${HAS_CREATE_EXCEL:-not found}"
echo ""
echo "Next steps:"
echo "  1. Add to Claude CLI: claude mcp add gateway node $MCP_SERVER"
echo "  2. Verify: claude mcp list"
echo "  3. Test: claude 'Use memory_search to find Alice'"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
