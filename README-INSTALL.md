# ZIMA Installation Guide

**ZIMA** - Your local AI coding assistant powered by Qwen2.5-Coder-14B

---

## 🖥️ System Requirements

### All Platforms
- **RAM**: 16GB minimum (24GB recommended)
- **Storage**: 10GB free space (9GB for model + 1GB for dependencies)
- **Network**: Internet required for initial download only

### Recommended
- **CPU**: Multi-core processor (8+ cores recommended)
- **GPU**: Optional (speeds up inference if supported)

---

## 🍎 macOS Installation

### Automated Installation

1. **Download or clone ZIMA**:
   ```bash
   cd /path/to/ZIMA
   ```

2. **Run installer**:
   ```bash
   bash install-macos.sh
   ```

3. **Open new terminal** and type:
   ```bash
   zima
   ```

### Manual Installation

1. **Install Homebrew** (if not installed):
   ```bash
   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
   ```

2. **Install Node.js**:
   ```bash
   brew install node
   ```

3. **Install Ollama**:
   ```bash
   brew install ollama
   brew services start ollama
   ```

4. **Download Qwen model**:
   ```bash
   ollama pull qwen2.5-coder:14b
   ```

5. **Install ZIMA**:
   ```bash
   cd /path/to/ZIMA
   npm install --production
   npm link
   ```

6. **Start using**:
   ```bash
   zima
   ```

---

## 🐧 Linux (Ubuntu/Debian) Installation

### Automated Installation

1. **Download or clone ZIMA**:
   ```bash
   cd /path/to/ZIMA
   ```

2. **Run installer**:
   ```bash
   bash install-linux.sh
   ```

3. **Open new terminal** and type:
   ```bash
   zima
   ```

### Manual Installation

1. **Install Node.js**:
   ```bash
   curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
   sudo apt-get install -y nodejs
   ```

2. **Install Ollama**:
   ```bash
   curl -fsSL https://ollama.com/install.sh | sh
   ```

3. **Start Ollama**:
   ```bash
   sudo systemctl start ollama
   sudo systemctl enable ollama
   ```

4. **Download Qwen model**:
   ```bash
   ollama pull qwen2.5-coder:14b
   ```

5. **Install ZIMA**:
   ```bash
   cd /path/to/ZIMA
   npm install --production
   sudo npm link
   ```

6. **Start using**:
   ```bash
   zima
   ```

---

## 🪟 Windows Installation

### Automated Installation (PowerShell)

1. **Open PowerShell as Administrator**:
   - Right-click PowerShell
   - Select "Run as Administrator"

2. **Navigate to ZIMA directory**:
   ```powershell
   cd C:\path\to\ZIMA
   ```

3. **Run installer**:
   ```powershell
   powershell -ExecutionPolicy Bypass -File install-windows.ps1
   ```

4. **Restart PowerShell** and type:
   ```powershell
   zima
   ```

### Manual Installation

1. **Install Chocolatey** (PowerShell as Admin):
   ```powershell
   Set-ExecutionPolicy Bypass -Scope Process -Force
   iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
   ```

2. **Install Node.js**:
   ```powershell
   choco install nodejs -y
   ```

3. **Install Ollama**:
   - Download from: https://ollama.com/download/windows
   - Run the installer

4. **Start Ollama** (it runs automatically after install, or run):
   ```powershell
   ollama serve
   ```

5. **Download Qwen model** (new PowerShell window):
   ```powershell
   ollama pull qwen2.5-coder:14b
   ```

6. **Install ZIMA**:
   ```powershell
   cd C:\path\to\ZIMA
   npm install --production
   npm link
   ```

7. **Start using**:
   ```powershell
   zima
   ```

---

## 🔧 Post-Installation

### Verify Installation

```bash
# Check Ollama is running
ollama list

# Check ZIMA command is available
which zima  # macOS/Linux
where zima  # Windows

# Test ZIMA
zima
```

### First Run

When you run `zima` for the first time:

1. The interactive TUI will open
2. Type your first message (e.g., "hello")
3. Wait for Qwen to respond
4. Start coding!

### Available Commands

Inside ZIMA:
- `/help` - Show help
- `/clear` - Clear chat history
- `/tools` - List available tools
- `/exit` - Exit ZIMA
- `Ctrl+C` - Quick exit

---

## 📦 What Gets Installed

### File Locations

**macOS/Linux**:
```
~/.zima/                    # Config directory
~/.zima_history.json        # Chat history
~/.zima_memory.json         # Persistent memory
/usr/local/lib/node_modules/zima/  # Global installation
```

**Windows**:
```
C:\Users\<username>\.zima\         # Config directory
C:\Users\<username>\.zima_history.json    # Chat history
C:\Users\<username>\.zima_memory.json     # Persistent memory
%APPDATA%\npm\node_modules\zima\   # Global installation
```

### Dependencies Installed

- **Node.js**: JavaScript runtime
- **Ollama**: Local LLM runner
- **Qwen2.5-Coder-14B**: 9GB AI model
- **blessed**: Terminal UI library
- **chalk**: Terminal colors
- **glob**: File pattern matching

---

## 🚨 Troubleshooting

### macOS

**"zima: command not found"**:
```bash
source ~/.zshrc  # or ~/.bashrc
```

**Homebrew issues**:
```bash
brew doctor
brew update
```

**Ollama not responding**:
```bash
brew services restart ollama
```

### Linux

**Permission denied**:
```bash
sudo npm link
sudo chown -R $USER ~/.npm
```

**Ollama service not starting**:
```bash
sudo systemctl status ollama
sudo systemctl restart ollama
```

**Port 11434 already in use**:
```bash
sudo lsof -i :11434
sudo kill -9 <PID>
```

### Windows

**PowerShell execution policy error**:
```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

**Ollama not found**:
- Manually download: https://ollama.com/download/windows
- Add to PATH: `C:\Program Files\Ollama\`

**npm link fails**:
```powershell
npm config set prefix "%APPDATA%\npm"
```

### All Platforms

**Model download stuck**:
```bash
# Cancel and retry
Ctrl+C
ollama pull qwen2.5-coder:14b
```

**Out of memory**:
- Close other applications
- Ensure 16GB+ RAM available
- Consider using smaller model: `ollama pull qwen2.5-coder:7b`

**Slow responses**:
- Check if GPU is being used (if available)
- Reduce context window in zima.js
- Use lighter quantization

---

## 🔄 Updating ZIMA

```bash
cd /path/to/ZIMA

# Pull latest code
git pull  # if using git

# Update dependencies
npm install --production

# Re-link globally
npm link

# Update model (if new version available)
ollama pull qwen2.5-coder:14b
```

---

## 🗑️ Uninstalling ZIMA

### macOS/Linux

```bash
# Remove global command
npm unlink -g zima

# Remove Ollama (optional)
brew uninstall ollama  # macOS
sudo systemctl stop ollama && sudo rm -rf /usr/local/bin/ollama  # Linux

# Remove model (optional)
ollama rm qwen2.5-coder:14b

# Remove config files
rm -rf ~/.zima ~/.zima_history.json ~/.zima_memory.json
```

### Windows

```powershell
# Remove global command
npm unlink -g zima

# Uninstall Ollama (optional)
# Go to: Settings > Apps > Ollama > Uninstall

# Remove config files
Remove-Item -Recurse -Force $env:USERPROFILE\.zima
Remove-Item -Force $env:USERPROFILE\.zima_history.json
Remove-Item -Force $env:USERPROFILE\.zima_memory.json
```

---

## 🎯 Next Steps

After successful installation:

1. **Read the guide**: `ZIMA_GUIDE.md`
2. **Explore tools**: `TOOLS_REFERENCE.md`
3. **Try examples**: See README_FINAL.md
4. **Start coding**: `zima`

---

## 💬 Support

- **Issues**: Check troubleshooting section above
- **Documentation**: See other .md files in this directory
- **Model info**: https://ollama.com/library/qwen2.5-coder
- **Ollama docs**: https://ollama.com/docs

---

## ✅ Installation Checklist

- [ ] Node.js installed (v18+)
- [ ] Ollama installed and running
- [ ] Qwen2.5-Coder-14B model downloaded (9GB)
- [ ] ZIMA dependencies installed
- [ ] Global `zima` command available
- [ ] First test run successful

---

**Ready to code with ZIMA!** 🚀

Type `zima` in your terminal to begin.
