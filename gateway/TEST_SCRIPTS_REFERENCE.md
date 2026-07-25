# ZIMA Gateway - Test Scripts Reference

Quick reference for all available test scripts.

## 📋 Overview

**Total Test Scripts:** 14 shell scripts + 6 TypeScript test files = **20 test resources**

---

## 🚀 Quick Start

### Run All Comprehensive Tests
```bash
./run-comprehensive-stream-tests.sh
```

### Run Existing Test Suite
```bash
npm test
```

---

## 📝 Shell Scripts (14)

### Main Test Scripts

#### 1. **test-comprehensive-streaming.sh** ⭐ NEW
**Description:** Comprehensive test of all system functionalities
**Coverage:** 20+ tests across 10 categories
**Usage:**
```bash
./test-comprehensive-streaming.sh
```

**Tests:**
- Task classification (Simple/Standard/Complex)
- Memory system (search, get)
- Web tools (search, fetch, browser)
- File operations (read, write)
- Command execution (safe, dangerous, background)
- Document generation (Excel, PDF)
- Multi-turn conversations
- Response caching
- Tool chaining
- Session management

---

#### 2. **run-comprehensive-stream-tests.sh** ⭐ NEW
**Description:** Interactive test runner
**Usage:**
```bash
./run-comprehensive-stream-tests.sh
```

**Features:**
- Checks if gateway is running
- Lets you choose shell/TypeScript/both
- Runs selected tests
- Shows reports location

---

### Individual Feature Tests

#### 3. **test-context.sh**
**Description:** Tests context manager and task classification
**Usage:**
```bash
./test-context.sh
```

**Tests:**
- Simple query → Haiku model
- Standard complexity → Sonnet model
- Cache verification

---

#### 4. **test-agent-runtime.sh**
**Description:** Tests agent runtime execution
**Usage:**
```bash
./test-agent-runtime.sh
```

**Tests:**
- Tool execution
- Model selection
- Agent loop

---

#### 5. **test-openclaw-prompt.sh**
**Description:** Tests OpenClaw system prompt generation
**Usage:**
```bash
./test-openclaw-prompt.sh
```

**Tests:**
- Prompt structure
- Tool listing
- System instructions

---

### Streaming Tests

#### 6. **test-stream-simple.sh**
**Description:** Simplest streaming test
**Usage:**
```bash
./test-stream-simple.sh
```

**Tests:**
- Basic POST to /api/chat/stream
- Simple prompt response

---

#### 7. **test-stream-endpoint.sh**
**Description:** Comprehensive streaming endpoint test
**Usage:**
```bash
./test-stream-endpoint.sh
```

**Tests:**
- ZIMA Core SSE endpoint
- Gateway WebSocket
- Hybrid agent runtime streaming

---

#### 8. **test-stream-final.sh**
**Description:** Final streaming verification
**Usage:**
```bash
./test-stream-final.sh
```

---

#### 9. **test-stream-workspace.sh**
**Description:** Streaming with workspace files
**Usage:**
```bash
./test-stream-workspace.sh
```

**Tests:**
- File context loading
- Workspace integration

---

### Workspace Tests

#### 10. **test-workspace-direct.sh**
**Description:** Direct workspace file access
**Usage:**
```bash
./test-workspace-direct.sh
```

---

#### 11. **test-workspace-files.sh**
**Description:** Workspace file operations
**Usage:**
```bash
./test-workspace-files.sh
```

---

#### 12. **test-updated-workspace.sh**
**Description:** Updated workspace tests
**Usage:**
```bash
./test-updated-workspace.sh
```

---

### Tool Tests

#### 13. **test-tools-expanded.sh**
**Description:** Tests expanded tool registry
**Usage:**
```bash
./test-tools-expanded.sh
```

**Tests:**
- 196+ ZIMA tools
- OpenClaw system tools
- Tool registry loading

---

### Core Tests

#### 14. **test-zima-core.sh**
**Description:** Tests ZIMA Core API connectivity
**Usage:**
```bash
./test-zima-core.sh
```

**Tests:**
- Health check
- Basic generate endpoint

---

## 🧪 TypeScript Test Files (6)

### Main Test Runner

#### 1. **tests/run-all-tests.ts**
**Description:** Master test runner for all TypeScript tests
**Usage:**
```bash
npm test
# or
ts-node tests/run-all-tests.ts
```

**Runs:**
- All E2E tests
- All performance tests
- All integration tests
- Generates TEST_REPORT.md

---

### E2E Tests

#### 2. **tests/e2e/01-memory-system.test.ts**
**Description:** Memory system end-to-end tests
**Usage:**
```bash
npm run test:memory
# or
ts-node tests/e2e/01-memory-system.test.ts
```

**Tests:**
- Workspace indexing
- Hybrid search
- BM25 keyword search
- Memory statistics

---

#### 3. **tests/e2e/02-web-tools.test.ts**
**Description:** Web tools end-to-end tests
**Usage:**
```bash
npm run test:web
# or
ts-node tests/e2e/02-web-tools.test.ts
```

**Tests:**
- Brave Search API
- Web fetch with Readability
- Browser automation (Playwright)

---

#### 4. **tests/e2e/03-exec-tools.test.ts**
**Description:** Exec & process tools tests
**Usage:**
```bash
npm run test:exec
# or
ts-node tests/e2e/03-exec-tools.test.ts
```

**Tests:**
- Safe command execution
- Dangerous command blocking
- Background process spawning
- Process termination

---

### Performance Tests

#### 5. **tests/performance/memory-load.test.ts**
**Description:** Memory system performance tests
**Usage:**
```bash
npm run test:perf
# or
ts-node tests/performance/memory-load.test.ts
```

**Tests:**
- Sequential search performance
- Parallel search performance
- 100 concurrent queries

---

### Integration Tests

#### 6. **tests/integration/comprehensive-stream.test.ts** ⭐ NEW
**Description:** Comprehensive streaming endpoint tests
**Usage:**
```bash
npm run test:comprehensive
# or
ts-node tests/integration/comprehensive-stream.test.ts
```

**Tests:** 22+ comprehensive tests across all system functionalities

**Generates:** `COMPREHENSIVE_STREAM_TEST_REPORT.md`

---

## 📊 Test Coverage Matrix

| Category | Shell Scripts | TypeScript Tests | Total |
|----------|--------------|------------------|-------|
| **Streaming Endpoints** | 5 | 1 | 6 |
| **Memory System** | 0 | 2 | 2 |
| **Web Tools** | 0 | 1 | 1 |
| **Exec Tools** | 0 | 1 | 1 |
| **Workspace** | 3 | 0 | 3 |
| **Context Manager** | 1 | 0 | 1 |
| **Tool Registry** | 1 | 0 | 1 |
| **Agent Runtime** | 1 | 0 | 1 |
| **Core Integration** | 1 | 0 | 1 |
| **Comprehensive** | 1 | 1 | 2 |
| **Test Runners** | 1 | 1 | 2 |
| **TOTAL** | **14** | **6** | **20** |

---

## 🎯 Recommended Test Workflow

### For Development
```bash
# 1. Quick smoke test
./test-stream-simple.sh

# 2. Test specific feature you're working on
./test-context.sh              # If working on context manager
npm run test:memory            # If working on memory system
npm run test:web               # If working on web tools

# 3. Run comprehensive tests before commit
./run-comprehensive-stream-tests.sh
```

### For CI/CD
```bash
# Run all TypeScript tests
npm test

# Run comprehensive streaming tests
npm run test:comprehensive
```

### For Production Verification
```bash
# 1. Check health
curl http://localhost:5555/health

# 2. Run comprehensive tests
./run-comprehensive-stream-tests.sh

# 3. Review reports
cat TEST_REPORT.md
cat COMPREHENSIVE_STREAM_TEST_REPORT.md
```

---

## 📈 Latest Test Results

### Standard Test Suite (TypeScript)
**From:** `TEST_REPORT.md`
**Status:** ✅ 100% Pass (13/13 tests)
**Categories:**
- Memory System (E2E): 4/4 ✅
- Web Tools (E2E): 3/3 ✅
- Exec & Process Tools (E2E): 5/5 ✅
- Memory System (Performance): 1/1 ✅

### Comprehensive Stream Tests
**Status:** Not yet run (awaiting gateway startup)
**Expected Coverage:** 22+ tests
**Categories:** 10 (all major functionalities)

---

## 🔧 Prerequisites

Before running tests:

1. **Start ZIMA Core:**
   ```bash
   cd /Volumes/DATA/QWEN/zima-file-service
   dotnet run
   ```

2. **Start Gateway:**
   ```bash
   cd /Volumes/DATA/QWEN/gateway
   npm start
   ```

3. **Set Environment Variables:**
   ```bash
   export ANTHROPIC_API_KEY=sk-ant-...
   export OPENAI_API_KEY=sk-...        # Optional
   export BRAVE_API_KEY=...            # Optional
   ```

---

## 📄 Generated Reports

| Report | Generator | Location |
|--------|-----------|----------|
| **TEST_REPORT.md** | run-all-tests.ts | `/Volumes/DATA/QWEN/gateway/TEST_REPORT.md` |
| **COMPREHENSIVE_STREAM_TEST_REPORT.md** | comprehensive-stream.test.ts | `/Volumes/DATA/QWEN/gateway/COMPREHENSIVE_STREAM_TEST_REPORT.md` |

---

## 🆘 Troubleshooting

### Test script not found
```bash
chmod +x test-*.sh run-*.sh
```

### Gateway not running
```bash
# Check if running
curl http://localhost:5555/health

# If not, start it
npm start
```

### Test timeout
- Increase timeout in test files
- Check API credits
- Use simpler prompts

### Test failed
1. Check error message
2. Review logs
3. Verify prerequisites
4. Run individual component tests

---

## 🚀 Adding New Tests

### Add to Shell Script
Edit `test-comprehensive-streaming.sh`:

```bash
test_endpoint \
    "Your Test Name" \
    "Your test prompt here"
```

### Add to TypeScript
Edit `tests/integration/comprehensive-stream.test.ts`:

```typescript
results.push(await runTest(
  'Your Test Name',
  'Category',
  'Your test prompt',
  undefined,
  (r) => r.output.includes('expected')
));
```

---

## 📚 Additional Resources

- **Testing Guide:** `COMPREHENSIVE_STREAM_TESTING_GUIDE.md`
- **Implementation Summary:** `WEEKS_7-10_COMPLETE_SUMMARY.md`
- **Memory System Docs:** `MEMORY_SYSTEM_PHASE_*.md`
- **Web Tools Docs:** `WEB_TOOLS_PHASE_9-10-A.md`
- **Exec Queue Docs:** `EXEC_QUEUE_PHASE_9-10-B.md`
- **Communication Docs:** `COMMUNICATION_PHASE_9-10-C.md`

---

**Last Updated:** 2026-01-31
**Version:** 1.0
**Total Test Scripts:** 20
