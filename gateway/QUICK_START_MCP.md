# Quick Start: Gateway MCP Server

**Status:** ✅ READY TO USE
**Time to setup:** 2 minutes

---

## What You Have

✅ **MCP Server built** - `/Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js`
✅ **216 tools ready** - memory_search, ZIMA tools, OpenClaw tools
✅ **Zero costs** - No API key needed, uses Claude CLI (free)

---

## Setup (2 minutes)

### Step 1: Edit Claude CLI Config

```bash
nano ~/Library/Application\ Support/Claude/claude_desktop_config.json
```

### Step 2: Add This

```json
{
  "mcpServers": {
    "gateway": {
      "type": "stdio",
      "command": "node",
      "args": ["/Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js"]
    }
  }
}
```

### Step 3: Verify

```bash
claude mcp list
```

You should see: `✓ gateway (available)`

---

## Test Commands

```bash
# Test memory search
claude "Use memory_search to find Alice"

# Test Excel creation
claude "Create an Excel file with products: Apple, Banana, Orange"

# Test web search
claude "Search the web for latest AI news"
```

---

## Expected Output

```
🔧 [MCP] Executing tool: memory_search
📥 [MCP] Input: { "query": "Alice", "limit": 5 }
✓ [MCP] Tool executed successfully

Result: Found Alice in memory: Alice likes cats
```

---

## All Tools Available (216 total)

### Memory (2)
- `memory_search` - Semantic workspace search
- `memory_get` - Read memory files

### ZIMA (196)
- `create_excel` - Excel spreadsheets
- `create_pdf` - PDF documents
- `create_word` - Word documents
- `create_powerpoint` - PowerPoint
- ... 192 more document tools

### OpenClaw (18)
- `read` - Read files
- `write` - Write files
- `grep` - Search files
- `bash` - Execute commands
- `web_search` - Web search
- ... 13 more system tools

---

## Troubleshooting

### MCP server not appearing?

```bash
# Check config file
cat ~/Library/Application\ Support/Claude/claude_desktop_config.json

# Test server manually
node /Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js <<< '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

### Tools not loading?

```bash
# Check ZIMA API (should be running)
curl http://localhost:5000/health

# If not running, start it:
cd /Volumes/DATA/QWEN/zima-file-service
dotnet run
```

---

## Documentation

- **Full docs**: `MCP_SERVER_SOLUTION.md`
- **Setup guide**: `MCP_SERVER_SETUP_GUIDE.md`
- **Success story**: `MCP_SOLUTION_SUCCESS.md`
- **Test script**: `test-mcp-server.sh`

---

## Key Points

✅ **Free** - No API costs
✅ **Fast** - Local execution
✅ **Complete** - All 216 tools
✅ **Standard** - MCP protocol
✅ **Compatible** - Works with Claude CLI

**Ready to use!** 🚀
