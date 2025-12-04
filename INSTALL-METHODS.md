# ZIMA - Installation Methods

## 🚀 Choose Your Installation Method

### Method 1: npm (Recommended) ⭐

**Easiest for most users**

```bash
# Install globally
npm install -g @zima-ai/zima

# Setup prerequisites
ollama pull qwen2.5-coder:14b

# Run
zima
```

**Pros**:
- ✅ One command install
- ✅ Automatic updates via `npm update -g @zima-ai/zima`
- ✅ Works on all platforms
- ✅ No need to clone repository

**Cons**:
- ❌ Requires npm/Node.js installed first
- ❌ Still need to install Ollama separately

---

### Method 2: Automated Installers 🤖

**Best for complete automation**

**macOS**:
```bash
curl -fsSL https://raw.githubusercontent.com/Andrew-Mashamba/zima_code/main/install-macos.sh | bash
```

**Linux**:
```bash
curl -fsSL https://raw.githubusercontent.com/Andrew-Mashamba/zima_code/main/install-linux.sh | bash
```

**Windows** (PowerShell as Admin):
```powershell
irm https://raw.githubusercontent.com/Andrew-Mashamba/zima_code/main/install-windows.ps1 | iex
```

**Pros**:
- ✅ Installs everything (Node.js, Ollama, ZIMA, Qwen model)
- ✅ No manual steps
- ✅ Platform-specific optimizations

**Cons**:
- ❌ Downloads ~9GB (Qwen model)
- ❌ May take 10-20 minutes

---

### Method 3: Manual Installation from Source 🔧

**For developers who want full control**

```bash
# Clone repository
git clone https://github.com/Andrew-Mashamba/zima_code.git
cd zima

# Install dependencies
npm install --production

# Create global link
npm link

# Setup Ollama and model
brew install ollama  # or your package manager
ollama pull qwen2.5-coder:14b

# Run
zima
```

**Pros**:
- ✅ Full control over installation
- ✅ Can modify source code
- ✅ Stay on bleeding edge (git pull to update)

**Cons**:
- ❌ More steps required
- ❌ Need to manage updates manually

---

### Method 4: npx (No Installation) 🎯

**Try ZIMA without installing**

```bash
# Requires Ollama and model already installed
npx @zima-ai/zima
```

**Pros**:
- ✅ No global installation
- ✅ Always runs latest version
- ✅ Perfect for trying ZIMA first

**Cons**:
- ❌ Slower startup (downloads each time)
- ❌ Still need Ollama + model pre-installed

---

### Method 5: Docker (Future) 🐳

**Containerized installation** (coming soon)

```bash
# Pull image
docker pull zimaai/zima:latest

# Run
docker run -it --rm -v $(pwd):/workspace zimaai/zima
```

**Pros**:
- ✅ Completely isolated
- ✅ No dependencies on host
- ✅ Reproducible environment

**Cons**:
- ❌ Larger download (~12GB including Ollama)
- ❌ Docker required
- ❌ Not yet available

---

## 📋 Prerequisites Comparison

| Method | Node.js | Ollama | Qwen Model | Git |
|--------|---------|--------|------------|-----|
| **npm** | ✅ Required | ⚠️ Manual | ⚠️ Manual | ❌ No |
| **Automated Installers** | ✅ Auto-installed | ✅ Auto-installed | ✅ Auto-installed | ❌ No |
| **Manual from Source** | ✅ Required | ⚠️ Manual | ⚠️ Manual | ✅ Required |
| **npx** | ✅ Required | ✅ Required | ✅ Required | ❌ No |
| **Docker** | ❌ No | ❌ No | ❌ No | ❌ No |

---

## 🎯 Which Method Should I Use?

### For End Users
👉 **npm installation** (Method 1)
- Quick and simple
- Standard Node.js workflow
- Easy updates

### For First-Time Users
👉 **Automated installers** (Method 2)
- Everything installed automatically
- No prior knowledge needed
- Just run one command

### For Developers
👉 **Manual from source** (Method 3)
- Modify and customize ZIMA
- Contribute to development
- Control everything

### For Quick Testing
👉 **npx** (Method 4)
- No commitment
- Try before installing
- Clean exit

### For Production Deployment
👉 **Docker** (Method 5, when available)
- Consistent environment
- Easy scaling
- Production-ready

---

## ⚡ Quick Start Matrix

| Platform | Fastest Method | Command |
|----------|---------------|---------|
| **macOS** | npm | `npm i -g @zima-ai/zima && ollama pull qwen2.5-coder:14b` |
| **Linux** | npm | `npm i -g @zima-ai/zima && ollama pull qwen2.5-coder:14b` |
| **Windows** | npm | `npm i -g @zima-ai/zima && ollama pull qwen2.5-coder:14b` |

---

## 🔄 Updating ZIMA

### Method 1 (npm):
```bash
npm update -g @zima-ai/zima
```

### Method 2 (Automated):
Re-run installer script

### Method 3 (Manual):
```bash
cd /path/to/zima
git pull
npm install
```

### Method 4 (npx):
Automatic (always latest)

---

## 🗑️ Uninstalling

### Method 1 (npm):
```bash
npm uninstall -g @zima-ai/zima
```

### Method 2 (Automated):
```bash
# macOS
brew uninstall ollama
npm uninstall -g @zima-ai/zima

# Linux
sudo apt remove ollama  # or yum remove
npm uninstall -g @zima-ai/zima

# Windows
# Uninstall via Settings > Apps
```

### Method 3 (Manual):
```bash
npm unlink -g
rm -rf /path/to/zima
```

---

## 🌐 Online Installation (One-Command)

After ZIMA is published to npm:

### Global Install:
```bash
npm install -g @zima-ai/zima
```

### With Prerequisites (all platforms):
```bash
# Complete one-liner (will prompt for each step)
npm install -g @zima-ai/zima && ollama pull qwen2.5-coder:14b && zima
```

---

## 📦 Package Managers

ZIMA will be available on:

- ✅ **npm** (Node Package Manager) - Primary
- 🔜 **Homebrew** (macOS/Linux) - `brew install zima`
- 🔜 **Chocolatey** (Windows) - `choco install zima`
- 🔜 **Snap** (Linux) - `snap install zima`
- 🔜 **Flatpak** (Linux) - `flatpak install zima`

---

**Choose your method and start coding with ZIMA!** 🚀
