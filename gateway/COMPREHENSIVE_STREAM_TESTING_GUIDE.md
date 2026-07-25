# Comprehensive Stream Testing Guide

This guide explains how to run comprehensive tests on the ZIMA Gateway streaming endpoint to validate all major system functionalities.

## Test Coverage

The comprehensive streaming tests cover **20+ different scenarios** across 10 categories:

### 1. **Task Classification & Model Selection** (3 tests)
- ✅ Simple greeting → Haiku model
- ✅ Standard complexity → Sonnet model
- ✅ Complex multi-step → Opus/Sonnet model

### 2. **Memory System (OpenClaw)** (2 tests)
- ✅ Memory search (semantic + keyword)
- ✅ Memory get (file retrieval)

### 3. **Web Tools** (3 tests)
- ✅ Web search (Brave API)
- ✅ Web fetch (Readability content extraction)
- ✅ Browser automation (Playwright)

### 4. **File Operations** (2 tests)
- ✅ File write
- ✅ File read

### 5. **Command Execution** (3 tests)
- ✅ Safe command execution
- ✅ Dangerous command blocking (security)
- ✅ Background process spawning

### 6. **Document Generation (ZIMA Tools)** (2 tests)
- ✅ Excel file creation
- ✅ PDF document generation

### 7. **Multi-Turn Conversation (Context Management)** (3 tests)
- ✅ Context setup
- ✅ Context recall
- ✅ Extended context reasoning

### 8. **Response Caching** (2 tests)
- ✅ First request (no cache)
- ✅ Second identical request (cached)

### 9. **Tool Chaining** (1 test)
- ✅ Multiple tools in sequence (search → fetch → summarize)

### 10. **Session Management** (1 test)
- ✅ List active sessions

---

## Prerequisites

### 1. Start ZIMA Core (.NET Service)

```bash
cd /Volumes/DATA/QWEN/zima-file-service
dotnet run
```

**Verify it's running:**
```bash
curl http://localhost:5000/health
# Should return: {"status":"healthy"}
```

### 2. Start ZIMA Gateway (Node.js Service)

```bash
cd /Volumes/DATA/QWEN/gateway
npm run build
npm start
# Or: PORT=5555 npm start
```

**Verify it's running:**
```bash
curl http://localhost:5555/health
# Should return: {"status":"ok","version":"1.0.0"}
```

### 3. Set Required Environment Variables

Create/update `.env` file:

```bash
# Required
ANTHROPIC_API_KEY=sk-ant-...

# Optional (for web tools)
OPENAI_API_KEY=sk-...              # For embeddings/memory
BRAVE_API_KEY=...                   # For web search

# Optional (for communication)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your-email@gmail.com
SMTP_PASS=your-app-password
```

---

## Running the Tests

### Option 1: Interactive Runner (Recommended)

```bash
cd /Volumes/DATA/QWEN/gateway
./run-comprehensive-stream-tests.sh
```

This will:
1. Check if gateway is running
2. Let you choose between shell/TypeScript/both
3. Run the selected tests
4. Generate reports

### Option 2: Shell Script (Quick & Simple)

```bash
cd /Volumes/DATA/QWEN/gateway
./test-comprehensive-streaming.sh
```

**Pros:**
- Fast execution
- Simple curl-based
- Easy to read output
- No dependencies

**Cons:**
- Basic response parsing
- No structured report
- No detailed validation

### Option 3: TypeScript Test (Detailed & Structured)

```bash
cd /Volumes/DATA/QWEN/gateway
ts-node tests/integration/comprehensive-stream.test.ts
```

**Pros:**
- Detailed response parsing
- Structured validation
- Auto-generated markdown report
- Tool usage tracking
- Cache detection
- Model verification

**Cons:**
- Slower (more comprehensive)
- Requires ts-node

### Option 4: Add to npm scripts

The TypeScript test can be added to `package.json`:

```json
{
  "scripts": {
    "test:comprehensive": "ts-node tests/integration/comprehensive-stream.test.ts"
  }
}
```

Then run:
```bash
npm run test:comprehensive
```

---

## Understanding the Output

### Shell Script Output

```
════════════════════════════════════════════════════════
Test: Simple Greeting (Task Classification: Simple)
════════════════════════════════════════════════════════

Prompt: Hello! How are you today?

Response:
---
data: {"type":"content","content":"Hello! "}
data: {"type":"content","content":"I'm doing well"}
data: {"type":"complete","output":"Hello! I'm doing well...","model":"claude-3-5-haiku-20241022"}
---
```

### TypeScript Test Output

```
══════════════════════════════════════════════════════
🧪 Simple Greeting
📂 Category: Task Classification
══════════════════════════════════════════════════════
Prompt: Hello! How are you today?

✅ SUCCESS (1234ms)
Model: claude-3-5-haiku-20241022
Cached: No
Tools Used: None
Response Length: 156 chars
Response Preview: Hello! I'm doing well, thank you for asking...
```

---

## Generated Reports

### Markdown Report (TypeScript only)

Location: `/Volumes/DATA/QWEN/gateway/COMPREHENSIVE_STREAM_TEST_REPORT.md`

Includes:
- Executive summary (pass/fail rate)
- Results by category
- Detailed test results table
- Failed tests with error details
- Tool usage statistics
- Model selection validation

Example:

```markdown
# ZIMA Gateway - Comprehensive Streaming Test Report

**Generated:** 2026-01-31T10:30:00.000Z
**Status:** ✅ ALL PASSED

## Summary

- **Total Tests:** 22
- **Passed:** 22 ✅
- **Failed:** 0 ❌
- **Success Rate:** 100.0%

## Test Categories

### Task Classification

**Success Rate:** 100.0% (3/3)

| Test | Status | Duration | Model | Tools | Cached |
|------|--------|----------|-------|-------|--------|
| Simple Greeting | ✅ | 1234ms | claude-3-5-haiku-20241022 | None | No |
| Standard Complexity Query | ✅ | 3456ms | claude-sonnet-4-20250514 | None | No |
| Complex Multi-Step Task | ✅ | 8901ms | claude-opus-4-20250514 | None | No |
```

---

## Common Issues

### 1. Gateway Not Running

**Error:**
```
❌ Gateway is not running on port 5555
```

**Solution:**
```bash
cd /Volumes/DATA/QWEN/gateway
npm start
```

### 2. ZIMA Core Not Running

**Error:**
```
Tool execution failed: Connection refused to localhost:5000
```

**Solution:**
```bash
cd /Volumes/DATA/QWEN/zima-file-service
dotnet run
```

### 3. Missing API Keys

**Error:**
```
Anthropic API error: Invalid API key
```

**Solution:**
Add to `.env`:
```
ANTHROPIC_API_KEY=sk-ant-api03-...
```

### 4. Brave Search Not Available

**Warning:**
```
⚠️ Brave Search not available (no API key)
```

**Solution:**
This is optional. To enable:
```
BRAVE_API_KEY=your-brave-api-key
```

### 5. Memory Database Not Initialized

**Error:**
```
Memory search failed: Database not found
```

**Solution:**
Initialize memory system:
```bash
cd /Volumes/DATA/QWEN/gateway
node dist/memory/cli.js memory:index
```

### 6. Request Timeout

**Error:**
```
Request timeout after 60000ms
```

**Solutions:**
- Check if Claude API is accessible
- Verify API credits/quota
- Increase timeout in test file
- Use simpler prompts for initial testing

---

## Interpreting Results

### Success Indicators

✅ **All tests passed:**
- Gateway routing working
- Context manager operational
- Tool execution functional
- Model selection correct
- Caching working
- All integrations healthy

✅ **Most tests passed (>90%):**
- Core functionality working
- Minor issues with optional features (web search, browser)
- Safe to use in development

⚠️ **Some tests failed (70-90%):**
- Some integrations not working
- Check failed test categories
- Review error messages
- May need configuration updates

❌ **Many tests failed (<70%):**
- Major system issues
- Check if all services running
- Verify API keys
- Review logs for errors

### Model Selection Validation

Tests verify that the correct model is selected based on task complexity:

- **Simple tasks** → `claude-3-5-haiku-20241022` (fast, cheap)
- **Standard tasks** → `claude-sonnet-4-20250514` (balanced)
- **Complex tasks** → `claude-opus-4-20250514` (powerful)

### Cache Detection

The test suite validates response caching:

- **First request:** `Cached: No`
- **Identical request:** `Cached: Yes` (within TTL)

### Tool Usage Tracking

The TypeScript test tracks which tools were called:

```
Tools Used: web_search, web_fetch, memory_search
```

This validates:
- Tool registry is working
- Tool execution is functional
- Tool routing is correct

---

## Advanced Usage

### Run Specific Test Categories

Edit the TypeScript test file to comment out unwanted categories:

```typescript
// results.push(await runTest(...)); // Skip this test
```

### Custom Validation

Add custom validators to tests:

```typescript
results.push(await runTest(
  'My Custom Test',
  'Custom Category',
  'My test prompt',
  undefined,
  (r) => {
    // Custom validation logic
    return r.output.includes('expected text') &&
           r.toolsUsed.includes('expected_tool');
  }
));
```

### Adjust Timeouts

Modify timeout for slow tests:

```typescript
const TIMEOUT = 120000; // 2 minutes
```

### Test Different Models

Force specific model usage:

```typescript
// In streamRequest function
{
  message: prompt,
  model: 'claude-opus-4-20250514', // Force Opus
  ...
}
```

---

## Performance Benchmarks

Expected performance (based on test history):

| Test Category | Avg Duration | P95 Duration |
|---------------|--------------|--------------|
| Simple tasks (Haiku) | 1-2s | 3s |
| Standard tasks (Sonnet) | 3-5s | 8s |
| Complex tasks (Opus) | 8-15s | 20s |
| Memory search | <100ms | 200ms |
| Web search | 500ms-2s | 3s |
| Web fetch | 1-3s | 5s |
| Browser automation | 3-6s | 10s |
| File operations | <100ms | 200ms |
| Command execution | <500ms | 1s |
| Document generation | 2-5s | 8s |

---

## Continuous Integration

To run in CI/CD:

```yaml
# .github/workflows/test.yml
name: Comprehensive Stream Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      redis:
        image: redis:7-alpine
        ports:
          - 6379:6379
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-node@v2
        with:
          node-version: '20'
      - uses: actions/setup-dotnet@v2
        with:
          dotnet-version: '9.0'

      - name: Start ZIMA Core
        run: |
          cd zima-file-service
          dotnet run &
          sleep 10

      - name: Start Gateway
        run: |
          cd gateway
          npm install
          npm run build
          npm start &
          sleep 10
        env:
          ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}

      - name: Run Tests
        run: |
          cd gateway
          npm run test:comprehensive
```

---

## Support

If tests fail consistently:

1. **Check logs:**
   ```bash
   # Gateway logs
   cd /Volumes/DATA/QWEN/gateway
   tail -f logs/*.log

   # ZIMA Core logs
   cd /Volumes/DATA/QWEN/zima-file-service
   # Check console output
   ```

2. **Verify services:**
   ```bash
   # Gateway health
   curl http://localhost:5555/health

   # ZIMA Core health
   curl http://localhost:5000/health

   # Gateway stats
   curl http://localhost:5555/api/stats
   ```

3. **Test individual components:**
   ```bash
   # Memory system
   npm run test:memory

   # Web tools
   npm run test:web

   # Exec tools
   npm run test:exec
   ```

4. **Check test report:**
   ```bash
   cat COMPREHENSIVE_STREAM_TEST_REPORT.md
   ```

---

## Next Steps

After running comprehensive tests:

1. ✅ Review the test report
2. ✅ Fix any failed tests
3. ✅ Run tests again to verify fixes
4. ✅ Add tests to CI/CD pipeline
5. ✅ Monitor production performance
6. ✅ Set up alerting for failures

---

**Last Updated:** 2026-01-31
**Version:** 1.0
**Maintainer:** ZIMA Team
