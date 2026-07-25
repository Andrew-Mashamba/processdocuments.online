# ZIMA Memory System - Complete Documentation

**Version:** 1.0
**Date:** 2026-01-31
**Status:** Production Ready ✅

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Quick Start](#quick-start)
4. [Configuration](#configuration)
5. [Core Components](#core-components)
6. [CLI Commands](#cli-commands)
7. [API Reference](#api-reference)
8. [Performance Tuning](#performance-tuning)
9. [Troubleshooting](#troubleshooting)
10. [Cost Management](#cost-management)
11. [Production Deployment](#production-deployment)

---

## Overview

The ZIMA Memory System is a production-grade semantic search engine that combines:

- **Vector Similarity Search** (OpenAI embeddings)
- **BM25 Keyword Search** (SQLite FTS5)
- **Hybrid Search** (Reciprocal Rank Fusion)
- **Real-time Synchronization** (file watching + scheduled sync)
- **Performance Monitoring** (latency tracking, cost analysis)
- **Batch Processing** (optimized embedding generation)

### Key Features

✅ **Incremental Indexing** - Only changed files are reprocessed
✅ **Auto-Sync** - Real-time file watching + hourly scheduled sync
✅ **Hybrid Search** - 70% vector + 30% keyword for best results
✅ **Cost Efficient** - Batch processing, caching, ~$0.0002-$0.0004 per workspace
✅ **Production Ready** - Lock-based concurrency, retry logic, graceful degradation
✅ **CLI Tools** - Full command-line interface for ops
✅ **Performance Tracking** - P50/P95/P99 latency, cost analysis

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     ZIMA Memory System                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │ memory_search│  │  memory_get  │  │  Tool Calls  │    │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘    │
│         │                 │                 │             │
│         └─────────────────┼─────────────────┘             │
│                           │                               │
│                  ┌────────▼────────┐                      │
│                  │ Memory Service  │                      │
│                  └────────┬────────┘                      │
│                           │                               │
│         ┌─────────────────┼─────────────────┐            │
│         │                 │                 │            │
│  ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐     │
│  │ Hybrid      │  │ Indexer     │  │ Sync Manager│     │
│  │ Search      │  │             │  │             │     │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘     │
│         │                │                 │            │
│  ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐     │
│  │ Vector +    │  │ Chunker +   │  │ File Watcher│     │
│  │ BM25 Search │  │ Embeddings  │  │ + Scheduler │     │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
│                                                          │
│  ┌───────────────────────────────────────────────────┐  │
│  │ Performance Monitor + Batch Processor             │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  ┌───────────────────────────────────────────────────┐  │
│  │ SQLite Database (WAL + FTS5 + Vector Extension)   │  │
│  └───────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Indexing**: Workspace files → Chunker → Embeddings → Database
2. **Search**: Query → Hybrid Search → RRF Fusion → Ranked Results
3. **Sync**: File Changes → Debounce → Reindex → Update Database

---

## Quick Start

### 1. Installation

Dependencies are already installed. Just set your OpenAI API key:

```bash
export OPENAI_API_KEY=sk-ant-...
```

### 2. Build

```bash
npm run build
```

### 3. Test Memory System

```bash
# Show current status
node dist/memory/cli.js memory:status

# Index workspace
node dist/memory/cli.js memory:index

# Test search
node dist/memory/cli.js memory:search "how to configure the gateway"

# Show performance metrics
node dist/memory/cli.js memory:perf
```

### 4. Use in Agent

```bash
curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Use memory_search to find information about agents",
    "channel": "webchat",
    "senderId": "test"
  }'
```

---

## Configuration

### Environment Variables

```bash
# Memory System
MEMORY_ENABLED=true
MEMORY_DATABASE=/path/to/memory.db
OPENAI_API_KEY=sk-...
EMBEDDING_MODEL=text-embedding-3-small

# Synchronization
MEMORY_SYNC_INTERVAL=3600000  # 1 hour
MEMORY_WATCH_ENABLED=true

# Performance
MEMORY_CHUNK_SIZE=400
MEMORY_CHUNK_OVERLAP=80
MEMORY_VECTOR_WEIGHT=0.7  # 70% vector, 30% BM25
```

### Config File (~/.zima/config.json)

```json
{
  "memory": {
    "enabled": true,
    "database": "/home/zima/.zima/agents/main/memory.db",
    "embeddingProvider": "openai",
    "embeddingModel": "text-embedding-3-small",
    "chunkSize": 400,
    "chunkOverlap": 80,
    "syncInterval": 3600000,
    "vectorWeight": 0.7
  }
}
```

---

## Core Components

### 1. Memory Database

**File:** `src/memory/memory-database.ts`

SQLite database with:
- WAL mode for concurrency
- FTS5 full-text search index
- Vector embedding storage (BLOB)
- Automatic schema migrations
- Hash-based change detection

**Tables:**
- `meta` - Database version, sync state
- `files` - Workspace file tracking (path, hash, size, mtime)
- `chunks` - Text chunks (content, tokens, line numbers)
- `embedding_cache` - Vector embeddings (Float32Array → BLOB)
- `chunks_fts` - FTS5 virtual table for keyword search

### 2. Embedding Provider

**File:** `src/memory/embedding-provider.ts`

OpenAI integration:
- Model: `text-embedding-3-small` (1536 dimensions)
- Batch API support (up to 2048 texts)
- Rate limiting (3000 RPM)
- Cost tracking ($0.02/1M tokens)
- Automatic caching

### 3. Text Chunker

**File:** `src/memory/chunker.ts`

Smart text splitting:
- ~400 tokens per chunk (tiktoken-based counting)
- 80-token overlap between chunks
- Sentence boundary awareness
- Line number tracking for precise references

### 4. Hybrid Search

**File:** `src/memory/hybrid-search.ts`

Reciprocal Rank Fusion (RRF) algorithm:
- Vector similarity search (cosine distance)
- BM25 keyword search (FTS5)
- Configurable fusion weights (default: 70% vector, 30% BM25)
- Graceful fallback to BM25-only if embeddings unavailable

**RRF Formula:**
```
RRF_score = Σ (weight / (k + rank))
where k = 60 (RRF constant)
```

### 5. Sync Manager

**File:** `src/memory/sync-manager.ts`

Three sync modes:
1. **Scheduled** - Periodic background sync (configurable interval)
2. **Watch** - Real-time file monitoring
3. **Manual** - On-demand via CLI

Features:
- Lock-based concurrency control
- Event-driven progress tracking
- Graceful shutdown

### 6. File Watcher

**File:** `src/memory/file-watcher.ts`

Auto-detection of file changes:
- Recursive directory watching
- 2-second debounce
- Extension filtering (.md, .txt, .json, .yaml, .yml)
- Ignores: node_modules, .git, memory/, temp files

### 7. Performance Monitor

**File:** `src/memory/performance-monitor.ts`

Tracks:
- Search latency (P50, P95, P99)
- Embedding API usage and cost
- Database query performance
- Slowest queries and operations

### 8. Batch Processor

**File:** `src/memory/batch-processor.ts`

Optimizations:
- Queues embedding requests
- Batches up to 2048 texts per API call
- Exponential backoff retry logic
- Automatic flushing on shutdown

---

## CLI Commands

### memory:status

Show memory system status and index statistics.

```bash
node dist/memory/cli.js memory:status
```

**Output:**
```
╔════════════════════════════════════════════════════════╗
║         ZIMA Memory System - Status                   ║
╚════════════════════════════════════════════════════════╝

📊 Index Statistics:
   Files:      15
   Chunks:     127
   Embeddings: 127
   DB Size:    2.34 MB

⏱️  Last Sync:
   2h 15m ago
   2026-01-31 14:30:00

📈 Embedding Coverage: 100.0%
```

### memory:index

Force reindex workspace files.

```bash
# Full reindex (default)
node dist/memory/cli.js memory:index

# Incremental (only changed files)
node dist/memory/cli.js memory:index --incremental

# Skip embeddings (faster, BM25-only)
node dist/memory/cli.js memory:index --no-embeddings
```

### memory:search

Test search functionality.

```bash
node dist/memory/cli.js memory:search "configuration"
```

**Output:**
```
🔍 Searching: "configuration"

   Found 5 results:

   1. workspace/AGENTS.md (score: 0.892)
      Lines 45-52 (342 tokens)
      ## Configuration  The gateway is configured...

   2. workspace/TOOLS.md (score: 0.781)
      Lines 12-18 (189 tokens)
      ### Gateway Configuration  Environment...
```

### memory:sync

Manually trigger workspace sync.

```bash
node dist/memory/cli.js memory:sync
```

### memory:stats

Show detailed statistics and capabilities.

```bash
node dist/memory/cli.js memory:stats
```

### memory:performance (or memory:perf)

Show performance metrics and analysis.

```bash
node dist/memory/cli.js memory:perf
```

**Output:**
```
╔════════════════════════════════════════════════════════╗
║         Memory System - Performance Report            ║
╚════════════════════════════════════════════════════════╝

🔍 Search Performance:
   Total Queries:  42
   Avg Latency:    34.2ms
   P50 Latency:    28.5ms
   P95 Latency:    67.3ms
   P99 Latency:    89.1ms
   Slowest Query:  91.4ms
                   "how to configure the gateway..."

🤖 Embedding Performance:
   Total Requests: 12
   Total Tokens:   15,234
   Total Cost:     $0.0003
   Avg Latency:    456.7ms
   Avg Tokens:     1,270 per request

💾 Database Performance:
   Total Ops:      234
   Avg Latency:    3.2ms
   Operation Breakdown:
     - keywordSearch: 42
     - insertChunk: 127
     - insertEmbedding: 127
```

### memory:help

Show help message with all commands.

```bash
node dist/memory/cli.js memory:help
```

---

## API Reference

### MemoryService

```typescript
import { MemoryService } from './memory/memory-service';

const service = new MemoryService(config);

// Initialize (auto-indexes if needed)
await service.initialize();

// Hybrid search (vector + BM25)
const results = await service.hybridSearch("query", 5);

// BM25-only search (fallback)
const results = await service.keywordSearch("query", 5);

// Custom search with options
const results = await service.search("query", {
  limit: 10,
  vectorWeight: 0.8,
  minScore: 0.5
});

// Index workspace
const stats = await service.indexWorkspace({
  forceReindex: false,
  skipEmbeddings: false
});

// Get statistics
const stats = await service.getMemoryStats();

// Check capabilities
const caps = service.getCapabilities();
// { vectorSearch: true, bm25Search: true, hybridSearch: true }

// Cleanup
service.close();
```

### Tool Calls

#### memory_search

Search workspace files using hybrid search.

**Input:**
```json
{
  "query": "how to configure the gateway",
  "limit": 5
}
```

**Output:**
```json
{
  "results": [
    {
      "file": "workspace/AGENTS.md",
      "content": "## Configuration\n\nThe gateway is configured...",
      "score": 0.892,
      "lines": "45-52"
    }
  ],
  "total": 5,
  "query": "how to configure the gateway"
}
```

#### memory_get

Read workspace file with pagination.

**Input:**
```json
{
  "path": "SOUL.md",
  "offset": 0,
  "limit": 100
}
```

**Output:**
```json
{
  "path": "SOUL.md",
  "content": "# ZIMA Soul\n\nI am ZIMA...",
  "total_lines": 37,
  "offset": 0,
  "limit": 100,
  "returned_lines": 37
}
```

---

## Performance Tuning

### Search Performance

**Default Settings:**
- Vector weight: 0.7 (70% vector, 30% BM25)
- Chunk size: 400 tokens
- Chunk overlap: 80 tokens

**Tune for Speed:**
```json
{
  "memory": {
    "vectorWeight": 0.5,  // More BM25, less vector compute
    "chunkSize": 300,     // Smaller chunks = faster indexing
    "chunkOverlap": 50    // Less overlap = fewer chunks
  }
}
```

**Tune for Accuracy:**
```json
{
  "memory": {
    "vectorWeight": 0.9,  // More vector similarity
    "chunkSize": 500,     // Larger chunks = more context
    "chunkOverlap": 100   // More overlap = better coverage
  }
}
```

### Embedding Costs

**Minimize Costs:**
1. Enable incremental indexing (default)
2. Use `--no-embeddings` flag when testing
3. Reduce chunk overlap
4. Cache embeddings (automatic)

**Typical Costs:**
- Small workspace (10-20 files): $0.0002
- Medium workspace (50-100 files): $0.001
- Large workspace (200+ files): $0.005

### Database Size

**Optimize Database:**
```bash
# Vacuum database (reclaim space)
sqlite3 ~/.zima/agents/main/memory.db "VACUUM;"

# Rebuild FTS5 index
sqlite3 ~/.zima/agents/main/memory.db "INSERT INTO chunks_fts(chunks_fts) VALUES('rebuild');"
```

---

## Troubleshooting

### No Search Results

**Symptoms:** `memory_search` returns empty results

**Solutions:**
1. Check if workspace is indexed:
   ```bash
   node dist/memory/cli.js memory:status
   ```
2. Force reindex:
   ```bash
   node dist/memory/cli.js memory:index
   ```
3. Test with simpler query:
   ```bash
   node dist/memory/cli.js memory:search "configuration"
   ```

### Slow Search Performance

**Symptoms:** Search latency >500ms

**Solutions:**
1. Check P95/P99 latencies:
   ```bash
   node dist/memory/cli.js memory:perf
   ```
2. Reduce vector weight (more BM25):
   ```json
   { "vectorWeight": 0.5 }
   ```
3. Optimize database:
   ```bash
   sqlite3 ~/.zima/agents/main/memory.db "ANALYZE;"
   ```

### High Embedding Costs

**Symptoms:** OPENAI_API_KEY usage > expected

**Solutions:**
1. Check embedding metrics:
   ```bash
   node dist/memory/cli.js memory:perf
   ```
2. Enable incremental indexing (default)
3. Reduce workspace size (ignore large files)
4. Use `--no-embeddings` for testing

### File Watcher Not Working

**Symptoms:** Changes not detected automatically

**Solutions:**
1. Check watcher status:
   ```typescript
   const status = syncManager.getStatus();
   console.log(status.watchEnabled);
   ```
2. Manually trigger sync:
   ```bash
   node dist/memory/cli.js memory:sync
   ```
3. Check file extensions (only .md, .txt, .json, .yaml, .yml)

---

## Cost Management

### Embedding Costs

**OpenAI Pricing:**
- text-embedding-3-small: $0.02 per 1M tokens
- text-embedding-3-large: $0.13 per 1M tokens (not used)

**Optimization Strategies:**

1. **Incremental Indexing** (default)
   - Only changed files are reprocessed
   - Saves ~95% of embedding costs on subsequent syncs

2. **Caching** (automatic)
   - Embeddings stored in database
   - Never re-generate for same content

3. **Batch Processing** (automatic)
   - Up to 2048 texts per API call
   - Reduces API overhead

4. **Smart Chunking**
   - ~400 tokens per chunk (configurable)
   - Balances context vs cost

**Example Cost Calculation:**
```
Workspace: 20 files, 50K tokens total
Chunks: 125 chunks @ 400 tokens each
Cost: (50,000 / 1,000,000) × $0.02 = $0.001
```

### Database Storage

**Disk Usage:**
- Text chunks: ~1 KB per chunk
- Embeddings: ~6 KB per embedding (1536 floats)
- FTS5 index: ~50% of text size
- Total: ~7-8 KB per chunk

**Example Storage:**
```
125 chunks × 8 KB = 1 MB
```

---

## Production Deployment

### System Requirements

- **CPU:** 1+ cores (2+ recommended)
- **RAM:** 512 MB + (50 MB per 1000 chunks)
- **Disk:** 10 MB + (8 KB per chunk)
- **Network:** Internet access for OpenAI API

### Environment Setup

```bash
# Production environment variables
export NODE_ENV=production
export MEMORY_ENABLED=true
export OPENAI_API_KEY=sk-...
export MEMORY_DATABASE=/var/lib/zima/memory.db
export MEMORY_SYNC_INTERVAL=3600000  # 1 hour
export MEMORY_WATCH_ENABLED=true
```

### Starting Sync Manager

```typescript
// In src/server.ts
import { SyncManager } from './memory/sync-manager';

class Server {
  private syncManager?: SyncManager;

  async start() {
    // Start memory sync if enabled
    if (this.config.memory?.enabled) {
      this.syncManager = new SyncManager(this.config);
      await this.syncManager.start({
        enableScheduled: true,
        enableWatch: true
      });

      // Listen to sync events
      this.syncManager.on('sync-complete', (result) => {
        console.log(`✓ Auto-sync completed: ${result.stats?.totalFiles} files`);
      });

      this.syncManager.on('sync-error', (result) => {
        console.error(`❌ Auto-sync failed: ${result.error}`);
      });
    }
  }

  async shutdown() {
    if (this.syncManager) {
      this.syncManager.stop();
      this.syncManager.close();
    }
  }
}
```

### Monitoring

```bash
# Check status periodically
*/30 * * * * node /path/to/dist/memory/cli.js memory:status >> /var/log/zima/memory.log

# Performance report daily
0 0 * * * node /path/to/dist/memory/cli.js memory:perf >> /var/log/zima/performance.log

# Force sync weekly
0 0 * * 0 node /path/to/dist/memory/cli.js memory:sync
```

### Backup

```bash
# Backup database
cp ~/.zima/agents/main/memory.db ~/.zima/agents/main/memory.db.backup

# Backup with timestamp
cp ~/.zima/agents/main/memory.db \
   ~/.zima/agents/main/memory.db.$(date +%Y%m%d)
```

### Health Checks

```bash
# Check database integrity
sqlite3 ~/.zima/agents/main/memory.db "PRAGMA integrity_check;"

# Check index status
node dist/memory/cli.js memory:status | grep "Embedding Coverage"

# Should output: 100.0% (or close to it)
```

---

## Summary

The ZIMA Memory System is a production-ready semantic search solution with:

✅ **2,500+ lines of code** across 11 components
✅ **Hybrid search** (vector + BM25)
✅ **Auto-sync** (real-time + scheduled)
✅ **Performance tracking** (P50/P95/P99)
✅ **Cost optimization** (batch processing, caching)
✅ **CLI tools** (6 commands)
✅ **Production deployment** ready

**Total Implementation:** Week 7-8 Complete (Phases A, B, C)

---

**Last Updated:** 2026-01-31
**Version:** 1.0
**Status:** ✅ Production Ready
