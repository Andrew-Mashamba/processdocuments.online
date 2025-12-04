#!/bin/bash
# ZIMA macOS Installer
# Installs ZIMA AI Coding Assistant on macOS

set -e

echo "🚀 ZIMA Installer for macOS"
echo "=============================="
echo ""

# Check if running on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "❌ This installer is for macOS only."
    echo "For Linux, use: bash install-linux.sh"
    exit 1
fi

# Check for Homebrew
echo "📦 Checking for Homebrew..."
if ! command -v brew &> /dev/null; then
    echo "❌ Homebrew not found. Installing Homebrew first..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
fi
echo "✅ Homebrew found"

# Check for Node.js
echo "📦 Checking for Node.js..."
if ! command -v node &> /dev/null; then
    echo "Installing Node.js..."
    brew install node
fi
NODE_VERSION=$(node --version)
echo "✅ Node.js $NODE_VERSION found"

# Check for Ollama
echo "📦 Checking for Ollama..."
if ! command -v ollama &> /dev/null; then
    echo "Installing Ollama..."
    brew install ollama
fi
echo "✅ Ollama found"

# Start Ollama service
echo "🔧 Starting Ollama service..."
brew services start ollama
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
npm link
echo "✅ Global command created"

# Create config directory
mkdir -p ~/.zima

# Test installation
echo ""
echo "🧪 Testing installation..."
if command -v zima &> /dev/null; then
    echo "✅ ZIMA installed successfully!"
else
    echo "⚠️  Command 'zima' not found. Try: source ~/.zshrc"
fi

echo ""
echo "=============================="
echo "✅ Installation Complete!"
echo "=============================="
echo ""
echo "Quick Start:"
echo "1. Open a new terminal (or run: source ~/.zshrc)"
echo "2. Type: zima"
echo "3. Start chatting with your AI coding assistant!"
echo ""
echo "Documentation: $(pwd)/ZIMA_GUIDE.md"
echo "Tools Reference: $(pwd)/TOOLS_REFERENCE.md"
echo ""
echo "🎉 Enjoy ZIMA!"
