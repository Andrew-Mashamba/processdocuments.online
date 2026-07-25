/**
 * Unified Tool Registry
 * Combines ZIMA's 196+ document tools with OpenClaw system tools
 */

import * as fs from 'fs-extra';
import * as path from 'path';
import axios from 'axios';
import { GatewayConfig } from '../types';

export interface Tool {
  name: string;
  description: string;
  usage?: string;
  category?: string;
  input_schema: {
    type: string;
    properties: Record<string, any>;
    required?: string[];
  };
}

export interface ToolRegistry {
  version: string;
  updatedAt: string;
  tools: Tool[];
}

export class UnifiedToolRegistry {
  private config: GatewayConfig;
  private registry: ToolRegistry | null = null;
  private lastUpdate: number = 0;
  private updateInterval: number = 300000; // 5 minutes

  constructor(config: GatewayConfig) {
    this.config = config;
  }

  /**
   * Get all tools (ZIMA + OpenClaw + Generated)
   */
  async getTools(): Promise<Tool[]> {
    // Refresh if cache is old
    if (!this.registry || Date.now() - this.lastUpdate > this.updateInterval) {
      await this.refresh();
    }

    // Include generated tools
    const generatedTools = await this.getGeneratedTools();
    const allTools = [...this.registry!.tools, ...generatedTools];

    // Remove duplicates (prefer generated tools if names conflict)
    const toolMap = new Map<string, Tool>();
    this.registry!.tools.forEach(tool => toolMap.set(tool.name, tool));
    generatedTools.forEach(tool => toolMap.set(tool.name, tool)); // Overwrite with generated

    return Array.from(toolMap.values());
  }

  /**
   * Refresh tool registry from ZIMA Core
   */
  async refresh(): Promise<void> {
    console.log('🔧 Refreshing tool registry...');

    try {
      // Try to fetch from ZIMA Core API first
      const response = await axios.get(`${this.config.zima.apiUrl}/api/tools/list`, {
        timeout: 5000
      });

      this.registry = {
        version: response.data.version || '2.0',
        updatedAt: response.data.updatedAt || new Date().toISOString(),
        tools: this.convertZimaTools(response.data.mcpTools || response.data.tools || [])
      };
    } catch (error) {
      // Fallback: load from file if API fails
      console.log('⚠️  API unavailable, loading from file...');
      await this.loadFromFile();
    }

    // Add OpenClaw system tools
    this.registry!.tools.push(...this.getOpenClawTools());

    this.lastUpdate = Date.now();
    console.log(`✓ Loaded ${this.registry!.tools.length} tools (${this.registry!.version})`);
  }

  /**
   * Load generated tools from disk
   */
  private async getGeneratedTools(): Promise<Tool[]> {
    const generatedDir = path.join(process.cwd(), 'src/tools/generated');

    if (!await fs.pathExists(generatedDir)) {
      return [];
    }

    const files = await fs.readdir(generatedDir);
    const jsonFiles = files.filter(f => f.endsWith('.json'));
    const tools: Tool[] = [];

    for (const file of jsonFiles) {
      try {
        const filePath = path.join(generatedDir, file);
        const toolDef = await fs.readJSON(filePath);

        // Ensure it has required fields
        if (toolDef.name && toolDef.description && toolDef.input_schema) {
          tools.push(toolDef);
        }
      } catch (err) {
        console.error(`   ⚠️  Failed to load generated tool ${file}:`, err instanceof Error ? err.message : String(err));
      }
    }

    if (tools.length > 0) {
      console.log(`   ✓ Loaded ${tools.length} generated tools`);
    }

    return tools;
  }

  /**
   * Load tools from .zima-tools.json file
   */
  private async loadFromFile(): Promise<void> {
    // Common paths to check
    const paths = [
      '/Volumes/DATA/QWEN/zima-file-service/bin/Debug/net9.0/.zima-tools.json',
      '/Volumes/DATA/QWEN/zima-file-service/bin/Debug/net8.0/.zima-tools.json',
      path.join(process.cwd(), '.zima-tools.json'),
    ];

    for (const toolsPath of paths) {
      if (await fs.pathExists(toolsPath)) {
        console.log(`  Loading from: ${toolsPath}`);
        const data = await fs.readJSON(toolsPath);

        this.registry = {
          version: data.version || '2.0',
          updatedAt: data.updatedAt || new Date().toISOString(),
          tools: this.convertZimaTools(data.mcpTools || data.tools || [])
        };

        return;
      }
    }

    // If no file found, use empty registry
    console.warn('⚠️  No tool registry file found, using minimal set');
    this.registry = {
      version: '2.0',
      updatedAt: new Date().toISOString(),
      tools: []
    };
  }

  /**
   * Convert ZIMA tool format to Anthropic format
   */
  private convertZimaTools(zimaTools: any[]): Tool[] {
    return zimaTools.map(tool => ({
      name: tool.name,
      description: tool.description,
      usage: tool.usage,
      category: tool.category,
      input_schema: tool.inputSchema || {
        type: 'object',
        properties: {},
        required: []
      }
    }));
  }

  /**
   * Get OpenClaw system tools
   */
  private getOpenClawTools(): Tool[] {
    return [
      // File & Execution Tools
      {
        name: 'read',
        description: 'Read file contents from the workspace or session storage',
        category: 'file',
        input_schema: {
          type: 'object',
          properties: {
            file_path: { type: 'string', description: 'Path to file' }
          },
          required: ['file_path']
        }
      },
      {
        name: 'write',
        description: 'Write content to a file in the workspace',
        category: 'file',
        input_schema: {
          type: 'object',
          properties: {
            file_path: { type: 'string', description: 'Path to file' },
            content: { type: 'string', description: 'Content to write' }
          },
          required: ['file_path', 'content']
        }
      },
      {
        name: 'edit',
        description: 'Edit existing files using search/replace operations',
        category: 'file',
        input_schema: {
          type: 'object',
          properties: {
            file_path: { type: 'string', description: 'Path to file' },
            old_string: { type: 'string', description: 'String to replace' },
            new_string: { type: 'string', description: 'Replacement string' }
          },
          required: ['file_path', 'old_string', 'new_string']
        }
      },
      {
        name: 'exec',
        description: 'Execute bash commands with environment management',
        category: 'execution',
        input_schema: {
          type: 'object',
          properties: {
            command: { type: 'string', description: 'Bash command to execute' },
            cwd: { type: 'string', description: 'Working directory' },
            env: { type: 'object', description: 'Environment variables' }
          },
          required: ['command']
        }
      },
      {
        name: 'process',
        description: 'Manage running processes (list, poll, log, kill)',
        category: 'execution',
        input_schema: {
          type: 'object',
          properties: {
            action: {
              type: 'string',
              enum: ['list', 'poll', 'log', 'write', 'kill'],
              description: 'Process action'
            },
            process_id: { type: 'string', description: 'Process ID for actions' },
            input: { type: 'string', description: 'Input to write to process stdin' }
          },
          required: ['action']
        }
      },

      // Web & Information Tools
      {
        name: 'web_search',
        description: 'Search the web using Brave or Perplexity search providers',
        category: 'web',
        input_schema: {
          type: 'object',
          properties: {
            query: { type: 'string', description: 'Search query' },
            max_results: { type: 'number', description: 'Maximum results', default: 10 }
          },
          required: ['query']
        }
      },
      {
        name: 'web_fetch',
        description: 'Fetch and parse web content with readability support',
        category: 'web',
        input_schema: {
          type: 'object',
          properties: {
            url: { type: 'string', description: 'URL to fetch' },
            readability: { type: 'boolean', description: 'Parse with readability', default: true }
          },
          required: ['url']
        }
      },
      {
        name: 'browser',
        description: 'Control browser automation: open/close tabs, navigate, screenshots',
        category: 'web',
        input_schema: {
          type: 'object',
          properties: {
            action: {
              type: 'string',
              enum: ['open', 'close', 'navigate', 'screenshot', 'pdf'],
              description: 'Browser action'
            },
            url: { type: 'string', description: 'URL for navigation' },
            tab_id: { type: 'string', description: 'Tab identifier' }
          },
          required: ['action']
        }
      },

      // Communication Tools
      {
        name: 'message',
        description: 'Send messages across channels (Telegram, Discord, Slack, WhatsApp, etc) with media and buttons',
        category: 'communication',
        input_schema: {
          type: 'object',
          properties: {
            channel: { type: 'string', description: 'Channel type (telegram, discord, slack, whatsapp, email)' },
            to: { type: 'string', description: 'Recipient identifier' },
            text: { type: 'string', description: 'Message text' },
            media: { type: 'array', description: 'Media attachments', items: { type: 'object' } },
            buttons: { type: 'array', description: 'Interactive buttons', items: { type: 'object' } }
          },
          required: ['channel', 'to', 'text']
        }
      },
      {
        name: 'tts',
        description: 'Convert text to speech and return audio file path',
        category: 'communication',
        input_schema: {
          type: 'object',
          properties: {
            text: { type: 'string', description: 'Text to convert to speech' },
            voice: { type: 'string', description: 'Voice identifier', default: 'default' }
          },
          required: ['text']
        }
      },

      // Session & Agent Management
      {
        name: 'sessions_list',
        description: 'List sessions with optional filters by kind, activity, message count',
        category: 'session',
        input_schema: {
          type: 'object',
          properties: {
            kind: { type: 'string', description: 'Filter by session kind' },
            active_only: { type: 'boolean', description: 'Only active sessions', default: false }
          }
        }
      },
      {
        name: 'sessions_send',
        description: 'Send messages into other sessions (agent-to-agent communication)',
        category: 'session',
        input_schema: {
          type: 'object',
          properties: {
            session_key: { type: 'string', description: 'Target session key' },
            message: { type: 'string', description: 'Message to send' }
          },
          required: ['session_key', 'message']
        }
      },
      {
        name: 'sessions_spawn',
        description: 'Spawn background sub-agent runs in isolated sessions',
        category: 'session',
        input_schema: {
          type: 'object',
          properties: {
            agent_id: { type: 'string', description: 'Agent to spawn' },
            task: { type: 'string', description: 'Task description for sub-agent' },
            model: { type: 'string', description: 'Model to use' }
          },
          required: ['agent_id', 'task']
        }
      },
      {
        name: 'sessions_history',
        description: 'Fetch message history for a specific session',
        category: 'session',
        input_schema: {
          type: 'object',
          properties: {
            session_key: { type: 'string', description: 'Session key' },
            limit: { type: 'number', description: 'Max messages to return', default: 50 }
          },
          required: ['session_key']
        }
      },
      {
        name: 'session_status',
        description: 'Get detailed status of a session including model, auth, provider usage',
        category: 'session',
        input_schema: {
          type: 'object',
          properties: {
            session_key: { type: 'string', description: 'Session key' }
          },
          required: ['session_key']
        }
      },

      // Memory Tools
      {
        name: 'memory_search',
        description: 'Semantically search MEMORY.md and memory/*.md files for prior work, decisions, preferences',
        category: 'memory',
        input_schema: {
          type: 'object',
          properties: {
            query: { type: 'string', description: 'Search query' },
            limit: { type: 'number', description: 'Max results', default: 5 },
            session_key: { type: 'string', description: 'Optional: limit to session' }
          },
          required: ['query']
        }
      },
      {
        name: 'memory_get',
        description: 'Get specific memory content from paths with pagination',
        category: 'memory',
        input_schema: {
          type: 'object',
          properties: {
            path: { type: 'string', description: 'Memory file path' },
            offset: { type: 'number', description: 'Pagination offset', default: 0 },
            limit: { type: 'number', description: 'Lines to return', default: 100 }
          },
          required: ['path']
        }
      },

      // Infrastructure Tools
      {
        name: 'gateway',
        description: 'Manage gateway operations: restart, config.get, config.apply, update.run',
        category: 'infrastructure',
        input_schema: {
          type: 'object',
          properties: {
            action: {
              type: 'string',
              enum: ['restart', 'config.get', 'config.schema', 'config.apply', 'config.patch'],
              description: 'Gateway action'
            },
            config: { type: 'object', description: 'Config for apply/patch actions' }
          },
          required: ['action']
        }
      },
      {
        name: 'cron',
        description: 'Manage scheduled jobs: status, list, add, update, remove, run',
        category: 'infrastructure',
        input_schema: {
          type: 'object',
          properties: {
            action: {
              type: 'string',
              enum: ['status', 'list', 'add', 'update', 'remove', 'run'],
              description: 'Cron action'
            },
            job_id: { type: 'string', description: 'Job identifier' },
            schedule: { type: 'string', description: 'Cron schedule expression' },
            task: { type: 'string', description: 'Task to execute' }
          },
          required: ['action']
        }
      },

      // Intelligence Tools
      {
        name: 'image',
        description: 'Analyze images using vision models with automatic fallback',
        category: 'intelligence',
        input_schema: {
          type: 'object',
          properties: {
            image: { type: 'string', description: 'Image file path, URL, or base64' },
            prompt: { type: 'string', description: 'Analysis prompt' },
            model: { type: 'string', description: 'Vision model to use' }
          },
          required: ['image', 'prompt']
        }
      }
    ];
  }

  /**
   * Get tool by name
   */
  async getTool(name: string): Promise<Tool | null> {
    const tools = await this.getTools();
    return tools.find(t => t.name === name) || null;
  }

  /**
   * Get tools by category
   */
  async getToolsByCategory(category: string): Promise<Tool[]> {
    const tools = await this.getTools();
    return tools.filter(t => t.category === category);
  }

  /**
   * Check if tool is a ZIMA document tool
   */
  isZimaTool(toolName: string): boolean {
    const zimaCategories = ['document', 'conversion', 'data'];
    const tool = this.registry?.tools.find(t => t.name === toolName);
    return tool ? zimaCategories.includes(tool.category || '') : false;
  }

  /**
   * Check if tool is an OpenClaw system tool
   */
  isOpenClawTool(toolName: string): boolean {
    const openClawTools = [
      // File & Execution
      'read', 'write', 'edit', 'exec', 'process',
      // Web & Information
      'web_search', 'web_fetch', 'browser',
      // Communication
      'message', 'tts',
      // Session & Agent Management
      'sessions_list', 'sessions_send', 'sessions_spawn', 'sessions_history', 'session_status',
      // Memory
      'memory_search', 'memory_get',
      // Infrastructure
      'gateway', 'cron',
      // Intelligence
      'image'
    ];
    return openClawTools.includes(toolName);
  }

  /**
   * Get registry stats
   */
  getStats(): {
    totalTools: number;
    zimaTools: number;
    openClawTools: number;
    version: string;
    lastUpdate: number;
  } {
    const tools = this.registry?.tools || [];
    const zimaTools = tools.filter(t => !this.isOpenClawTool(t.name));
    const openClawTools = tools.filter(t => this.isOpenClawTool(t.name));

    return {
      totalTools: tools.length,
      zimaTools: zimaTools.length,
      openClawTools: openClawTools.length,
      version: this.registry?.version || '0.0',
      lastUpdate: this.lastUpdate
    };
  }
}
