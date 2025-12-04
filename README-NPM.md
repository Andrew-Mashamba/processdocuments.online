# ZIMA - Local AI Coding Assistant

**100% local, 100% private, 100% free**

ZIMA is a Claude Code alternative that runs entirely on your machine using Qwen2.5-Coder-14B via Ollama.

## ⚡ Quick Install

```bash
npm install -g @zima-ai/zima
```

Then:
```bash
zima
```

## 📋 Prerequisites

### 1. Install Ollama

**macOS**:
```bash
brew install ollama
brew services start ollama
```

**Linux**:
```bash
curl -fsSL https://ollama.com/install.sh | sh
```

**Windows**:
Download from [ollama.com/download/windows](https://ollama.com/download/windows)

### 2. Download Qwen Model (9GB, one-time)

```bash
ollama pull qwen2.5-coder:14b
```

### 3. Start ZIMA

```bash
zima
```

## 🎯 Features

- **23 Tools**: File operations, shell commands, database safety, planning mode, memory, semantic search
- **Interactive TUI**: Beautiful terminal interface with streaming responses
- **100% Local**: Zero data sent to external servers
- **Smart Assistance**: Auto-testing, verification workflow, chain-of-thought reasoning
- **Task Management**: Built-in todo system for complex workflows
- **Database Safety**: Two-step migrations, RLS enforcement
- **Planning Mode**: Create detailed plans for complex tasks

## 🛠️ Built-in Commands

- `/help` - Show help
- `/clear` - Clear chat history
- `/tools` - List all 23 available tools
- `/exit` - Exit ZIMA

## 📖 Usage Examples

```bash
# General questions
You: How do I create a REST API in Node.js?

# File operations (automatic tool use)
You: Read package.json and list all dependencies

# Code generation
You: Create a migration for a users table with email and password

# Debugging
You: I'm getting "Cannot find module" error, help debug
```

## 🔧 System Requirements

- **Node.js**: 16.0.0 or higher
- **RAM**: 16GB minimum (24GB recommended)
- **Storage**: 10GB free (9GB for model)
- **OS**: macOS, Linux, or Windows

## 📚 Documentation

After installation, check these files in your global node_modules:

- `ZIMA_GUIDE.md` - Complete user guide
- `TOOLS_REFERENCE.md` - All 23 tools documented
- `README-INSTALL.md` - Detailed installation guide

## 🚀 What Makes ZIMA Special

### vs Claude Code
- ✅ 100% local (no cloud)
- ✅ Free (no API costs)
- ✅ Full privacy
- ✅ Works offline
- ✅ 23 powerful tools

### vs GitHub Copilot
- ✅ Interactive chat (not just autocomplete)
- ✅ File operations
- ✅ Shell command execution
- ✅ No subscription

## 🏗️ Architecture

```
Terminal (zima command)
    ↓
Blessed.js TUI (interactive interface)
    ↓
Ollama API (localhost:11434)
    ↓
Qwen2.5-Coder-14B (local 9GB model)
```

## 🔄 Updating

```bash
npm update -g @zima-ai/zima
```

To update the AI model:
```bash
ollama pull qwen2.5-coder:14b
```

## 🗑️ Uninstalling

```bash
npm uninstall -g @zima-ai/zima

# Optional: Remove model
ollama rm qwen2.5-coder:14b

# Optional: Uninstall Ollama
brew uninstall ollama  # macOS
```

## 🐛 Troubleshooting

### "zima: command not found"
```bash
# Ensure global npm bin is in PATH
npm config get prefix
# Add to PATH: export PATH="$(npm config get prefix)/bin:$PATH"
```

### "Ollama not responding"
```bash
# Check Ollama status
ollama list

# Restart Ollama
brew services restart ollama  # macOS
sudo systemctl restart ollama  # Linux
```

### "Model not found"
```bash
# Download the model
ollama pull qwen2.5-coder:14b
```

## 📊 Performance

- **Time to First Token**: 0.3-0.5 seconds
- **Generation Speed**: 50-100 tokens/second
- **Context Window**: 32K tokens
- **Memory Usage**: ~10-12GB during inference

## 🎓 Learning Resources

- **ZIMA Guide**: Comprehensive usage guide
- **Tools Reference**: Detailed tool documentation
- **Examples**: Built-in examples via `/help`

## 🤝 Contributing

Found a bug or have a feature request? 

Visit: [github.com/Andrew-Mashamba/zima_code](https://github.com/Andrew-Mashamba/zima_code)

## 📜 License

MIT License - Free to use, modify, and distribute

## 🙏 Credits

- **Model**: Qwen2.5-Coder-14B by Alibaba
- **Runtime**: Ollama
- **UI**: Blessed.js
- **Inspired by**: Claude Code CLI

---

**Start coding with ZIMA today!**

```bash
npm install -g @zima-ai/zima
ollama pull qwen2.5-coder:14b
zima
```

🎉 Enjoy your local AI coding assistant!
