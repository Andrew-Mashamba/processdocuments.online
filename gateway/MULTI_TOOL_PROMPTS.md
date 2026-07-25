# Multi-Tool Test Prompts - Ready to Use

These prompts require multiple tools and test the complete MCP tool execution pipeline.

---

## 🟢 EASY: 2 Tools (Start here!)

### Prompt 1: Create + Verify

```
Create an Excel file called "fruits.xlsx" with 3 rows of fruit data (name, price, quantity).
After creating it, use get_file_info to verify the file was created successfully.
```

**Expected tools:**
1. `create_excel` - Create the spreadsheet
2. `get_file_info` - Verify file exists

**Expected output:**
```
Created fruits.xlsx with 3 rows of data.
File info: Size: 5.2 KB, Created: 2026-01-31, Type: Excel
```

---

### Prompt 2: Memory + Document

```
Search my memory for information about Alice, then create a Word document
called "alice_profile.docx" with her name and preferences as the content.
```

**Expected tools:**
1. `memory_search` - Find Alice
2. `create_word` - Create Word doc

**Expected output:**
```
Found in memory: Alice likes cats
Created alice_profile.docx with her profile information
```

---

## 🟡 MEDIUM: 3-4 Tools

### Prompt 3: Search + Create + Verify

```
First, search my memory for "Alice" using memory_search.
Then create an Excel file "alice_info.xlsx" with columns: Name, Preferences, Date.
Fill it with data from memory.
Finally, read the Excel file using read_excel to verify the content is correct.
```

**Expected tools:**
1. `memory_search` - Search for Alice
2. `create_excel` - Create spreadsheet
3. `read_excel` - Verify content

**Expected output:**
```
Memory search found: Alice (likes cats)
Created alice_info.xlsx with 3 columns
Verification: File contains 1 row with Alice's data
```

---

### Prompt 4: Data Pipeline

```
Create an Excel file "products.xlsx" with 5 sample products (name, price, stock).
Convert it to JSON using excel_to_json.
Then read the JSON content using read_file_content to show me what's in it.
Finally, list all files in the generated_files directory.
```

**Expected tools:**
1. `create_excel` - Create products.xlsx
2. `excel_to_json` - Convert to JSON
3. `read_file_content` - Read JSON
4. `list_files` - List all files

**Expected output:**
```
Created products.xlsx with 5 products
Converted to products.json
JSON content: [{"name":"Product1","price":9.99,...}]
Generated files: products.xlsx, products.json (2 files)
```

---

## 🔴 HARD: 5+ Tools

### Prompt 5: Complete Document Workflow

```
I need you to create a comprehensive product report:

1. Create an Excel file "inventory.xlsx" with 10 products (name, price, quantity, category)
2. Convert it to PDF using excel_to_pdf
3. Get the PDF info using get_pdf_info to check page count
4. Create a Word document "report_summary.docx" that includes:
   - Title: "Inventory Report Summary"
   - A paragraph describing the inventory
   - Total number of products
   - PDF page count from step 3
5. Convert the Word doc to PDF using word_to_pdf
6. List all generated files using list_files

Show me a summary of everything created.
```

**Expected tools:**
1. `create_excel` - Create inventory
2. `excel_to_pdf` - Convert to PDF
3. `get_pdf_info` - Get PDF metadata
4. `create_word` - Create summary doc
5. `word_to_pdf` - Convert to PDF
6. `list_files` - List all files

**Expected output:**
```
Created:
- inventory.xlsx (10 products)
- inventory.pdf (2 pages)
- report_summary.docx (summary document)
- report_summary.pdf (1 page)

Total: 4 files generated
PDF pages: 3 total
```

---

### Prompt 6: Memory + Research + Report

```
Create a research report about cats:

1. Search my memory for any notes about cats or Alice's preferences using memory_search
2. Search the web for "cat care tips 2026" using web_search
3. Create a Word document "cat_research.docx" with:
   - Section 1: What I know (from memory)
   - Section 2: Latest tips (from web)
4. Convert to PDF using word_to_pdf
5. Calculate the file's checksum using calculate_checksum
6. List all files using list_files

Provide a summary of findings.
```

**Expected tools:**
1. `memory_search` - Search workspace
2. `web_search` - Research online
3. `create_word` - Create report
4. `word_to_pdf` - Convert to PDF
5. `calculate_checksum` - Get MD5/SHA256
6. `list_files` - Show files

**Expected output:**
```
Memory: Found Alice likes cats
Web: Found 5 cat care tips
Created cat_research.docx (2 sections)
Converted to cat_research.pdf
Checksum (SHA256): a3f5c8d9...
Files: 2 total (docx, pdf)
```

---

## How to Test

### Option 1: Claude CLI (MCP)

```bash
# Easy test
claude "Create an Excel file called fruits.xlsx with 3 fruits, then verify it was created using get_file_info"

# Medium test
claude "Search my memory for Alice, create an Excel with her info, then verify the content"

# Hard test
claude "Create Excel with products, convert to PDF, get PDF info, create summary Word doc, convert to PDF, list all files"
```

### Option 2: Gateway API

```bash
# Run the test script
chmod +x /Volumes/DATA/QWEN/gateway/test-multi-tool-execution.sh
./test-multi-tool-execution.sh
```

### Option 3: Manual via curl

```bash
curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Create Excel with fruits, then verify file exists",
    "channel": "webchat",
    "senderId": "test",
    "sessionKey": "multi-tool-'$(date +%s)'"
  }' | jq -r '.output'
```

---

## What to Look For

### ✅ Success Indicators

1. **Multiple tools mentioned** in response
   ```
   "Created Excel file..."
   "Verified file exists..."
   "Read content successfully..."
   ```

2. **Sequential execution** visible
   ```
   First, I searched memory...
   Then, I created the Excel file...
   Finally, I verified...
   ```

3. **Tool results chained** correctly
   ```
   Found "Alice" in memory
   Used that data to create alice_info.xlsx
   Verified: 1 row with Alice's preferences
   ```

4. **MCP server logs** show multiple executions
   ```
   🔧 [MCP] Executing tool: memory_search
   ✓ [MCP] Tool executed successfully
   🔧 [MCP] Executing tool: create_excel
   ✓ [MCP] Tool executed successfully
   ```

### ❌ Failure Indicators

1. **Only first tool executes**
   ```
   "I searched memory and found Alice"
   [No mention of Excel creation]
   ```

2. **Tools skipped**
   ```
   "I'll create the Excel file"
   [File not actually created, just mentioned]
   ```

3. **Generic responses**
   ```
   "I can help with that..."
   [No actual tool execution]
   ```

4. **Error messages**
   ```
   "Tool execution failed"
   "Tool not available"
   ```

---

## Recommended Test Order

1. **Start:** Prompt 1 (2 tools) - Verify basic chaining works
2. **Next:** Prompt 3 (3 tools) - Test with memory integration
3. **Then:** Prompt 4 (4 tools) - Test data pipeline
4. **Finally:** Prompt 5 (6 tools) - Test complex workflow

---

## Expected Timeline

- **2 tools:** ~5-15 seconds
- **3 tools:** ~15-30 seconds
- **4 tools:** ~30-60 seconds
- **6+ tools:** ~60-120 seconds

**Note:** ZIMA API calls add ~1-3 seconds per document operation

---

## Quick Copy-Paste Prompts

**Easy:**
```
Create an Excel file called "test.xlsx" with 3 rows of data, then verify it exists using get_file_info
```

**Medium:**
```
Search my memory for Alice, create an Excel file with her information, then read it back to verify
```

**Hard:**
```
Create Excel with products, convert to JSON, read JSON content, create PDF summary, list all files
```

---

**Ready to test!** Start with the easy prompts and work your way up. 🚀
