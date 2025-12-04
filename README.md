# ZIMA - Local AI Coding Assistant

<div align="center">

**100% Local • 100% Private • 100% Free**

Your Claude Code alternative powered by Qwen2.5-Coder-14B

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

</div>

---

## ⚡ Quick Install

### Via npm (Recommended)
```bash
npm install -g @zima-ai/zima
ollama pull qwen2.5-coder:14b
zima
```

### Via Installer Scripts

**macOS:**
```bash
curl -fsSL https://raw.githubusercontent.com/Andrew-Mashamba/zima_code/main/install-macos.sh | bash
```

**Linux (Ubuntu/Debian):**
```bash
curl -fsSL https://raw.githubusercontent.com/Andrew-Mashamba/zima_code/main/install-linux.sh | bash
```

**Windows (PowerShell as Admin):**
```powershell
irm https://raw.githubusercontent.com/Andrew-Mashamba/zima_code/main/install-windows.ps1 | iex
```

---

## 🎯 What is ZIMA?

ZIMA is an interactive AI coding assistant that runs **entirely on your machine**. No cloud, no API costs, no data leaving your computer.

### Key Features

- 🛠️ **23 Powerful Tools** - File operations, shell commands, database safety, planning mode, memory system
- 💬 **Interactive TUI** - Beautiful terminal interface with real-time streaming
- ⚡ **Speed Optimized** - <0.5s response time with smart caching
- 🔒 **100% Private** - All processing happens locally via Ollama
- 🌍 **Cross-Platform** - macOS, Linux, and Windows support
- 🆓 **Completely Free** - No subscriptions, no API costs

---

## 📖 Usage

Start ZIMA in any directory:
```bash
zima
```

### Built-in Commands
- `/help` - Show help and commands
- `/tools` - List all 23 available tools  
- `/clear` - Clear chat history
- `/exit` - Exit ZIMA

### Example Interactions

```bash
# Ask questions
You: How do I create a REST API in Node.js?

# File operations (automatic)
You: Read package.json and list dependencies

# Code generation
You: Create a migration for users table with email and password

# Debugging
You: I'm getting "Cannot find module" error, help debug
```

---

## 🛠️ What Can ZIMA Do?

### File Operations
- `create_file`, `read_file`, `edit_file`, `multi_edit`
- Smart context tracking and caching

### Directory Operations  
- `list_files`, `glob_files`, `grep`
- Pattern matching and content search

### Shell Operations
- `run_command`, `bash_output`, `kill_bash`
- Background process management

### Advanced Features
- `verify_changes` - Auto-test and validate code
- `create_migration`, `execute_migration`, `validate_rls` - Database safety
- `create_plan`, `approve_plan` - Planning mode for complex tasks
- `remember`, `recall`, `list_learnings` - Persistent memory
- `semantic_search` - Find code by meaning
- `create_contracts` - API contract generation

See [TOOLS_REFERENCE.md](./TOOLS_REFERENCE.md) for complete documentation.

---

## 🔧 Architecture

```
Terminal (zima command)
    ↓
Blessed.js TUI (interactive interface)
    ↓
Ollama API (localhost:11434)
    ↓
Qwen2.5-Coder-14B (local 9GB model)
```

**No cloud required** - Everything runs on your machine.

---

## 📋 System Requirements

- **Node.js**: 16.0.0 or higher
- **RAM**: 16GB minimum (24GB recommended)
- **Storage**: 10GB free space
- **OS**: macOS, Linux, or Windows

---

## 📚 Documentation

- **[Installation Guide](./README-INSTALL.md)** - Detailed installation instructions
- **[Tools Reference](./TOOLS_REFERENCE.md)** - All 23 tools documented
- **[User Guide](./ZIMA_GUIDE.md)** - Complete usage guide
- **[npm Publishing](./NPM-PUBLISH-GUIDE.md)** - For maintainers

---

## 🚀 How ZIMA Compares

### vs Claude Code
- ✅ 100% local (no cloud required)
- ✅ Free (no API costs)
- ✅ Full privacy (data never leaves your machine)
- ✅ Works offline (after initial setup)

### vs GitHub Copilot
- ✅ Interactive chat (not just autocomplete)
- ✅ Full file operations and shell access
- ✅ Task management and planning
- ✅ No monthly subscription

### vs ChatGPT
- ✅ Context-aware of your codebase
- ✅ Can execute tools and make changes
- ✅ Faster (local inference)
- ✅ Complete privacy

---

## 🔄 Updating

### Update ZIMA
```bash
npm update -g @zima-ai/zima
```

### Update AI Model
```bash
ollama pull qwen2.5-coder:14b
```

---

## 🗑️ Uninstalling

```bash
# Remove ZIMA
npm uninstall -g @zima-ai/zima

# Optional: Remove Ollama and model
brew uninstall ollama  # macOS
ollama rm qwen2.5-coder:14b
```

---

## 🐛 Troubleshooting

### "zima: command not found"
```bash
source ~/.zshrc  # or ~/.bashrc
```

### Ollama not responding
```bash
# macOS
brew services restart ollama

# Linux  
sudo systemctl restart ollama

# Check if running
ollama list
```

### Model not found
```bash
ollama pull qwen2.5-coder:14b
```

See [Installation Guide](./README-INSTALL.md) for more troubleshooting.

---

## 🤝 Contributing

Contributions are welcome! Feel free to:
- Report bugs via [Issues](https://github.com/Andrew-Mashamba/zima_code/issues)
- Submit pull requests
- Suggest new features
- Improve documentation

---

## 📜 License

MIT License - see [LICENSE](./LICENSE) file for details.

Free to use, modify, and distribute.

---

## 🙏 Credits

- **Model**: [Qwen2.5-Coder-14B](https://github.com/QwenLM/Qwen) by Alibaba
- **Runtime**: [Ollama](https://ollama.com)
- **UI**: [Blessed.js](https://github.com/chjj/blessed)
- **Inspired by**: Claude Code CLI

---

## ⭐ Show Your Support

If you find ZIMA helpful, please:
- ⭐ Star this repository
- 🐦 Share on social media
- 📝 Write a blog post or review
- 🤝 Contribute improvements

---

<div align="center">

**Start coding with ZIMA today!**

```bash
npm install -g @zima-ai/zima
ollama pull qwen2.5-coder:14b
zima
```

Made with ❤️ by [Andrew Mashamba](https://github.com/Andrew-Mashamba)

</div>
