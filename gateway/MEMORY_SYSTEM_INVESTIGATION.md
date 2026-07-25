# Memory System Investigation - Complete Analysis

**Date:** 2026-01-31
**Status:** ⚠️ **CRITICAL FINDING - memory_search Tool Not Available to Claude CLI**

---

## Executive Summary

The memory system **infrastructure is fully implemented and working**, but the `memory_search` tool **is not accessible to Claude CLI**, which is the runtime being used for agent execution.

**Key Finding:** Tools are loaded into the gateway's registry but **never passed to Claude CLI process**.

---

## Test Results

### Test 1: Implicit Memory Search ✅ (But Not Using memory_search)
**Prompt:** "What do you know about my preferences and who I am? Search your memory."
**Result:** Found Alice's information successfully
**Method Used:** Claude CLI's built-in file reading tools (Read/Grep), NOT memory_search
**Duration:** 19s, 5 turns

### Test 2: After Indexing MEMORY.md ✅ (But Not Using memory_search)
**Prompt:** "Search your memory for information about Alice. What are her preferences?"
**Result:** Found Alice from MEMORY.md successfully
**Method Used:** Claude CLI's built-in tools
**Duration:** 19s, 4 turns

### Test 3: Explicit memory_search Request ❌ **CRITICAL**
**Prompt:** "Use the memory_search tool to find all information about cats"
**Result:**
```
**Result**: The `memory_search` tool referenced in the operational
manual (AGENTS.md) is **not currently available** in this environment.
```
**Duration:** 42s, 10 turns
**Evidence:** Claude CLI explicitly states the tool does not exist

---

## Root Cause Analysis

### Architecture Issue

**File:** `src/agent/claude-cli-runtime.ts`

**Lines 38-41:**
```typescript
// Load tools
await this.registry.refresh();
const tools = await this.registry.getTools();
console.log(`✓ Loaded ${tools.length} tools`); // Shows 216 tools
```

**Lines 47-54:**
```typescript
const proc = spawn('claude', [
  '--print',
  '--output-format', 'json',
  '--dangerously-skip-permissions'
], {
  cwd: this.config.storage?.workspace || process.cwd(),
  stdio: ['pipe', 'pipe', 'pipe']
});
```

**Lines 238-258 (buildPrompt):**
```typescript
private buildPrompt(context: AgentContext): string {
  let prompt = '';

  // Add system prompt
  if (context.systemPrompt) {
    prompt += context.systemPrompt + '\n\n';
  }

  // Add conversation history + current message
  // ...

  return prompt;
}
```

### The Problem

1. **Tools loaded:** 216 tools loaded into gateway registry ✅
2. **Tools passed to Claude CLI:** **0 tools** ❌
3. **buildPrompt():** Only includes text prompt, NO tool definitions
4. **spawn():** No `--tools` or similar argument

**Result:** Claude CLI runs with only its **built-in tools** (Read, Write, Bash, Grep, Glob, Edit, etc.)

---

## Memory System Component Status

| Component | Status | Evidence |
|-----------|--------|----------|
| **Memory Database** | ✅ Working | 131 KB, 13 chunks from 5 files |
| **MEMORY.md File** | ✅ Created | 375 bytes, contains Alice info |
| **Indexer** | ✅ Working | Successfully indexed workspace files |
| **HybridSearch** | ✅ Implemented | Vector + BM25 search code exists |
| **memory_search Tool** | ✅ Defined | Registered in tool-registry.ts:356 |
| **Tool Passing** | ❌ **NOT IMPLEMENTED** | Tools not passed to Claude CLI |
| **memory_search Available** | ❌ **NOT AVAILABLE** | Claude CLI cannot see it |

---

## Database Verification

### Files Indexed:
```bash
$ sqlite3 storage/memory.db "SELECT path, chunks FROM ..."

AGENTS.md     | 5 chunks
MEMORY.md     | 1 chunk  ← Contains Alice info
README.md     | 3 chunks
SOUL.md       | 2 chunks
TOOLS.md      | 2 chunks
```

### MEMORY.md Content (Indexed):
```markdown
## User Information
**Name**: Alice
**Preferences**:
- Likes cats

## Project Context
Working directory: /Volumes/DATA/QWEN/gateway
Git repo: dev/major-refactor branch
```

**Status:** ✅ Properly indexed and searchable (if tool were available)

---

## What Actually Works

### ✅ Session Context Recall
- Session transcripts in `~/.zima/agents/main/sessions/*.jsonl`
- Claude recalls information within same session perfectly
- Test 8 (Multi-Turn) used this successfully

### ✅ Claude CLI Built-in Tools
- Read, Write, Grep, Glob, Bash, Edit (20+ tools)
- Can search files manually
- This is what Claude used to find Alice's info

### ❌ Gateway Custom Tools
- memory_search, memory_get, web_search, web_fetch, browser, etc.
- Defined in registry but **inaccessible**
- Not passed to Claude CLI process

---

## Implementation Gap

### Current Flow:
```
Gateway loads tools (216) → Claude CLI spawned → Text prompt sent
                         ↓
                         Tools NOT passed ❌
                         ↓
                         Claude CLI uses only built-in tools
```

### Required Flow:
```
Gateway loads tools (216) → Claude CLI spawned → Tools passed ✅
                         ↓
                         Claude CLI has access to all tools
                         ↓
                         memory_search available
```

---

## Workspace Directory Issue

**Two separate workspaces identified:**

1. **Gateway workspace:** `/Volumes/DATA/QWEN/gateway/workspace/`
   - Contains indexed files (AGENTS.md, SOUL.md, TOOLS.md, README.md)
   - Memory indexer targets this directory ✅

2. **Claude CLI workspace:** `~/.zima/workspace/`
   - Where Claude created MEMORY.md initially
   - Not indexed by default ❌

**Solution:** MEMORY.md manually copied to gateway workspace and re-indexed ✅

---

## Evidence Summary

### Logs:
```bash
$ grep "memory_search" /tmp/gateway.log
# Result: EMPTY (no matches)
```

**No memory_search tool was ever invoked** during any test.

### Claude CLI Output (Test 3):
```
The `memory_search` tool referenced in the operational manual
(AGENTS.md) is **not currently available** in this environment.

This appears to be aspirational documentation for a feature
that hasn't been built yet.
```

**Claude CLI is correct** - from its perspective, the tool doesn't exist.

---

## Conclusion

### What the Tests Actually Validated:

✅ **Session transcripts** - Multi-turn context persistence works
✅ **File reading** - Claude CLI built-in tools work
✅ **Memory database** - Storage layer works
✅ **Indexing** - Workspace files indexed correctly
✅ **ZIMA tools** - Document generation works (routed via API)

❌ **memory_search tool** - Not available to Claude CLI
❌ **Custom OpenClaw tools** - Not passed to runtime

### Upgrade Plan Status:

**Week 7-8: Memory System**
- ✅ SQLite + vector database: Implemented
- ✅ Hybrid search (vector + BM25): Implemented
- ✅ Workspace indexing: Implemented
- ⚠️ **Tool integration: NOT IMPLEMENTED**
- **Status: 75% Complete**

---

## Recommendations

### Fix Required:

**Option 1:** Pass tools to Claude CLI (if supported)
- Research if Claude CLI accepts custom tool definitions
- Modify spawn() call to include tools

**Option 2:** Implement tool interception
- Gateway intercepts Claude CLI output
- Detects tool calls
- Executes tools via ToolExecutor
- Returns results to Claude CLI

**Option 3:** Use Anthropic API instead
- Switch from Claude CLI to direct API
- Full tool support out of the box
- May hit rate limits

### Immediate Workaround:

For now, Claude CLI's built-in file tools (Read, Grep) can search memory files manually, which is what happened in the successful tests. This works but bypasses the semantic search capabilities.

---

**Last Updated:** 2026-01-31 14:10
**Investigation By:** Claude Sonnet 4.5
**Gateway Status:** memory_search infrastructure exists but not connected to runtime
