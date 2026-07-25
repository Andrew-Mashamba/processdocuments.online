# Streaming API Guide - How Frontend Calls `/api/chat/stream`

**Date:** 2026-01-31

---

## Overview

The ZIMA frontend uses **Server-Sent Events (SSE)** for real-time streaming of AI responses. This guide explains the complete flow from frontend to backend.

---

## Architecture Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                         COMPLETE FLOW                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. User types message in UI                                    │
│  2. Livewire component (FileGenerator.php) triggers streaming   │
│  3. JavaScript StreamingHandler opens SSE connection            │
│  4. Gateway processes request and streams response              │
│  5. JavaScript receives chunks and updates UI in real-time      │
│  6. On completion, saves message to database                    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. Frontend Component (Laravel Livewire)

### File: `app/Livewire/FileGenerator.php`

**Step 1: User Sends Message**

```php
public function generate()
{
    // Validate input
    if (empty(trim($this->prompt))) {
        return;
    }

    // Set loading state
    $this->isLoading = true;
    $this->streamingContent = '';

    // Get or create session
    $session = $this->getOrCreateSession();

    // Build request data
    $requestData = [
        'sessionId' => $this->currentSessionId,
        'streamUrl' => $this->getStreamingUrl(), // URL to stream endpoint
        'prompt' => $userPrompt,
        'messages' => $cachedMessages // Previous conversation
    ];

    // Dispatch event to JavaScript
    $this->dispatch('startStreaming', $requestData);
}
```

**Step 2: Determine Stream URL**

```php
public function getStreamingUrl(): string
{
    // Agent mode uses different endpoint
    if ($this->agentMode) {
        return "{$this->apiUrl}/api/agent/stream";
    }

    // Standard chat uses /api/generate/stream
    return "{$this->apiUrl}/api/generate/stream";
}
```

**Configuration:**
```php
// $this->apiUrl comes from environment or config
// Default: http://localhost:5000 (ZIMA API)
// Gateway: http://localhost:18790
```

---

## 2. JavaScript Streaming Handler

### File: `resources/views/livewire/file-generator.blade.php`

**Complete StreamingHandler Class:**

```javascript
class StreamingHandler {
    constructor(wire) {
        this.wire = wire;
        this.content = '';
        this.usage = null;
        this.files = [];
        this.model = null;
        this.currentEvent = null;
    }

    async startStreaming(streamUrl, requestData) {
        this.content = '';
        this.usage = null;
        this.files = [];
        this.model = null;

        console.log('StreamingHandler.startStreaming called:', {
            streamUrl,
            requestData
        });

        try {
            // CRITICAL: Use fetch with POST (EventSource only supports GET)
            const response = await fetch(streamUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'text/event-stream', // SSE format
                },
                body: JSON.stringify(requestData)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Read streaming response
            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                // Decode chunk
                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n');
                buffer = lines.pop(); // Keep incomplete line in buffer

                // Process each complete line
                for (const line of lines) {
                    this.processLine(line);
                }
            }

            // Process any remaining buffer
            if (buffer.trim()) {
                this.processLine(buffer);
            }

            // Complete - parse markdown to HTML
            const htmlContent = marked.parse(this.content);
            this.wire.completeStreaming(htmlContent, this.usage || {}, this.files, this.model);

        } catch (error) {
            console.error('Streaming error:', error);
            this.wire.completeStreaming('Error: ' + error.message, {}, [], null);
        }
    }

    processLine(line) {
        if (line.startsWith('event:')) {
            this.currentEvent = line.substring(6).trim();
        } else if (line.startsWith('data:')) {
            const jsonStr = line.substring(5).trim();
            if (jsonStr) {
                try {
                    const data = JSON.parse(jsonStr);
                    this.handleEvent(this.currentEvent, data);
                } catch (e) {
                    console.warn('Failed to parse SSE data:', jsonStr);
                }
            }
        }
    }

    handleEvent(eventType, data) {
        switch (eventType) {
            case 'content':
                // Real-time content streaming
                if (data.content) {
                    this.content += data.content;
                    const htmlContent = marked.parse(this.content);
                    this.wire.updateStreamingContent(htmlContent);
                }
                break;

            case 'complete':
                // Final event with metadata
                if (data.output) this.content = data.output;
                if (data.usage) this.usage = data.usage;
                if (data.files) this.files = data.files;
                if (data.model) this.model = data.model;
                break;

            case 'files':
                if (data.files) this.files = data.files;
                break;

            case 'error':
                console.error('Stream error:', data.message);
                this.wire.set('error', data.message);
                break;

            case 'start':
                console.log('Streaming started:', data);
                break;
        }
    }
}

// Initialize handler
let streamingHandler = null;

// Listen for Livewire events
document.addEventListener('livewire:initialized', () => {
    Livewire.on('startStreaming', (eventData) => {
        const data = Array.isArray(eventData) ? eventData[0] : eventData;

        // Get Livewire component reference
        let component = Livewire.find('{{ $this->getId() }}');

        // Create handler if needed
        if (!streamingHandler) {
            streamingHandler = new StreamingHandler(component);
        }

        // Start streaming
        streamingHandler.startStreaming(data.streamUrl, data);
    });
});
```

---

## 3. Backend SSE Response (Gateway)

### File: `src/server.ts`

**Streaming Endpoint:**

```typescript
// Chat streaming endpoint (SSE)
this.app.post('/api/chat/stream', async (req, res) => {
  try {
    if (!this.router) {
      return res.status(503).json({ error: 'Router not initialized' });
    }

    // Set SSE headers
    res.setHeader('Content-Type', 'text/event-stream');
    res.setHeader('Cache-Control', 'no-cache');
    res.setHeader('Connection', 'keep-alive');
    res.setHeader('X-Accel-Buffering', 'no'); // Disable nginx buffering

    // Send start event
    res.write(`event: start\ndata: ${JSON.stringify({
      timestamp: Date.now()
    })}\n\n`);

    // Stream the response
    const result = await this.router.routeMessageStream(req.body, (chunk: any) => {
      // Send each chunk as SSE
      res.write(`event: content\ndata: ${JSON.stringify(chunk)}\n\n`);
    });

    // Send final result
    res.write(`event: complete\ndata: ${JSON.stringify(result)}\n\n`);
    res.end();

  } catch (error: any) {
    console.error('Error in /api/chat/stream:', error);
    res.write(`event: error\ndata: ${JSON.stringify({
      error: error.message
    })}\n\n`);
    res.end();
  }
});
```

---

## 4. SSE Format (Server-Sent Events)

### SSE Message Structure

```
event: <event-type>
data: <json-data>

```

**Example Stream:**

```
event: start
data: {"timestamp":1738339200000,"requestId":"abc123"}

event: content
data: {"content":"Hello"}

event: content
data: {"content":" world"}

event: content
data: {"content":"!"}

event: complete
data: {"output":"Hello world!","usage":{"inputTokens":10,"outputTokens":3},"model":"claude-3-5-sonnet-20241022"}

```

**Event Types:**

| Event | Data | Description |
|-------|------|-------------|
| `start` | `{ timestamp, requestId }` | Stream started |
| `content` | `{ content: "text chunk" }` | Incremental text chunk |
| `complete` | `{ output, usage, files, model }` | Final response with metadata |
| `files` | `{ files: [...] }` | Generated files |
| `error` | `{ error: "message" }` | Error occurred |

---

## 5. Request/Response Examples

### Request to `/api/chat/stream`

```javascript
POST http://localhost:18790/api/chat/stream
Content-Type: application/json
Accept: text/event-stream

{
  "message": "Create an Excel file with product data",
  "sessionKey": "agent:main:webchat:direct:user-123",
  "channel": "webchat",
  "sender": {
    "id": "user-123",
    "name": "John Doe"
  },
  "attachments": []
}
```

### Response (SSE Stream)

```
event: start
data: {"timestamp":1738339200000}

event: content
data: {"content":"I'll"}

event: content
data: {"content":" create"}

event: content
data: {"content":" an"}

event: content
data: {"content":" Excel"}

event: content
data: {"content":" file"}

event: content
data: {"content":" for"}

event: content
data: {"content":" you"}

event: content
data: {"content":"."}

event: complete
data: {"output":"I'll create an Excel file for you.","usage":{"inputTokens":25,"outputTokens":12,"cost":0.0015},"files":[{"name":"products.xlsx","path":"/files/products.xlsx","size":8192}],"model":"claude-3-5-sonnet-20241022","complexity":"standard","tier":1}

```

---

## 6. Complete Code Flow Summary

### Frontend (Livewire + JavaScript)

```php
// 1. Livewire PHP Component
public function generate() {
    $this->dispatch('startStreaming', [
        'streamUrl' => 'http://localhost:18790/api/chat/stream',
        'message' => $this->prompt,
        'sessionKey' => $this->sessionKey
    ]);
}
```

```javascript
// 2. JavaScript Handler
Livewire.on('startStreaming', async (data) => {
    const response = await fetch(data.streamUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'text/event-stream'
        },
        body: JSON.stringify({
            message: data.message,
            sessionKey: data.sessionKey,
            channel: 'webchat',
            sender: { id: userId, name: userName },
            attachments: []
        })
    });

    // Read streaming response
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        // Process SSE lines...
    }
});
```

### Backend (Gateway)

```typescript
// 3. Gateway Streaming Endpoint
app.post('/api/chat/stream', async (req, res) => {
    res.setHeader('Content-Type', 'text/event-stream');

    await router.routeMessageStream(req.body, (chunk) => {
        res.write(`event: content\ndata: ${JSON.stringify(chunk)}\n\n`);
    });

    res.write(`event: complete\ndata: ${JSON.stringify(result)}\n\n`);
    res.end();
});
```

---

## 7. Key Implementation Details

### Why Not EventSource?

**EventSource Limitations:**
- ❌ Only supports GET requests
- ❌ Cannot send request body
- ❌ Cannot set custom headers (like Authorization)

**Fetch API Solution:**
- ✅ Supports POST requests
- ✅ Can send JSON body
- ✅ Supports custom headers
- ✅ Works with SSE format via ReadableStream

### Buffer Management

```javascript
let buffer = '';

while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop(); // Keep incomplete line in buffer

    for (const line of lines) {
        processLine(line); // Process complete lines
    }
}

// Process remaining buffer
if (buffer.trim()) {
    processLine(buffer);
}
```

**Why Buffer?**
- Chunks may arrive mid-line
- Need to wait for complete lines before parsing
- Last line in array is incomplete (unless empty)

### Real-Time UI Updates

```javascript
handleEvent(eventType, data) {
    if (eventType === 'content' && data.content) {
        this.content += data.content;

        // Parse markdown to HTML for proper rendering
        const htmlContent = marked.parse(this.content);

        // Update Livewire component (triggers UI update)
        this.wire.updateStreamingContent(htmlContent);
    }
}
```

**Livewire Method:**
```php
public function updateStreamingContent($content)
{
    $this->streamingContent = $content;
    // Livewire automatically updates UI
}
```

---

## 8. Error Handling

### Frontend Error Handling

```javascript
try {
    const response = await fetch(streamUrl, {...});

    if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    // Process stream...

} catch (error) {
    console.error('Streaming error:', error);

    // Show error to user
    this.wire.completeStreaming('Error: ' + error.message, {}, [], null);
}
```

### Backend Error Handling

```typescript
try {
    const result = await this.router.routeMessageStream(req.body, callback);
    // Send result...
} catch (error: any) {
    console.error('Error in /api/chat/stream:', error);
    res.write(`event: error\ndata: ${JSON.stringify({
        error: error.message
    })}\n\n`);
    res.end();
}
```

---

## 9. Testing the Stream

### Using cURL

```bash
curl -N -X POST http://localhost:18790/api/chat/stream \
  -H "Content-Type: application/json" \
  -H "Accept: text/event-stream" \
  -d '{
    "message": "Hello, how are you?",
    "sessionKey": "test-session",
    "channel": "webchat",
    "sender": {"id": "test-user", "name": "Test User"},
    "attachments": []
  }'
```

**Expected Output:**
```
event: start
data: {"timestamp":1738339200000}

event: content
data: {"content":"Hello"}

event: content
data: {"content":"!"}

event: complete
data: {"output":"Hello! I'm doing well...","usage":{...}}
```

### Using JavaScript Console

```javascript
// Open browser console on chat page
const response = await fetch('http://localhost:18790/api/chat/stream', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'text/event-stream'
    },
    body: JSON.stringify({
        message: 'Test message',
        sessionKey: 'test-123',
        channel: 'webchat',
        sender: { id: '1', name: 'Test' },
        attachments: []
    })
});

const reader = response.body.getReader();
const decoder = new TextDecoder();

while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    console.log(decoder.decode(value));
}
```

---

## 10. Configuration

### Laravel `.env`

```bash
# Gateway URL (where streaming endpoint lives)
GATEWAY_URL=http://localhost:18790
GATEWAY_TIMEOUT=120

# Or legacy ZIMA API
API_URL=http://localhost:5000
```

### Gateway Environment

```bash
# Gateway port
PORT=18790

# CORS (allow frontend)
CORS_ORIGINS=http://localhost:8000,http://localhost:3000

# API keys
ANTHROPIC_API_KEY=sk-ant-...
```

---

## Conclusion

The frontend uses a **JavaScript Fetch API** with **ReadableStream** to consume Server-Sent Events from the Gateway's `/api/chat/stream` endpoint. This provides real-time streaming of AI responses with proper error handling and UI updates.

**Key Points:**
- ✅ POST request with JSON body
- ✅ SSE response format (`event:` / `data:` lines)
- ✅ Incremental content chunks
- ✅ Real-time UI updates via Livewire
- ✅ Markdown parsing for rich formatting
- ✅ Proper error handling

---

**Documentation Date:** 2026-01-31
**Gateway Version:** 1.0.0
**Laravel Version:** 11.x
