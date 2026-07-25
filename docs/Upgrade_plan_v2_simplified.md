# ZIMA 2.0 Upgrade Plan (Simplified Server Edition)

**Version:** 2.0 (Server-Based)
**Date:** January 31, 2026
**Status:** Planning Phase
**Branch:** `dev/major-refactor`
**Deployment:** Linux Server with Laravel Frontend

---

## Executive Summary

This simplified plan transforms **ZIMA** into a **server-based AI assistant** with:

✅ **3 Channels**: WebChat (Laravel), WhatsApp, Email
✅ **Existing Frontend**: Use `/Volumes/DATA/QWEN/zima-frontend` (Laravel)
✅ **Server Deployment**: Linux server, no desktop app
✅ **Server-Managed Skills**: No marketplace, all skills on server
🚀 **Focus**: Core features, no community/desktop overhead

**Timeline:** 8-10 weeks (vs 16 weeks in full plan)

---

## Table of Contents

1. [Scope Definition](#1-scope-definition)
2. [Simplified Architecture](#2-simplified-architecture)
3. [Phase 1: Foundation (Weeks 1-3)](#phase-1-foundation-weeks-1-3)
4. [Phase 2: Channels (Weeks 4-6)](#phase-2-channels-weeks-4-6)
5. [Phase 3: Advanced Features (Weeks 7-8)](#phase-3-advanced-features-weeks-7-8)
6. [Phase 4: Deployment (Weeks 9-10)](#phase-4-deployment-weeks-9-10)
7. [Implementation Guide](#implementation-guide)
8. [Deployment Configuration](#deployment-configuration)

---

## 1. Scope Definition

### 1.1 What We're Building

**IN SCOPE:**
- ✅ WebChat (existing Laravel frontend)
- ✅ WhatsApp channel integration
- ✅ Email channel integration
- ✅ Skills system (SKILL.md format, server-managed)
- ✅ Persistent memory (vector database)
- ✅ Browser automation (Playwright)
- ✅ Workspace integration (Git + LSP)
- ✅ Proactive heartbeat system
- ✅ Multi-user support

**OUT OF SCOPE:**
- ❌ Desktop applications (Electron)
- ❌ SkillHub/marketplace
- ❌ Discord, Slack, Telegram, Signal
- ❌ Native mobile apps
- ❌ Community features
- ❌ Advanced collaboration (real-time sync)

### 1.2 Channel Details

| Channel | Implementation | Status |
|---------|---------------|--------|
| **WebChat** | Laravel frontend at `/Volumes/DATA/QWEN/zima-frontend` | ✅ Existing |
| **WhatsApp** | Baileys (Node.js gateway) | 🆕 New |
| **Email** | IMAP/SMTP integration | 🆕 New |

---

## 2. Simplified Architecture

### 2.1 System Overview

```
┌─────────────────────────────────────────────────────────┐
│                     FRONTEND LAYER                       │
│  Laravel WebChat (/Volumes/DATA/QWEN/zima-frontend)    │
│  Port: 8000                                             │
└────────────────────────┬────────────────────────────────┘
                         │
                         │ HTTP/SSE
                         │
┌────────────────────────▼────────────────────────────────┐
│                 CHANNEL GATEWAY (Node.js)                │
│  Port: 18789 (WebSocket for internal comms)            │
│  ┌──────────────┬──────────────┬──────────────┐        │
│  │  WebChat     │  WhatsApp    │   Email      │        │
│  │  Proxy       │  (Baileys)   │  (Nodemailer)│        │
│  └──────┬───────┴──────┬───────┴──────┬───────┘        │
└─────────┼──────────────┼──────────────┼────────────────┘
          │              │              │
          └──────────────┴──────────────┘
                         │
                         │ HTTP API
                         │
┌────────────────────────▼────────────────────────────────┐
│               ZIMA CORE (C#/.NET 9.0)                   │
│  Port: 5000 (HTTP API)                                  │
│                                                          │
│  ┌───────────────────────────────────────────────┐     │
│  │  Core Services                                 │     │
│  │  • 196 Document Tools                         │     │
│  │  • MCP Server                                 │     │
│  │  • Agent System                               │     │
│  │  • Session Management                         │     │
│  │  • Permission System                          │     │
│  └───────────────────────────────────────────────┘     │
│                                                          │
│  ┌───────────────────────────────────────────────┐     │
│  │  New Services                                  │     │
│  │  • Skills System                              │     │
│  │  • Vector Memory (Qdrant)                     │     │
│  │  • Browser Service (Playwright)               │     │
│  │  • Git Service                                │     │
│  │  • Email Service                              │     │
│  │  • Heartbeat/Scheduler                        │     │
│  └───────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────┘
                         │
          ┌──────────────┴──────────────┐
          │                             │
┌─────────▼─────────┐         ┌─────────▼─────────┐
│  Qdrant           │         │  Redis            │
│  (Vector DB)      │         │  (Cache/Queue)    │
│  Port: 6333       │         │  Port: 6379       │
└───────────────────┘         └───────────────────┘
```

### 2.2 Technology Stack

| Component | Technology | Notes |
|-----------|-----------|-------|
| **Frontend** | Laravel + Blade/Livewire | ✅ Keep existing at port 8000 |
| **Core** | C#/.NET 9.0 | ✅ Keep existing at port 5000 |
| **Gateway** | Node.js/TypeScript | 🆕 New for WhatsApp/Email routing |
| **Vector DB** | Qdrant | 🆕 New for persistent memory |
| **Cache/Queue** | Redis | 🆕 New for performance |
| **Browser** | Playwright (C#) | 🆕 New for automation |
| **Email** | Nodemailer (Node.js) | 🆕 New for email channel |
| **WhatsApp** | Baileys (Node.js) | 🆕 New for WhatsApp channel |

---

## Phase 1: Foundation (Weeks 1-3)

### Week 1: Gateway Setup + Laravel Integration

**Goal:** Create Node.js gateway that routes WhatsApp/Email to ZIMA, integrate with Laravel frontend

#### Day 1-2: Gateway Scaffold
```bash
# Create gateway project
cd /Volumes/DATA/QWEN
mkdir gateway
cd gateway
npm init -y
npm install ws express axios dotenv cors
npm install -D typescript @types/node @types/express @types/ws
npx tsc --init
```

**Files to Create:**
```
gateway/
├── src/
│   ├── server.ts                    # Main entry point
│   ├── zima-client.ts               # HTTP client to ZIMA API
│   ├── channels/
│   │   ├── base.ts                  # IChannelAdapter interface
│   │   ├── webchat.ts               # WebChat proxy (Laravel integration)
│   │   └── index.ts
│   ├── config/
│   │   └── index.ts
│   └── types/
│       └── index.ts
├── package.json
├── tsconfig.json
└── .env.example
```

**Gateway Server (`src/server.ts`):**
```typescript
import express from 'express';
import { WebSocketServer } from 'ws';
import cors from 'cors';
import { ZimaClient } from './zima-client';
import { WebChatAdapter } from './channels/webchat';

const app = express();
const PORT = process.env.GATEWAY_PORT || 18789;
const ZIMA_URL = process.env.ZIMA_API_URL || 'http://localhost:5000';

app.use(cors());
app.use(express.json({ limit: '100mb' }));

const zimaClient = new ZimaClient(ZIMA_URL);
const wss = new WebSocketServer({ noServer: true });

// HTTP endpoint for Laravel frontend
app.post('/api/chat', async (req, res) => {
  const { message, sessionId, files } = req.body;

  try {
    const response = await zimaClient.sendMessage({
      prompt: message,
      sessionId,
      files
    });

    res.json(response);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// SSE endpoint for streaming
app.get('/api/chat/stream', async (req, res) => {
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');

  const { message, sessionId } = req.query;

  const stream = await zimaClient.streamMessage({
    prompt: message as string,
    sessionId: sessionId as string
  });

  stream.on('data', (chunk) => {
    res.write(`data: ${JSON.stringify(chunk)}\n\n`);
  });

  stream.on('end', () => res.end());
});

app.listen(PORT, () => {
  console.log(`Gateway running on port ${PORT}`);
});
```

**ZIMA Client (`src/zima-client.ts`):**
```typescript
import axios from 'axios';
import { EventEmitter } from 'events';

export interface ChatRequest {
  prompt: string;
  sessionId?: string;
  files?: any[];
}

export class ZimaClient {
  private baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  async sendMessage(request: ChatRequest): Promise<any> {
    const response = await axios.post(`${this.baseUrl}/api/generate`, {
      prompt: request.prompt,
      sessionId: request.sessionId,
      files: request.files
    });

    return response.data;
  }

  async streamMessage(request: ChatRequest): Promise<EventEmitter> {
    const emitter = new EventEmitter();

    const response = await axios.post(
      `${this.baseUrl}/api/generate/stream`,
      request,
      {
        responseType: 'stream'
      }
    );

    response.data.on('data', (chunk: Buffer) => {
      const lines = chunk.toString().split('\n');
      for (const line of lines) {
        if (line.startsWith('data: ')) {
          const data = JSON.parse(line.slice(6));
          emitter.emit('data', data);
        }
      }
    });

    response.data.on('end', () => emitter.emit('end'));

    return emitter;
  }
}
```

#### Day 3-4: Laravel Frontend Integration

**Update Laravel routes (`zima-frontend/routes/api.php`):**
```php
<?php
use Illuminate\Support\Facades\Route;
use App\Http\Controllers\ChatController;

Route::post('/chat', [ChatController::class, 'sendMessage']);
Route::get('/chat/stream', [ChatController::class, 'streamMessage']);
Route::post('/files/upload', [ChatController::class, 'uploadFile']);
Route::get('/sessions/{sessionId}', [ChatController::class, 'getSession']);
```

**Create ChatController (`zima-frontend/app/Http/Controllers/ChatController.php`):**
```php
<?php
namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Http;

class ChatController extends Controller
{
    private $gatewayUrl;

    public function __construct()
    {
        $this->gatewayUrl = env('GATEWAY_URL', 'http://localhost:18789');
    }

    public function sendMessage(Request $request)
    {
        $validated = $request->validate([
            'message' => 'required|string',
            'sessionId' => 'nullable|string',
            'files' => 'nullable|array'
        ]);

        $response = Http::post("{$this->gatewayUrl}/api/chat", [
            'message' => $validated['message'],
            'sessionId' => $validated['sessionId'] ?? null,
            'files' => $validated['files'] ?? []
        ]);

        return $response->json();
    }

    public function streamMessage(Request $request)
    {
        $message = $request->query('message');
        $sessionId = $request->query('sessionId');

        return response()->stream(function () use ($message, $sessionId) {
            $url = "{$this->gatewayUrl}/api/chat/stream";
            $url .= "?message=" . urlencode($message);
            if ($sessionId) {
                $url .= "&sessionId=" . urlencode($sessionId);
            }

            $ch = curl_init($url);
            curl_setopt($ch, CURLOPT_WRITEFUNCTION, function($ch, $data) {
                echo $data;
                ob_flush();
                flush();
                return strlen($data);
            });
            curl_exec($ch);
            curl_close($ch);
        }, 200, [
            'Content-Type' => 'text/event-stream',
            'Cache-Control' => 'no-cache',
            'X-Accel-Buffering' => 'no'
        ]);
    }
}
```

**Update `.env` in Laravel frontend:**
```env
GATEWAY_URL=http://localhost:18789
ZIMA_API_URL=http://localhost:5000
```

#### Day 5: Testing & Integration

**Tasks:**
- [ ] Test Laravel → Gateway → ZIMA flow
- [ ] Test file uploads
- [ ] Test streaming responses
- [ ] Verify session management

**Deliverables Week 1:**
- ✅ Gateway running on port 18789
- ✅ Laravel can send messages through gateway
- ✅ Streaming works from Laravel
- ✅ File uploads functional

---

### Week 2: Skills System Implementation

**Goal:** Create SKILL.md system for server-managed skills

#### Day 1-2: Skill Infrastructure (C#)

**Create Skill Model (`Core/Skills/Skill.cs`):**
```csharp
namespace ZimaFileService.Core.Skills;

public class Skill
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public List<string> Tools { get; set; } = new();
    public List<string> Os { get; set; } = new();
    public Dictionary<string, List<string>> Requires { get; set; } = new();
    public required string Content { get; set; }
    public bool IsActive { get; set; } = true;
    public string FilePath { get; set; } = string.Empty;
}
```

**Create Skill Loader (`Core/Skills/SkillLoader.cs`):**
```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ZimaFileService.Core.Skills;

public class SkillLoader
{
    private readonly string _skillsDirectory;

    public SkillLoader(string skillsDirectory)
    {
        _skillsDirectory = skillsDirectory;
    }

    public async Task<List<Skill>> LoadAllSkillsAsync()
    {
        var skills = new List<Skill>();
        var skillDirs = Directory.GetDirectories(_skillsDirectory);

        foreach (var dir in skillDirs)
        {
            var skillFile = Path.Combine(dir, "SKILL.md");
            if (!File.Exists(skillFile)) continue;

            var skill = await LoadSkillFromFileAsync(skillFile);
            if (skill != null) skills.Add(skill);
        }

        return skills;
    }

    private async Task<Skill?> LoadSkillFromFileAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);

        // Extract YAML frontmatter
        if (!content.StartsWith("---")) return null;

        var endOfFrontmatter = content.IndexOf("---", 3);
        if (endOfFrontmatter == -1) return null;

        var yaml = content[3..endOfFrontmatter].Trim();
        var markdown = content[(endOfFrontmatter + 3)..].Trim();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var metadata = deserializer.Deserialize<Dictionary<string, object>>(yaml);

        var skill = new Skill
        {
            Name = metadata.GetValueOrDefault("name")?.ToString() ?? "",
            Description = metadata.GetValueOrDefault("description")?.ToString() ?? "",
            Category = metadata.GetValueOrDefault("category")?.ToString() ?? "general",
            Content = markdown,
            FilePath = filePath
        };

        if (metadata.ContainsKey("tools") && metadata["tools"] is List<object> tools)
        {
            skill.Tools = tools.Select(t => t.ToString() ?? "").ToList();
        }

        if (metadata.ContainsKey("os") && metadata["os"] is List<object> os)
        {
            skill.Os = os.Select(o => o.ToString() ?? "").ToList();
        }

        return skill;
    }
}
```

**Add to `zima-file-service.csproj`:**
```xml
<PackageReference Include="YamlDotNet" Version="15.1.0" />
```

#### Day 3-4: Create Initial Skills

**Create skill directories:**
```bash
mkdir -p Skills/bundled/document-processing
mkdir -p Skills/bundled/excel-operations
mkdir -p Skills/bundled/pdf-manipulation
mkdir -p Skills/bundled/word-processing
mkdir -p Skills/bundled/email-automation
```

**Example: `Skills/bundled/document-processing/SKILL.md`:**
```markdown
---
name: document-processing
description: Comprehensive document generation and manipulation
category: documents
tools:
  - create_excel
  - create_word
  - create_pdf
  - create_powerpoint
  - read_excel
os: [windows, macos, linux]
---

# Document Processing

ZIMA can create and manipulate professional documents in all major formats.

## Excel Spreadsheets

Create Excel files with:
- **Data tables** - Structured data in rows/columns
- **Formulas** - SUM, AVERAGE, VLOOKUP, etc.
- **Charts** - Bar, line, pie, scatter charts
- **Formatting** - Colors, borders, fonts, conditional formatting

Example: "Create an Excel file with Q1 sales data and a chart"

## Word Documents

Create Word documents with:
- **Headings and styles** - Professional formatting
- **Tables** - Data presentation
- **Images** - Embedded pictures
- **Table of Contents** - Auto-generated from headings

Example: "Create a meeting notes document with today's agenda"

## PDF Documents

Create PDF files with:
- **Professional layout** - Multi-column, headers/footers
- **Images and graphics** - Embedded media
- **Digital signatures** - Sign documents electronically
- **Encryption** - Password-protected PDFs

Example: "Create a PDF invoice for customer XYZ"

## PowerPoint Presentations

Create presentations with:
- **Multiple slides** - Title, content, image slides
- **Transitions** - Slide animations
- **Charts and data** - Visual data presentation
- **Speaker notes** - Hidden presenter notes

Example: "Create a 10-slide presentation about renewable energy"
```

**Example: `Skills/bundled/email-automation/SKILL.md`:**
```markdown
---
name: email-automation
description: Send and manage emails automatically
category: communication
tools:
  - email_send
  - email_read
  - email_search
os: [windows, macos, linux]
requires:
  env_vars: [EMAIL_HOST, EMAIL_USER, EMAIL_PASSWORD]
---

# Email Automation

ZIMA can send and manage emails on your behalf.

## Sending Emails

Use `email_send` to send emails:
- **To/CC/BCC** - Multiple recipients
- **Attachments** - Include generated files
- **HTML/Plain text** - Rich or simple formatting
- **Templates** - Use email templates

Example: "Send an email to john@example.com with the Q1 report attached"

## Reading Emails

Use `email_read` to check inbox:
- **Filter by sender** - From specific people
- **Filter by subject** - Search subject lines
- **Date range** - Recent emails only
- **Mark as read/unread** - Update status

Example: "Check for emails from the boss about the project"

## Searching Emails

Use `email_search` to find specific messages:
- **Keyword search** - Search email content
- **Date filters** - Within date range
- **Attachment filters** - Emails with files
- **Folder/label filters** - Search specific folders

Example: "Find all emails with 'invoice' in the subject from last month"
```

#### Day 5: Skill Registry & API

**Create Skill Registry (`Core/Skills/SkillRegistry.cs`):**
```csharp
namespace ZimaFileService.Core.Skills;

public class SkillRegistry
{
    private readonly List<Skill> _skills = new();
    private readonly SkillLoader _loader;

    public SkillRegistry(string skillsDirectory)
    {
        _loader = new SkillLoader(skillsDirectory);
    }

    public async Task LoadAllAsync()
    {
        _skills.Clear();
        var skills = await _loader.LoadAllSkillsAsync();
        _skills.AddRange(skills);
    }

    public List<Skill> GetActiveSkills() =>
        _skills.Where(s => s.IsActive).ToList();

    public Skill? GetSkill(string name) =>
        _skills.FirstOrDefault(s => s.Name == name);

    public string GetSkillsContext()
    {
        var activeSkills = GetActiveSkills();
        if (!activeSkills.Any()) return "";

        var sb = new StringBuilder();
        sb.AppendLine("# Available Skills\n");

        foreach (var skill in activeSkills)
        {
            sb.AppendLine($"## {skill.Name}");
            sb.AppendLine($"{skill.Description}\n");
            sb.AppendLine(skill.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
```

**Create Skills Controller (`Api/SkillsController.cs`):**
```csharp
using Microsoft.AspNetCore.Mvc;
using ZimaFileService.Core.Skills;

namespace ZimaFileService.Api;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly SkillRegistry _registry;

    public SkillsController(SkillRegistry registry)
    {
        _registry = registry;
    }

    [HttpGet]
    public IActionResult ListSkills()
    {
        return Ok(_registry.GetActiveSkills());
    }

    [HttpGet("{name}")]
    public IActionResult GetSkill(string name)
    {
        var skill = _registry.GetSkill(name);
        if (skill == null) return NotFound();
        return Ok(skill);
    }

    [HttpPost("{name}/activate")]
    public IActionResult ActivateSkill(string name)
    {
        var skill = _registry.GetSkill(name);
        if (skill == null) return NotFound();
        skill.IsActive = true;
        return Ok();
    }

    [HttpPost("{name}/deactivate")]
    public IActionResult DeactivateSkill(string name)
    {
        var skill = _registry.GetSkill(name);
        if (skill == null) return NotFound();
        skill.IsActive = false;
        return Ok();
    }
}
```

**Register in `Program.cs`:**
```csharp
// Add to services
var skillsPath = Path.Combine(FileManager.Instance.WorkingDirectory, "Skills/bundled");
builder.Services.AddSingleton(new SkillRegistry(skillsPath));

// Initialize on startup
var app = builder.Build();
var skillRegistry = app.Services.GetRequiredService<SkillRegistry>();
await skillRegistry.LoadAllAsync();
```

**Deliverables Week 2:**
- ✅ Skill system loads SKILL.md files
- ✅ 5 initial skills created
- ✅ API endpoints for skill management
- ✅ Skills injected into agent context

---

### Week 3: Vector Memory (Qdrant)

**Goal:** Add persistent memory with semantic search

#### Day 1-2: Qdrant Setup

**Install Qdrant (Docker):**
```bash
docker run -p 6333:6333 -p 6334:6334 \
  -v $(pwd)/qdrant_storage:/qdrant/storage:z \
  qdrant/qdrant:latest
```

**Add NuGet package:**
```bash
cd zima-file-service
dotnet add package Qdrant.Client
```

**Create QdrantService (`Services/QdrantService.cs`):**
```csharp
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace ZimaFileService.Services;

public class QdrantService
{
    private readonly QdrantClient _client;
    private const string ConversationsCollection = "zima_conversations";
    private const string FilesCollection = "zima_files";

    public QdrantService(string endpoint)
    {
        _client = new QdrantClient(endpoint);
    }

    public async Task InitializeCollectionsAsync()
    {
        // Create conversations collection
        try
        {
            await _client.CreateCollectionAsync(ConversationsCollection, new VectorParams
            {
                Size = 1536, // OpenAI embedding size
                Distance = Distance.Cosine
            });
        }
        catch { /* Collection exists */ }

        // Create files collection
        try
        {
            await _client.CreateCollectionAsync(FilesCollection, new VectorParams
            {
                Size = 1536,
                Distance = Distance.Cosine
            });
        }
        catch { /* Collection exists */ }
    }

    public async Task<ulong> UpsertPointAsync(
        string collection,
        string id,
        float[] vector,
        Dictionary<string, object> payload)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = id },
            Vectors = vector,
            Payload = { payload }
        };

        var response = await _client.UpsertAsync(collection, new[] { point });
        return response.SequenceNumber;
    }

    public async Task<List<ScoredPoint>> SearchAsync(
        string collection,
        float[] queryVector,
        int limit = 10,
        Dictionary<string, object>? filter = null)
    {
        var result = await _client.SearchAsync(collection, queryVector, limit: (ulong)limit);
        return result.ToList();
    }
}
```

#### Day 3-4: Embedding Service

**Create EmbeddingService (`Services/EmbeddingService.cs`):**
```csharp
using System.Net.Http.Json;

namespace ZimaFileService.Services;

public class EmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model = "text-embedding-3-small";

    public EmbeddingService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var request = new
        {
            model = _model,
            input = text
        };

        var response = await _httpClient.PostAsJsonAsync("embeddings", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        return result?.Data[0].Embedding ?? Array.Empty<float>();
    }

    private class EmbeddingResponse
    {
        public EmbeddingData[] Data { get; set; } = Array.Empty<EmbeddingData>();
    }

    private class EmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
```

#### Day 5: Memory Manager

**Create MemoryManager (`Core/Memory/MemoryManager.cs`):**
```csharp
namespace ZimaFileService.Core.Memory;

public class MemoryManager
{
    private readonly QdrantService _qdrant;
    private readonly EmbeddingService _embedding;

    public MemoryManager(QdrantService qdrant, EmbeddingService embedding)
    {
        _qdrant = qdrant;
        _embedding = embedding;
    }

    public async Task IndexConversationAsync(string sessionId, string message, string response)
    {
        var combined = $"User: {message}\nAssistant: {response}";
        var embedding = await _embedding.GetEmbeddingAsync(combined);

        await _qdrant.UpsertPointAsync(
            "zima_conversations",
            Guid.NewGuid().ToString(),
            embedding,
            new Dictionary<string, object>
            {
                ["session_id"] = sessionId,
                ["message"] = message,
                ["response"] = response,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            }
        );
    }

    public async Task<List<string>> SearchSimilarConversationsAsync(string query, int limit = 5)
    {
        var embedding = await _embedding.GetEmbeddingAsync(query);
        var results = await _qdrant.SearchAsync("zima_conversations", embedding, limit);

        return results.Select(r =>
        {
            var msg = r.Payload["message"].StringValue;
            var resp = r.Payload["response"].StringValue;
            return $"User: {msg}\nAssistant: {resp}";
        }).ToList();
    }
}
```

**Register in `Program.cs`:**
```csharp
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
var qdrantEndpoint = Environment.GetEnvironmentVariable("QDRANT_ENDPOINT") ?? "http://localhost:6333";

builder.Services.AddSingleton(new QdrantService(qdrantEndpoint));
builder.Services.AddSingleton(new EmbeddingService(openAiKey));
builder.Services.AddSingleton<MemoryManager>();

// Initialize on startup
var app = builder.Build();
var qdrant = app.Services.GetRequiredService<QdrantService>();
await qdrant.InitializeCollectionsAsync();
```

**Deliverables Week 3:**
- ✅ Qdrant running and configured
- ✅ Embedding service functional
- ✅ Conversations auto-indexed
- ✅ Semantic search working

---

## Phase 2: Channels (Weeks 4-6)

### Week 4: WhatsApp Channel

**Goal:** Integrate WhatsApp via Baileys

#### Day 1-2: Baileys Setup

**Install dependencies:**
```bash
cd gateway
npm install @whiskeysockets/baileys qrcode-terminal
```

**Create WhatsApp adapter (`src/channels/whatsapp.ts`):**
```typescript
import makeWASocket, {
  DisconnectReason,
  useMultiFileAuthState,
  WAMessage
} from '@whiskeysockets/baileys';
import qrcode from 'qrcode-terminal';
import { ZimaClient } from '../zima-client';

export class WhatsAppAdapter {
  private sock: any;
  private zimaClient: ZimaClient;

  constructor(zimaClient: ZimaClient) {
    this.zimaClient = zimaClient;
  }

  async start() {
    const { state, saveCreds } = await useMultiFileAuthState('auth_info_baileys');

    this.sock = makeWASocket({
      auth: state,
      printQRInTerminal: true
    });

    this.sock.ev.on('creds.update', saveCreds);
    this.sock.ev.on('connection.update', this.handleConnection.bind(this));
    this.sock.ev.on('messages.upsert', this.handleMessage.bind(this));
  }

  private handleConnection(update: any) {
    const { connection, lastDisconnect, qr } = update;

    if (qr) {
      qrcode.generate(qr, { small: true });
    }

    if (connection === 'close') {
      const shouldReconnect =
        (lastDisconnect?.error as any)?.output?.statusCode !== DisconnectReason.loggedOut;

      if (shouldReconnect) {
        this.start();
      }
    }
  }

  private async handleMessage(m: any) {
    const msg: WAMessage = m.messages[0];
    if (!msg.message || msg.key.fromMe) return;

    const text = msg.message.conversation ||
                 msg.message.extendedTextMessage?.text || '';

    const sessionId = msg.key.remoteJid; // Use WhatsApp chat ID as session

    // Send to ZIMA
    const response = await this.zimaClient.sendMessage({
      prompt: text,
      sessionId
    });

    // Reply on WhatsApp
    await this.sock.sendMessage(msg.key.remoteJid, {
      text: response.output
    });

    // Send generated files if any
    if (response.generatedFiles) {
      for (const file of response.generatedFiles) {
        await this.sendFile(msg.key.remoteJid, file);
      }
    }
  }

  private async sendFile(jid: string, file: any) {
    // Download file from ZIMA
    const fileBuffer = await this.zimaClient.downloadFile(file.downloadUrl);

    // Determine message type
    const ext = file.filename.split('.').pop()?.toLowerCase();
    const mimeType = this.getMimeType(ext);

    if (mimeType.startsWith('image/')) {
      await this.sock.sendMessage(jid, {
        image: fileBuffer,
        caption: file.filename
      });
    } else {
      await this.sock.sendMessage(jid, {
        document: fileBuffer,
        mimetype: mimeType,
        fileName: file.filename
      });
    }
  }

  private getMimeType(ext?: string): string {
    const types: Record<string, string> = {
      'pdf': 'application/pdf',
      'xlsx': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      'docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'pptx': 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
      'jpg': 'image/jpeg',
      'jpeg': 'image/jpeg',
      'png': 'image/png'
    };
    return types[ext || ''] || 'application/octet-stream';
  }
}
```

#### Day 3-4: File Upload Handling

**Update WhatsApp adapter to handle received files:**
```typescript
private async handleMessage(m: any) {
  const msg: WAMessage = m.messages[0];
  if (!msg.message || msg.key.fromMe) return;

  let text = '';
  let files: any[] = [];

  // Handle text
  if (msg.message.conversation) {
    text = msg.message.conversation;
  } else if (msg.message.extendedTextMessage) {
    text = msg.message.extendedTextMessage.text;
  }

  // Handle document/image attachments
  if (msg.message.documentMessage) {
    const buffer = await downloadMediaMessage(msg, 'buffer', {}, { reuploadRequest: this.sock.updateMediaMessage });
    files.push({
      filename: msg.message.documentMessage.fileName,
      mimetype: msg.message.documentMessage.mimetype,
      data: buffer.toString('base64')
    });
  }

  if (msg.message.imageMessage) {
    const buffer = await downloadMediaMessage(msg, 'buffer', {}, { reuploadRequest: this.sock.updateMediaMessage });
    files.push({
      filename: 'image.jpg',
      mimetype: 'image/jpeg',
      data: buffer.toString('base64')
    });
  }

  const sessionId = msg.key.remoteJid;

  // Upload files to ZIMA first if any
  if (files.length > 0) {
    await this.zimaClient.uploadFiles(sessionId, files);
  }

  // Send message
  const response = await this.zimaClient.sendMessage({
    prompt: text || 'Process the uploaded files',
    sessionId,
    files
  });

  // Reply...
}
```

#### Day 5: Testing

**Tasks:**
- [ ] QR code authentication works
- [ ] Text messages route to ZIMA
- [ ] ZIMA responses appear on WhatsApp
- [ ] File uploads work (WhatsApp → ZIMA)
- [ ] Generated files sent back to WhatsApp

**Deliverables Week 4:**
- ✅ WhatsApp bot connected
- ✅ Messages route through gateway to ZIMA
- ✅ File handling functional
- ✅ Generated documents sent back

---

### Week 5: Email Channel

**Goal:** Add email (IMAP/SMTP) integration

#### Day 1-2: Email Service Setup

**Install dependencies:**
```bash
cd gateway
npm install nodemailer imap-simple mailparser
npm install -D @types/nodemailer
```

**Create Email adapter (`src/channels/email.ts`):**
```typescript
import nodemailer from 'nodemailer';
import imaps from 'imap-simple';
import { simpleParser } from 'mailparser';
import { ZimaClient } from '../zima-client';

export class EmailAdapter {
  private transporter: nodemailer.Transporter;
  private imapConfig: any;
  private zimaClient: ZimaClient;
  private polling: boolean = false;

  constructor(zimaClient: ZimaClient) {
    this.zimaClient = zimaClient;

    // SMTP config
    this.transporter = nodemailer.createTransport({
      host: process.env.EMAIL_HOST,
      port: parseInt(process.env.EMAIL_PORT || '587'),
      secure: process.env.EMAIL_SECURE === 'true',
      auth: {
        user: process.env.EMAIL_USER,
        pass: process.env.EMAIL_PASSWORD
      }
    });

    // IMAP config
    this.imapConfig = {
      imap: {
        user: process.env.EMAIL_USER,
        password: process.env.EMAIL_PASSWORD,
        host: process.env.EMAIL_IMAP_HOST,
        port: parseInt(process.env.EMAIL_IMAP_PORT || '993'),
        tls: true,
        tlsOptions: { rejectUnauthorized: false }
      }
    };
  }

  async start() {
    this.polling = true;
    this.poll();
  }

  async stop() {
    this.polling = false;
  }

  private async poll() {
    while (this.polling) {
      try {
        await this.checkInbox();
      } catch (error) {
        console.error('Email polling error:', error);
      }

      // Poll every 60 seconds
      await new Promise(resolve => setTimeout(resolve, 60000));
    }
  }

  private async checkInbox() {
    const connection = await imaps.connect(this.imapConfig);

    await connection.openBox('INBOX');

    const searchCriteria = ['UNSEEN'];
    const fetchOptions = {
      bodies: ['HEADER', 'TEXT'],
      markSeen: true
    };

    const messages = await connection.search(searchCriteria, fetchOptions);

    for (const item of messages) {
      const all = item.parts.find((p: any) => p.which === 'TEXT');
      const parsed = await simpleParser(all.body);

      await this.handleEmail(parsed);
    }

    connection.end();
  }

  private async handleEmail(email: any) {
    const from = email.from.value[0].address;
    const subject = email.subject;
    const text = email.text || '';
    const html = email.html || '';

    // Use email address as session ID
    const sessionId = from;

    // Handle attachments
    const files: any[] = [];
    if (email.attachments) {
      for (const att of email.attachments) {
        files.push({
          filename: att.filename,
          mimetype: att.contentType,
          data: att.content.toString('base64')
        });
      }
    }

    // Upload attachments
    if (files.length > 0) {
      await this.zimaClient.uploadFiles(sessionId, files);
    }

    // Send to ZIMA
    const prompt = `Subject: ${subject}\n\n${text}`;
    const response = await this.zimaClient.sendMessage({
      prompt,
      sessionId,
      files
    });

    // Reply via email
    await this.sendEmail(from, `Re: ${subject}`, response.output, response.generatedFiles);
  }

  async sendEmail(to: string, subject: string, text: string, attachments?: any[]) {
    const mailOptions: any = {
      from: process.env.EMAIL_USER,
      to,
      subject,
      text
    };

    if (attachments && attachments.length > 0) {
      mailOptions.attachments = [];

      for (const file of attachments) {
        const buffer = await this.zimaClient.downloadFile(file.downloadUrl);
        mailOptions.attachments.push({
          filename: file.filename,
          content: buffer
        });
      }
    }

    await this.transporter.sendMail(mailOptions);
  }
}
```

#### Day 3-4: Email Tools (C#)

**Create EmailTool (`Tools/EmailTool.cs`):**
```csharp
using System.Net;
using System.Net.Mail;

namespace ZimaFileService.Tools;

public class EmailTool
{
    public async Task<string> SendEmailAsync(Dictionary<string, object> args)
    {
        var to = GetString(args, "to");
        var subject = GetString(args, "subject");
        var body = GetString(args, "body");
        var attachments = GetArray(args, "attachments");

        var from = Environment.GetEnvironmentVariable("EMAIL_USER") ?? "";
        var password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? "";
        var host = Environment.GetEnvironmentVariable("EMAIL_HOST") ?? "";
        var port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_PORT") ?? "587");

        using var client = new SmtpClient(host, port);
        client.Credentials = new NetworkCredential(from, password);
        client.EnableSsl = true;

        var mail = new MailMessage(from, to, subject, body);

        foreach (var attachment in attachments)
        {
            var filePath = FileManager.Instance.ResolveGeneratedFilePath(attachment.ToString() ?? "");
            if (File.Exists(filePath))
            {
                mail.Attachments.Add(new Attachment(filePath));
            }
        }

        await client.SendMailAsync(mail);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Email sent successfully"
        });
    }

    private string GetString(Dictionary<string, object> args, string key)
    {
        return args.TryGetValue(key, out var value) ? value.ToString() ?? "" : "";
    }

    private List<string> GetArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return new();
        if (value is JsonElement elem && elem.ValueKind == JsonValueKind.Array)
        {
            return elem.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
        }
        return new();
    }
}
```

**Register in ToolRegistry:**
```csharp
// In Api/ToolsRegistry.cs
new ToolDefinition
{
    Name = "email_send",
    Description = "Send an email with optional attachments",
    Category = "communication",
    Parameters = new List<ToolParameter>
    {
        new() { Name = "to", Type = "string", Required = true, Description = "Recipient email address" },
        new() { Name = "subject", Type = "string", Required = true, Description = "Email subject" },
        new() { Name = "body", Type = "string", Required = true, Description = "Email body text" },
        new() { Name = "attachments", Type = "array", Required = false, Description = "List of file paths to attach" }
    }
}
```

#### Day 5: Testing

**Tasks:**
- [ ] IMAP polling works
- [ ] Emails trigger ZIMA processing
- [ ] SMTP replies sent successfully
- [ ] Attachments handled correctly

**Deliverables Week 5:**
- ✅ Email channel operational
- ✅ Inbox monitoring working
- ✅ Automatic email replies
- ✅ File attachments functional

---

### Week 6: Channel Integration & Testing

**Goal:** Ensure all 3 channels work together seamlessly

#### Day 1-2: Gateway Enhancement

**Update gateway server (`src/server.ts`):**
```typescript
import { WhatsAppAdapter } from './channels/whatsapp';
import { EmailAdapter } from './channels/email';

// ... existing code ...

// Initialize channels
const whatsappAdapter = new WhatsAppAdapter(zimaClient);
const emailAdapter = new EmailAdapter(zimaClient);

// Start channels
if (process.env.ENABLE_WHATSAPP === 'true') {
  await whatsappAdapter.start();
  console.log('WhatsApp channel started');
}

if (process.env.ENABLE_EMAIL === 'true') {
  await emailAdapter.start();
  console.log('Email channel started');
}

app.listen(PORT, () => {
  console.log(`Gateway running on port ${PORT}`);
  console.log(`Channels: WebChat, ${process.env.ENABLE_WHATSAPP === 'true' ? 'WhatsApp, ' : ''}${process.env.ENABLE_EMAIL === 'true' ? 'Email' : ''}`);
});
```

**Configuration (`.env`):**
```env
# Gateway
GATEWAY_PORT=18789
ZIMA_API_URL=http://localhost:5000

# Channels
ENABLE_WHATSAPP=true
ENABLE_EMAIL=true

# Email Configuration
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_SECURE=false
EMAIL_USER=your-email@gmail.com
EMAIL_PASSWORD=your-app-password
EMAIL_IMAP_HOST=imap.gmail.com
EMAIL_IMAP_PORT=993
```

#### Day 3-4: End-to-End Testing

**Test Scenarios:**

1. **WebChat Test**
   - Open Laravel frontend
   - Send: "Create an Excel file with Q1 sales data"
   - Verify: File generated and downloadable

2. **WhatsApp Test**
   - Send message on WhatsApp
   - Verify: Response received
   - Verify: Generated file sent back

3. **Email Test**
   - Send email to ZIMA address
   - Verify: Email reply received
   - Verify: Attachment included if file generated

4. **Cross-Channel Session Test**
   - Start conversation on WebChat (session A)
   - Continue same session on WhatsApp
   - Verify: Context preserved

5. **Multi-File Test**
   - Upload CSV via email
   - Ask to convert to Excel
   - Verify: Excel file attached in reply

#### Day 5: Documentation

**Create `docs/channels.md`:**
```markdown
# Channel Configuration

## WebChat

Access: http://localhost:8000
No configuration needed (built-in)

## WhatsApp

1. Install dependencies:
   ```bash
   cd gateway && npm install
   ```

2. Start gateway:
   ```bash
   npm start
   ```

3. Scan QR code with WhatsApp mobile app

4. Start chatting!

## Email

1. Configure `.env`:
   ```env
   EMAIL_HOST=smtp.gmail.com
   EMAIL_USER=your-email@gmail.com
   EMAIL_PASSWORD=your-app-password
   ENABLE_EMAIL=true
   ```

2. For Gmail: Enable "App Passwords" in Google Account settings

3. Start gateway (will poll inbox every 60s)

4. Send emails to configured address
```

**Deliverables Week 6:**
- ✅ All 3 channels working
- ✅ Cross-channel sessions
- ✅ End-to-end tests passing
- ✅ Documentation complete

---

## Phase 3: Advanced Features (Weeks 7-8)

### Week 7: Browser Automation + Workspace Integration

**Goal:** Add Playwright browser automation and complete LSP/Git integration

#### Days 1-3: Browser Service (as in original plan Week 7)

**Files to create:**
- `Services/BrowserService.cs`
- `Tools/BrowserAutomationTool.cs`
- `Skills/bundled/browser-automation/SKILL.md`

#### Days 4-5: Complete LSP + Git (as in original plan Week 8)

**Files to complete/create:**
- `Core/LSP/LspServer.cs`
- `Services/GitService.cs`
- `Tools/GitTool.cs`
- `Skills/bundled/workspace/SKILL.md`

**Deliverables Week 7:**
- ✅ Browser automation working
- ✅ LSP server functional
- ✅ Git operations available

---

### Week 8: Proactive Features + Multi-User

**Goal:** Add heartbeat system and basic multi-user support

#### Days 1-3: Heartbeat/Scheduler (as in original plan Week 9)

**Tasks:**
- Install Hangfire for .NET
- Implement HeartbeatService
- Create scheduler tools
- Morning briefing feature

#### Days 4-5: Multi-User Basics

**Tasks:**
- User authentication (JWT)
- Session sharing (read-only for now)
- Permission system enhancement

**Deliverables Week 8:**
- ✅ Scheduled tasks working
- ✅ Proactive messages sent
- ✅ Basic multi-user support

---

## Phase 4: Deployment (Weeks 9-10)

### Week 9: Performance + Security

**Goal:** Production-ready optimization

#### Days 1-2: Performance

**Tasks:**
- Redis caching layer
- Database indexing
- Connection pooling tuning
- Load testing (JMeter)

#### Days 3-4: Security

**Tasks:**
- Security audit (OWASP checklist)
- SSL/TLS certificates
- Rate limiting tuning
- Vulnerability scanning

#### Day 5: Monitoring

**Tasks:**
- Application Insights setup
- Error tracking (Sentry)
- Usage analytics
- Alerting

**Deliverables Week 9:**
- ✅ Performance optimized
- ✅ Security hardened
- ✅ Monitoring active

---

### Week 10: Documentation + Launch

**Goal:** Complete documentation and deploy to production

#### Days 1-2: Documentation

**Create:**
- User guide
- Admin guide
- API reference (Swagger)
- Deployment guide

#### Days 3-4: Production Deployment

**Tasks:**
- Setup Linux server (Ubuntu 22.04 LTS)
- Docker Compose configuration
- Nginx reverse proxy
- SSL certificates (Let's Encrypt)
- Systemd services

**Docker Compose (`docker-compose.yml`):**
```yaml
version: '3.8'

services:
  zima-core:
    build: ./zima-file-service
    ports:
      - "5000:5000"
    environment:
      - ANTHROPIC_API_KEY=${ANTHROPIC_API_KEY}
      - OPENAI_API_KEY=${OPENAI_API_KEY}
      - QDRANT_ENDPOINT=http://qdrant:6333
    volumes:
      - ./data/generated_files:/app/generated_files
      - ./data/uploaded_files:/app/uploaded_files
      - ./Skills:/app/Skills
    depends_on:
      - qdrant
      - redis

  gateway:
    build: ./gateway
    ports:
      - "18789:18789"
    environment:
      - ZIMA_API_URL=http://zima-core:5000
      - ENABLE_WHATSAPP=true
      - ENABLE_EMAIL=true
      - EMAIL_HOST=${EMAIL_HOST}
      - EMAIL_USER=${EMAIL_USER}
      - EMAIL_PASSWORD=${EMAIL_PASSWORD}
    volumes:
      - ./data/whatsapp_auth:/app/auth_info_baileys
    depends_on:
      - zima-core

  frontend:
    build: ./zima-frontend
    ports:
      - "8000:8000"
    environment:
      - GATEWAY_URL=http://gateway:18789
      - APP_ENV=production
    depends_on:
      - gateway

  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
    volumes:
      - ./data/qdrant_storage:/qdrant/storage

  redis:
    image: redis:alpine
    ports:
      - "6379:6379"
    volumes:
      - ./data/redis:/data

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - frontend
      - gateway
      - zima-core
```

**Nginx Config (`nginx.conf`):**
```nginx
events {
  worker_connections 1024;
}

http {
  upstream frontend {
    server frontend:8000;
  }

  upstream gateway {
    server gateway:18789;
  }

  upstream zima {
    server zima-core:5000;
  }

  server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$server_name$request_uri;
  }

  server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;

    # Frontend
    location / {
      proxy_pass http://frontend;
      proxy_set_header Host $host;
      proxy_set_header X-Real-IP $remote_addr;
    }

    # Gateway API
    location /gateway/ {
      proxy_pass http://gateway/;
      proxy_http_version 1.1;
      proxy_set_header Upgrade $http_upgrade;
      proxy_set_header Connection "upgrade";
    }

    # ZIMA API (for advanced users)
    location /api/ {
      proxy_pass http://zima/api/;
      proxy_set_header Host $host;
    }
  }
}
```

#### Day 5: Launch

**Tasks:**
- Deploy to production server
- DNS configuration
- Smoke tests
- Announce launch

**Deliverables Week 10:**
- ✅ Production deployment complete
- ✅ Documentation published
- ✅ System live and operational

---

## Implementation Guide

### Development Environment Setup

**Prerequisites:**
- .NET 9.0 SDK
- Node.js 20+
- PHP 8.2+ (for Laravel)
- Docker & Docker Compose
- Git

**Initial Setup:**
```bash
# Clone repository
cd /Volumes/DATA/QWEN

# Backend (ZIMA Core)
cd zima-file-service
dotnet restore
dotnet build

# Gateway
mkdir gateway
cd gateway
npm init -y
npm install ws express axios dotenv cors @whiskeysockets/baileys nodemailer
npm install -D typescript @types/node @types/express

# Frontend (Already exists)
cd ../zima-frontend
composer install
php artisan migrate

# Docker services
docker-compose up -d qdrant redis
```

### Configuration Files

**`gateway/.env`:**
```env
GATEWAY_PORT=18789
ZIMA_API_URL=http://localhost:5000

# Channels
ENABLE_WHATSAPP=true
ENABLE_EMAIL=true

# WhatsApp
# (Configured via QR code)

# Email
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_SECURE=false
EMAIL_USER=zima@yourdomain.com
EMAIL_PASSWORD=your-app-password
EMAIL_IMAP_HOST=imap.gmail.com
EMAIL_IMAP_PORT=993
```

**`zima-file-service/appsettings.json`:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Qdrant": {
    "Endpoint": "http://localhost:6333"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Security": {
    "ApiKeyEnabled": true,
    "RateLimitPerMinute": 100
  }
}
```

**`zima-frontend/.env`:**
```env
GATEWAY_URL=http://localhost:18789
ZIMA_API_URL=http://localhost:5000
```

---

## Deployment Configuration

### Server Requirements

**Minimum:**
- 4 vCPUs
- 16 GB RAM
- 100 GB SSD
- Ubuntu 22.04 LTS

**Recommended:**
- 8 vCPUs
- 32 GB RAM
- 200 GB SSD
- Ubuntu 22.04 LTS

### Systemd Services

**`/etc/systemd/system/zima-core.service`:**
```ini
[Unit]
Description=ZIMA Core Service
After=network.target

[Service]
Type=simple
User=zima
WorkingDirectory=/opt/zima/zima-file-service
ExecStart=/usr/bin/dotnet /opt/zima/zima-file-service/bin/Release/net9.0/zima-file-service.dll --http
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

**`/etc/systemd/system/zima-gateway.service`:**
```ini
[Unit]
Description=ZIMA Gateway Service
After=network.target zima-core.service

[Service]
Type=simple
User=zima
WorkingDirectory=/opt/zima/gateway
ExecStart=/usr/bin/node /opt/zima/gateway/dist/server.js
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

---

## Success Metrics

### Technical Metrics

| Metric | Target |
|--------|--------|
| **Response Time** | < 2 seconds (p95) |
| **Concurrent Users** | 500+ |
| **Uptime** | 99.5% |
| **Channel Availability** | 3/3 channels operational |

### User Metrics

| Metric | Target (3 months) |
|--------|-------------------|
| **Daily Active Users** | 100+ |
| **Messages/Day** | 1,000+ |
| **Files Generated/Day** | 5,000+ |
| **Skills Created** | 10+ |

---

## Risks & Mitigation

| Risk | Mitigation |
|------|-----------|
| **WhatsApp account ban** | Use official business API in production |
| **Email deliverability** | Use SendGrid/Mailgun for outbound |
| **Vector DB cost** | Self-hosted Qdrant (Docker) |
| **Scaling issues** | Horizontal scaling with load balancer |

---

## Timeline Summary

**Total Duration:** 10 weeks

- **Week 1:** Gateway + Laravel integration
- **Week 2:** Skills system
- **Week 3:** Vector memory
- **Week 4:** WhatsApp channel
- **Week 5:** Email channel
- **Week 6:** Integration testing
- **Week 7:** Browser + Workspace features
- **Week 8:** Proactive features + Multi-user
- **Week 9:** Performance + Security
- **Week 10:** Documentation + Deployment

---

## Next Steps

1. **Review & Approve** this simplified plan
2. **Setup development environment** (Week 1 Day 1)
3. **Create gateway project** (Week 1 Day 2)
4. **Integrate with Laravel** (Week 1 Day 3-4)
5. **Start implementing!**

---

**Status:** ✅ Ready for Implementation
**Version:** 2.0 Simplified (Server Edition)
**Date:** January 31, 2026
