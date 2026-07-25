# Mandatory Session-Based File Paths - Update

**Date:** 2026-01-31
**Issue:** System prompt said session paths were "recommended" but they're actually MANDATORY
**Solution:** Updated all file saving instructions to enforce session-based organization

## Changes Made

### 1. Changed "Recommended" to "MANDATORY"

**Before:**
```
2. **Session-Organized Files** (Recommended):
   - Path: `/Volumes/DATA/QWEN/zima-file-service/generated_files/{sessionId}/`
   - Use current sessionId from runtime context
   - Keeps files organized by conversation
```

**After:**
```
1. **MANDATORY: Session-Organized Files**:
   - Path: `/Volumes/DATA/QWEN/zima-file-service/generated_files/{sessionId}/`
   - **ALWAYS use the sessionId from the request context** - it's passed from the frontend
   - Every file MUST go in its session folder
   - Session ID is ALWAYS available in the runtime context
```

### 2. Added Session ID Availability Notice

Added prominent warning at the start of FILE SAVING section:
```
⚠️ **SESSION ID IS ALWAYS AVAILABLE**: Check the <runtime_context> section below -
your current sessionId is shown there (e.g., "Session: agent:main:webchat:direct:019c1462...").
Extract and use this sessionId for ALL file operations.
```

### 3. Simplified Download URL Format

**Before:** Three different URL formats (API, Static, Session-specific)

**After:** ONE mandatory format:
```
**Session-Specific Download URL** (REQUIRED):
http://localhost:5000/api/files/generated/{sessionId}/{filename}/download

ALWAYS use the sessionId from the current request context when building download URLs.
```

### 4. Updated File Path Construction

**Before:** Multiple resolution rules for different path formats

**After:** Single mandatory format:
```
**File Path Construction** (MANDATORY):
ALWAYS construct the full path with session ID:
/Volumes/DATA/QWEN/zima-file-service/generated_files/{sessionId}/{filename}

Where:
- {sessionId} = Current session ID from request context (ALWAYS available)
- {filename} = Descriptive filename (e.g., budget_2026.xlsx, invoice.pdf)
```

### 5. Updated All Examples

All examples now show session-based paths:

**Before:**
```javascript
create_excel({
  filePath: "/Volumes/DATA/QWEN/zima-file-service/generated_files/countries.xlsx",
  data: [["Country"], ["Japan"], ["Brazil"]]
})
// → Download: http://localhost:5000/api/files/countries.xlsx/download
```

**After:**
```javascript
// If sessionId = "019c1462-77c6-7048-a254-d7595e8843c5"

create_excel({
  filePath: "/Volumes/DATA/QWEN/zima-file-service/generated_files/019c1462-77c6-7048-a254-d7595e8843c5/countries.xlsx",
  data: [["Country"], ["Japan"], ["Brazil"]]
})
// → Download: http://localhost:5000/api/files/generated/019c1462-77c6-7048-a254-d7595e8843c5/countries.xlsx/download
```

Added examples for PDF and Word with same pattern.

### 6. Added Explanation for Why Session Organization is Mandatory

```
**Why Session Organization is Mandatory**:
- Frontend tracks files by session
- Users can manage files per conversation
- Prevents file conflicts between different users/sessions
- Automatic cleanup when sessions are deleted
```

## Technical Implementation

**File Modified:** `src/agent/openclaw-system-prompt.ts`
**Method:** `buildToolUsageSection()`
**Lines Changed:** ~250-320

**Key Points:**
1. Session ID is passed from frontend in every request
2. Session ID is shown in `<runtime_context>` section of every prompt
3. Claude must extract sessionId from runtime context
4. All file paths MUST include the sessionId
5. All download URLs MUST be session-specific format

## Runtime Context Example

Every prompt includes this section showing the sessionId:

```xml
<runtime_context>
Agent: main | Host: Andrews-MacBook-Air.local | OS: darwin 25.2.0 | Model: claude-sonnet-4-20250514 | Channel: webchat | Capabilities: document_processing, web_research, file_operations, session_management, memory_system
Session: agent:main:webchat:direct:019c1462-77c6-7048-a254-d7595e8843c5 | Tools: 217 | Timestamp: 2026-01-31T14:32:49.124Z
</runtime_context>
```

The sessionId is the full "Session:" value.

## Session ID Format

From the logs, we can see the frontend sends:
```json
{
  "goal": "create an excel file",
  "sessionId": "019c1462-77c6-7048-a254-d7595e8843c5"
}
```

This becomes the session key in the Gateway:
```
agent:main:webchat:direct:019c1462-77c6-7048-a254-d7595e8843c5
```

But for file paths, use the FULL session key as shown in runtime_context.

## Expected Claude Behavior

When Claude receives a file creation request, it should:

1. ✅ Extract sessionId from `<runtime_context>` section
2. ✅ Construct full path: `/Volumes/DATA/QWEN/zima-file-service/generated_files/{sessionId}/{filename}`
3. ✅ Call the tool with the full path
4. ✅ Generate download URL: `http://localhost:5000/api/files/generated/{sessionId}/{filename}/download`
5. ✅ Provide file size and description

## Testing

**Test Request:**
```bash
curl -X POST http://localhost:18790/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Create an Excel file with 3 fruits",
    "sessionKey": "agent:main:webchat:direct:test-123",
    "channel": "webchat",
    "sender": {"id": "test-user", "name": "Test"}
  }'
```

**Expected File Path:**
```
/Volumes/DATA/QWEN/zima-file-service/generated_files/agent:main:webchat:direct:test-123/fruits.xlsx
```

**Expected Download URL:**
```
http://localhost:5000/api/files/generated/agent:main:webchat:direct:test-123/fruits.xlsx/download
```

## Deployment Status

- ✅ Code updated
- ✅ Compiled successfully
- ✅ Gateway restarted (17:53)
- ✅ New prompt active
- ✅ Ready for production use

## Benefits of Mandatory Session Paths

1. **User Experience**: Each conversation has its own files
2. **File Management**: Frontend can list files per session
3. **Cleanup**: Delete all files when session is deleted
4. **Security**: Files isolated per session/user
5. **Organization**: No file name conflicts between sessions
6. **Tracking**: Easy to see which conversation created which file

---

**Status:** ✅ Complete
**Priority:** Critical (was bug - marked as "recommended" when actually mandatory)
**Impact:** High (fixes file organization and download links)
