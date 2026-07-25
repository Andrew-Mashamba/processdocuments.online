# Linux Deployment Guide

**Date:** 2026-01-31
**Status:** Solution ready - Scripts created

---

## Quick Answer

**Current state:** ❌ **NOT portable** - uses hardcoded macOS paths

**Solution:** ✅ **Use the deployment scripts** we created

---

## The Problem

The gateway currently has **hardcoded macOS paths** in 3 places:

### 1. MCP Server (`src/mcp/gateway-mcp-server.ts`)

```typescript
// ❌ BEFORE (macOS-specific)
storage: {
  root: "/Volumes/DATA/QWEN/gateway/storage",
  workspace: "/Volumes/DATA/QWEN/gateway/workspace",
  // ...
}
```

### 2. Tool Registry (`src/agent/tool-registry.ts`)

```typescript
// ❌ BEFORE (macOS-specific)
const paths = [
  '/Volumes/DATA/QWEN/zima-file-service/bin/Debug/net9.0/.zima-tools.json',
  // ...
];
```

### 3. MCP CLI Config (`~/.claude.json`)

```json
{
  "gateway": {
    "args": ["/Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js"]
  }
}
```

---

## Two Solutions

### Option 1: Quick Deploy (As-Is)

**For:** Testing on Linux, temporary deployment

**Steps:**

1. **Copy project to Linux**
   ```bash
   scp -r /Volumes/DATA/QWEN/gateway user@linux-server:/opt/
   ```

2. **Set environment variables on Linux**
   ```bash
   export WORKSPACE_DIR=/opt/gateway/workspace
   export STORAGE_ROOT=/opt/gateway/storage
   export MEMORY_DB=/opt/gateway/storage/memory.db
   export FILES_DIR=/opt/gateway/generated_files
   export ZIMA_API_URL=http://localhost:5000
   ```

3. **Configure MCP on Linux**
   ```bash
   claude mcp add gateway \
     node /opt/gateway/dist/mcp/gateway-mcp-server.js
   ```

**Pros:** Fast, no code changes
**Cons:** Environment variables must be set correctly

---

### Option 2: Make Portable (Recommended)

**For:** Production deployment, permanent solution

**Steps:**

#### 1. Run Portability Script (macOS)

```bash
cd /Volumes/DATA/QWEN/gateway
./make-portable.sh
```

This will:
- ✅ Update MCP server to use relative paths
- ✅ Update tool registry to use environment variables
- ✅ Create `.env.example` template
- ✅ Rebuild project

#### 2. Copy to Linux Server

```bash
# From macOS
scp -r /Volumes/DATA/QWEN/gateway user@linux-server:/tmp/

# On Linux
sudo mv /tmp/gateway /opt/gateway
cd /opt/gateway
```

#### 3. Run Deployment Script (Linux)

```bash
cd /opt/gateway
chmod +x deploy-to-linux.sh
./deploy-to-linux.sh
```

This will:
- ✅ Create directories
- ✅ Install dependencies
- ✅ Build project
- ✅ Create `.env` file
- ✅ Configure Claude CLI MCP
- ✅ Create systemd service (optional)

#### 4. Verify

```bash
# Test MCP server
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | \
  node /opt/gateway/dist/mcp/gateway-mcp-server.js

# Check Claude CLI
claude mcp list

# Test with Claude
claude "List available tools from gateway"
```

---

## Path Mapping: macOS → Linux

| Component | macOS | Linux (default) |
|-----------|-------|-----------------|
| **Project Root** | `/Volumes/DATA/QWEN/gateway` | `/opt/gateway` |
| **Storage** | `/Volumes/DATA/QWEN/gateway/storage` | `/opt/gateway/storage` |
| **Workspace** | `/Volumes/DATA/QWEN/gateway/workspace` | `/opt/gateway/workspace` |
| **Memory DB** | `./storage/memory.db` | `/opt/gateway/storage/memory.db` |
| **ZIMA API** | `http://localhost:5000` | `http://localhost:5000` (same) |
| **ZIMA Tools** | `/Volumes/DATA/QWEN/zima-file-service/...` | `/opt/zima-file-service/...` |

---

## Environment Variables Reference

**After running `make-portable.sh`, these variables control paths:**

```bash
# Core Settings
PORT=18790                    # Gateway HTTP port
BIND=0.0.0.0                 # Bind address
CORS_ORIGINS=*               # CORS origins

# ZIMA Integration
ZIMA_API_URL=http://localhost:5000          # ZIMA API endpoint
ZIMA_TIMEOUT=30000                           # API timeout (ms)
ZIMA_TOOLS_PATH=/opt/zima-file-service/...  # Tools JSON path

# Storage Locations
STORAGE_ROOT=/opt/gateway/storage           # Storage root
WORKSPACE_DIR=/opt/gateway/workspace        # Workspace files
MEMORY_DB=/opt/gateway/storage/memory.db    # Memory database
TRANSCRIPTS_DIR=~/.zima/agents/main/sessions # Session transcripts
FILES_DIR=/opt/gateway/generated_files       # Generated files
```

---

## Custom Paths (Advanced)

### Deploy to Custom Location

```bash
# Set custom path before deployment
export PROJECT_DIR=/home/myuser/gateway
export ZIMA_API=http://192.168.1.100:5000
export ZIMA_TOOLS_PATH=/home/myuser/zima-tools/.zima-tools.json

# Run deployment
./deploy-to-linux.sh
```

### Multiple Environments

```bash
# Development
PROJECT_DIR=/home/user/gateway-dev ./deploy-to-linux.sh

# Staging
PROJECT_DIR=/opt/gateway-staging ./deploy-to-linux.sh

# Production
PROJECT_DIR=/opt/gateway ./deploy-to-linux.sh
```

---

## Docker Alternative (Fully Portable)

**Create:** `Dockerfile`

```dockerfile
FROM node:20-alpine

WORKDIR /app

# Copy files
COPY package*.json ./
RUN npm install --production
COPY . .
RUN npm run build

# Create directories
RUN mkdir -p storage workspace generated_files

# Default environment
ENV WORKSPACE_DIR=/app/workspace
ENV STORAGE_ROOT=/app/storage
ENV MEMORY_DB=/app/storage/memory.db
ENV ZIMA_API_URL=http://zima:5000

EXPOSE 18790

CMD ["node", "dist/mcp/gateway-mcp-server.js"]
```

**Build & Run:**

```bash
# Build
docker build -t gateway-mcp .

# Run standalone
docker run -d \
  -p 18790:18790 \
  -v $(pwd)/storage:/app/storage \
  -v $(pwd)/workspace:/app/workspace \
  -e ZIMA_API_URL=http://host.docker.internal:5000 \
  gateway-mcp

# Run with Claude CLI
docker run -d \
  --name gateway-mcp \
  -v $(pwd)/storage:/app/storage \
  -v $(pwd)/workspace:/app/workspace \
  gateway-mcp

# Configure MCP
claude mcp add gateway \
  docker exec -i gateway-mcp node dist/mcp/gateway-mcp-server.js
```

---

## Systemd Service (Linux)

After deployment, you can run gateway as a service:

```bash
# Enable service
sudo systemctl enable gateway

# Start service
sudo systemctl start gateway

# Check status
sudo systemctl status gateway

# View logs
sudo journalctl -u gateway -f
```

**Service file location:** `/etc/systemd/system/gateway.service`

---

## Testing After Deployment

### 1. Test MCP Server Directly

```bash
cd /opt/gateway
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | \
  node dist/mcp/gateway-mcp-server.js | \
  jq '.result.tools | length'
# Expected: 216
```

### 2. Test Claude CLI Integration

```bash
# List MCP servers
claude mcp list
# Expected: gateway - ✓ Connected

# Get server details
claude mcp get gateway
# Expected: Shows /opt/gateway/... paths
```

### 3. Test Tool Execution

```bash
# Test memory_search
claude "Use memory_search to find test data"

# Test ZIMA tools
claude "Create an Excel file with sample data"
```

---

## Troubleshooting

### MCP Server Won't Start

```bash
# Check Node.js version
node --version  # Should be v18+

# Check file exists
ls -lh /opt/gateway/dist/mcp/gateway-mcp-server.js

# Check permissions
chmod +x /opt/gateway/dist/mcp/gateway-mcp-server.js

# Test with verbose output
NODE_ENV=development node /opt/gateway/dist/mcp/gateway-mcp-server.js
```

### Tools Not Loading

```bash
# Check ZIMA API
curl http://localhost:5000/health

# Check ZIMA tools file
ls -lh /opt/zima-file-service/bin/Debug/net9.0/.zima-tools.json

# Set ZIMA_TOOLS_PATH explicitly
export ZIMA_TOOLS_PATH=/path/to/.zima-tools.json
```

### Memory Database Missing

```bash
# Create storage directory
mkdir -p /opt/gateway/storage

# Initialize database (will be created on first use)
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"memory_search","arguments":{"query":"test"}}}' | \
  MEMORY_DB=/opt/gateway/storage/memory.db \
  node /opt/gateway/dist/mcp/gateway-mcp-server.js
```

---

## Migration Checklist

Before deploying to Linux:

- [ ] Run `make-portable.sh` on macOS
- [ ] Test locally after making portable
- [ ] Copy project to Linux server
- [ ] Run `deploy-to-linux.sh` on Linux
- [ ] Verify MCP server starts
- [ ] Check Claude CLI sees server
- [ ] Test tool execution
- [ ] Configure systemd (optional)
- [ ] Set up backups for storage/

---

## Summary

| Aspect | Current (macOS) | After Portability Fix |
|--------|----------------|----------------------|
| **Paths** | Hardcoded `/Volumes/DATA/...` | Relative or environment variables |
| **Linux Compatible** | ❌ No | ✅ Yes |
| **Docker Compatible** | ❌ No | ✅ Yes |
| **Environment Config** | Partial | ✅ Complete (.env support) |
| **Deploy Script** | ❌ None | ✅ `deploy-to-linux.sh` |
| **Portable** | ❌ No | ✅ Yes |

---

## Quick Start (TL;DR)

```bash
# On macOS: Make portable
cd /Volumes/DATA/QWEN/gateway
./make-portable.sh

# Copy to Linux
scp -r /Volumes/DATA/QWEN/gateway user@linux:/tmp/

# On Linux: Deploy
cd /tmp/gateway
sudo mv . /opt/gateway
cd /opt/gateway
./deploy-to-linux.sh

# Test
claude mcp list
claude "Use memory_search to find test data"
```

---

**Status:** Ready to deploy! 🚀

Use `make-portable.sh` first, then `deploy-to-linux.sh` on the target server.
