# Deployment Portability Issues & Solutions

**Date:** 2026-01-31
**Status:** ⚠️ NOT PORTABLE - Requires fixes

---

## Current Issues

### ❌ Issue 1: Hardcoded macOS Paths

**File:** `src/mcp/gateway-mcp-server.ts` (Lines 165-169)

```typescript
storage: {
  root: process.env.STORAGE_ROOT || "/Volumes/DATA/QWEN/gateway/storage", // ❌ macOS specific
  transcripts: process.env.TRANSCRIPTS_DIR || (process.env.HOME + "/.zima/agents/main/sessions"),
  files: process.env.FILES_DIR || "/Volumes/DATA/QWEN/gateway/generated_files", // ❌ macOS specific
  workspace: process.env.WORKSPACE_DIR || "/Volumes/DATA/QWEN/gateway/workspace", // ❌ macOS specific
  memory: process.env.MEMORY_DB || "/Volumes/DATA/QWEN/gateway/storage/memory.db", // ❌ macOS specific
}
```

**Problem:** `/Volumes/DATA/...` doesn't exist on Linux

---

### ❌ Issue 2: ZIMA Tools Path

**File:** `src/agent/tool-registry.ts` (Lines 87-88)

```typescript
const paths = [
  '/Volumes/DATA/QWEN/zima-file-service/bin/Debug/net9.0/.zima-tools.json', // ❌ Absolute
  '/Volumes/DATA/QWEN/zima-file-service/bin/Debug/net8.0/.zima-tools.json', // ❌ Absolute
  path.join(process.cwd(), '.zima-tools.json'), // ✅ Relative (OK)
];
```

**Problem:** Won't find ZIMA tools on Linux

---

### ❌ Issue 3: MCP CLI Configuration

**File:** `~/.claude.json`

```json
{
  "gateway": {
    "command": "node",
    "args": ["/Volumes/DATA/QWEN/gateway/dist/mcp/gateway-mcp-server.js"], // ❌ Absolute
    "env": {
      "WORKSPACE_DIR": "/Volumes/DATA/QWEN/gateway/workspace", // ❌ Absolute
      "MEMORY_DB": "/Volumes/DATA/QWEN/gateway/storage/memory.db" // ❌ Absolute
    }
  }
}
```

**Problem:** All paths are absolute and macOS-specific

---

## Solutions

### ✅ Solution 1: Use Relative Paths

**Update:** `src/mcp/gateway-mcp-server.ts`

```typescript
import path from 'path';
import { fileURLToPath } from 'url';

// Get project root (relative to this file)
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const projectRoot = path.join(__dirname, '../..'); // Go up to project root

// Load config with relative paths
const config: GatewayConfig = {
  gateway: {
    port: parseInt(process.env.PORT || "18790"),
    bind: "0.0.0.0",
    cors: { origins: ["*"] }
  },
  channels: {
    webchat: { enabled: true },
    whatsapp: { enabled: false },
    email: { enabled: false }
  },
  zima: {
    apiUrl: process.env.ZIMA_API_URL || "http://localhost:5000",
    timeout: parseInt(process.env.ZIMA_TIMEOUT || "30000")
  },
  storage: {
    root: process.env.STORAGE_ROOT || path.join(projectRoot, 'storage'),
    transcripts: process.env.TRANSCRIPTS_DIR || path.join(process.env.HOME || '/tmp', '.zima/agents/main/sessions'),
    files: process.env.FILES_DIR || path.join(projectRoot, 'generated_files'),
    workspace: process.env.WORKSPACE_DIR || path.join(projectRoot, 'workspace'),
    memory: process.env.MEMORY_DB || path.join(projectRoot, 'storage/memory.db')
  }
};
```

---

### ✅ Solution 2: Deployment Script

**Create:** `deploy-to-linux.sh`

```bash
#!/bin/bash
set -e

echo "🚀 Deploying Gateway to Linux Server..."

# Configuration
PROJECT_DIR="${PROJECT_DIR:-/opt/gateway}"
ZIMA_API="${ZIMA_API:-http://localhost:5000}"

echo "📂 Project directory: $PROJECT_DIR"

# Create directories
mkdir -p "$PROJECT_DIR/storage"
mkdir -p "$PROJECT_DIR/workspace"
mkdir -p "$PROJECT_DIR/generated_files"

# Copy project files
echo "📦 Copying project files..."
rsync -av --exclude 'node_modules' --exclude '.git' \
  ./ "$PROJECT_DIR/"

# Install dependencies
echo "📥 Installing dependencies..."
cd "$PROJECT_DIR"
npm install --production

# Build project
echo "🔨 Building project..."
npm run build

# Configure MCP for Linux
echo "⚙️  Configuring MCP server..."
cat > ~/.claude.json << EOF
{
  "mcpServers": {
    "gateway": {
      "type": "stdio",
      "command": "node",
      "args": ["$PROJECT_DIR/dist/mcp/gateway-mcp-server.js"],
      "env": {
        "WORKSPACE_DIR": "$PROJECT_DIR/workspace",
        "ZIMA_API_URL": "$ZIMA_API",
        "MEMORY_DB": "$PROJECT_DIR/storage/memory.db",
        "STORAGE_ROOT": "$PROJECT_DIR/storage"
      }
    }
  }
}
EOF

echo "✅ Deployment complete!"
echo ""
echo "Test with: claude mcp list"
```

---

### ✅ Solution 3: Environment Variable Configuration

**Create:** `.env.example`

```bash
# Gateway Configuration
PORT=18790
BIND=0.0.0.0

# ZIMA API
ZIMA_API_URL=http://localhost:5000
ZIMA_TIMEOUT=30000

# Storage Paths (auto-detected if not set)
STORAGE_ROOT=./storage
WORKSPACE_DIR=./workspace
MEMORY_DB=./storage/memory.db
TRANSCRIPTS_DIR=~/.zima/agents/main/sessions
FILES_DIR=./generated_files
```

**Usage on Linux:**

```bash
# Copy example
cp .env.example .env

# Edit for your environment
nano .env

# Run with environment
export $(cat .env | xargs)
npm start
```

---

## Path Comparison: macOS vs Linux

| Path Type | macOS | Linux |
|-----------|-------|-------|
| **Project Root** | `/Volumes/DATA/QWEN/gateway` | `/opt/gateway` or `/home/user/gateway` |
| **Storage** | `/Volumes/DATA/QWEN/gateway/storage` | `/opt/gateway/storage` |
| **Workspace** | `/Volumes/DATA/QWEN/gateway/workspace` | `/opt/gateway/workspace` |
| **ZIMA Tools** | `/Volumes/DATA/QWEN/zima-file-service/bin/...` | `/opt/zima-file-service/bin/...` |
| **Home Dir** | `/Users/andrewmashamba` | `/home/username` |

---

## Required Changes for Linux Deployment

### 1. Update MCP Server Code ✅

```bash
# Edit src/mcp/gateway-mcp-server.ts
# Replace hardcoded paths with path.join(projectRoot, ...)
```

### 2. Update Tool Registry ✅

```bash
# Edit src/agent/tool-registry.ts
# Make ZIMA tools path configurable via env var
```

### 3. Create Deployment Script ✅

```bash
# Create deploy-to-linux.sh
chmod +x deploy-to-linux.sh
```

### 4. Configure for Target Server ✅

```bash
# Set environment variables
export PROJECT_DIR=/opt/gateway
export ZIMA_API=http://localhost:5000

# Run deployment
./deploy-to-linux.sh
```

---

## Docker Alternative (Fully Portable)

**Create:** `Dockerfile`

```dockerfile
FROM node:20-alpine

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install --production

# Copy source
COPY . .

# Build
RUN npm run build

# Create directories
RUN mkdir -p /app/storage /app/workspace /app/generated_files

# Environment variables
ENV WORKSPACE_DIR=/app/workspace
ENV STORAGE_ROOT=/app/storage
ENV MEMORY_DB=/app/storage/memory.db
ENV ZIMA_API_URL=http://zima:5000

# Expose port
EXPOSE 18790

# Start MCP server
CMD ["node", "dist/mcp/gateway-mcp-server.js"]
```

**Usage:**

```bash
# Build
docker build -t gateway-mcp .

# Run
docker run -d \
  -p 18790:18790 \
  -v $(pwd)/storage:/app/storage \
  -v $(pwd)/workspace:/app/workspace \
  -e ZIMA_API_URL=http://host.docker.internal:5000 \
  gateway-mcp
```

---

## Immediate Fix (Temporary)

**Before deploying to Linux**, set these environment variables:

```bash
export WORKSPACE_DIR=/opt/gateway/workspace
export STORAGE_ROOT=/opt/gateway/storage
export MEMORY_DB=/opt/gateway/storage/memory.db
export FILES_DIR=/opt/gateway/generated_files
export ZIMA_API_URL=http://localhost:5000
```

Then rebuild and configure MCP with absolute paths for your Linux server.

---

## Recommended Approach

### For Production Linux Deployment:

1. **Use relative paths** (update source code)
2. **Create deployment script** (automates setup)
3. **Use environment variables** (no hardcoded paths)
4. **Document paths** (README with Linux instructions)

### For Development:

1. **Keep current setup** (works on macOS)
2. **Add `.env` support** (easier configuration)
3. **Test on Linux VM** (validate portability)

---

## Summary

| Aspect | Current Status | Linux Compatible? | Fix Required |
|--------|---------------|-------------------|--------------|
| **MCP Server Paths** | Hardcoded macOS | ❌ No | ✅ Use relative paths |
| **Tool Registry** | Hardcoded macOS | ❌ No | ✅ Add env var |
| **MCP CLI Config** | Absolute paths | ❌ No | ✅ Regenerate on target |
| **Environment Vars** | Partial | ⚠️ Some | ✅ Add .env support |
| **Docker Support** | None | ❌ No | ✅ Add Dockerfile |

---

**Action Required:** Update source code to use relative paths before deploying to Linux server.

**Estimated effort:** 2-3 hours to make fully portable
