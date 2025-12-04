#!/bin/bash
# ZIMA Linux/Ubuntu Installer
# Installs ZIMA AI Coding Assistant on Ubuntu/Debian-based systems

set -e

echo "🚀 ZIMA Installer for Linux (Ubuntu/Debian)"
echo "==========================================="
echo ""

# Check if running on Linux
if [[ "$OSTYPE" != "linux-gnu"* ]]; then
    echo "❌ This installer is for Linux only."
    echo "For macOS, use: bash install-macos.sh"
    exit 1
fi

# Detect package manager
if command -v apt-get &> /dev/null; then
    PKG_MANAGER="apt-get"
    UPDATE_CMD="sudo apt-get update"
    INSTALL_CMD="sudo apt-get install -y"
elif command -v yum &> /dev/null; then
    PKG_MANAGER="yum"
    UPDATE_CMD="sudo yum update -y"
    INSTALL_CMD="sudo yum install -y"
else
    echo "❌ Unsupported package manager. This installer supports apt-get and yum."
    exit 1
fi

echo "📦 Detected package manager: $PKG_MANAGER"

# Update package list
echo "📦 Updating package list..."
$UPDATE_CMD

# Check for Node.js
echo "📦 Checking for Node.js..."
if ! command -v node &> /dev/null; then
    echo "Installing Node.js..."
    if [[ "$PKG_MANAGER" == "apt-get" ]]; then
        curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
        $INSTALL_CMD nodejs
    else
        $INSTALL_CMD nodejs npm
    fi
fi
NODE_VERSION=$(node --version)
echo "✅ Node.js $NODE_VERSION found"

# Check for curl
if ! command -v curl &> /dev/null; then
    echo "Installing curl..."
    $INSTALL_CMD curl
fi

# Install Ollama
echo "📦 Checking for Ollama..."
if ! command -v ollama &> /dev/null; then
    echo "Installing Ollama..."
    curl -fsSL https://ollama.com/install.sh | sh
fi
echo "✅ Ollama found"

# Start Ollama service
echo "🔧 Starting Ollama service..."
if command -v systemctl &> /dev/null; then
    sudo systemctl start ollama
    sudo systemctl enable ollama
else
    # Fallback: run ollama serve in background
    nohup ollama serve > /dev/null 2>&1 &
fi
sleep 3
echo "✅ Ollama service started"

# Pull Qwen2.5-Coder model
echo "📥 Downloading Qwen2.5-Coder-14B model (9GB)..."
echo "This may take 5-15 minutes depending on your connection..."
ollama pull qwen2.5-coder:14b
echo "✅ Model downloaded"

# Install npm dependencies
echo "📦 Installing ZIMA dependencies..."
npm install --production
echo "✅ Dependencies installed"

# Make zima.js executable
chmod +x zima.js

# Create global link
echo "🔗 Creating global 'zima' command..."
sudo npm link
echo "✅ Global command created"

# Create config directory
mkdir -p ~/.zima

# Add to PATH if needed
SHELL_RC="$HOME/.bashrc"
if [[ -f "$HOME/.zshrc" ]]; then
    SHELL_RC="$HOME/.zshrc"
fi

# Test installation
echo ""
echo "🧪 Testing installation..."
if command -v zima &> /dev/null; then
    echo "✅ ZIMA installed successfully!"
else
    echo "⚠️  Command 'zima' not found. Try: source $SHELL_RC"
fi

echo ""
echo "==========================================="
echo "✅ Installation Complete!"
echo "==========================================="
echo ""
echo "Quick Start:"
echo "1. Open a new terminal (or run: source $SHELL_RC)"
echo "2. Type: zima"
echo "3. Start chatting with your AI coding assistant!"
echo ""
echo "Documentation: $(pwd)/ZIMA_GUIDE.md"
echo "Tools Reference: $(pwd)/TOOLS_REFERENCE.md"
echo ""
echo "🎉 Enjoy ZIMA!"
