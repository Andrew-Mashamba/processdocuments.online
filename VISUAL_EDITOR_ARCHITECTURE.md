# ZIMA Visual Code Editor - Architecture & Developer Guide

## Overview

ZIMA Visual Code Editor enables developers to edit their web applications directly from the browser by selecting UI elements and describing desired changes in natural language. The system uses Claude CLI with a full agent runtime to understand context, locate source files, and apply changes automatically.

## Key Innovation

**This is NOT a simple API integration.** It's a full ZIMA agent runtime running locally on the developer's machine with:
- Claude CLI integration (spawning `claude` as child process)
- Complete tool system (200+ tools available)
- Memory & session management (SQLite + vector search)
- Smart context optimization
- Multi-channel support

## Architecture

### Two-Package System

```
@zima/agent                      Core agent runtime (copy of gateway agent)
    ↓
@zima/visual-code-editor         Visual editing layer built on top
```

## Package 1: `@zima/agent`

**Purpose**: Standalone NPM package containing ZIMA's complete agent system.

### Directory Structure

```
@zima/agent/
├── src/
│   ├── agent/
│   │   ├── claude-cli-runtime.ts        # Spawns 'claude' CLI process
│   │   ├── tool-registry.ts             # 196+ ZIMA + 50+ system tools
│   │   ├── tool-executor.ts             # Execute any tool
│   │   └── openclaw-system-prompt.ts    # Dynamic prompt builder
│   │
│   ├── context/
│   │   ├── hybrid-context-manager.ts    # Complete context pipeline
│   │   ├── transcript-manager.ts        # JSONL session storage
│   │   ├── session-lock-manager.ts      # Concurrent safety
│   │   └── task-classifier.ts           # Complexity detection
│   │
│   ├── memory/
│   │   ├── memory-service.ts            # SQLite + vector database
│   │   ├── hybrid-search.ts             # Semantic + keyword search
│   │   └── indexer.ts                   # Auto-index workspace
│   │
│   ├── router/
│   │   └── session-key-builder.ts       # OpenClaw session format
│   │
│   └── mcp/
│       └── gateway-mcp-server.ts        # Expose tools to Claude CLI
│
├── package.json
├── tsconfig.json
└── README.md
```

### Core Features

#### 1. Claude CLI Integration

ZIMA doesn't use the Anthropic API directly. It spawns the `claude` CLI as a child process:

```typescript
// From: src/agent/claude-cli-runtime.ts:159

const proc = spawn('claude', [
  '--print',                        // Output to stdout
  '--output-format', 'stream-json', // Streaming JSON format
  '--verbose',                      // Debug info
  '--include-partial-messages',     // Stream tokens as typing
  '--dangerously-skip-permissions'  // No permission prompts
], {
  cwd: workspacePath,
  stdio: ['pipe', 'pipe', 'pipe'],
  env: process.env
});

// Send prompt via stdin
proc.stdin.write(JSON.stringify(prompt));
proc.stdin.end();

// Parse output line-by-line (NDJSON)
readline.on('line', (line) => {
  const event = JSON.parse(line);
  // {type: 'stream_event', event: {type: 'content_block_delta', delta: {...}}}
});
```

#### 2. Tool System

**Built-in Tools:**
- File Operations: `read`, `write`, `edit`
- Shell Execution: `exec`, `process`
- Web Tools: `web_search`, `web_fetch`, `browser`
- Memory: `memory_search`, `memory_get`
- Sessions: `sessions_list`, `sessions_send`, `sessions_spawn`

**Optional ZIMA File Service (196+ tools):**
- Excel: `create_excel`, `read_excel`, `add_chart`, `pivot_summary`
- PDF: `create_pdf`, `merge_pdf`, `split_pdf`, `compress_pdf`, `ocr_pdf`
- Word: `create_word`, `merge_word`, `mail_merge`, `word_to_pdf`
- PowerPoint: `create_powerpoint`, `add_slide`, `animations`
- JSON: `parse`, `transform`, `validate`, `repair`, `convert`
- Images: `ocr_image`, `watermark`, `redact`, `resize`, `convert`

#### 3. Memory System

```typescript
// SQLite + vector embeddings
await memoryService.search({
  query: "How did we implement user authentication?",
  limit: 5
});

// Results include:
// - Semantic matches (vector similarity)
// - Keyword matches (FTS5 full-text search)
// - Ranked by relevance
```

#### 4. Session Management

```typescript
// Sessions stored as JSONL transcripts
{
  role: 'user',
  content: 'Make the button blue',
  timestamp: '2026-02-02T10:30:00Z'
}
{
  role: 'assistant',
  content: [
    {type: 'tool_use', id: 'xyz', name: 'read', input: {file_path: '...'}},
    {type: 'text', text: 'I found the button component...'}
  ]
}
```

## Package 2: `@zima/visual-code-editor`

**Purpose**: Visual editing interface + specialized tools built on `@zima/agent`.

### Directory Structure

```
@zima/visual-code-editor/
├── src/
│   ├── cli/
│   │   └── init.ts                      # Setup wizard
│   │
│   ├── agent/
│   │   ├── VisualEditAgent.ts           # Extends @zima/agent runtime
│   │   ├── visual-system-prompt.ts      # Specialized for visual editing
│   │   └── tools/
│   │       ├── visual-analyze.ts        # DOM + screenshot analysis
│   │       ├── component-mapper.ts      # Map UI → source files
│   │       └── code-gen.ts              # Generate code changes
│   │
│   ├── frontend/
│   │   ├── injector.ts                  # Browser script
│   │   ├── overlay/                     # UI overlay components
│   │   ├── capture/                     # Screenshot + DOM extraction
│   │   └── detectors/                   # Framework detection (Laravel, React, Vue)
│   │
│   └── backend/
│       ├── laravel/                     # Livewire component mapper
│       ├── react/                       # React component mapper
│       └── vue/                         # Vue component mapper
│
├── package.json                         # Depends on @zima/agent
├── tsconfig.json
└── README.md
```

## Installation & Setup

### Prerequisites

1. **Claude CLI** must be installed:

```bash
# Check if Claude CLI exists
claude --version

# If not installed:
npm install -g @anthropic-ai/claude-cli
# OR
brew install claude-cli

# Authenticate
claude auth login
```

2. **Node.js** 18+ required

### Installation

```bash
# In your Laravel/React/Vue project
npm install --save-dev @zima/visual-code-editor

# Run setup wizard
npx visual-editor init
```

### Setup Wizard Flow

```
🔍 Checking prerequisites...

✓ Claude CLI found: v1.x.x

? Framework detected: Laravel + Livewire (confirm?) Yes
? Build tool: Vite
? Component paths: app/Livewire, resources/views/livewire
? Connect to ZIMA file-service? [optional] → http://localhost:5000

✓ Created: .visual-editor/config.json
✓ Created: .visual-editor/agent-workspace/
✓ Updated: vite.config.js (added plugin)
✓ Ready! Press Ctrl+Alt+D in browser to activate
```

### Configuration File

`.visual-editor/config.json`:

```json
{
  "framework": "laravel-livewire",
  "buildTool": "vite",
  "componentPaths": [
    "app/Livewire",
    "resources/views/livewire"
  ],
  "zimaFileService": "http://localhost:5000",
  "websocketPort": 9876,
  "agentConfig": {
    "model": "claude-sonnet-4-5-20250514",
    "temperature": 0.7,
    "contextOptimization": "adaptive",
    "memoryEnabled": true
  }
}
```

## How It Works

### Complete Flow

```
1. Developer installs @zima/visual-code-editor
   ├─ npx visual-editor init
   ├─ Checks: Claude CLI installed? ✓
   └─ Creates: .visual-editor/ workspace

2. Developer starts dev server (npm run dev)
   ├─ Vite plugin injects overlay script
   └─ Background Node process starts (@zima/agent)

3. In browser, dev presses Ctrl+Alt+D
   ├─ Overlay UI appears
   ├─ Dev selects element + types command
   └─ Frontend sends to localhost:9876 (WebSocket)

4. @zima/agent receives request
   ├─ Loads: Session (JSONL transcript)
   ├─ Builds: System prompt (visual editing mode)
   ├─ Executes: spawn('claude', [...args])
   └─ Streams: Response tokens

5. Claude CLI processes request
   ├─ Analyzes: DOM + screenshot + command
   ├─ Uses tools:
   │   ├─ read (component source files)
   │   ├─ component-mapper (find target files)
   │   ├─ visual-analyze (understand context)
   │   └─ write (apply changes)
   └─ Returns: File changes

6. @zima/agent writes changes to disk
   ├─ Uses: 'write' tool
   ├─ Saves: Transcript (conversation history)
   └─ Returns: Success + diff

7. Vite detects file change → hot reload
   └─ Developer sees change instantly in browser
```

### Runtime Architecture

```
┌─────────────────────┐
│  Browser            │
│  (Laravel app on    │
│   localhost:8000)   │
└──────────┬──────────┘
           │
           │ Injected Script (@zima/visual-code-editor/injector.js)
           │ - Overlay UI
           │ - Element selector
           │ - Screenshot capture
           │ - Command input
           ▼
┌─────────────────────┐
│  Local Node Process │
│  @zima/agent        │
│  + visual tools     │
│  localhost:9876     │
└──────────┬──────────┘
           │
           ├─→ Tools Available:
           │   ├─ read/write (file operations)
           │   ├─ exec (run commands)
           │   ├─ visual-analyze (understand DOM/screenshot)
           │   ├─ component-mapper (find source files)
           │   └─ MCP tools (optional: ZIMA file-service)
           │
           ├─→ Memory System (SQLite + vector search)
           ├─→ Session Storage (JSONL transcripts)
           └─→ Context Management (smart prompts)

           │
           ▼
┌─────────────────────┐
│  Claude CLI         │
│  (spawned process)  │
│                     │
│  claude --print ... │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Claude API         │
│  (Anthropic)        │
└─────────────────────┘
```

## Tool Availability Matrix

| Tool Category | Count | Source | Notes |
|--------------|-------|--------|-------|
| ZIMA Document Tools | 196+ | Optional (via ZIMA file-service) | Excel, PDF, Word, PPT, JSON, Images |
| File Operations | 3 | Built-in | read, write, edit |
| Shell Execution | 2 | Built-in | exec, process |
| Web Tools | 3 | Built-in | search, fetch, browser |
| Memory & Sessions | 8 | Built-in | memory_search, sessions_* |
| Visual Editing | 3 | Custom | analyze, map, generate |

## Developer Workflow

### Example: Making a Button Blue

```
1. User opens Laravel app in browser
2. Presses Ctrl+Alt+D
3. Overlay appears
4. Selects button element
5. Types: "Make this button blue"
6. Presses Enter
```

**Behind the scenes:**

```typescript
// 1. Frontend captures context
const context = {
  element: {
    tag: 'button',
    classes: ['btn', 'btn-primary'],
    text: 'Submit',
    xpath: '/html/body/div[1]/form/button'
  },
  component: 'FileGenerator', // detected Livewire component
  screenshot: '<base64 image>',
  command: 'Make this button blue'
};

// 2. Sent to local agent via WebSocket
ws.send(JSON.stringify(context));

// 3. Agent builds prompt
const prompt = buildVisualEditPrompt({
  systemPrompt: visualEditSystemPrompt,
  context: context,
  sessionHistory: await loadSession('visual-edit-123')
});

// 4. Agent spawns Claude CLI
const response = await claudeCliRuntime.run({
  prompt,
  tools: [
    'read', 'write', 'edit',
    'visual-analyze',
    'component-mapper',
    'code-gen'
  ]
});

// 5. Claude uses tools:
// - read: app/Livewire/FileGenerator.php
// - read: resources/views/livewire/file-generator.blade.php
// - visual-analyze: Confirms button location
// - write: Updates Blade template with blue class

// 6. Agent returns changes
return {
  success: true,
  filesChanged: ['resources/views/livewire/file-generator.blade.php'],
  diff: '...',
  message: 'Updated button to blue using bg-blue-500 class'
};

// 7. Vite hot-reloads → user sees blue button
```

## Framework-Specific Notes

### Laravel + Livewire

**Component Mapping:**
```php
// backend/laravel/LivewireMapper.php
public function mapComponentToFile(string $componentName): array
{
    return [
        'class' => app_path("Livewire/{$componentName}.php"),
        'view' => resource_path("views/livewire/" .
                  Str::kebab($componentName) . ".blade.php")
    ];
}
```

### React

**Component Detection:**
```javascript
// frontend/detectors/react-detector.js
function detectReactComponent(element) {
  const fiber = element._reactFiber ||
                element._reactInternalFiber;
  if (fiber) {
    return {
      name: fiber.type.name,
      props: fiber.memoizedProps,
      file: fiber._debugSource?.fileName
    };
  }
}
```

### Vue

**Component Detection:**
```javascript
// frontend/detectors/vue-detector.js
function detectVueComponent(element) {
  const vnode = element.__vnode;
  if (vnode) {
    return {
      name: vnode.type.name,
      props: vnode.props,
      file: vnode.type.__file
    };
  }
}
```

## CLI Setup Code

### Claude CLI Detection

```typescript
// src/cli/init.ts
import { execSync } from 'child_process';

async function checkClaudeCli(): Promise<boolean> {
  try {
    const version = execSync('claude --version', {
      encoding: 'utf8',
      stdio: 'pipe'
    }).trim();

    console.log(`✓ Claude CLI found: ${version}`);
    return true;

  } catch (error) {
    console.error('✗ Claude CLI not found');
    console.error('');
    console.error('Please install Claude CLI first:');
    console.error('  npm install -g @anthropic-ai/claude-cli');
    console.error('  OR');
    console.error('  brew install claude-cli');
    console.error('');
    console.error('Then authenticate:');
    console.error('  claude auth login');
    return false;
  }
}

export async function init() {
  console.log('🔍 Checking prerequisites...\n');

  const hasClaudeCli = await checkClaudeCli();
  if (!hasClaudeCli) {
    process.exit(1);
  }

  // Continue with setup...
  await detectFramework();
  await configureBuildTool();
  await setupWorkspace();
  await startAgentProcess();
}
```

## Custom Visual Editing Tools

### 1. Visual Analyze

```typescript
// src/agent/tools/visual-analyze.ts
export const visualAnalyzeTool = {
  name: 'visual-analyze',
  description: 'Analyze DOM structure and screenshot to understand UI element',
  inputSchema: {
    type: 'object',
    properties: {
      element: {type: 'object'},
      screenshot: {type: 'string'},
      command: {type: 'string'}
    }
  },
  async execute(input) {
    const analysis = await analyzeVisualContext({
      dom: input.element,
      image: input.screenshot,
      userIntent: input.command
    });

    return {
      elementType: analysis.type,
      purpose: analysis.purpose,
      suggestedChanges: analysis.suggestions,
      confidence: analysis.confidence
    };
  }
};
```

### 2. Component Mapper

```typescript
// src/agent/tools/component-mapper.ts
export const componentMapperTool = {
  name: 'component-mapper',
  description: 'Map UI component to source files',
  inputSchema: {
    type: 'object',
    properties: {
      componentName: {type: 'string'},
      framework: {type: 'string'}
    }
  },
  async execute(input) {
    const mapper = getMapperForFramework(input.framework);
    const files = await mapper.findSourceFiles(input.componentName);

    return {
      files: files.map(f => ({
        path: f.path,
        type: f.type, // 'component', 'view', 'style'
        exists: fs.existsSync(f.path)
      }))
    };
  }
};
```

### 3. Code Generator

```typescript
// src/agent/tools/code-gen.ts
export const codeGenTool = {
  name: 'code-generator',
  description: 'Generate code changes based on user command',
  inputSchema: {
    type: 'object',
    properties: {
      file: {type: 'string'},
      intent: {type: 'string'},
      context: {type: 'object'}
    }
  },
  async execute(input) {
    const currentCode = await fs.readFile(input.file, 'utf8');
    const changes = await generateChanges({
      code: currentCode,
      intent: input.intent,
      framework: input.context.framework
    });

    return {
      original: currentCode,
      modified: changes.newCode,
      diff: changes.diff,
      explanation: changes.reasoning
    };
  }
};
```

## System Prompt Template

```typescript
// src/agent/visual-system-prompt.ts
export function buildVisualEditSystemPrompt(context: VisualContext): string {
  return `
You are a visual code editor agent. The user has selected a UI element and described a change they want.

**Your Task:**
1. Analyze the selected element (DOM + screenshot)
2. Use component-mapper to find source files
3. Read the relevant files
4. Generate the necessary code changes
5. Write the changes to disk
6. Explain what you did

**Context:**
- Framework: ${context.framework}
- Component: ${context.componentName}
- Element: ${JSON.stringify(context.element)}
- User Command: "${context.command}"

**Available Tools:**
- visual-analyze: Understand the UI element
- component-mapper: Find source files
- read/write/edit: File operations
- exec: Run build commands if needed

**Guidelines:**
- Make minimal changes (only what's requested)
- Preserve existing styles and functionality
- Use framework best practices
- Provide clear explanations

**Important:**
- Don't ask for confirmation, just make the change
- If multiple files need updates, modify all of them
- Test your changes mentally before writing
  `;
}
```

## Session Storage

Sessions are stored as JSONL (JSON Lines) files:

```
.visual-editor/sessions/visual-edit-<timestamp>.jsonl
```

Example session:

```jsonl
{"role":"user","content":"Make the submit button blue","timestamp":"2026-02-02T10:30:00Z","context":{"element":{"tag":"button","classes":["btn"]}}}
{"role":"assistant","content":[{"type":"tool_use","id":"t1","name":"visual-analyze","input":{"element":{"tag":"button"},"command":"Make the submit button blue"}}]}
{"role":"user","content":[{"type":"tool_result","tool_use_id":"t1","content":"{\"elementType\":\"button\",\"purpose\":\"form submission\"}"}]}
{"role":"assistant","content":[{"type":"tool_use","id":"t2","name":"component-mapper","input":{"componentName":"FileGenerator","framework":"laravel-livewire"}}]}
{"role":"user","content":[{"type":"tool_result","tool_use_id":"t2","content":"{\"files\":[{\"path\":\"resources/views/livewire/file-generator.blade.php\",\"type\":\"view\"}]}"}]}
{"role":"assistant","content":[{"type":"tool_use","id":"t3","name":"write","input":{"file_path":"resources/views/livewire/file-generator.blade.php","content":"..."}}]}
{"role":"assistant","content":[{"type":"text","text":"I've updated the button to use the bg-blue-500 class."}]}
```

## Memory System

The agent maintains long-term memory using SQLite + vector embeddings:

```sql
-- Memory database schema
CREATE TABLE memories (
  id INTEGER PRIMARY KEY,
  content TEXT,
  embedding BLOB,  -- Vector embedding (1536 dimensions)
  metadata JSON,   -- {type, timestamp, session_id, tags}
  created_at TEXT
);

CREATE VIRTUAL TABLE memories_fts USING fts5(content);
```

**Usage:**

```typescript
// Search memory for similar past work
const similar = await memoryService.search({
  query: "How did we change button colors before?",
  limit: 3
});

// Results:
[
  {
    content: "Changed login button to blue using bg-blue-500 class",
    similarity: 0.89,
    metadata: {
      file: "resources/views/auth/login.blade.php",
      timestamp: "2026-02-01T14:20:00Z"
    }
  }
]
```

## Development Roadmap

### Phase 1: Core Package Extraction
- [ ] Extract `@zima/agent` from gateway
- [ ] Test standalone functionality
- [ ] Publish to NPM

### Phase 2: Visual Editor Foundation
- [ ] Create `@zima/visual-code-editor` package
- [ ] Implement CLI setup wizard
- [ ] Build browser injector script
- [ ] Create overlay UI

### Phase 3: Framework Support
- [ ] Laravel + Livewire mapper
- [ ] React component mapper
- [ ] Vue component mapper

### Phase 4: Visual Tools
- [ ] `visual-analyze` tool
- [ ] `component-mapper` tool
- [ ] `code-generator` tool

### Phase 5: Polish & Release
- [ ] Documentation
- [ ] Example projects
- [ ] Video demos
- [ ] Public beta

## Contributing

(To be added: contribution guidelines)

## License

(To be determined)

---

**Last Updated**: 2026-02-02
**Version**: 1.0.0-draft
**Status**: Architecture Design Phase
