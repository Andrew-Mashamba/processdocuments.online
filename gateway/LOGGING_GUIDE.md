# Request Logging Guide

## Overview

The Gateway now has comprehensive request logging that tracks every step from HTTP request to Claude CLI response.

## Log Files Location

All logs are written to: `./logs/requests/`

```
logs/requests/
├── requests.jsonl           # Main log file (JSON Lines format)
├── prompts/                 # Full prompts sent to Claude
│   └── prompt_agent_main_webchat_direct_user-123_2026-01-31T10-30-00.txt
└── responses/               # Full responses from Claude
    └── response_agent_main_webchat_direct_user-123_2026-01-31T10-30-00.txt
```

## Log Format

### Main Log File (requests.jsonl)

Each line is a JSON object representing a phase in the request lifecycle:

```json
{
  "timestamp": "2026-01-31T10:30:00.000Z",
  "requestId": "abc123-def456",
  "sessionKey": "agent:main:webchat:direct:user-123",
  "phase": "CLAUDE_PROMPT",
  "service": "ClaudeCliRuntime",
  "data": { ... }
}
```

### Phases Logged

1. **HTTP_REQUEST_RECEIVED** (Server)
   - Request body, method, path

2. **MESSAGE_NORMALIZED** (HybridMessageRouter)
   - Original message and normalized version

3. **SESSION_KEY_BUILT** (SessionKeyBuilder)
   - Session key components

4. **REQUEST_PREPARED** (HybridMessageRouter)
   - Complete request object

5. **ACQUIRING_LOCK** (LockManager)
   - Session lock acquisition

6. **LOCK_ACQUIRED** (LockManager)
   - Lock successfully acquired

7. **TRANSCRIPT_LOADED** (TranscriptManager)
   - Conversation history loaded

8. **TASK_CLASSIFIED** (TaskClassifier)
   - Task complexity: quick/standard/complex

9. **MODEL_SELECTED** (TaskClassifier)
   - Selected Claude model

10. **CONTEXT_OPTIMIZED** (TierOptimizer)
    - Context tier and optimization stats

11. **FILES_LOADED** (FileLoader)
    - Session files loaded

12. **SYSTEM_PROMPT_BUILT** (OpenClawSystemPromptBuilder)
    - System prompt created

13. **AGENT_CONTEXT_PREPARED** (HybridContextManager)
    - Agent context ready

14. **INVOKING_AGENT** (HybridContextManager)
    - Agent runtime invoked

15. **TOOLS_LOADED** (UnifiedToolRegistry)
    - Available tools

16. **CLAUDE_PROMPT** (ClaudeCliRuntime)
    - **Full prompt sent to Claude** (also saved to `prompts/` directory)

17. **CLAUDE_CLI_SPAWN** (ClaudeCliRuntime)
    - CLI command and arguments

18. **STREAM_CHUNK** (ClaudeCliRuntime)
    - Individual streaming chunks (many)

19. **CLAUDE_RESPONSE** (ClaudeCliRuntime)
    - **Full response from Claude** (also saved to `responses/` directory)

20. **CLAUDE_CLI_COMPLETE** (ClaudeCliRuntime)
    - CLI process completed with usage stats

21. **AGENT_COMPLETED** (HybridContextManager)
    - Agent execution finished

22. **TOKENS_PROCESSED** (TokenProcessor)
    - Special tokens processed

23. **TRANSCRIPT_SAVED** (TranscriptManager)
    - Conversation saved

24. **RESPONSE_CACHED** (HybridMessageRouter)
    - Response cached

25. **HTTP_RESPONSE_COMPLETE** (Server)
    - Final HTTP response sent

26. **REQUEST_END** (Server)
    - Request completed with duration

## Reading Logs

### View Full Request Flow

```bash
# Filter by request ID
cat logs/requests/requests.jsonl | grep "abc123-def456" | jq .

# View all phases for a session
cat logs/requests/requests.jsonl | grep "agent:main:webchat:direct:user-123" | jq .
```

### View Prompts

```bash
# List all prompts
ls -la logs/requests/prompts/

# View a specific prompt
cat logs/requests/prompts/prompt_agent_main_webchat_direct_user-123_2026-01-31T10-30-00.txt
```

### View Responses

```bash
# List all responses
ls -la logs/requests/responses/

# View a specific response
cat logs/requests/responses/response_agent_main_webchat_direct_user-123_2026-01-31T10-30-00.txt
```

### Extract Specific Phases

```bash
# View all prompts sent to Claude
cat logs/requests/requests.jsonl | grep "CLAUDE_PROMPT" | jq .

# View all responses from Claude
cat logs/requests/requests.jsonl | grep "CLAUDE_RESPONSE" | jq .

# View task classifications
cat logs/requests/requests.jsonl | grep "TASK_CLASSIFIED" | jq .

# View model selections
cat logs/requests/requests.jsonl | grep "MODEL_SELECTED" | jq .

# View usage stats
cat logs/requests/requests.jsonl | grep "CLAUDE_CLI_COMPLETE" | jq '.data.usage'
```

### Track Request Performance

```bash
# View request start and end times
cat logs/requests/requests.jsonl | grep -E "HTTP_REQUEST_RECEIVED|REQUEST_END" | jq '{phase, timestamp, duration: .data.duration}'
```

### Monitor Streaming

```bash
# Count stream chunks per request
cat logs/requests/requests.jsonl | grep "STREAM_CHUNK" | jq -r .requestId | sort | uniq -c
```

## Console Output

The logger also prints to console for immediate visibility:

```
📝 [Server] HTTP_REQUEST_RECEIVED
   Message: Create an Excel file with product data

📝 [HybridMessageRouter] MESSAGE_NORMALIZED

📝 [TaskClassifier] TASK_CLASSIFIED

📝 [ClaudeCliRuntime] CLAUDE_PROMPT
   Prompt Length: 2456 chars
   📄 Prompt saved to: prompt_agent_main_webchat_direct_user-123_2026-01-31T10-30-00.txt

📝 [ClaudeCliRuntime] CLAUDE_RESPONSE
   Response Length: 1234 chars
   📄 Response saved to: response_agent_main_webchat_direct_user-123_2026-01-31T10-30-00.txt

✅ Request abc123-def456 completed in 3245ms
```

## Analyzing Logs

### Common Use Cases

**1. Debug why a request failed:**
```bash
# Find the error
cat logs/requests/requests.jsonl | grep "REQUEST_END" | jq 'select(.data.success == false)'
```

**2. Compare prompt vs response:**
```bash
# Find the request ID from logs
cat logs/requests/requests.jsonl | grep "CLAUDE_PROMPT" | jq -r '.requestId' | head -1

# View prompt
cat logs/requests/prompts/prompt_*_<timestamp>.txt

# View response
cat logs/requests/responses/response_*_<timestamp>.txt
```

**3. Track model usage:**
```bash
# Count model selections
cat logs/requests/requests.jsonl | grep "MODEL_SELECTED" | jq -r '.data.model' | sort | uniq -c
```

**4. Monitor token usage:**
```bash
# Extract all usage stats
cat logs/requests/requests.jsonl | grep "CLAUDE_CLI_COMPLETE" | jq '.data.usage'
```

**5. Track context optimization:**
```bash
# See how much context was reduced
cat logs/requests/requests.jsonl | grep "CONTEXT_OPTIMIZED" | jq '{tier: .data.tier, original: .data.originalMessagesCount, optimized: .data.optimizedMessagesCount}'
```

## Log Rotation

Logs will accumulate over time. Consider implementing log rotation:

```bash
# Manual cleanup (older than 7 days)
find logs/requests/ -name "*.txt" -mtime +7 -delete
find logs/requests/ -name "*.jsonl" -mtime +7 -delete

# Or use logrotate (Linux)
# Create /etc/logrotate.d/zima-gateway
/path/to/gateway/logs/requests/*.jsonl {
    daily
    rotate 7
    compress
    missingok
    notifempty
}
```

## Troubleshooting

### No logs appearing?

1. Check directory permissions:
```bash
ls -la logs/requests/
```

2. Check if logger is initialized:
```bash
# Should see log directory created
ls -la logs/
```

3. Enable debug logging:
```bash
export DEBUG=true
npm start
```

### Logs too large?

Disable stream chunk logging by removing the `logStreamChunk` call in `src/server.ts:196`.

### Want more detail?

Add custom logging phases:
```typescript
import { getRequestLogger } from './observability/request-logger';

const reqLogger = getRequestLogger();
reqLogger.log('CUSTOM_PHASE', 'MyService', {
  customData: 'value'
}, sessionKey);
```

## Performance Impact

The logging system is designed to be lightweight:
- Uses append-only file writes (fast)
- Asynchronous logging (non-blocking)
- JSONL format (easy parsing, no memory overhead)
- Separate files for prompts/responses (easy access)

Expected overhead: **< 5ms per request**

## Security Considerations

⚠️ **WARNING:** Log files contain:
- User messages
- AI responses
- Full prompts (including system prompts)
- API usage statistics

**Recommendations:**
1. Store logs outside web root
2. Add to `.gitignore`
3. Implement log rotation
4. Restrict file permissions: `chmod 600 logs/requests/*.jsonl`
5. Consider encrypting logs at rest

## Example: Full Request Analysis

```bash
# 1. Find a recent request
REQUEST_ID=$(cat logs/requests/requests.jsonl | grep "HTTP_REQUEST_RECEIVED" | tail -1 | jq -r .requestId)

# 2. Extract all phases for this request
cat logs/requests/requests.jsonl | grep "$REQUEST_ID" | jq '{phase, service, timestamp}' > request_flow.json

# 3. View the prompt
cat logs/requests/prompts/prompt_*_$(date +%Y-%m-%d)*.txt | tail -1

# 4. View the response
cat logs/requests/responses/response_*_$(date +%Y-%m-%d)*.txt | tail -1

# 5. Check performance
cat logs/requests/requests.jsonl | grep "$REQUEST_ID" | grep "REQUEST_END" | jq .data.duration
```

## Integration with Monitoring Tools

### Send to Elasticsearch

```bash
cat logs/requests/requests.jsonl | curl -X POST "localhost:9200/gateway-logs/_bulk" \
  -H 'Content-Type: application/x-ndjson' \
  --data-binary @-
```

### Send to Datadog

```bash
cat logs/requests/requests.jsonl | while read line; do
  echo "$line" | curl -X POST "https://http-intake.logs.datadoghq.com/v1/input" \
    -H "DD-API-KEY: $DD_API_KEY" \
    -d @-
done
```

### Parse with Python

```python
import json

with open('logs/requests/requests.jsonl', 'r') as f:
    for line in f:
        log = json.loads(line)
        if log['phase'] == 'CLAUDE_PROMPT':
            print(f"Prompt length: {log['data']['promptLength']}")
```

---

**Last Updated:** 2026-01-31
**Gateway Version:** 1.0.0
