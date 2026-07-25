# System Prompt Update - File Handling Instructions

**Date:** 2026-01-31
**File Modified:** `src/agent/openclaw-system-prompt.ts`
**Method Updated:** `buildToolUsageSection()`

## Summary

Updated the system prompt to include comprehensive instructions on:
1. How MCP tools work and are accessed
2. **Where and how to save generated files** (critical addition)
3. **How to generate proper download links** (critical addition)

## What Was Added

### 1. MCP Tools Explanation
Added clarification that all tools are provided via MCP (Model Context Protocol) server and accessible through Claude CLI's tool calling system.

### 2. File Saving Instructions (NEW - CRITICAL)

Based on analysis of ZIMA file service codebase, added detailed section:

#### **Primary Save Location**
```
/Volumes/DATA/QWEN/zima-file-service/generated_files/
```
All generated files MUST be saved to this directory.

#### **Session-Organized Files** (Recommended)
```
/Volumes/DATA/QWEN/zima-file-service/generated_files/{sessionId}/
```
Organizes files by conversation/session for better management.

#### **File Naming**
- Use descriptive names: `budget_2026.xlsx`, `meeting_notes.pdf`
- System auto-versions if exists: `file_v2.xlsx`, `file_v3.xlsx`
- Avoid special characters

### 3. Download Link Generation (NEW - CRITICAL)

Added three download URL formats with examples:

#### **Format A: API Download** (Recommended)
```
http://localhost:5000/api/files/{filename}/download
```
Example: `http://localhost:5000/api/files/budget_2026.xlsx/download`

#### **Format B: Direct Static Download**
```
http://localhost:5000/downloads/{filename}
```
Example: `http://localhost:5000/downloads/budget_2026.xlsx`

#### **Format C: Session-Specific Download**
```
http://localhost:5000/api/files/generated/{sessionId}/{filename}/download
```
Example: `http://localhost:5000/api/files/generated/019c1462-77c6-7048-a254-d7595e8843c5/report.xlsx/download`

### 4. File Path Resolution Rules

Clarified how file paths are interpreted:
- Full path → saved as-is
- Filename only → saved to `generated_files/filename`
- Session path → saved to `generated_files/{sessionId}/filename`

### 5. Example Tool Calls

Added concrete examples showing proper usage:

```javascript
// Excel file
create_excel({
  filePath: "/Volumes/DATA/QWEN/zima-file-service/generated_files/countries.xlsx",
  data: [["Country"], ["Japan"], ["Brazil"], ["Germany"]]
})
// → Download: http://localhost:5000/api/files/countries.xlsx/download

// PDF in session folder
create_pdf({
  filePath: "/Volumes/DATA/QWEN/zima-file-service/generated_files/019c1462.../invoice.pdf",
  content: "Invoice content"
})
// → Download: http://localhost:5000/api/files/generated/019c1462.../invoice.pdf/download
```

### 6. Always Provide Requirements

Added instruction that Claude should always provide:
- File size (check via file system)
- Download URL (properly formatted)
- Brief description of contents

## Research Conducted

Analyzed ZIMA file service codebase to understand file handling:

**Files Analyzed:**
- `FileManager.cs` - File path resolution, versioning, session management
- `FilesController.cs` - Download endpoints, URL generation
- `Tools/ExcelTool.cs`, `Tools/PdfTool.cs`, `Tools/WordTool.cs` - File creation
- `Program.cs` - Static file serving configuration

**Key Findings:**
1. Generated files directory: `/Volumes/DATA/QWEN/zima-file-service/generated_files/`
2. Three download endpoint types: API, Static, Session-specific
3. Automatic file versioning when names conflict
4. Session-based organization support
5. File security validation and URL encoding

## Impact

**Before Update:**
- Claude didn't know where to save files
- Download links were incorrect or missing
- Files saved to wrong locations
- No session organization

**After Update:**
- Clear instructions on exact save paths
- Three proper download URL formats
- Session-based file organization encouraged
- Examples showing correct usage

## Testing

To test the updated system prompt:

```bash
# 1. Restart gateway (already done)
npm start

# 2. Test via frontend - ask to create a file
curl -X POST http://localhost:18790/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{"message":"Create an Excel file with 5 countries","sessionKey":"test"}'

# 3. Check the logs
cat logs/requests/prompts/*.txt | tail -1 | grep "FILE SAVING"

# 4. Verify Claude provides:
#    - Correct file path
#    - Proper download URL
#    - File size and description
```

## Files Modified

1. **`src/agent/openclaw-system-prompt.ts`**
   - Method: `buildToolUsageSection()`
   - Lines: 224-258 (expanded to ~290)
   - Added: ~1,500 characters of file handling instructions

## Deployment

- ✅ Code compiled successfully
- ✅ Gateway restarted
- ✅ New system prompt active
- ✅ Ready for testing

## Next Steps

1. Test with real file generation requests
2. Monitor logs to verify Claude follows instructions
3. Check download links are correctly formatted
4. Verify files are saved to correct locations

---

**Status:** ✅ Complete
**Build:** Successful
**Gateway:** Running with updated prompt
**Documentation:** Complete
