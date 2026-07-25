# ZIMA Hybrid Architecture Upgrade Plan
## Combining ZIMA Document Processing + OpenClaw Multi-Channel Platform

**Version**: 3.0 Hybrid
**Target**: Production-ready multi-channel AI assistant with document specialization
**Timeline**: 12 weeks
**Channels**: WebChat (Laravel), WhatsApp, Email
**Deployment**: Linux server with horizontal scaling capability

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                  Multi-Channel Gateway                      │
│         (Node.js/TypeScript - Port 18789 WebSocket)         │
│  • Message Router         • Session Key Generator           │
│  • Send Policy            • Idempotency Deduplication       │
└───────────┬─────────────────────────────────────────────────┘
            │
            ├─ WebChat Channel → Laravel Frontend (Port 8000)
            ├─ WhatsApp Channel → Baileys adapter
            └─ Email Channel → IMAP/SMTP adapter
            │
            ▼
┌─────────────────────────────────────────────────────────────┐
│              Hybrid Context Manager                         │
│  • Session Write Locks (OpenClaw)                          │
│  • Vector Memory (LanceDB - OpenClaw)                      │
│  • Task Classification (ZIMA)                              │
│  • Tier Optimization (ZIMA)                                │
│  • Response Caching (ZIMA)                                 │
└───────────┬─────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────┐
│              Agent Runtime (Hybrid)                         │
│  • Pi Agent Core (autonomy - OpenClaw)                     │
│  • Model Selection (Haiku/Sonnet/Opus - ZIMA)             │
│  • Unified Tool Registry (296+ tools)                      │
│  • Skills System (OpenClaw)                                │
└───────────┬─────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────┐
│            ZIMA Core (.NET 9.0 - Port 5000)                │
│  • 196+ Document Tools (Excel, PDF, Word, PPT, Image)      │
│  • File Generation & Processing                           │
│  • Claude CLI Integration                                 │
└─────────────────────────────────────────────────────────────┘
```

---

## Week-by-Week Implementation Plan

### **Week 1-2: Gateway Foundation**

#### **Goal**: Build the multi-channel message router

**Tasks**:

1. **Initialize Gateway Project**
```bash
mkdir gateway
cd gateway
npm init -y
npm install typescript @types/node ws express cors dotenv
npm install @whiskeysockets/baileys nodemailer imap
npx tsc --init
```

2. **Project Structure**
```
gateway/
├── src/
│   ├── server.ts                    # Main entry point
│   ├── router/
│   │   ├── message-router.ts        # Channel normalization
│   │   └── session-key-builder.ts   # Session key generation
│   ├── channels/
│   │   ├── webchat-adapter.ts       # Laravel integration
│   │   ├── whatsapp-adapter.ts      # Baileys integration
│   │   └── email-adapter.ts         # IMAP/SMTP integration
│   ├── context/
│   │   └── context-manager.ts       # Hybrid context handling
│   ├── config/
│   │   ├── config.ts                # Configuration loader
│   │   └── sessions.ts              # Session management
│   └── types/
│       └── index.ts                 # TypeScript interfaces
├── config.json                       # Gateway configuration
├── tsconfig.json
└── package.json
```

3. **Implement Core Router** (`src/router/message-router.ts`)
```typescript
import { WebSocketServer } from 'ws';
import { SessionKeyBuilder } from './session-key-builder';
import { ContextManager } from '../context/context-manager';

export class HybridMessageRouter {
  private wss: WebSocketServer;
  private sessionKeyBuilder: SessionKeyBuilder;
  private contextManager: ContextManager;
  private dedupCache: Map<string, any>;

  constructor(port: number = 18789) {
    this.wss = new WebSocketServer({ port });
    this.sessionKeyBuilder = new SessionKeyBuilder();
    this.contextManager = new ContextManager();
    this.dedupCache = new Map();

    this.setupWebSocket();
  }

  private setupWebSocket() {
    this.wss.on('connection', (ws) => {
      console.log('Client connected');

      ws.on('message', async (data) => {
        const message = JSON.parse(data.toString());
        const result = await this.routeMessage(message);
        ws.send(JSON.stringify(result));
      });
    });
  }

  async routeMessage(rawMessage: any) {
    // 1. Normalize message from channel
    const normalized = this.normalizeChannelMessage(rawMessage);

    // 2. Build session key (OpenClaw format)
    const sessionKey = this.sessionKeyBuilder.build({
      agentId: normalized.agentId || 'main',
      channel: normalized.channel,
      chatType: normalized.chatType || 'direct',
      accountId: normalized.senderId,
      threadId: normalized.threadId
    });

    // 3. Idempotency check
    const idemKey = normalized.messageId || this.generateIdempotencyKey(normalized);
    if (this.dedupCache.has(idemKey)) {
      return this.dedupCache.get(idemKey);
    }

    // 4. Dispatch to context manager
    const result = await this.contextManager.processMessage({
      sessionKey,
      message: normalized.text,
      attachments: normalized.attachments,
      sender: normalized.sender,
      channel: normalized.channel,
      metadata: { timestamp: Date.now() }
    });

    // 5. Cache result
    this.dedupCache.set(idemKey, result);
    setTimeout(() => this.dedupCache.delete(idemKey), 300000); // 5 min TTL

    return result;
  }

  private normalizeChannelMessage(raw: any): NormalizedMessage {
    // Convert different channel formats to unified structure
    return {
      text: raw.message || raw.text || raw.body,
      senderId: raw.sender?.id || raw.from || raw.senderId,
      channel: raw.channel || 'webchat',
      chatType: raw.chatType || 'direct',
      attachments: raw.attachments || [],
      messageId: raw.messageId || raw.id,
      threadId: raw.threadId,
      agentId: raw.agentId,
      sender: {
        channel: raw.channel,
        channelUserId: raw.sender?.id || raw.from,
        displayName: raw.sender?.name || 'Unknown'
      }
    };
  }

  private generateIdempotencyKey(message: NormalizedMessage): string {
    const str = `${message.channel}:${message.senderId}:${message.text}:${message.metadata.timestamp}`;
    return require('crypto').createHash('sha256').update(str).digest('hex');
  }
}

interface NormalizedMessage {
  text: string;
  senderId: string;
  channel: string;
  chatType: string;
  attachments: any[];
  messageId?: string;
  threadId?: string;
  agentId?: string;
  sender: {
    channel: string;
    channelUserId: string;
    displayName: string;
  };
}
```

4. **Session Key Builder** (`src/router/session-key-builder.ts`)
```typescript
export class SessionKeyBuilder {
  build(params: {
    agentId: string;
    channel: string;
    chatType: string;
    accountId: string;
    threadId?: string;
  }): string {
    // OpenClaw format: agent:{id}:{channel}:{type}:{account}:{thread}
    const parts = [
      'agent',
      params.agentId,
      params.channel,
      params.chatType,
      params.accountId
    ];

    if (params.threadId) {
      parts.push(params.threadId);
    }

    return parts.join(':');
  }

  parse(sessionKey: string): SessionKeyComponents {
    const parts = sessionKey.split(':');
    return {
      prefix: parts[0], // 'agent'
      agentId: parts[1],
      channel: parts[2],
      chatType: parts[3],
      accountId: parts[4],
      threadId: parts[5]
    };
  }
}

interface SessionKeyComponents {
  prefix: string;
  agentId: string;
  channel: string;
  chatType: string;
  accountId: string;
  threadId?: string;
}
```

5. **Configuration** (`config.json`)
```json
{
  "gateway": {
    "port": 18789,
    "bind": "0.0.0.0",
    "cors": {
      "origins": ["http://localhost:8000", "http://localhost:3000"]
    }
  },
  "channels": {
    "webchat": {
      "enabled": true,
      "laravelUrl": "http://localhost:8000",
      "webhookSecret": "your-secret-key"
    },
    "whatsapp": {
      "enabled": true,
      "sessionPath": "./sessions/whatsapp",
      "dmPairing": {
        "mode": "allow",
        "allowedNumbers": []
      }
    },
    "email": {
      "enabled": true,
      "imap": {
        "host": "imap.gmail.com",
        "port": 993,
        "user": "your-email@gmail.com",
        "password": "your-app-password"
      },
      "smtp": {
        "host": "smtp.gmail.com",
        "port": 587,
        "user": "your-email@gmail.com",
        "password": "your-app-password"
      }
    }
  },
  "zima": {
    "apiUrl": "http://localhost:5000",
    "timeout": 120000
  },
  "storage": {
    "root": "/home/zima/.zima",
    "transcripts": "/home/zima/.zima/agents/main/sessions",
    "files": "/home/zima/.zima/agents/main/files"
  }
}
```

**Deliverables**:
- ✅ Gateway server running on port 18789
- ✅ WebSocket RPC working
- ✅ Session key generation (OpenClaw format)
- ✅ Idempotency deduplication
- ✅ Configuration loading

---

### **Week 3-4: Hybrid Context Manager**

#### **Goal**: Implement intelligent context handling combining ZIMA + OpenClaw approaches

**Tasks**:

1. **Install Dependencies**
```bash
npm install fs-extra lockfile uuid crypto
npm install --save-dev @types/fs-extra @types/lockfile
```

2. **Context Manager Implementation** (`src/context/context-manager.ts`)
```typescript
import * as fs from 'fs-extra';
import * as lockfile from 'lockfile';
import { createHash } from 'crypto';
import axios from 'axios';

export class ContextManager {
  private config: any;
  private sessionLocks: Map<string, boolean>;
  private responseCache: Map<string, CachedResponse>;

  constructor() {
    this.config = this.loadConfig();
    this.sessionLocks = new Map();
    this.responseCache = new Map();
  }

  async processMessage(request: MessageRequest): Promise<MessageResponse> {
    // ========================================
    // STEP 1: ACQUIRE SESSION LOCK (OpenClaw)
    // ========================================
    const lock = await this.acquireSessionWriteLock(request.sessionKey);

    try {
      // ========================================
      // STEP 2: LOAD SESSION TRANSCRIPT (OpenClaw)
      // ========================================
      const transcriptPath = this.getTranscriptPath(request.sessionKey);
      const messages = await this.readTranscript(transcriptPath);
      const messageCount = messages.filter(m => m.type === 'message').length;

      // ========================================
      // STEP 3: TASK CLASSIFICATION (ZIMA)
      // ========================================
      const complexity = this.classifyTask(request.message, messageCount);
      const model = this.selectModel(complexity);

      // ========================================
      // STEP 4: RESPONSE CACHE CHECK (ZIMA)
      // ========================================
      const cacheKey = this.generateCacheKey(request, messageCount);
      const cached = this.responseCache.get(cacheKey);
      if (cached && (Date.now() - cached.timestamp) < 3600000) {
        return { ...cached.response, fromCache: true };
      }

      // ========================================
      // STEP 5: CONTEXT TIER OPTIMIZATION (ZIMA)
      // ========================================
      const tier = this.determineContextTier(messageCount, complexity);
      const optimizedMessages = this.filterByTier(messages, tier);

      // ========================================
      // STEP 6: VECTOR MEMORY SEARCH (OpenClaw)
      // ========================================
      const memories = await this.searchMemory(request.message, request.sessionKey);

      // ========================================
      // STEP 7: LOAD SESSION FILES (ZIMA)
      // ========================================
      const files = await this.loadSessionFiles(request.sessionKey);
      const fileContext = this.buildFileContext(files, tier);

      // ========================================
      // STEP 8: BUILD SYSTEM PROMPT (Combined)
      // ========================================
      const systemPrompt = this.buildHybridSystemPrompt({
        channel: request.channel,
        fileContext,
        memories,
        skills: await this.loadSkills()
      });

      // ========================================
      // STEP 9: INVOKE AGENT RUNTIME
      // ========================================
      const agentResult = await this.invokeAgent({
        sessionKey: request.sessionKey,
        model,
        systemPrompt,
        messages: optimizedMessages,
        newMessage: request.message,
        attachments: request.attachments
      });

      // ========================================
      // STEP 10: POST-PROCESSING
      // ========================================

      // Cache response
      this.responseCache.set(cacheKey, {
        response: agentResult,
        timestamp: Date.now()
      });

      // Append to transcript
      await this.appendToTranscript(transcriptPath, {
        type: 'message',
        role: 'user',
        content: request.message,
        timestamp: Date.now()
      });

      await this.appendToTranscript(transcriptPath, {
        type: 'message',
        role: 'assistant',
        content: agentResult.output,
        usage: agentResult.usage,
        model: agentResult.model,
        timestamp: Date.now()
      });

      // Capture to memory
      if (memories.length < 100) { // Don't overflow memory
        await this.captureToMemory(request.sessionKey, agentResult.output);
      }

      // Compact if needed
      if (messageCount > 100) {
        await this.compactSession(transcriptPath);
      }

      return agentResult;

    } finally {
      await lock.release();
    }
  }

  // ========================================
  // ZIMA: Task Classification
  // ========================================
  private classifyTask(message: string, messageCount: number): TaskComplexity {
    const lower = message.toLowerCase();

    // Simple
    if (
      message.length < 50 ||
      ['what is', 'explain', 'list', 'show'].some(k => lower.startsWith(k)) ||
      messageCount < 3
    ) {
      return 'simple';
    }

    // Complex
    if (
      message.length > 1000 ||
      ['create multiple', 'architecture', 'implement'].some(k => lower.includes(k))
    ) {
      return 'complex';
    }

    return 'standard';
  }

  private selectModel(complexity: TaskComplexity): string {
    const models = {
      simple: 'claude-3-5-haiku-20241022',
      standard: 'claude-sonnet-4-20250514',
      complex: 'claude-opus-4-20250514'
    };
    return models[complexity];
  }

  // ========================================
  // ZIMA: Context Tier Optimization
  // ========================================
  private determineContextTier(messageCount: number, complexity: TaskComplexity): number {
    if (messageCount < 5) return 0; // Full
    if (messageCount < 20) return 1; // Summarized
    if (messageCount < 50) return 2; // Recent only
    return 3; // Minimal
  }

  private filterByTier(messages: any[], tier: number): any[] {
    switch (tier) {
      case 0: return messages;
      case 1: return this.summarizeOld(messages, 20);
      case 2: return messages.slice(-5);
      case 3: return [];
      default: return messages;
    }
  }

  // ========================================
  // OpenClaw: Session Write Lock
  // ========================================
  private async acquireSessionWriteLock(sessionKey: string): Promise<Lock> {
    const lockPath = `/tmp/zima-lock-${sessionKey.replace(/:/g, '-')}.lock`;

    return new Promise((resolve, reject) => {
      lockfile.lock(lockPath, { wait: 30000, retries: 3 }, (err) => {
        if (err) reject(err);
        else resolve({
          release: () => new Promise((res, rej) => {
            lockfile.unlock(lockPath, (err) => err ? rej(err) : res());
          })
        });
      });
    });
  }

  // ========================================
  // OpenClaw: Transcript Management
  // ========================================
  private getTranscriptPath(sessionKey: string): string {
    const safe = sessionKey.replace(/:/g, '-');
    return `${this.config.storage.transcripts}/${safe}.jsonl`;
  }

  private async readTranscript(path: string): Promise<any[]> {
    if (!await fs.pathExists(path)) return [];

    const content = await fs.readFile(path, 'utf-8');
    return content.split('\n')
      .filter(line => line.trim())
      .map(line => JSON.parse(line));
  }

  private async appendToTranscript(path: string, entry: any): Promise<void> {
    await fs.ensureFile(path);
    await fs.appendFile(path, JSON.stringify(entry) + '\n');
  }

  // ========================================
  // OpenClaw: Vector Memory (Placeholder)
  // ========================================
  private async searchMemory(query: string, sessionKey: string): Promise<any[]> {
    // TODO: Implement LanceDB integration in Week 7-8
    return [];
  }

  private async captureToMemory(sessionKey: string, text: string): Promise<void> {
    // TODO: Implement LanceDB integration in Week 7-8
  }

  // ========================================
  // ZIMA: File Context
  // ========================================
  private async loadSessionFiles(sessionKey: string): Promise<any[]> {
    const filesPath = `${this.config.storage.files}/${sessionKey}/uploads`;
    if (!await fs.pathExists(filesPath)) return [];

    const filenames = await fs.readdir(filesPath);
    return Promise.all(filenames.map(async (name) => {
      const path = `${filesPath}/${name}`;
      const stats = await fs.stat(path);
      return {
        name,
        path,
        size: stats.size,
        content: stats.size < 50000 ? await fs.readFile(path, 'utf-8') : null
      };
    }));
  }

  private buildFileContext(files: any[], tier: number): string {
    if (files.length === 0) return '';

    let context = '## Session Files\n\n';
    for (const file of files) {
      if (tier === 0 && file.content) {
        context += `### ${file.name}\n${file.content}\n\n`;
      } else if (tier <= 2) {
        context += `- ${file.name} (${(file.size / 1024).toFixed(1)} KB)\n`;
      }
    }
    return context;
  }

  // ========================================
  // Combined: Hybrid System Prompt
  // ========================================
  private buildHybridSystemPrompt(params: any): string {
    return `You are ZIMA, an AI assistant with comprehensive capabilities.

## Capabilities
- Multi-channel communication (currently: ${params.channel})
- Document generation (196+ tools: Excel, PDF, Word, PowerPoint, Image processing)
- System control and automation
- Persistent memory with semantic search

## Available Tools

### Document Processing (196+ tools)
- Excel: create_excel, read_excel, edit_excel, add_chart, add_formulas, pivot_table
- PDF: create_pdf, merge_pdf, split_pdf, compress_pdf, ocr_pdf, sign_pdf
- Word: create_word, edit_word, merge_word, watermark, word_to_pdf
- PowerPoint: create_powerpoint, add_slide, animations, ppt_to_pdf
- Image: resize_image, crop_image, convert_format, watermark
- Conversions: json_to_excel, excel_to_pdf, csv_to_json, html_to_pdf

### System & Messaging
- session_send: Send messages to channels
- memory_search: Search persistent memory
- read_file, write_file: File operations
- browser_navigate, browser_screenshot: Web automation

${params.fileContext}

${params.memories.length > 0 ? `## Relevant Memories\n${params.memories.map(m => `- ${m.text}`).join('\n')}` : ''}

Be helpful, accurate, and efficient. Use tools when appropriate.`;
  }

  // ========================================
  // Agent Invocation (connects to Pi Agent Runtime)
  // ========================================
  private async invokeAgent(params: any): Promise<any> {
    // This will be implemented in Week 5-6 (Agent Runtime)
    // For now, call ZIMA Core directly
    const response = await axios.post(`${this.config.zima.apiUrl}/api/generate/stream`, {
      prompt: params.newMessage,
      messages: params.messages,
      sessionId: params.sessionKey,
      model: params.model,
      systemPrompt: params.systemPrompt
    });

    return {
      output: response.data.output,
      usage: response.data.usage,
      model: params.model,
      files: response.data.files || []
    };
  }

  private generateCacheKey(request: MessageRequest, messageCount: number): string {
    const str = `${request.message}:${request.sessionKey}:${messageCount}`;
    return createHash('sha256').update(str).digest('hex');
  }

  private async compactSession(transcriptPath: string): Promise<void> {
    // Keep last 20 + important messages, summarize rest
    const messages = await this.readTranscript(transcriptPath);
    const recent = messages.slice(-20);
    const toSummarize = messages.slice(0, -20);

    if (toSummarize.length === 0) return;

    // TODO: Use AI to summarize old messages
    const summary = {
      type: 'summary',
      content: `Summarized ${toSummarize.length} messages`,
      timestamp: Date.now()
    };

    // Rewrite transcript
    await fs.writeFile(transcriptPath, '');
    await this.appendToTranscript(transcriptPath, summary);
    for (const msg of recent) {
      await this.appendToTranscript(transcriptPath, msg);
    }
  }
}

type TaskComplexity = 'simple' | 'standard' | 'complex';

interface Lock {
  release: () => Promise<void>;
}

interface MessageRequest {
  sessionKey: string;
  message: string;
  attachments: any[];
  sender: any;
  channel: string;
  metadata: any;
}

interface MessageResponse {
  output: string;
  usage: any;
  model: string;
  files: any[];
  fromCache?: boolean;
}

interface CachedResponse {
  response: MessageResponse;
  timestamp: number;
}
```

**Deliverables**:
- ✅ Session write locks working
- ✅ Transcript read/write (.jsonl format)
- ✅ Task classification (Simple/Standard/Complex)
- ✅ Model selection (Haiku/Sonnet/Opus)
- ✅ Response caching (1-hour TTL)
- ✅ Context tier optimization (Tier 0-3)
- ✅ File context loading
- ✅ Hybrid system prompt builder
- ✅ Session compaction

---

### **Week 5-6: Agent Runtime Integration**

#### **Goal**: Integrate Pi Agent Core with ZIMA tools

**Tasks**:

1. **Install Pi Agent SDK**
```bash
npm install @mariozechner/pi-agent-core
npm install @anthropic-ai/sdk openai
```

2. **Agent Runtime** (`src/agent/hybrid-agent-runtime.ts`)
```typescript
import { createAgentSession, streamSimple } from '@mariozechner/pi-agent-core';
import Anthropic from '@anthropic-ai/sdk';
import axios from 'axios';

export class HybridAgentRuntime {
  private anthropic: Anthropic;
  private config: any;

  constructor() {
    this.anthropic = new Anthropic({
      apiKey: process.env.ANTHROPIC_API_KEY
    });
    this.config = this.loadConfig();
  }

  async executeAgent(context: AgentContext): Promise<AgentResult> {
    // ========================================
    // Initialize Agent Session (OpenClaw)
    // ========================================
    const agentSession = await createAgentSession({
      sessionKey: context.sessionKey,
      model: context.model,
      systemPrompt: context.systemPrompt,
      tools: await this.buildUnifiedToolRegistry()
    });

    // ========================================
    // Execute Agent Loop
    // ========================================
    let conversationHistory = [
      ...context.messages,
      {
        role: 'user',
        content: context.newMessage
      }
    ];

    const result = await streamSimple({
      model: context.model,
      messages: conversationHistory,
      system: context.systemPrompt,
      tools: agentSession.tools,
      onChunk: (chunk) => {
        // Broadcast real-time chunks via WebSocket
        this.broadcastChatDelta({
          runId: context.runId,
          sessionKey: context.sessionKey,
          delta: chunk.content,
          state: 'streaming'
        });
      },
      onToolCall: async (toolCall) => {
        return await this.executeUnifiedTool(toolCall, context);
      }
    });

    return {
      output: result.content,
      usage: result.usage,
      model: context.model,
      files: result.files || []
    };
  }

  // ========================================
  // Unified Tool Registry (Combined)
  // ========================================
  private async buildUnifiedToolRegistry(): Promise<Tool[]> {
    const tools: Tool[] = [];

    // 1. ZIMA Document Tools (196+)
    tools.push(...this.getZimaDocumentTools());

    // 2. OpenClaw System Tools
    tools.push(...this.getOpenClawSystemTools());

    // 3. Custom Skills
    tools.push(...await this.loadSkills());

    return tools;
  }

  private getZimaDocumentTools(): Tool[] {
    return [
      {
        name: 'create_excel',
        description: 'Create an Excel spreadsheet with data, formulas, and formatting',
        inputSchema: {
          type: 'object',
          properties: {
            filename: { type: 'string' },
            sheets: { type: 'array' },
            data: { type: 'object' }
          },
          required: ['filename', 'data']
        }
      },
      {
        name: 'create_pdf',
        description: 'Create a PDF document from text or template',
        inputSchema: {
          type: 'object',
          properties: {
            filename: { type: 'string' },
            content: { type: 'string' },
            layout: { type: 'string' }
          },
          required: ['filename', 'content']
        }
      },
      // ... 194+ more tools
    ];
  }

  private getOpenClawSystemTools(): Tool[] {
    return [
      {
        name: 'session_send',
        description: 'Send a message to a channel',
        inputSchema: {
          type: 'object',
          properties: {
            channel: { type: 'string' },
            to: { type: 'string' },
            message: { type: 'string' }
          },
          required: ['channel', 'to', 'message']
        }
      },
      {
        name: 'memory_search',
        description: 'Search persistent memory for relevant information',
        inputSchema: {
          type: 'object',
          properties: {
            query: { type: 'string' },
            limit: { type: 'number' }
          },
          required: ['query']
        }
      }
      // ... more system tools
    ];
  }

  // ========================================
  // Execute Unified Tool
  // ========================================
  private async executeUnifiedTool(toolCall: ToolCall, context: AgentContext): Promise<ToolResult> {
    const { name, input } = toolCall;

    // Route to appropriate executor
    if (this.isZimaDocumentTool(name)) {
      return await this.executeZimaTool(name, input, context);
    } else if (this.isOpenClawSystemTool(name)) {
      return await this.executeOpenClawTool(name, input, context);
    } else {
      throw new Error(`Unknown tool: ${name}`);
    }
  }

  private async executeZimaTool(
    name: string,
    input: any,
    context: AgentContext
  ): Promise<ToolResult> {
    // Call ZIMA's .NET backend
    const response = await axios.post(`${this.config.zima.apiUrl}/api/tools/execute`, {
      tool: name,
      args: input,
      sessionId: context.sessionKey
    });

    return {
      success: response.data.success,
      output: response.data.output,
      files: response.data.files
    };
  }

  private async executeOpenClawTool(
    name: string,
    input: any,
    context: AgentContext
  ): Promise<ToolResult> {
    switch (name) {
      case 'session_send':
        return await this.handleSessionSend(input, context);

      case 'memory_search':
        return await this.handleMemorySearch(input, context);

      default:
        throw new Error(`Unimplemented tool: ${name}`);
    }
  }

  private async handleSessionSend(input: any, context: AgentContext): Promise<ToolResult> {
    // TODO: Implement in Week 9-10 (Outbound delivery)
    return { success: true, output: 'Message queued' };
  }

  private async handleMemorySearch(input: any, context: AgentContext): Promise<ToolResult> {
    // TODO: Implement in Week 7-8 (Memory system)
    return { success: true, output: 'No memories found' };
  }

  private isZimaDocumentTool(name: string): boolean {
    const zimaTools = [
      'create_excel', 'read_excel', 'edit_excel', 'add_chart', 'add_formulas',
      'create_pdf', 'merge_pdf', 'split_pdf', 'compress_pdf', 'ocr_pdf',
      'create_word', 'edit_word', 'merge_word', 'watermark',
      'create_powerpoint', 'add_slide', 'animations',
      'resize_image', 'crop_image', 'convert_image_format',
      // ... 180+ more
    ];
    return zimaTools.includes(name);
  }

  private isOpenClawSystemTool(name: string): boolean {
    return ['session_send', 'memory_search', 'read_file', 'write_file'].includes(name);
  }

  private broadcastChatDelta(data: any): void {
    // Broadcast to WebSocket clients
    // TODO: Implement WebSocket broadcasting
  }
}

interface AgentContext {
  sessionKey: string;
  model: string;
  systemPrompt: string;
  messages: any[];
  newMessage: string;
  runId: string;
}

interface AgentResult {
  output: string;
  usage: any;
  model: string;
  files: any[];
}

interface Tool {
  name: string;
  description: string;
  inputSchema: any;
}

interface ToolCall {
  name: string;
  input: any;
}

interface ToolResult {
  success: boolean;
  output: string;
  files?: any[];
}
```

**Deliverables**:
- ✅ Pi Agent Core integrated
- ✅ Unified tool registry (296+ tools)
- ✅ Tool routing (ZIMA vs OpenClaw)
- ✅ Agent loop execution
- ✅ Streaming support

---

### **Week 7-8: Vector Memory System**

#### **Goal**: Implement LanceDB vector memory for semantic recall

**Tasks**:

1. **Install Dependencies**
```bash
npm install vectordb @lancedb/lancedb
npm install openai  # For embeddings
```

2. **Memory System** (`src/memory/memory-manager.ts`)
```typescript
import { connect, Table } from 'vectordb';
import OpenAI from 'openai';

export class MemoryManager {
  private db: any;
  private table: Table | null;
  private openai: OpenAI;

  constructor() {
    this.openai = new OpenAI({
      apiKey: process.env.OPENAI_API_KEY
    });
    this.table = null;
  }

  async initialize(dbPath: string) {
    this.db = await connect(dbPath);

    try {
      this.table = await this.db.openTable('memories');
    } catch {
      // Create table if doesn't exist
      this.table = await this.db.createTable('memories', [
        { id: 'mem-001', text: 'Sample memory', vector: new Array(1536).fill(0), importance: 0.5, category: 'fact', createdAt: Date.now() }
      ]);
    }
  }

  async search(query: string, limit: number = 5): Promise<Memory[]> {
    if (!this.table) throw new Error('Memory not initialized');

    // Embed query
    const queryVector = await this.embed(query);

    // Search
    const results = await this.table
      .search(queryVector)
      .limit(limit)
      .execute();

    return results.map((r: any) => ({
      id: r.id,
      text: r.text,
      importance: r.importance,
      category: r.category,
      createdAt: r.createdAt
    }));
  }

  async store(text: string, category: string = 'general'): Promise<void> {
    if (!this.table) throw new Error('Memory not initialized');

    const vector = await this.embed(text);
    const importance = this.calculateImportance(text);

    await this.table.add([{
      id: `mem-${Date.now()}-${Math.random()}`,
      text,
      vector,
      importance,
      category,
      createdAt: Date.now()
    }]);
  }

  private async embed(text: string): Promise<number[]> {
    const response = await this.openai.embeddings.create({
      model: 'text-embedding-3-small',
      input: text
    });
    return response.data[0].embedding;
  }

  private calculateImportance(text: string): number {
    // Simple heuristic - can be improved with AI
    let score = 0.5;

    if (text.length > 100) score += 0.1;
    if (/\b(important|critical|remember|note)\b/i.test(text)) score += 0.2;
    if (text.includes('?')) score += 0.1; // Questions are important

    return Math.min(score, 1.0);
  }
}

interface Memory {
  id: string;
  text: string;
  importance: number;
  category: string;
  createdAt: number;
}
```

**Deliverables**:
- ✅ LanceDB integration
- ✅ Vector embeddings (OpenAI)
- ✅ Semantic search
- ✅ Memory storage
- ✅ Importance calculation

---

### **Week 9-10: Channel Adapters**

#### **Goal**: Implement WebChat, WhatsApp, and Email channels

**1. WebChat Adapter** (`src/channels/webchat-adapter.ts`)
```typescript
import { WebSocket } from 'ws';
import axios from 'axios';

export class WebChatAdapter {
  private ws: WebSocket;
  private laravelUrl: string;

  constructor(config: any) {
    this.laravelUrl = config.laravelUrl;
  }

  async handleMessage(message: any) {
    // Normalize Laravel Livewire message
    return {
      text: message.message,
      senderId: message.sessionId,
      channel: 'webchat',
      chatType: 'direct',
      attachments: message.files || [],
      sender: {
        channel: 'webchat',
        channelUserId: message.sessionId,
        displayName: message.userName || 'User'
      }
    };
  }

  async sendMessage(params: any) {
    // Send to Laravel via webhook
    await axios.post(`${this.laravelUrl}/api/gateway/webhook`, {
      sessionId: params.sessionId,
      message: params.message,
      files: params.files
    });
  }
}
```

**2. WhatsApp Adapter** (`src/channels/whatsapp-adapter.ts`)
```typescript
import makeWASocket, { useMultiFileAuthState } from '@whiskeysockets/baileys';

export class WhatsAppAdapter {
  private sock: any;

  async start() {
    const { state, saveCreds } = await useMultiFileAuthState('./sessions/whatsapp');

    this.sock = makeWASocket({
      auth: state,
      printQRInTerminal: true
    });

    this.sock.ev.on('creds.update', saveCreds);
    this.sock.ev.on('messages.upsert', async ({ messages }) => {
      for (const m of messages) {
        if (m.message) {
          await this.handleMessage(m);
        }
      }
    });
  }

  async handleMessage(m: any) {
    const text = m.message.conversation || m.message.extendedTextMessage?.text || '';

    return {
      text,
      senderId: m.key.remoteJid,
      channel: 'whatsapp',
      chatType: m.key.remoteJid.endsWith('@g.us') ? 'group' : 'direct',
      attachments: [],
      sender: {
        channel: 'whatsapp',
        channelUserId: m.key.remoteJid,
        displayName: m.pushName || 'Unknown'
      }
    };
  }

  async sendMessage(params: any) {
    await this.sock.sendMessage(params.jid, {
      text: params.message
    });
  }
}
```

**3. Email Adapter** (`src/channels/email-adapter.ts`)
```typescript
import Imap from 'imap';
import nodemailer from 'nodemailer';
import { simpleParser } from 'mailparser';

export class EmailAdapter {
  private imap: any;
  private smtp: any;

  constructor(config: any) {
    this.imap = new Imap({
      user: config.imap.user,
      password: config.imap.password,
      host: config.imap.host,
      port: config.imap.port,
      tls: true
    });

    this.smtp = nodemailer.createTransport({
      host: config.smtp.host,
      port: config.smtp.port,
      auth: {
        user: config.smtp.user,
        pass: config.smtp.password
      }
    });
  }

  async start() {
    this.imap.once('ready', () => {
      this.imap.openBox('INBOX', false, () => {
        this.imap.on('mail', () => this.fetchNewMessages());
      });
    });

    this.imap.connect();
  }

  async fetchNewMessages() {
    this.imap.search(['UNSEEN'], (err, results) => {
      if (err || !results.length) return;

      const fetch = this.imap.fetch(results, { bodies: '' });
      fetch.on('message', (msg) => {
        msg.on('body', async (stream) => {
          const parsed = await simpleParser(stream);
          await this.handleMessage(parsed);
        });
      });
    });
  }

  async handleMessage(parsed: any) {
    return {
      text: parsed.text || '',
      senderId: parsed.from.value[0].address,
      channel: 'email',
      chatType: 'direct',
      attachments: parsed.attachments || [],
      sender: {
        channel: 'email',
        channelUserId: parsed.from.value[0].address,
        displayName: parsed.from.value[0].name || 'Unknown'
      },
      threadId: parsed.messageId,
      subject: parsed.subject
    };
  }

  async sendMessage(params: any) {
    await this.smtp.sendMail({
      from: process.env.EMAIL_FROM,
      to: params.to,
      subject: params.subject || 'Re: Your message',
      html: this.markdownToHtml(params.message),
      attachments: params.files?.map(f => ({
        filename: f.name,
        path: f.url
      }))
    });
  }

  private markdownToHtml(markdown: string): string {
    // Simple markdown conversion
    return markdown
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n/g, '<br>');
  }
}
```

**Deliverables**:
- ✅ WebChat adapter (Laravel integration)
- ✅ WhatsApp adapter (Baileys)
- ✅ Email adapter (IMAP/SMTP)
- ✅ Message normalization
- ✅ Outbound sending

---

### **Week 11-12: Laravel Integration & Deployment**

#### **Goal**: Connect Laravel frontend to gateway and deploy

**1. Laravel Webhook Controller** (`app/Http/Controllers/GatewayWebhookController.php`)
```php
<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Models\ChatSession;
use App\Models\ChatMessage;

class GatewayWebhookController extends Controller
{
    public function handleIncomingMessage(Request $request)
    {
        $validated = $request->validate([
            'sessionId' => 'required|string',
            'message' => 'required|string',
            'files' => 'nullable|array'
        ]);

        $session = ChatSession::where('id', $validated['sessionId'])->first();

        if (!$session) {
            return response()->json(['error' => 'Session not found'], 404);
        }

        // Add assistant message to database
        $message = $session->addMessage('assistant', $validated['message']);

        // Broadcast to Livewire component
        broadcast(new \App\Events\MessageReceived([
            'sessionId' => $validated['sessionId'],
            'message' => $validated['message'],
            'files' => $validated['files'] ?? [],
            'timestamp' => now()
        ]));

        return response()->json(['success' => true]);
    }
}
```

**2. Update Livewire Component** (`app/Livewire/FileGenerator.php`)
```php
public function generate()
{
    // Instead of calling ZIMA directly, call gateway
    $response = Http::post('http://localhost:18789/api/chat', [
        'sessionKey' => $this->currentSessionId,
        'message' => $this->prompt,
        'channel' => 'webchat',
        'sender' => [
            'userId' => Auth::id() ?? 'guest',
            'userName' => Auth::user()->name ?? 'Guest'
        ]
    ]);

    if ($response->successful()) {
        $data = $response->json();
        $this->messages[] = [
            'role' => 'assistant',
            'content' => $data['output'],
            'files' => $data['files'] ?? [],
            'timestamp' => now()->format('H:i')
        ];
    }
}
```

**3. Docker Compose** (`docker-compose.yml`)
```yaml
version: '3.8'

services:
  gateway:
    build: ./gateway
    ports:
      - "18789:18789"
    environment:
      - ANTHROPIC_API_KEY=${ANTHROPIC_API_KEY}
      - OPENAI_API_KEY=${OPENAI_API_KEY}
    volumes:
      - ./storage/zima:/home/zima/.zima
    depends_on:
      - zima-core

  zima-core:
    build: ./zima-file-service
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_URLS=http://+:5000
    volumes:
      - ./storage/generated_files:/app/generated_files

  frontend:
    build: ./zima-frontend
    ports:
      - "8000:8000"
    environment:
      - APP_URL=http://localhost:8000
      - GATEWAY_URL=http://gateway:18789
    depends_on:
      - gateway
      - mysql

  mysql:
    image: mysql:8.0
    ports:
      - "3306:3306"
    environment:
      - MYSQL_ROOT_PASSWORD=root
      - MYSQL_DATABASE=zima
    volumes:
      - mysql_data:/var/lib/mysql

volumes:
  mysql_data:
```

**Deliverables**:
- ✅ Laravel webhook integration
- ✅ Gateway HTTP API
- ✅ Docker Compose setup
- ✅ Production deployment guide
- ✅ Full system testing

---

## **Architecture Benefits**

### **From OpenClaw:**
✅ Multi-channel routing (WebChat, WhatsApp, Email)
✅ Session write locks (prevent race conditions)
✅ Vector memory (LanceDB semantic search)
✅ Transcript-based storage (.jsonl durability)
✅ Skills system (extensible workflows)
✅ Reply dispatcher (human delay simulation)
✅ Idempotency deduplication

### **From ZIMA:**
✅ 196+ document processing tools
✅ Task classification (Simple/Standard/Complex)
✅ Dynamic model selection (Haiku/Sonnet/Opus)
✅ Response caching (1-hour TTL)
✅ Context tier optimization (Tier 0-3)
✅ File context loading
✅ Usage tracking & cost optimization
✅ Laravel authentication

### **Combined Innovations:**
✅ Unified tool registry (296+ tools)
✅ Hybrid system prompt (identity + tools + files + memory)
✅ Dual streaming (WebSocket + SSE)
✅ Intelligent compaction (summarize old, keep important)
✅ Multi-layer security (DM pairing + auth + policies)
✅ Hybrid storage (JSONL + LanceDB + MySQL)

---

## **Performance Targets**

| Metric | Target |
|--------|--------|
| Cold Start | <2s |
| First Response (cached) | <500ms |
| First Response (uncached) | 2-8s |
| Streaming Latency | <100ms |
| Concurrent Sessions | 500+ |
| Cache Hit Rate | 25-35% |
| Cost Savings (simple tasks) | 73% (via Haiku) |

---

## **Security Checklist**

- [ ] DM pairing for WhatsApp
- [ ] Laravel authentication for WebChat
- [ ] Send policy enforcement
- [ ] Tool policy filtering
- [ ] Session write locks
- [ ] HTTPS/TLS for gateway
- [ ] Environment variable secrets
- [ ] Rate limiting
- [ ] Input sanitization

---

## **Success Criteria**

1. ✅ Gateway routes messages from 3 channels (WebChat, WhatsApp, Email)
2. ✅ Context manager implements both ZIMA + OpenClaw techniques
3. ✅ Agent runtime executes 296+ unified tools
4. ✅ Vector memory provides semantic recall
5. ✅ Response caching reduces costs by 25%+
6. ✅ Laravel frontend integrates seamlessly
7. ✅ Docker deployment works end-to-end
8. ✅ Streaming works in real-time (<100ms latency)

---

**Next Steps**: Begin Week 1 implementation - Gateway Foundation!
