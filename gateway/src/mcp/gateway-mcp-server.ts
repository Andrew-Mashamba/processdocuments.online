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
import * as fs from 'fs';
import * as path from 'path';

/**
 * Gateway MCP Server
 *
 * Exposes all gateway tools (memory_search, ZIMA tools, OpenClaw tools)
 * to Claude CLI via the Model Context Protocol.
 *
 * Usage:
 *   1. Build: npm run mcp:build
 *   2. Add to Claude: claude mcp add gateway node /path/to/gateway-mcp-server.js
 *   3. Use: claude "Use memory_search to find Alice"
 */
export class GatewayMcpServer {
  private server: Server;
  private registry: UnifiedToolRegistry;
  private executor: ToolExecutor;
  private config: GatewayConfig;
  private generatedToolsDir: string;
  private generatedTools: Map<string, any>;
  private fileWatcher?: fs.FSWatcher;

  constructor(config: GatewayConfig) {
    this.config = config;
    this.generatedToolsDir = path.join(process.cwd(), 'src/tools/generated');
    this.generatedTools = new Map();

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
    this.loadGeneratedTools();
    this.setupFileWatcher();
  }

  private setupHandlers() {
    // Handle: List available tools
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      console.error("📋 [MCP] Claude requesting tool list...");

      try {
        // Load all gateway tools (including generated ones)
        await this.registry.refresh();
        const tools = await this.getAllTools();

        const generatedCount = this.generatedTools.size;
        const totalCount = tools.length;

        console.error(`✓ [MCP] Exposing ${totalCount} tools (${generatedCount} generated)`);

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

  /**
   * Load generated tools from disk
   */
  private loadGeneratedTools(): void {
    if (!fs.existsSync(this.generatedToolsDir)) {
      fs.mkdirSync(this.generatedToolsDir, { recursive: true });
      console.error(`✓ [MCP] Created generated tools directory: ${this.generatedToolsDir}`);
      return;
    }

    const files = fs.readdirSync(this.generatedToolsDir);
    const jsonFiles = files.filter(f => f.endsWith('.json'));

    console.error(`🔍 [MCP] Loading ${jsonFiles.length} generated tools...`);

    jsonFiles.forEach(file => {
      try {
        const filePath = path.join(this.generatedToolsDir, file);
        const definition = JSON.parse(fs.readFileSync(filePath, 'utf-8'));

        this.generatedTools.set(definition.name, definition);
        console.error(`   ✓ Loaded: ${definition.name}`);
      } catch (err) {
        console.error(`   ✗ Failed to load ${file}:`, err instanceof Error ? err.message : String(err));
      }
    });

    if (jsonFiles.length > 0) {
      console.error(`✓ [MCP] Loaded ${this.generatedTools.size} generated tools`);
    }
  }

  /**
   * Set up file watcher for hot-reload
   */
  private setupFileWatcher(): void {
    if (!fs.existsSync(this.generatedToolsDir)) {
      return;
    }

    try {
      this.fileWatcher = fs.watch(this.generatedToolsDir, (eventType, filename) => {
        if (!filename || !filename.endsWith('.json')) {
          return;
        }

        console.error(`\n🔄 [MCP] Detected file change: ${filename} (${eventType})`);

        // Reload all generated tools
        this.loadGeneratedTools();

        // Refresh the tool registry
        this.registry.refresh().then(() => {
          console.error(`✓ [MCP] Tool registry refreshed with new tools`);
        }).catch(err => {
          console.error(`❌ [MCP] Failed to refresh registry:`, err);
        });
      });

      console.error(`✓ [MCP] File watcher enabled for: ${this.generatedToolsDir}`);
    } catch (err) {
      console.error(`⚠️  [MCP] Could not set up file watcher:`, err instanceof Error ? err.message : String(err));
    }
  }

  /**
   * Dynamically register a new tool
   */
  async registerNewTool(toolDefinition: any): Promise<void> {
    console.error(`\n🔨 [MCP] Registering new tool: ${toolDefinition.name}`);

    // Save tool definition
    const defPath = path.join(this.generatedToolsDir, `${toolDefinition.name}.json`);
    fs.writeFileSync(defPath, JSON.stringify(toolDefinition, null, 2));

    // Add to in-memory map
    this.generatedTools.set(toolDefinition.name, toolDefinition);

    // Refresh registry
    await this.registry.refresh();

    console.error(`✓ [MCP] Tool registered: ${toolDefinition.name}`);
  }

  /**
   * Get all available tools (including generated ones)
   */
  async getAllTools(): Promise<any[]> {
    const registryTools = await this.registry.getTools();
    const generatedToolsList = Array.from(this.generatedTools.values());

    // Merge, preferring generated tools if there are conflicts
    const toolMap = new Map<string, any>();

    registryTools.forEach(tool => toolMap.set(tool.name, tool));
    generatedToolsList.forEach(tool => toolMap.set(tool.name, tool));

    return Array.from(toolMap.values());
  }

  /**
   * Shutdown the server gracefully
   */
  async shutdown(): Promise<void> {
    console.error("\n🛑 [MCP] Shutting down Gateway MCP Server...");

    if (this.fileWatcher) {
      this.fileWatcher.close();
      console.error("✓ [MCP] File watcher closed");
    }

    console.error("✓ [MCP] Server shutdown complete");
  }

  async start() {
    console.error("🚀 [MCP] Starting Gateway MCP Server...");
    console.error(`📂 [MCP] Workspace: ${this.config.storage?.workspace || 'default'}`);
    console.error(`🔧 [MCP] Generated tools directory: ${this.generatedToolsDir}`);

    // Use stdio transport (Claude CLI communicates via stdin/stdout)
    const transport = new StdioServerTransport();
    await this.server.connect(transport);

    console.error("✓ [MCP] Server connected and ready");
    console.error("✓ [MCP] Claude can now access all gateway tools");
    console.error(`✓ [MCP] Hot-reload enabled: New tools will be loaded automatically`);
  }
}

// Main entry point
async function main() {
  // Load config from environment or defaults
  const config: GatewayConfig = {
    gateway: {
      port: parseInt(process.env.PORT || "18790"),
      bind: "0.0.0.0",
      cors: {
        origins: ["*"],
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
      root: process.env.STORAGE_ROOT || "/Volumes/DATA/QWEN/gateway/storage",
      transcripts: process.env.TRANSCRIPTS_DIR || (process.env.HOME + "/.zima/agents/main/sessions"),
      files: process.env.FILES_DIR || "/Volumes/DATA/QWEN/gateway/generated_files",
      workspace: process.env.WORKSPACE_DIR || "/Volumes/DATA/QWEN/gateway/workspace",
      memory: process.env.MEMORY_DB || "/Volumes/DATA/QWEN/gateway/storage/memory.db",
    },
  };

  const server = new GatewayMcpServer(config);
  await server.start();
}

// Start server if run directly
main().catch((error) => {
  console.error("❌ [MCP] Fatal error:", error);
  process.exit(1);
});

export { main };
