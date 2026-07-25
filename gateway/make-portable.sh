#!/bin/bash

# Make Gateway Portable for Linux Deployment
# This script updates hardcoded paths to use relative paths

set -e

echo "🔧 Making Gateway portable for Linux deployment..."
echo ""

# Backup original files
echo "📦 Creating backups..."
cp src/mcp/gateway-mcp-server.ts src/mcp/gateway-mcp-server.ts.backup
cp src/agent/tool-registry.ts src/agent/tool-registry.ts.backup

echo "✓ Backups created"
echo ""

# Update MCP server to use relative paths
echo "🔨 Updating MCP server (gateway-mcp-server.ts)..."
cat > src/mcp/gateway-mcp-server.ts << 'EOF'
#!/usr/bin/env node
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  Tool as MCPTool,
} from "@modelcontextprotocol/sdk/types.js";
import { GatewayConfig } from "../types/index.js";
import { UnifiedToolRegistry } from "../agent/tool-registry.js";
import { ToolExecutor } from "../agent/tool-executor.js";
import path from 'path';
import { fileURLToPath } from 'url';

// Get project root directory
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const projectRoot = path.resolve(__dirname, '../..');

/**
 * Gateway MCP Server
 *
 * Exposes all gateway tools (memory_search, ZIMA tools, OpenClaw tools)
 * to Claude CLI via the Model Context Protocol.
 *
 * NOW PORTABLE! Uses relative paths and environment variables.
 */
export class GatewayMcpServer {
  private server: Server;
  private registry: UnifiedToolRegistry;
  private executor: ToolExecutor;
  private config: GatewayConfig;

  constructor(config: GatewayConfig) {
    this.config = config;

    // Initialize MCP server
    this.server = new Server(
      {
        name: "gateway",
        version: "1.0.0",
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    // Initialize tool registry and executor
    this.registry = new UnifiedToolRegistry(config);
    this.executor = new ToolExecutor(config, this.registry);

    this.setupHandlers();
  }

  private setupHandlers() {
    // Handle: List available tools
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      console.error("📋 [MCP] Claude requesting tool list...");

      try {
        // Load all gateway tools
        await this.registry.refresh();
        const tools = await this.registry.getTools();

        console.error(`✓ [MCP] Exposing ${tools.length} tools`);

        // Convert gateway tools to MCP format
        const mcpTools: MCPTool[] = tools.map(tool => ({
          name: tool.name,
          description: tool.description || "No description",
          inputSchema: {
            type: "object" as const,
            properties: tool.input_schema.properties || {},
            required: tool.input_schema.required || [],
          },
        }));

        return { tools: mcpTools };
      } catch (error) {
        console.error(`❌ [MCP] Failed to load tools:`, error);
        return { tools: [] };
      }
    });

    // Handle: Execute tool
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      console.error(`🔧 [MCP] Executing tool: ${name}`);
      console.error(`📥 [MCP] Input:`, JSON.stringify(args, null, 2));

      try {
        // Execute tool via gateway's ToolExecutor
        const result = await this.executor.execute(
          {
            id: `mcp_${Date.now()}`,
            name: name,
            input: args as Record<string, any>,
          },
          "mcp-session" // Session key for MCP calls
        );

        console.error(`✓ [MCP] Tool executed successfully`);

        // Return result in MCP format
        return {
          content: [
            {
              type: "text",
              text: typeof result.content === "string"
                ? result.content
                : JSON.stringify(result.content, null, 2),
            },
          ],
          isError: result.is_error,
        };
      } catch (error) {
        console.error(`❌ [MCP] Tool execution failed:`, error);

        return {
          content: [
            {
              type: "text",
              text: `Error executing tool: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
          isError: true,
        };
      }
    });
  }

  async start() {
    console.error("🚀 [MCP] Starting Gateway MCP Server...");
    console.error(`📂 [MCP] Project Root: ${projectRoot}`);
    console.error(`📂 [MCP] Workspace: ${this.config.storage?.workspace || 'default'}`);

    // Use stdio transport (Claude CLI communicates via stdin/stdout)
    const transport = new StdioServerTransport();
    await this.server.connect(transport);

    console.error("✓ [MCP] Server connected and ready");
    console.error("✓ [MCP] Claude can now access all gateway tools");
  }
}

// Main entry point
async function main() {
  console.error(`📍 [MCP] Project root: ${projectRoot}`);

  // Load config from environment or use relative paths
  const config: GatewayConfig = {
    gateway: {
      port: parseInt(process.env.PORT || "18790"),
      bind: process.env.BIND || "0.0.0.0",
      cors: {
        origins: (process.env.CORS_ORIGINS || "*").split(','),
      },
    },
    channels: {
      webchat: { enabled: true },
      whatsapp: { enabled: false },
      email: { enabled: false },
    },
    zima: {
      apiUrl: process.env.ZIMA_API_URL || "http://localhost:5000",
      timeout: parseInt(process.env.ZIMA_TIMEOUT || "30000"),
    },
    storage: {
      root: process.env.STORAGE_ROOT || path.join(projectRoot, 'storage'),
      transcripts: process.env.TRANSCRIPTS_DIR || path.join(process.env.HOME || '/tmp', '.zima/agents/main/sessions'),
      files: process.env.FILES_DIR || path.join(projectRoot, 'generated_files'),
      workspace: process.env.WORKSPACE_DIR || path.join(projectRoot, 'workspace'),
      memory: process.env.MEMORY_DB || path.join(projectRoot, 'storage/memory.db'),
    },
  };

  console.error("✓ [MCP] Configuration loaded (using relative paths)");

  const server = new GatewayMcpServer(config);
  await server.start();
}

// Start server if run directly
main().catch((error) => {
  console.error("❌ [MCP] Fatal error:", error);
  process.exit(1);
});

export { main };
EOF

echo "✓ MCP server updated with relative paths"
echo ""

# Update tool registry to use environment variable
echo "🔨 Updating Tool Registry (tool-registry.ts)..."
sed -i.bak "s|'/Volumes/DATA/QWEN/zima-file-service/bin/Debug/net9.0/.zima-tools.json'|process.env.ZIMA_TOOLS_PATH || path.join(process.cwd(), '../zima-file-service/bin/Debug/net9.0/.zima-tools.json')|" src/agent/tool-registry.ts
sed -i.bak "s|'/Volumes/DATA/QWEN/zima-file-service/bin/Debug/net8.0/.zima-tools.json'|process.env.ZIMA_TOOLS_PATH_ALT || path.join(process.cwd(), '../zima-file-service/bin/Debug/net8.0/.zima-tools.json')|" src/agent/tool-registry.ts

echo "✓ Tool registry updated"
echo ""

# Create .env.example
echo "📝 Creating .env.example..."
cat > .env.example << 'EOF'
# Gateway Configuration
PORT=18790
BIND=0.0.0.0
CORS_ORIGINS=*

# ZIMA API
ZIMA_API_URL=http://localhost:5000
ZIMA_TIMEOUT=30000

# ZIMA Tools Path (optional, auto-detected)
# ZIMA_TOOLS_PATH=/path/to/zima-file-service/bin/Debug/net9.0/.zima-tools.json

# Storage Paths (optional, uses relative paths if not set)
# STORAGE_ROOT=./storage
# WORKSPACE_DIR=./workspace
# MEMORY_DB=./storage/memory.db
# TRANSCRIPTS_DIR=~/.zima/agents/main/sessions
# FILES_DIR=./generated_files
EOF

echo "✓ .env.example created"
echo ""

# Rebuild
echo "🔨 Rebuilding project..."
npm run build

echo ""
echo "✅ Gateway is now portable!"
echo ""
echo "📋 Next steps for Linux deployment:"
echo "   1. Copy project to Linux server"
echo "   2. Run: npm install"
echo "   3. Run: npm run build"
echo "   4. Configure MCP: claude mcp add gateway node /path/to/dist/mcp/gateway-mcp-server.js"
echo ""
echo "💡 Or use the deployment script: ./deploy-to-linux.sh"
