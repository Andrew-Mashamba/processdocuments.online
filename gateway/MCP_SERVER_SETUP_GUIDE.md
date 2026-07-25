# Gateway MCP Server - Setup Guide

**Status:** ✅ MCP server built successfully!
**File:** `/Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js`

---

## Quick Setup

### Step 1: Start ZIMA API (if not running)

```bash
cd /Volumes/DATA/QWEN/zima-file-service
dotnet run
```

### Step 2: Add MCP Server to Claude CLI

**Option A: Using CLI command**

```bash
claude mcp add gateway \
  --scope user \
  node /Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js
```

**Option B: Edit config file directly** (Recommended)

```bash
# Open config file
nano ~/Library/Application\ Support/Claude/claude_desktop_config.json
```

Add this configuration:

```json
{
  "mcpServers": {
    "gateway": {
      "type": "stdio",
      "command": "node",
      "args": ["/Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js"],
      "env": {
        "WORKSPACE_DIR": "/Volumes/DATA/QWEN/gateway/workspace",
        "ZIMA_API_URL": "http://localhost:5000",
        "MEMORY_DB": "/Volumes/DATA/QWEN/gateway/storage/memory.db",
        "STORAGE_ROOT": "/Volumes/DATA/QWEN/gateway/storage"
      }
    }
  }
}
```

### Step 3: Verify MCP Server

```bash
# List MCP servers
claude mcp list

# Expected output:
# ✓ gateway (available)
```

### Step 4: Test Memory Search

```bash
# Test with Claude CLI
claude "Use memory_search to find information about Alice"

# Expected behavior:
# 🔧 Tool: memory_search
# 🔍 Query: "Alice"
# ✓ Found: Alice likes cats
```

---

## Manual Test (Without Claude CLI)

Test the MCP server directly:

```bash
# Start MCP server (it listens on stdin/stdout)
node /Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js
```

Then in another terminal, send a test request:

```bash
# Test list tools
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | \
  node /Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js | \
  jq '.result.tools | length'

# Expected: 216
```

---

## Tool Names in Claude

When using tools via MCP, they will appear with the `mcp__gateway__` prefix:

| Gateway Tool | Claude MCP Tool |
|--------------|-----------------|
| `memory_search` | `mcp__gateway__memory_search` |
| `memory_get` | `mcp__gateway__memory_get` |
| `create_excel` | `mcp__gateway__create_excel` |
| `create_pdf` | `mcp__gateway__create_pdf` |
| `web_search` | `mcp__gateway__web_search` |

**Note:** You don't need to use the full MCP name in your prompts. Just say:
- ❌ "Use mcp__gateway__memory_search to find Alice"
- ✅ "Use memory_search to find Alice" (Claude will map it automatically)

---

## Environment Variables

The MCP server accepts these environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `WORKSPACE_DIR` | `/Volumes/DATA/QWEN/gateway/workspace` | Workspace directory |
| `ZIMA_API_URL` | `http://localhost:5000` | ZIMA API endpoint |
| `MEMORY_DB` | `../storage/memory.db` | Memory database path |
| `STORAGE_ROOT` | `../storage` | Storage root directory |

---

## Troubleshooting

### MCP Server Not Appearing

```bash
# Check config file exists
cat ~/Library/Application\ Support/Claude/claude_desktop_config.json

# Check Node.js version
node --version  # Should be v18+

# Test server manually
node /Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js <<< '{"jsonrpc":"2.0","id":1,"method":"ping"}'
```

### Tools Not Loading

```bash
# Check ZIMA API is running
curl http://localhost:5000/health

# Check memory database exists
ls -lh /Volumes/DATA/QWEN/gateway/storage/memory.db

# Check workspace files
ls -lh /Volumes/DATA/QWEN/gateway/workspace/
```

### Tool Execution Fails

```bash
# Check MCP server logs (stdout/stderr)
# MCP server writes logs to stderr (console.error)

# Enable debug mode
DEBUG=1 node /Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js
```

---

## Expected Logs

When working correctly, you should see:

```
🚀 [MCP] Starting Gateway MCP Server...
📂 [MCP] Workspace: /Volumes/DATA/QWEN/gateway/workspace
✓ [MCP] Server connected and ready
✓ [MCP] Claude can now access all gateway tools

📋 [MCP] Claude requesting tool list...
✓ [MCP] Exposing 216 tools

🔧 [MCP] Executing tool: memory_search
📥 [MCP] Input: { "query": "Alice", "limit": 5 }
✓ [MCP] Tool executed successfully
```

---

## Rebuilding After Changes

If you modify the MCP server code:

```bash
# Rebuild
npm run build

# Restart Claude CLI session (close and reopen terminal)
# Or restart MCP server if running separately
```

---

## Next Steps

1. ✅ MCP server built
2. ⏳ Add to Claude CLI config
3. ⏳ Test with memory_search
4. ⏳ Verify all 216 tools available
5. ⏳ Test ZIMA document generation
6. ⏳ Test streaming responses

---

**Status:** Ready to configure! 🚀
**No API costs** - Uses Claude CLI (free)
**All 216 tools** - memory_search, ZIMA tools, OpenClaw tools
