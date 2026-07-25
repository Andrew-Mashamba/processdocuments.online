# @zima/visual-code-editor

> **Visual code editing powered by Claude AI** - Edit web applications directly from the browser using natural language

[![npm version](https://img.shields.io/npm/v/@zima/visual-code-editor.svg)](https://www.npmjs.com/package/@zima/visual-code-editor)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

## What is ZIMA Visual Code Editor?

ZIMA Visual Code Editor is a revolutionary NPM package that enables developers to edit web applications directly from the browser using natural language commands. Simply select a UI element, describe what you want, and watch AI generate and apply the code changes in real-time.

### ✨ Key Features

- 🎯 **Visual Element Selection** - Click any element in your running app
- 🗣️ **Natural Language Commands** - Describe changes in plain English
- 🤖 **AI Code Generation** - Claude generates framework-specific code
- ⚡ **Instant Hot Reload** - See changes immediately via HMR
- 🛠️ **246+ Tools Available** - Excel, PDF, Word, PowerPoint, and more
- 🧠 **Smart Memory System** - SQLite + vector search for context
- 📊 **4-Tier Context Optimization** - 50-90% cost reduction
- 🔄 **Multi-Channel Support** - WebChat, WhatsApp, Email ready

### 🎨 Supported Frameworks

- **Laravel + Livewire**
- **React** (with Vite/Webpack)
- **Vue** (2 & 3)
- **Angular**
- **Next.js** (coming soon)
- **Nuxt** (coming soon)

---

## 📦 Installation

### Prerequisites

1. **Node.js 18+** required
2. **Claude CLI** must be installed:

```bash
# Install Claude CLI
npm install -g @anthropic-ai/claude-cli

# Authenticate
claude auth login
```

### Install Package

```bash
# npm
npm install --save-dev @zima/visual-code-editor

# yarn
yarn add -D @zima/visual-code-editor

# pnpm
pnpm add -D @zima/visual-code-editor
```

---

## 🚀 Quick Start

### 1. Initialize

Run the setup wizard in your project:

```bash
npx visual-editor init
```

This will:
- Detect your framework (Laravel, React, Vue, Angular)
- Create `.visual-editor/config.json`
- Set up workspace directories
- Configure your build tool (Vite/Webpack)

### 2. Start Agent Server

```bash
npx visual-editor start
```

The agent server will start on port **9876** by default.

### 3. Start Dev Server

```bash
npm run dev
```

### 4. Activate Inspector

In your browser, press **`Cmd+Shift+D`** to activate the visual inspector.

---

## 🎥 How It Works

1. **Select Element** - Click any UI element in your app
2. **Describe Change** - Type or speak what you want (e.g., "Add a blue download button here")
3. **Preview Code** - AI generates code changes for review
4. **Accept Changes** - Code is applied to your source files
5. **See Results** - Vite/Webpack hot reloads the page instantly

---

## 📖 Usage Examples

### Example 1: Add a Button

**User selects an empty div and types:**
> "Add a blue download button here that saves the chat history"

**AI generates:**
- Adds `<button>` element with Tailwind classes
- Creates `downloadChat()` method in Livewire component
- Includes download icon (SVG)
- Applies to correct source files

### Example 2: Style Changes

**User selects text and types:**
> "Make this text bold and larger"

**AI generates:**
- Adds `font-bold text-xl` classes (Tailwind)
- Preserves existing content

### Example 3: Layout Changes

**User selects a section and types:**
> "Move this section below the header and add padding"

**AI generates:**
- Restructures HTML in Blade template
- Adds appropriate spacing classes

---

## 🛠️ Configuration

Edit `.visual-editor/config.json`:

```json
{
  "version": "1.0.0",
  "project": {
    "framework": "laravel-livewire",
    "buildTool": "vite",
    "componentPaths": ["app/Livewire", "resources/views/livewire"]
  },
  "agent": {
    "port": 9876,
    "model": "claude-sonnet-4-5-20250514",
    "temperature": 0.7,
    "maxTokens": 8192,
    "contextOptimization": "adaptive",
    "memoryEnabled": true,
    "streamingEnabled": true
  },
  "visual": {
    "hotkey": "Cmd+Shift+D",
    "highlightColor": "#3b82f6",
    "screenshotQuality": 0.8,
    "ocrEnabled": true
  },
  "tools": {
    "zimaFileService": "http://localhost:5000",
    "enableDocumentTools": true
  },
  "performance": {
    "promptCaching": true,
    "tierOptimization": true
  }
}
```

### Key Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `agent.model` | Claude model to use | `claude-sonnet-4-5-20250514` |
| `agent.contextOptimization` | none, adaptive, aggressive | `adaptive` |
| `agent.memoryEnabled` | Enable vector search memory | `true` |
| `visual.hotkey` | Keyboard shortcut to activate | `Cmd+Shift+D` |
| `tools.zimaFileService` | Optional ZIMA backend URL | - |

---

## 🔧 API Reference

### HTTP Endpoints

#### `POST /api/chat`
Non-streaming chat completion

**Request:**
```json
{
  "sessionKey": "agent:main:webchat:direct:2",
  "message": "Create an Excel file with sales data"
}
```

**Response:**
```json
{
  "content": "I've created an Excel file...",
  "usage": { "input_tokens": 1234, "output_tokens": 567 },
  "model": "claude-sonnet-4-5-20250514"
}
```

#### `POST /api/chat/stream`
Streaming chat with Server-Sent Events (SSE)

**Response Events:**
- `event: start` - Stream started
- `event: content` - Text chunk
- `event: complete` - Stream finished with usage data

#### `GET /api/sessions`
List all active sessions

#### `POST /api/memory/search`
Search memory with semantic + keyword search

#### `GET /api/tools`
List all available tools (246+)

### WebSocket API

Connect to `ws://localhost:9877` for visual editor.

**Client → Server Messages:**
- `init` - Initialize with project info
- `element_selected` - Element clicked
- `command_submitted` - User command
- `code_preview_accepted` - Accept changes

**Server → Client Messages:**
- `ready` - Connection established
- `ai_thinking` - AI processing
- `code_preview` - Show code changes
- `changes_applied` - Changes saved

---

## 🎯 Tool Categories

### System Tools (28 tools)
- File operations: `read`, `write`, `edit`
- Shell execution: `exec`, `process`
- Web tools: `web_search`, `web_fetch`
- Memory: `memory_search`, `memory_store`
- Sessions: `sessions_list`, `sessions_send`

### Document Tools (196 tools)
- **Excel**: create, read, merge, convert, formulas, charts
- **PDF**: create, merge, split, compress, protect, OCR
- **Word**: create, merge, mail merge, track changes
- **PowerPoint**: create, merge, transitions, animations
- **JSON**: format, validate, query, transform
- **Images**: resize, convert, compress, watermark

### Visual Tools (22 tools)
- Framework detection
- Component mapping
- Code generation
- Style matching
- Screenshot analysis

---

## 🎨 Framework-Specific Features

### Laravel + Livewire

**Component Mapping:**
- Maps Livewire component name → PHP class + Blade view
- Detects `wire:id` and `wire:click` bindings
- Generates Livewire-specific syntax

**Example:**
```php
// app/Livewire/ChatInterface.php
public function downloadChat() {
    return response()->streamDownload(...);
}
```

### React

**Component Detection:**
- Uses React Fiber to detect component tree
- Maps JSX components to source files
- Generates React hooks and state management

### Vue

**Component Detection:**
- Detects Vue SFC components
- Maps template → script → styles
- Generates Vue 3 Composition API

---

## 💡 Advanced Usage

### Custom Tools

Register custom tools in your config:

```typescript
import { ToolRegistry } from '@zima/visual-code-editor';

const registry = new ToolRegistry();

registry.register({
  name: 'custom_tool',
  description: 'My custom tool',
  category: 'custom',
  provider: 'custom',
  inputSchema: { /* JSON Schema */ },
  execute: async (input) => {
    // Tool implementation
    return { success: true };
  },
});
```

### Programmatic API

```typescript
import {
  ClaudeCliRuntime,
  ToolRegistry,
  MemoryService,
  TranscriptManager,
} from '@zima/visual-code-editor';

// Initialize services
const runtime = new ClaudeCliRuntime({ workspacePath: '/path/to/project' });
const tools = new ToolRegistry();

// Execute Claude CLI
const response = await runtime.run(prompt, { streaming: true });
```

---

## 📊 Performance & Cost Optimization

### 4-Tier Context Optimization

- **Tier 0** (< 10 messages): Full context
- **Tier 1** (10-50 messages): Last 30 + summary
- **Tier 2** (50-100 messages): Last 20 + summary
- **Tier 3** (> 100 messages): Last 10 + summary

### Multi-Model Routing

- **Haiku**: Simple tasks, greetings (0.25¢/M)
- **Sonnet**: Standard operations (3.0¢/M)
- **Opus**: Complex tasks (15.0¢/M)

### Prompt Caching

System prompts cached for 5 minutes:
- **50-90% cost reduction** on repeated requests
- Automatic cache invalidation

---

## 🔐 Security

- API keys stored in environment variables only
- File operations restricted to project directory
- Command injection prevention
- CORS configuration with whitelisted origins
- Rate limiting (100 req/hour default)

---

## 🐛 Troubleshooting

### Claude CLI not found

```bash
npm install -g @anthropic-ai/claude-cli
claude auth login
```

### WebSocket connection failed

- Check firewall settings (port 9876-9877)
- Ensure agent server is running: `npx visual-editor start`
- Check browser console for errors

### Hot reload not working

- Verify Vite plugin is configured in `vite.config.js`
- Restart dev server
- Check file watcher limits (Linux): `sudo sysctl fs.inotify.max_user_watches=524288`

### Memory search not working

- Check Voyage AI API key (if using embeddings)
- Verify `.visual-editor/memory.db` exists
- Run manual indexing: `npx visual-editor memory:index`

---

## 📚 Documentation

- **Full Documentation**: See `/docs/ZIMA_VISUAL_CODE_EDITOR_COMPLETE_SPECIFICATION.md`
- **API Reference**: See source code TSDoc comments
- **Examples**: See `/examples` directory

---

## 🤝 Contributing

Contributions welcome! Please read our contributing guidelines.

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

---

## 📄 License

MIT License - see LICENSE file for details

---

## 🙏 Acknowledgments

- Built on [Claude AI](https://www.anthropic.com/) by Anthropic
- Inspired by browser DevTools and visual editors
- Part of the ZIMA ecosystem

---

## 📞 Support

- **GitHub Issues**: [github.com/zima/visual-code-editor/issues](https://github.com/zima/visual-code-editor/issues)
- **Discord**: [discord.gg/zima](https://discord.gg/zima)
- **Email**: support@zima.ai

---

**Made with ❤️ by the ZIMA team**
