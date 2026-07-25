#!/bin/bash

# Deploy Gateway to Linux Server
# This script sets up the gateway with correct paths for Linux

set -e

echo "🚀 Deploying Gateway to Linux Server..."
echo ""

# Configuration (can be overridden with environment variables)
PROJECT_DIR="${PROJECT_DIR:-/opt/gateway}"
ZIMA_API="${ZIMA_API:-http://localhost:5000}"
ZIMA_TOOLS_PATH="${ZIMA_TOOLS_PATH:-/opt/zima-file-service/bin/Debug/net9.0/.zima-tools.json}"

echo "📋 Configuration:"
echo "   Project directory: $PROJECT_DIR"
echo "   ZIMA API: $ZIMA_API"
echo "   ZIMA tools: $ZIMA_TOOLS_PATH"
echo ""

# Check if running as root
if [ "$EUID" -eq 0 ]; then
  echo "⚠️  Running as root. Creating directories in /opt/..."
  USE_SUDO=""
else
  echo "📋 Running as user. Will use sudo for system directories."
  USE_SUDO="sudo"
fi

# Create project directory
echo "📂 Creating project directory..."
$USE_SUDO mkdir -p "$PROJECT_DIR"
$USE_SUDO chown -R $USER:$USER "$PROJECT_DIR" 2>/dev/null || true

# Create subdirectories
echo "📂 Creating storage directories..."
mkdir -p "$PROJECT_DIR/storage"
mkdir -p "$PROJECT_DIR/workspace"
mkdir -p "$PROJECT_DIR/generated_files"
mkdir -p "$PROJECT_DIR/workspace/memory"

# Copy project files (if not already in target directory)
if [ "$(pwd)" != "$PROJECT_DIR" ]; then
  echo "📦 Copying project files..."
  rsync -av \
    --exclude 'node_modules' \
    --exclude '.git' \
    --exclude 'dist' \
    --exclude 'storage' \
    --exclude 'generated_files' \
    ./ "$PROJECT_DIR/"
fi

# Navigate to project directory
cd "$PROJECT_DIR"

# Install dependencies
echo "📥 Installing dependencies..."
npm install --production

# Build project
echo "🔨 Building project..."
npm run build

# Create .env file
echo "⚙️  Creating .env configuration..."
cat > .env << EOF
# Gateway Configuration
PORT=18790
BIND=0.0.0.0
CORS_ORIGINS=*

# ZIMA API
ZIMA_API_URL=$ZIMA_API
ZIMA_TIMEOUT=30000

# ZIMA Tools Path
ZIMA_TOOLS_PATH=$ZIMA_TOOLS_PATH

# Storage Paths
STORAGE_ROOT=$PROJECT_DIR/storage
WORKSPACE_DIR=$PROJECT_DIR/workspace
MEMORY_DB=$PROJECT_DIR/storage/memory.db
TRANSCRIPTS_DIR=\${HOME}/.zima/agents/main/sessions
FILES_DIR=$PROJECT_DIR/generated_files
EOF

echo "✓ .env created"

# Configure Claude CLI MCP
echo "⚙️  Configuring Claude CLI MCP server..."

# Check if Claude CLI is installed
if ! command -v claude &> /dev/null; then
  echo "⚠️  Claude CLI not found. Skipping MCP configuration."
  echo "   Install Claude CLI and run: claude mcp add gateway node $PROJECT_DIR/dist/mcp/gateway-mcp-server.js"
else
  # Remove existing gateway server (if any)
  claude mcp remove gateway --scope user 2>/dev/null || true

  # Use Python to update .claude.json with proper formatting
  python3 << EOFPY
import json
import os

config_path = os.path.expanduser('~/.claude.json')

# Read existing config or create new
if os.path.exists(config_path):
    with open(config_path, 'r') as f:
        config = json.load(f)
else:
    config = {}

# Ensure mcpServers exists
if 'mcpServers' not in config:
    config['mcpServers'] = {}

# Add gateway server
config['mcpServers']['gateway'] = {
    "type": "stdio",
    "command": "node",
    "args": ["$PROJECT_DIR/dist/mcp/gateway-mcp-server.js"],
    "env": {
        "WORKSPACE_DIR": "$PROJECT_DIR/workspace",
        "ZIMA_API_URL": "$ZIMA_API",
        "MEMORY_DB": "$PROJECT_DIR/storage/memory.db",
        "STORAGE_ROOT": "$PROJECT_DIR/storage",
        "ZIMA_TOOLS_PATH": "$ZIMA_TOOLS_PATH"
    }
}

# Write back
with open(config_path, 'w') as f:
    json.dump(config, f, indent=2)

print("✓ Claude CLI MCP configured")
EOFPY

  echo "✓ MCP configuration complete"
fi

# Create systemd service (optional)
if [ -d "/etc/systemd/system" ]; then
  echo ""
  echo "📝 Creating systemd service (optional)..."

  $USE_SUDO tee /etc/systemd/system/gateway.service > /dev/null << EOF
[Unit]
Description=Gateway MCP Server
After=network.target

[Service]
Type=simple
User=$USER
WorkingDirectory=$PROJECT_DIR
EnvironmentFile=$PROJECT_DIR/.env
ExecStart=/usr/bin/node $PROJECT_DIR/dist/mcp/gateway-mcp-server.js
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

  echo "✓ Systemd service created at /etc/systemd/system/gateway.service"
  echo "   Enable with: sudo systemctl enable gateway"
  echo "   Start with: sudo systemctl start gateway"
fi

echo ""
echo "✅ Deployment complete!"
echo ""
echo "📋 Verification:"
echo "   1. Test MCP server:"
echo "      echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}' | node $PROJECT_DIR/dist/mcp/gateway-mcp-server.js"
echo ""
echo "   2. Check Claude CLI:"
echo "      claude mcp list"
echo ""
echo "   3. Test with Claude:"
echo "      claude \"List available tools from gateway\""
echo ""
echo "💡 Configuration file: $PROJECT_DIR/.env"
echo "💡 MCP server: $PROJECT_DIR/dist/mcp/gateway-mcp-server.js"
