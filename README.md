# ZIMA - Local AI Coding Assistant

AI-assisted coding using **ZIMA** with **Qwen2.5-Coder-14B** running locally via Ollama.

ZIMA is a rebranded fork of OpenCode, configured for 100% local AI coding.

## Prerequisites

- [Go](https://go.dev) >= 1.21
- [Ollama](https://ollama.ai) installed and running
- Qwen model: `ollama pull qwen2.5-coder:14b`

## Installation

### Build from Source

```bash
cd zima
go build -o zima .
go install .
```

The binary will be installed to `~/go/bin/zima`.

Add to your PATH (add to `~/.zshrc` or `~/.bashrc`):
```bash
export PATH=$PATH:$HOME/go/bin
```

### Verify Installation

```bash
zima --version
```

## Configuration

Create the config file at `~/.config/zima/zima.json`:

```bash
mkdir -p ~/.config/zima
```

```json
{
  "$schema": "https://zima.ai/config.json",
  "provider": {
    "ollama": {
      "npm": "@ai-sdk/openai-compatible",
      "name": "Ollama (Qwen Local)",
      "options": {
        "baseURL": "http://localhost:11434/v1"
      },
      "models": {
        "qwen2.5-coder:14b": {
          "name": "Qwen2.5-Coder-14B"
        }
      }
    }
  }
}
```

## Usage

```bash
# Start interactive TUI
zima

# Run with debug logging
zima -d

# Run in specific directory
zima -c /path/to/project

# Non-interactive mode
zima -p "Explain this code" -f json
```

### Key Shortcuts

| Action | Shortcut |
|--------|----------|
| Help | `Ctrl+?` |
| Command dialog | `Ctrl+K` |
| Model selection | `Ctrl+O` |
| Send message | `Ctrl+S` or `Enter` |
| New session | `Ctrl+N` |
| Switch session | `Ctrl+A` |

## Features

- 100% Local AI (Qwen2.5-Coder-14B via Ollama)
- Interactive TUI with Bubble Tea
- LSP integration for code intelligence
- File operations and shell commands
- Session management
- Multi-provider support (Ollama, OpenAI, Anthropic, etc.)

## License

MIT (forked from OpenCode)
