# ZIMA - Interactive AI Coding Assistant

**⚡ Your local Claude Code alternative powered by Qwen2.5-Coder-14B**

## Installation Complete! ✅

The `zima` command is now available globally in your terminal.

## Quick Start

Simply type in any directory:

```bash
zima
```

This opens an interactive chat interface similar to Claude Code.

## Features

### 🎨 **Beautiful TUI Interface**
- Full-screen chat area
- Colored syntax highlighting
- Scrollable conversation history
- Real-time streaming responses

### 💬 **Interactive Chat**
- Natural conversation with AI
- Context-aware responses
- Multi-turn conversations
- Persistent chat history (~/.zima_history.json)

### 🛠️ **Built-in Commands**
- `/help` - Show help message
- `/clear` - Clear chat history
- `/tools` - List available tools
- `/exit` or `/quit` - Exit ZIMA
- `Ctrl+C` - Quick exit

### ⌨️ **Keyboard Shortcuts**
- `Enter` - Send message
- `↑/↓` - Scroll chat
- `Mouse Wheel` - Scroll chat
- `Ctrl+C` - Exit

## Usage Examples

### General Coding Help
```
You: How do I create a Laravel controller?
ZIMA: [Provides detailed explanation with code examples]
```

### Code Explanation
```
You: Explain this PHP code: array_map(fn($x) => $x * 2, $numbers)
ZIMA: [Explains the arrow function and array_map]
```

### Debugging
```
You: I'm getting "undefined variable" error in my PHP script
ZIMA: [Asks for context and helps debug]
```

### File Operations
```
You: Read README.md and summarize it
ZIMA: [Automatically uses read tool and provides summary]
```

## How It Works

1. **Local Processing**: All AI inference runs on your M4 Mac via Ollama
2. **No Cloud**: Zero data sent to external servers
3. **Context Retention**: Maintains conversation context across messages
4. **Smart Tool Use**: Automatically decides when to use file/bash tools
5. **Fast**: Optimized for M4 Neural Engine

## Architecture

```
Terminal (zima)
    ↓
Blessed TUI (interactive interface)
    ↓
Ollama API (localhost:11434)
    ↓
Qwen2.5-Coder-14B (local model)
```

## Configuration

- **Model**: Qwen2.5-Coder-14B (9GB, Q4 quantized)
- **Context**: Maintains last 5 messages
- **History**: Stored in `~/.zima_history.json`
- **Workspace**: Automatically detects current directory

## Comparison with Claude Code

| Feature | Claude Code | ZIMA |
|---------|------------|------|
| Interface | ✅ TUI | ✅ TUI |
| Chat | ✅ Interactive | ✅ Interactive |
| File Ops | ✅ Full suite | ✅ Basic |
| Privacy | ❌ Cloud | ✅ 100% Local |
| Cost | ❌ API fees | ✅ Free |
| Speed | ⚡ Fast (cloud) | ⚡ Fast (local) |
| Offline | ❌ No | ✅ Yes |
| Model | Claude Opus | Qwen2.5-Coder-14B |

## Tips

1. **Be Specific**: The more context you provide, the better the responses
2. **Use Commands**: Try `/tools` to see what ZIMA can do
3. **Scroll Back**: Use arrow keys or mouse to review previous messages
4. **Save Important Chats**: History is auto-saved, but limited to last 50 messages
5. **Start Fresh**: Use `/clear` for new contexts

## Troubleshooting

### "zima: command not found"
Run:
```bash
source ~/.zshrc
```

### Model not responding
Check if Ollama is running:
```bash
brew services list | grep ollama
```

### Interface looks broken
Make sure your terminal supports colors and has sufficient size (minimum 80x24)

## Advanced Usage

### Combine with Other Tools
```bash
# Open ZIMA in a project directory
cd ~/my-project
zima

# ZIMA automatically knows your workspace context
You: List all PHP files in this project
```

### Quick Questions (Without Chat)
For single questions, use the enhanced agent:
```bash
node /Volumes/DATA/QWEN/qwen-agent-enhanced.js "your question"
```

## What's Next?

Try these:
1. Ask ZIMA to explain complex code
2. Request code generation
3. Get debugging help
4. Learn new programming concepts
5. Automate file operations

## Credits

- **Model**: Qwen2.5-Coder-14B by Alibaba
- **Runtime**: Ollama
- **Interface**: Blessed.js
- **Inspired by**: Claude Code CLI

---

**Enjoy your local AI coding assistant!** 🚀

Type `zima` to get started.
