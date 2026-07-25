import { Tool } from '../types';
import * as fs from 'fs/promises';
import * as path from 'path';
import { exec as execCallback } from 'child_process';
import { promisify } from 'util';
import { domTools } from '../tools/dom-tools';

const exec = promisify(execCallback);

export class ToolRegistry {
  private tools: Map<string, Tool> = new Map();
  private zimaFileServiceUrl?: string;

  constructor(options?: { zimaFileServiceUrl?: string }) {
    this.zimaFileServiceUrl = options?.zimaFileServiceUrl;
    this.registerBuiltInTools();
  }

  /**
   * Register built-in tools
   */
  private registerBuiltInTools(): void {
    // System File Operations
    this.register({
      name: 'read',
      description: 'Read file contents from the workspace',
      category: 'system',
      provider: 'openclaw',
      inputSchema: {
        type: 'object',
        properties: {
          file_path: {
            type: 'string',
            description: 'Absolute or relative path to the file'
          },
        },
        required: ['file_path'],
      },
      execute: async (input) => {
        const content = await fs.readFile(input.file_path, 'utf-8');
        return { success: true, content, file: input.file_path };
      },
    });

    this.register({
      name: 'write',
      description: 'Write content to a file',
      category: 'system',
      provider: 'openclaw',
      inputSchema: {
        type: 'object',
        properties: {
          file_path: { type: 'string', description: 'Path to the file' },
          content: { type: 'string', description: 'Content to write' },
        },
        required: ['file_path', 'content'],
      },
      execute: async (input) => {
        const dir = path.dirname(input.file_path);
        await fs.mkdir(dir, { recursive: true });
        await fs.writeFile(input.file_path, input.content, 'utf-8');
        return { success: true, file: input.file_path, size: input.content.length };
      },
    });

    this.register({
      name: 'edit',
      description: 'Edit a file by replacing old_string with new_string',
      category: 'system',
      provider: 'openclaw',
      inputSchema: {
        type: 'object',
        properties: {
          file_path: { type: 'string' },
          old_string: { type: 'string', description: 'Text to find and replace' },
          new_string: { type: 'string', description: 'Replacement text' },
        },
        required: ['file_path', 'old_string', 'new_string'],
      },
      execute: async (input) => {
        let content = await fs.readFile(input.file_path, 'utf-8');
        if (!content.includes(input.old_string)) {
          throw new Error('old_string not found in file');
        }
        content = content.replace(input.old_string, input.new_string);
        await fs.writeFile(input.file_path, content, 'utf-8');
        return { success: true, file: input.file_path };
      },
    });

    // Shell Execution
    this.register({
      name: 'exec',
      description: 'Execute a shell command',
      category: 'system',
      provider: 'openclaw',
      inputSchema: {
        type: 'object',
        properties: {
          command: { type: 'string', description: 'Shell command to execute' },
          cwd: { type: 'string', description: 'Working directory' },
        },
        required: ['command'],
      },
      execute: async (input) => {
        const { stdout, stderr } = await exec(input.command, {
          cwd: input.cwd || process.cwd(),
        });
        return { success: true, stdout, stderr };
      },
    });

    // Web Tools
    this.register({
      name: 'web_search',
      description: 'Search the web using a search engine',
      category: 'web',
      provider: 'openclaw',
      inputSchema: {
        type: 'object',
        properties: {
          query: { type: 'string', description: 'Search query' },
          limit: { type: 'number', description: 'Number of results' },
        },
        required: ['query'],
      },
      execute: async (input) => {
        // Placeholder: Would integrate with search API
        return {
          success: true,
          results: [],
          message: 'Web search not implemented in this version',
        };
      },
    });

    this.register({
      name: 'web_fetch',
      description: 'Fetch content from a URL',
      category: 'web',
      provider: 'openclaw',
      inputSchema: {
        type: 'object',
        properties: {
          url: { type: 'string', description: 'URL to fetch' },
        },
        required: ['url'],
      },
      execute: async (input) => {
        const fetch = (await import('node-fetch')).default;
        const response = await fetch(input.url);
        const content = await response.text();
        return { success: true, content, url: input.url, status: response.status };
      },
    });

    // Visual Editing Tools
    this.register({
      name: 'visual_analyze',
      description: 'Analyze DOM structure and screenshot to understand UI element context',
      category: 'visual',
      provider: 'custom',
      inputSchema: {
        type: 'object',
        properties: {
          element: { type: 'object', description: 'DOM element info' },
          screenshot: { type: 'string', description: 'Base64 screenshot' },
          command: { type: 'string', description: 'User command' },
        },
        required: ['element', 'command'],
      },
      execute: async (input) => {
        // Placeholder: Would use AI vision to analyze
        return {
          success: true,
          elementType: input.element.tag || 'div',
          purpose: 'container',
          suggestedChanges: [],
          confidence: 0.85,
        };
      },
    });

    this.register({
      name: 'component_mapper',
      description: 'Map UI component name to source file paths',
      category: 'visual',
      provider: 'custom',
      inputSchema: {
        type: 'object',
        properties: {
          componentName: { type: 'string', description: 'Component name' },
          framework: { type: 'string', description: 'Framework type' },
        },
        required: ['componentName', 'framework'],
      },
      execute: async (input) => {
        // Placeholder: Would use framework-specific mapper
        return {
          success: true,
          files: [],
          message: 'Component mapping not implemented',
        };
      },
    });

    this.register({
      name: 'detect_framework',
      description: 'Detect project framework from workspace',
      category: 'visual',
      provider: 'custom',
      inputSchema: {
        type: 'object',
        properties: {
          workspacePath: { type: 'string' },
        },
        required: ['workspacePath'],
      },
      execute: async (input) => {
        try {
          const packageJson = await fs.readFile(
            path.join(input.workspacePath, 'package.json'),
            'utf-8'
          );
          const pkg = JSON.parse(packageJson);

          let framework: string = 'unknown';
          if (pkg.dependencies?.['@livewire/livewire']) framework = 'laravel-livewire';
          else if (pkg.dependencies?.react) framework = 'react';
          else if (pkg.dependencies?.vue) framework = 'vue';
          else if (pkg.dependencies?.['@angular/core']) framework = 'angular';

          return { success: true, framework };
        } catch {
          return { success: false, framework: 'unknown' };
        }
      },
    });

    // Register DOM manipulation tools
    for (const domTool of domTools) {
      this.register({
        name: domTool.name,
        description: domTool.description,
        category: 'visual',
        provider: 'custom',
        inputSchema: domTool.input_schema,
        // DOM tools are executed in browser, not server-side
        execute: async (input) => {
          return {
            success: true,
            message: 'DOM tool executed in browser',
            tool: domTool.name,
            input,
          };
        },
      });
    }

    // ZIMA Document Tools (placeholders - would connect to ZIMA file service)
    const zimaTools = [
      // Excel
      'create_excel', 'read_excel', 'merge_workbooks', 'split_workbook',
      'excel_to_csv', 'excel_to_json', 'csv_to_excel', 'json_to_excel',
      // PDF
      'create_pdf', 'merge_pdf', 'split_pdf', 'compress_pdf', 'protect_pdf',
      'pdf_to_text', 'pdf_to_word', 'ocr_pdf',
      // Word
      'create_word', 'merge_word', 'word_to_pdf', 'mail_merge',
      // PowerPoint
      'create_powerpoint', 'merge_ppt', 'ppt_to_pdf',
      // Images
      'resize_image', 'convert_image_format', 'compress_image',
      // JSON
      'format_json', 'validate_json', 'merge_json', 'query_json',
    ];

    for (const toolName of zimaTools) {
      this.register({
        name: toolName,
        description: `ZIMA document tool: ${toolName}`,
        category: 'document',
        provider: 'zima',
        inputSchema: {
          type: 'object',
          properties: {},
        },
      });
    }
  }

  /**
   * Register a tool
   */
  register(tool: Tool): void {
    this.tools.set(tool.name, tool);
  }

  /**
   * Get tool by name
   */
  get(name: string): Tool | undefined {
    return this.tools.get(name);
  }

  /**
   * Get all tools
   */
  getAll(): Tool[] {
    return Array.from(this.tools.values());
  }

  /**
   * Get tools by category
   */
  getByCategory(category: Tool['category']): Tool[] {
    return this.getAll().filter((t) => t.category === category);
  }

  /**
   * Get tool definitions for Claude (MCP format)
   */
  getToolDefinitions(): any[] {
    return this.getAll().map((tool) => ({
      name: tool.name,
      description: tool.description,
      input_schema: tool.inputSchema,
    }));
  }

  /**
   * Execute a tool
   */
  async execute(name: string, input: any): Promise<any> {
    const tool = this.get(name);
    if (!tool) {
      throw new Error(`Tool not found: ${name}`);
    }

    if (tool.provider === 'zima' && this.zimaFileServiceUrl) {
      // Execute via ZIMA file service
      const fetch = (await import('node-fetch')).default;
      const response = await fetch(`${this.zimaFileServiceUrl}/api/tools/${name}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input),
      });

      if (!response.ok) {
        throw new Error(`ZIMA tool execution failed: ${response.statusText}`);
      }

      return await response.json();
    } else if (tool.execute) {
      // Execute locally
      return await tool.execute(input);
    } else {
      throw new Error(`Tool execution not available: ${name}`);
    }
  }

  /**
   * Get tool count
   */
  getCount(): number {
    return this.tools.size;
  }

  /**
   * Check if tool exists
   */
  has(name: string): boolean {
    return this.tools.has(name);
  }
}
