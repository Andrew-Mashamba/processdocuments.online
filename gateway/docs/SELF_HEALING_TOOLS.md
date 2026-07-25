# Self-Healing Tool System

## Overview

The Gateway system includes **automatic self-healing capabilities** for tool execution. When tools fail, the system automatically:

1. **Diagnoses** the failure
2. **Attempts to fix** the tool
3. **Retries** the operation
4. **Generates new tool** if fixing fails
5. **Registers** the new tool dynamically
6. **Retries** with the new implementation

**You (the LLM) do not need to handle tool failures manually.** The system handles recovery automatically.

---

## How It Works

### Standard Execution Flow

When you call a tool (e.g., `create_excel`):

```
1. Tool executes
2. If success → Return result ✅
3. If failure → Auto-healing begins 🔧
```

### Self-Healing Flow

```
Tool Fails
    ↓
📝 Log failure (type, error, timing)
    ↓
🔍 Diagnose root cause
    ↓
🔧 Apply fix strategy
    ↓
🔄 Retry tool execution
    ↓
Success? → Return result ✅
    ↓
Still failing?
    ↓
🔨 Generate new tool implementation
    ↓
📋 Register to MCP server
    ↓
🔄 Retry with new tool
    ↓
Success? → Return result ✅
    ↓
Still failing? → Return detailed error report
```

---

## Failure Types & Automatic Fixes

### 1. Timeout Failures

**Symptoms:**
- Tool takes longer than timeout threshold
- Error contains "timeout" or "timed out"

**Automatic Fix:**
- Increases timeout configuration (doubles timeout, max 2 minutes)
- Retries with new timeout

**Example:**
```
create_excel times out (30s)
  → System increases timeout to 60s
  → Retries automatically
```

### 2. API Connection Errors

**Symptoms:**
- ECONNREFUSED, ETIMEDOUT
- API not responding
- Network errors

**Automatic Fixes:**
1. Check API health endpoint
2. If API down → Suggest restart (logs recommendation)
3. Apply exponential backoff retry
4. If still failing → Generate alternative implementation (e.g., Python-based instead of API-based)

**Example:**
```
create_excel → ZIMA API timeout
  → System checks API health
  → API not responding
  → Generates Python-based Excel creator
  → Retries with Python implementation ✅
```

### 3. Missing Dependencies

**Symptoms:**
- "Cannot find module"
- "ENOENT" (file not found)
- Missing library errors

**Automatic Fix:**
- Validates required dependencies
- Logs missing dependencies
- (Future: Auto-install dependencies)

### 4. Invalid Input

**Symptoms:**
- "Invalid parameter"
- "Validation error"
- Missing required fields

**Automatic Fix:**
- Validates input against tool schema
- Identifies missing fields
- Provides detailed validation report

### 5. Execution Errors

**Symptoms:**
- Tool-specific errors
- Unexpected exceptions

**Automatic Fix:**
- Analyzes error pattern
- Applies retry with exponential backoff
- Generates alternative implementation if persistent

---

## Tool Generation

When automatic fixes don't work, the system **generates a new tool implementation**.

### Available Tool Templates

1. **Excel Creator (exceljs)** - Node.js library-based Excel creation
2. **Excel Creator (Python)** - Python openpyxl-based Excel creation
3. **API Wrapper with Retry** - Wraps API calls with retry logic
4. **File Operation Tool** - Generic file read/write/delete

### Example: Auto-Generated Tool

Original tool: `create_excel` (via ZIMA API)

```typescript
// Fails with API timeout
create_excel({ filename: "test.xlsx", data: [...] })
// ❌ ZIMA API timeout after 30s
```

System generates: `create_excel_generated` (using exceljs)

```typescript
// Auto-generated implementation
import * as ExcelJS from 'exceljs';

export async function create_excel_generated(input: any): Promise<any> {
  const workbook = new ExcelJS.Workbook();
  // ... implementation using exceljs library ...
  await workbook.xlsx.writeFile(filePath);
  return { success: true, file_path: filePath };
}
```

Next execution automatically uses new tool:

```typescript
create_excel({ filename: "test.xlsx", data: [...] })
// ✅ Success using exceljs implementation
```

---

## What You (LLM) Need to Know

### ✅ Do This

1. **Call tools normally** - Don't worry about failures
   ```
   Use create_excel to create a spreadsheet
   ```

2. **Trust the self-healing** - System handles retries
   ```
   If create_excel fails, it will:
   - Try fixing the timeout
   - Generate Python-based alternative
   - Return success with new implementation
   ```

3. **Check tool results** - Sometimes new implementation is used
   ```
   Result: "Excel file created successfully using exceljs: test.xlsx"
   (Note: "using exceljs" indicates auto-generated tool was used)
   ```

### ❌ Don't Do This

1. **Don't manually retry tools** - System does this automatically
   ```
   ❌ BAD:
   Try create_excel
   If fails, try Python script manually
   If fails, try another approach

   ✅ GOOD:
   Use create_excel
   (System handles retries and alternatives)
   ```

2. **Don't implement fallback logic** - Built-in
   ```
   ❌ BAD:
   try {
     create_excel(...)
   } catch {
     // Manual Python fallback
   }

   ✅ GOOD:
   create_excel(...)
   (Fallback happens automatically)
   ```

3. **Don't ask user to fix tool issues** - System self-heals
   ```
   ❌ BAD:
   "The ZIMA API is not responding. Please restart the service."

   ✅ GOOD:
   (System attempts fix, generates alternative, returns result)
   ```

---

## Monitoring & Transparency

### Logs You'll See

When self-healing activates, you'll see:

```
🔧 [Self-Healing] Executing tool: create_excel (attempt 1/3)
⚠️  [Self-Healing] Tool returned error: ZIMA API timeout
📝 [Self-Healing] Logging failure: TIMEOUT

🔍 [Self-Healing] Max retries reached. Attempting self-healing...
🔧 Attempting to fix tool: create_excel
   Failure type: timeout
   Error: Request timeout after 30000ms

   Applying strategy: increase_timeout
   ✓ Increased timeout: 30000ms → 60000ms

🔄 [Self-Healing] Fix applied. Retrying tool execution...
⚠️  [Self-Healing] Tool still failing after fix

🔨 [Self-Healing] Generating new tool implementation...
   Auto-selected template: excel_creator_exceljs
   ✅ Generated tool code: src/tools/generated/create_excel_generated.ts
   ✅ Generated tool definition: src/tools/generated/create_excel_generated.json
   ✅ Tool registered to MCP server

🔄 [Self-Healing] Retrying with generated tool...
✅ [Self-Healing] Generated tool succeeded!
```

### Success Indicators

- `✅ [Self-Healing] Tool executed successfully` - Normal success
- `✅ [Self-Healing] Tool succeeded after fix!` - Succeeded after timeout fix
- `✅ [Self-Healing] Generated tool succeeded!` - Succeeded with new implementation

### Failure Indicators

If all recovery attempts fail:

```
❌ [Self-Healing] All recovery attempts exhausted for create_excel

📊 [Self-Healing] Failure Report:
====================================================================
Tool Failure Report: create_excel
====================================================================

Total Failures: 5
Average Execution Time: 30234.50ms

Failure Types:
  - timeout: 5 (100.0%)

Recent Failures:
  1. 2026-01-31T15:30:45.123Z
     Type: timeout
     Error: Request timeout after 30000ms
     Duration: 30001ms
...
```

---

## Configuration

Self-healing can be configured:

```typescript
const healingConfig = {
  maxRetries: 3,              // Max retry attempts
  enableAutoFix: true,        // Enable automatic fixing
  enableAutoGenerate: true,   // Enable tool generation
  logDir: './logs/tools',     // Failure logs directory
  configDir: './config/tools' // Tool config directory
};
```

---

## MCP Hot-Reload

The MCP server supports **hot-reload** for generated tools:

1. Tool is generated → Saved to `src/tools/generated/`
2. MCP server detects new file
3. Tool registry refreshes automatically
4. New tool available immediately (no restart needed)

You can verify with:

```bash
claude mcp list
# Shows updated tool count including generated tools
```

---

## Summary for LLM

**Key Principle:** Call tools normally. The system handles failures automatically.

- ✅ Tools self-diagnose and self-heal
- ✅ Timeouts are auto-increased
- ✅ API failures trigger alternative implementations
- ✅ New tools are generated and registered dynamically
- ✅ You receive working results without manual intervention

**Your role:** Use tools confidently. Trust the self-healing system to handle failures.

**System's role:** Detect failures, diagnose, fix, generate alternatives, and deliver results.

---

## Examples

### Example 1: Successful Self-Healing

**You request:**
```
Create an Excel file called "sales_report.xlsx" with quarterly sales data
```

**What happens:**
1. System calls `create_excel` via ZIMA API
2. ZIMA API times out (30s)
3. System increases timeout to 60s
4. Still times out
5. System generates `create_excel_generated` using exceljs
6. New tool succeeds
7. You receive: "Created sales_report.xlsx successfully"

**You see:** Successful result (no failure visible to you)

### Example 2: All Recovery Fails

**You request:**
```
Create a PDF from invalid data
```

**What happens:**
1. System tries `create_pdf`
2. Validation fails (invalid data)
3. System retries 3x
4. System attempts fix (input validation)
5. All attempts fail
6. You receive detailed failure report

**You see:**
```
Tool create_pdf failed after 3 attempts and self-healing.
Error: Invalid input: missing required field 'content'

Failure Report:
- Total failures: 3
- Failure type: invalid_input (100%)
- Recommendation: Verify input parameters match schema
```

---

**Last Updated:** 2026-01-31
**Version:** 1.0.0
