# ZIMA Hybrid Gateway

Multi-channel AI assistant gateway combining OpenClaw's architecture with ZIMA's document processing intelligence.

## Architecture

```
Multi-Channel Input → Gateway Router → Context Manager → Agent Runtime → ZIMA Core
     ↓                      ↓                ↓                ↓              ↓
  WebChat              Session Keys    Task Classify    Pi Agent      196+ Tools
  WhatsApp             Idempotency     Model Select     Tool Exec     Documents
  Email                Normalization   Cache Check      Streaming     Processing
```

## Features

- ✅ **Multi-Channel Routing**: WebChat, WhatsApp, Email
- ✅ **Session Management**: OpenClaw-style session keys (`agent:main:channel:type:user`)
- ✅ **Idempotency**: Automatic duplicate message detection
- ✅ **WebSocket + HTTP**: Real-time and REST API support
- ⏳ **Hybrid Context**: ZIMA task classification + OpenClaw memory (Week 3-4)
- ⏳ **Vector Memory**: LanceDB semantic search (Week 7-8)
- ⏳ **296+ Tools**: Combined ZIMA + OpenClaw tools (Week 5-6)

## Installation

```bash
# Install dependencies
npm install

# Copy environment template
cp .env.example .env

# Edit .env with your settings
nano .env

# Build TypeScript
npm run build

# Start gateway
npm start
```

## Development

```bash
# Run in development mode (auto-reload)
npm run dev

# Watch TypeScript compilation
npm run watch
```

## Quick Start

### 1. Start the Gateway

```bash
npm run dev
```

You should see:

```
╔════════════════════════════════════════════════════════╗
║  Gateway Server Running                                ║
║  WebSocket: ws://0.0.0.0:18789                        ║
║  HTTP API:  http://0.0.0.0:18789                      ║
╚════════════════════════════════════════════════════════╝
```

### 2. Test HTTP API

```bash
# Health check
curl http://localhost:18789/health

# Send a message
curl -X POST http://localhost:18789/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Create an Excel file with 10 random names",
    "channel": "webchat",
    "senderId": "user123"
  }'

# Get stats
curl http://localhost:18789/api/stats
```

### 3. Test WebSocket

Create a test file `test-ws.js`:

```javascript
const WebSocket = require('ws');

const ws = new WebSocket('ws://localhost:18789');

ws.on('open', () => {
  console.log('Connected to gateway');

  // Send chat message
  ws.send(JSON.stringify({
    type: 'chat',
    payload: {
      message: 'Hello from WebSocket!',
      channel: 'webchat',
      senderId: 'test-user'
    }
  }));
});

ws.on('message', (data) => {
  const msg = JSON.parse(data.toString());
  console.log('Received:', msg.type, JSON.stringify(msg.payload, null, 2));
});
```

Run: `node test-ws.js`

## API Endpoints

### HTTP Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/api/chat` | POST | Send chat message (HTTP alternative to WS) |
| `/api/stats` | GET | Gateway statistics |
| `/api/config` | GET | Configuration info |

### WebSocket Messages

**Client → Server:**

```json
{
  "type": "chat",
  "payload": {
    "message": "Your message here",
    "channel": "webchat",
    "senderId": "user123",
    "attachments": []
  }
}
```

**Server → Client:**

```json
{
  "type": "chat:complete",
  "payload": {
    "output": "AI response",
    "usage": { "inputTokens": 100, "outputTokens": 50 },
    "model": "claude-sonnet-4",
    "files": []
  },
  "timestamp": 1706543210000
}
```

## Configuration

Edit `config.json` (or `~/.zima/config.json`):

```json
{
  "gateway": {
    "port": 18789,
    "bind": "0.0.0.0"
  },
  "channels": {
    "webchat": {
      "enabled": true,
      "laravelUrl": "http://localhost:8000"
    },
    "whatsapp": {
      "enabled": false
    },
    "email": {
      "enabled": false
    }
  },
  "zima": {
    "apiUrl": "http://localhost:5000"
  },
  "storage": {
    "root": "/home/zima/.zima"
  }
}
```

## Session Keys

Session keys follow OpenClaw's format:

```
agent:{agentId}:{channel}:{chatType}:{accountId}:{threadId?}
```

Examples:
- `agent:main:webchat:direct:user123`
- `agent:main:whatsapp:direct:+1234567890`
- `agent:main:email:direct:user@example.com:thread-abc123`

## Storage Structure

```
~/.zima/
├── config.json                          # Gateway config
├── agents/
│   └── main/
│       ├── sessions.json                # Session registry
│       ├── sessions/
│       │   ├── agent-main-webchat-direct-user123.jsonl
│       │   └── agent-main-whatsapp-direct-+1234567890.jsonl
│       ├── files/
│       │   └── {sessionKey}/
│       │       ├── uploads/
│       │       └── generated/
│       └── memory.db                    # LanceDB (Week 7-8)
```

## Week-by-Week Progress

- ✅ **Week 1-2**: Gateway Foundation (Current)
  - Message router
  - Session key builder
  - Configuration system
  - WebSocket server
  - HTTP API

- ⏳ **Week 3-4**: Hybrid Context Manager
  - Session write locks
  - Transcript management
  - Task classification
  - Model selection
  - Response caching
  - Context tier optimization

- ⏳ **Week 5-6**: Agent Runtime
  - Pi Agent Core integration
  - Unified tool registry (296+ tools)
  - Tool routing

- ⏳ **Week 7-8**: Vector Memory
  - LanceDB integration
  - Embedding generation
  - Semantic search

- ⏳ **Week 9-10**: Channel Adapters
  - WhatsApp (Baileys)
  - Email (IMAP/SMTP)
  - WebChat (Laravel integration)

- ⏳ **Week 11-12**: Deployment
  - Docker Compose
  - Production configuration
  - Laravel integration

## Troubleshooting

### Gateway won't start

```bash
# Check if port 18789 is already in use
lsof -i :18789

# Kill existing process
kill -9 <PID>

# Or use a different port
GATEWAY_PORT=18790 npm start
```

### WebSocket connection fails

- Check firewall settings
- Ensure gateway is running on `0.0.0.0` (not `localhost`)
- Verify CORS settings in `config.json`

### "Router not initialized" error

- Wait for gateway to fully start (check logs)
- Ensure configuration is valid
- Check storage directories exist

## Next Steps

1. ✅ Complete Week 1-2 (Gateway Foundation)
2. Start Week 3-4 (Hybrid Context Manager)
3. Integrate with ZIMA Core backend
4. Add channel adapters

## License

MIT
