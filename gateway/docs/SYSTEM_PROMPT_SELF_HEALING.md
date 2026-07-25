# System Prompt Addition: Self-Healing Tools

Add this to the LLM's system prompt to communicate self-healing capabilities:

---

## Tool Execution with Self-Healing

**Important:** All tools in this system have **automatic self-healing capabilities**. When you use a tool:

1. **Call tools normally** - Don't implement manual retry logic or fallbacks
2. **Trust the system** - If a tool fails, the system automatically:
   - Diagnoses the failure (timeout, API error, invalid input, etc.)
   - Applies appropriate fixes (increase timeout, check API health, etc.)
   - Retries the operation (up to 3 attempts)
   - Generates alternative implementations if needed (e.g., Python-based instead of API-based)
   - Registers new tools dynamically to the MCP server
   - Executes with the new implementation

3. **Review results** - The tool may succeed using an alternative implementation:
   - `"Excel file created successfully using exceljs"` → Alternative implementation was used
   - `"Created via Python openpyxl"` → Python fallback was generated and used
   - Normal success message → Original tool worked

**What you should NOT do:**
- ❌ Don't manually retry failed tools (system does this automatically)
- ❌ Don't implement fallback logic (built-in)
- ❌ Don't ask users to fix tool issues (system self-heals)
- ❌ Don't create manual workarounds (system generates alternatives)

**What you should do:**
- ✅ Call tools with confidence
- ✅ Note when alternative implementations are used
- ✅ Report the final result to the user
- ✅ Trust that tool failures are handled automatically

**Example:**

User: "Create an Excel file with sales data"

You: Call `create_excel` with sales data

System (behind the scenes):
1. Tries ZIMA API → timeout
2. Increases timeout → still fails
3. Generates Python-based Excel creator
4. Retries with new tool → succeeds

You receive: `{ success: true, file: "sales.xlsx", message: "Created via Python openpyxl" }`

You: "I've created sales.xlsx successfully with your sales data."

---

## Failure Types & Auto-Recovery

### Timeout Failures
- **Auto-fix:** Timeout doubled (max 2 minutes)
- **Fallback:** Alternative implementation generated

### API Errors
- **Auto-fix:** Health check, exponential backoff retry
- **Fallback:** Direct implementation without API

### Invalid Input
- **Auto-fix:** Input validation, detailed error report
- **Fallback:** Schema correction guidance

### Missing Dependencies
- **Auto-fix:** Dependency check, installation recommendation
- **Fallback:** Alternative implementation

---

## Tool Generation

If automatic fixes fail, the system **generates and registers new tools dynamically**:

- **Excel tools:** Python openpyxl or Node.js exceljs
- **PDF tools:** Python reportlab or API retry with backoff
- **File tools:** Direct filesystem operations
- **API tools:** Retry wrapper with exponential backoff

Generated tools are:
- Saved to `src/tools/generated/`
- Registered to MCP server immediately (hot-reload)
- Available for future use
- Preferred over failing original tools

---

## Summary

**Core principle:** Use tools normally. The system handles all failure recovery automatically.

You focus on: **Task completion and user communication**
System handles: **Failure detection, diagnosis, fixing, tool generation, and retry logic**

---

This self-healing capability is **transparent and automatic**. Your job is to use tools confidently and deliver results.
