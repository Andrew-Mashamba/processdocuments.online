/**
 * Tool Executor
 * Routes and executes tools from the unified registry
 */

import axios from 'axios';
import * as fs from 'fs-extra';
import * as path from 'path';
import { GatewayConfig } from '../types';
import { UnifiedToolRegistry } from './tool-registry';
import { MemoryService } from '../memory/memory-service';
import { getSearchProvider } from '../tools/web-search';
import { getWebFetcher } from '../tools/web-fetch';
import { getBrowserManager } from '../tools/browser-automation';
import { getExecRunner } from '../tools/exec-runner';
import { getProcessManager } from '../tools/process-manager';
import { getMessageSender } from '../communication/message-sender';
import { getSubAgentSpawner } from './sub-agent-spawner';
import { ToolAnalyticsService } from '../analytics/tool-analytics-service';

export interface ToolCall {
  id: string;
  name: string;
  input: Record<string, any>;
}

export interface ToolResult {
  tool_use_id: string;
  content: string | any;
  is_error?: boolean;
}

export class ToolExecutor {
  private config: GatewayConfig;
  private registry: UnifiedToolRegistry;
  private analytics: ToolAnalyticsService;

  constructor(config: GatewayConfig, registry: UnifiedToolRegistry) {
    this.config = config;
    this.registry = registry;
    this.analytics = new ToolAnalyticsService(
      path.join(config.storage?.root || './storage', 'analytics/tools')
    );
  }

  /**
   * Get analytics service
   */
  getAnalytics(): ToolAnalyticsService {
    return this.analytics;
  }

  /**
   * Check if tool is a generated tool
   */
  private isGeneratedTool(toolName: string): boolean {
    return toolName.endsWith('_generated');
  }

  /**
   * Execute a generated tool
   */
  private async executeGeneratedTool(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    try {
      const generatedDir = path.join(process.cwd(), 'src/tools/generated');
      const jsPath = path.join(generatedDir, `${toolCall.name}.js`);
      const tsPath = path.join(generatedDir, `${toolCall.name}.ts`);

      console.log(`   🔍 Looking for generated tool: ${toolCall.name}`);

      // Prefer compiled JS, fall back to TS
      let modulePath: string;
      if (await fs.pathExists(jsPath)) {
        modulePath = jsPath;
        console.log(`   ✅ Found compiled version: ${path.basename(jsPath)}`);
      } else if (await fs.pathExists(tsPath)) {
        modulePath = tsPath;
        console.log(`   ✅ Found TypeScript version: ${path.basename(tsPath)}`);
      } else {
        throw new Error(`Generated tool not found: ${toolCall.name}`);
      }

      // Dynamically import the tool
      console.log(`   📦 Loading module: ${modulePath}`);

      // Clear require cache to ensure fresh load
      delete require.cache[require.resolve(modulePath)];

      const toolModule = require(modulePath);

      // Find the exported function (should match tool name)
      const toolFunction = toolModule[toolCall.name] || toolModule.default;

      if (typeof toolFunction !== 'function') {
        throw new Error(`Generated tool ${toolCall.name} does not export a function`);
      }

      console.log(`   🚀 Executing generated tool function`);

      // Execute the generated tool
      const result = await toolFunction(toolCall.input);

      console.log(`   ✅ Generated tool executed successfully`);

      return {
        tool_use_id: toolCall.id,
        content: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
      };

    } catch (error: any) {
      console.error(`   ❌ Generated tool execution error:`, error.message);
      throw error;
    }
  }

  /**
   * Execute a tool call
   */
  async execute(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    console.log(`🔧 Executing tool: ${toolCall.name}`);

    const startTime = Date.now();
    const inputSize = JSON.stringify(toolCall.input).length;
    let result: ToolResult;
    let success = false;
    let error: string | undefined;

    try {
      // Check if it's a generated tool first
      if (this.isGeneratedTool(toolCall.name)) {
        result = await this.executeGeneratedTool(toolCall, sessionKey);
      }
      // Route to appropriate executor
      else if (this.registry.isZimaTool(toolCall.name)) {
        result = await this.executeZimaTool(toolCall, sessionKey);
      } else if (this.registry.isOpenClawTool(toolCall.name)) {
        result = await this.executeOpenClawTool(toolCall, sessionKey);
      } else {
        result = {
          tool_use_id: toolCall.id,
          content: `Unknown tool: ${toolCall.name}`,
          is_error: true
        };
      }

      success = !result.is_error;
      if (result.is_error) {
        error = typeof result.content === 'string' ? result.content : JSON.stringify(result.content);
      }

    } catch (err: any) {
      console.error(`❌ Tool execution error (${toolCall.name}):`, err.message);
      success = false;
      error = err.message;
      result = {
        tool_use_id: toolCall.id,
        content: `Error executing ${toolCall.name}: ${err.message}`,
        is_error: true
      };
    } finally {
      // Track execution analytics
      const duration = Date.now() - startTime;
      const outputSize = result ? JSON.stringify(result.content).length : 0;

      this.analytics.trackExecution({
        toolName: toolCall.name,
        timestamp: new Date().toISOString(),
        duration,
        success,
        error,
        sessionKey,
        inputSize,
        outputSize
      });
    }

    return result;
  }

  /**
   * Execute ZIMA document tool via backend
   */
  private async executeZimaTool(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    try {
      // Call ZIMA Core MCP server
      const response = await axios.post(
        `${this.config.zima.apiUrl}/api/mcp/call`,
        {
          method: 'tools/call',
          params: {
            name: toolCall.name,
            arguments: toolCall.input
          }
        },
        {
          timeout: this.config.zima.timeout,
          headers: {
            'Content-Type': 'application/json',
            'X-Session-Key': sessionKey
          }
        }
      );

      // Extract result from MCP response
      const result = response.data.result || response.data;

      // Check if tool execution succeeded
      if (result.isError || result.error) {
        return {
          tool_use_id: toolCall.id,
          content: result.error || result.content || 'Tool execution failed',
          is_error: true
        };
      }

      // Format successful response
      return {
        tool_use_id: toolCall.id,
        content: this.formatToolResult(toolCall.name, result)
      };
    } catch (error: any) {
      // Handle timeout or connection errors
      if (error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT') {
        return {
          tool_use_id: toolCall.id,
          content: `Tool ${toolCall.name} timed out after ${this.config.zima.timeout}ms. The operation may still be processing.`,
          is_error: true // Mark as error for self-healing
        };
      }

      // Handle connection refused (API down)
      if (error.code === 'ECONNREFUSED' || error.errno === 'ECONNREFUSED') {
        return {
          tool_use_id: toolCall.id,
          content: `ZIMA API connection refused at ${this.config.zima.apiUrl}. API may not be running.`,
          is_error: true
        };
      }

      // Handle network errors
      if (error.code === 'ENOTFOUND' || error.code === 'ENETUNREACH') {
        return {
          tool_use_id: toolCall.id,
          content: `Network error: Cannot reach ZIMA API at ${this.config.zima.apiUrl}. Error: ${error.message}`,
          is_error: true
        };
      }

      // Other errors
      throw error;
    }
  }

  /**
   * Execute OpenClaw system tool locally
   */
  private async executeOpenClawTool(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    switch (toolCall.name) {
      // File & Execution Tools
      case 'read':
        return await this.handleReadFile(toolCall, sessionKey);
      case 'write':
        return await this.handleWriteFile(toolCall, sessionKey);
      case 'edit':
        return await this.handleEditFile(toolCall, sessionKey);
      case 'exec':
        return await this.handleExec(toolCall, sessionKey);
      case 'process':
        return await this.handleProcess(toolCall, sessionKey);

      // Web & Information Tools
      case 'web_search':
        return await this.handleWebSearch(toolCall, sessionKey);
      case 'web_fetch':
        return await this.handleWebFetch(toolCall, sessionKey);
      case 'browser':
        return await this.handleBrowser(toolCall, sessionKey);

      // Communication Tools
      case 'message':
      case 'sessions_send':
        return await this.handleSessionSend(toolCall, sessionKey);
      case 'tts':
        return await this.handleTTS(toolCall, sessionKey);

      // Session & Agent Management
      case 'sessions_list':
        return await this.handleSessionsList(toolCall, sessionKey);
      case 'sessions_spawn':
        return await this.handleSessionsSpawn(toolCall, sessionKey);
      case 'sessions_history':
        return await this.handleSessionsHistory(toolCall, sessionKey);
      case 'session_status':
        return await this.handleSessionStatus(toolCall, sessionKey);

      // Memory Tools
      case 'memory_search':
        return await this.handleMemorySearch(toolCall, sessionKey);
      case 'memory_get':
        return await this.handleMemoryGet(toolCall, sessionKey);

      // Infrastructure Tools
      case 'gateway':
        return await this.handleGateway(toolCall, sessionKey);
      case 'cron':
        return await this.handleCron(toolCall, sessionKey);

      // Intelligence Tools
      case 'image':
        return await this.handleImage(toolCall, sessionKey);

      default:
        return {
          tool_use_id: toolCall.id,
          content: `Unimplemented OpenClaw tool: ${toolCall.name}`,
          is_error: true
        };
    }
  }

  /**
   * Handle session_send tool
   */
  private async handleSessionSend(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { channel, to, message, priority, metadata } = toolCall.input;

    try {
      const messageSender = getMessageSender();

      const result = await messageSender.send(channel, to, message, {
        priority,
        metadata,
        retries: 3
      });

      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: result.success,
          messageId: result.messageId,
          channel: result.channel,
          to: result.to,
          error: result.error,
          timestamp: result.timestamp
        })
      };
    } catch (error: any) {
      console.error('❌ Message send error:', error.message);
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: false,
          channel,
          to,
          error: error.message
        }),
        is_error: false
      };
    }
  }

  /**
   * Handle memory_search tool
   */
  private async handleMemorySearch(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { query, limit = 5 } = toolCall.input;

    console.log(`🔍 [memory_search] Query: "${query}", Limit: ${limit}`);

    try {
      // Create memory service
      const memoryService = new MemoryService(this.config);

      // Perform hybrid search
      const results = await memoryService.hybridSearch(query, limit);

      // Close resources
      memoryService.close();

      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          results: results.map(r => ({
            file: r.file_path,
            content: r.content,
            score: r.score,
            lines: `${r.start_line}-${r.end_line}`
          })),
          total: results.length,
          query
        })
      };
    } catch (error: any) {
      console.error('❌ Memory search error:', error.message);
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          results: [],
          error: error.message,
          query
        }),
        is_error: false // Don't fail completely, just return empty results
      };
    }
  }

  /**
   * Handle read tool (OpenClaw file reading)
   */
  private async handleReadFile(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { file_path } = toolCall.input;

    // Build full path (session files directory)
    const filesDir = path.join(this.config.storage.files, sessionKey, 'uploads');
    const fullPath = path.join(filesDir, path.basename(file_path));

    if (!await fs.pathExists(fullPath)) {
      return {
        tool_use_id: toolCall.id,
        content: `File not found: ${file_path}`,
        is_error: true
      };
    }

    const stats = await fs.stat(fullPath);
    if (stats.size > 1024 * 1024) { // 1MB limit
      return {
        tool_use_id: toolCall.id,
        content: `File too large to read: ${file_path} (${stats.size} bytes)`,
        is_error: true
      };
    }

    const content = await fs.readFile(fullPath, 'utf-8');

    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        file_path,
        size: stats.size,
        content
      })
    };
  }

  /**
   * Handle write tool
   */
  private async handleWriteFile(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { file_path, content } = toolCall.input;

    const filesDir = path.join(this.config.storage.files, sessionKey, 'uploads');
    await fs.ensureDir(filesDir);

    const fullPath = path.join(filesDir, path.basename(file_path));
    await fs.writeFile(fullPath, content, 'utf-8');

    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        success: true,
        file_path,
        size: Buffer.byteLength(content, 'utf-8')
      })
    };
  }

  /**
   * Handle edit tool
   */
  private async handleEditFile(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { file_path, old_string, new_string } = toolCall.input;

    const filesDir = path.join(this.config.storage.files, sessionKey, 'uploads');
    const fullPath = path.join(filesDir, path.basename(file_path));

    if (!await fs.pathExists(fullPath)) {
      return {
        tool_use_id: toolCall.id,
        content: `File not found: ${file_path}`,
        is_error: true
      };
    }

    let content = await fs.readFile(fullPath, 'utf-8');

    if (!content.includes(old_string)) {
      return {
        tool_use_id: toolCall.id,
        content: `String not found in file: ${old_string}`,
        is_error: true
      };
    }

    content = content.replace(old_string, new_string);
    await fs.writeFile(fullPath, content, 'utf-8');

    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        success: true,
        file_path,
        replacements: 1
      })
    };
  }

  /**
   * Handle exec tool
   */
  private async handleExec(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { command, cwd, env, timeout } = toolCall.input;

    try {
      const execRunner = getExecRunner({
        whitelistMode: process.env.EXEC_WHITELIST_MODE === 'true',
        allowDangerous: process.env.EXEC_ALLOW_DANGEROUS === 'true',
        defaultTimeout: parseInt(process.env.EXEC_TIMEOUT || '60000')
      });

      const result = await execRunner.run(command, {
        cwd,
        env,
        timeout
      });

      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: result.success,
          stdout: result.stdout,
          stderr: result.stderr,
          exitCode: result.exitCode,
          duration: result.duration,
          command: result.command,
          error: result.error
        })
      };
    } catch (error: any) {
      console.error('❌ Exec error:', error.message);
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: false,
          stdout: '',
          stderr: error.message,
          exitCode: -1,
          duration: 0,
          command,
          error: error.message
        }),
        is_error: false
      };
    }
  }

  /**
   * Handle process tool
   */
  private async handleProcess(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { action, process_id, command, args, options } = toolCall.input;

    try {
      const processManager = getProcessManager();

      switch (action) {
        case 'spawn':
          if (!command) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'command required for spawn action'
              }),
              is_error: true
            };
          }

          const processId = await processManager.spawn(command, args || [], options || {});
          const spawnedProcess = processManager.getProcess(processId);

          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: true,
              action: 'spawn',
              process_id: processId,
              pid: spawnedProcess?.pid,
              status: spawnedProcess?.status
            })
          };

        case 'list':
          const processes = processManager.list();
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: true,
              action: 'list',
              processes
            })
          };

        case 'get':
          if (!process_id) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'process_id required for get action'
              }),
              is_error: true
            };
          }

          const process = processManager.getProcess(process_id);
          if (!process) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: `Process not found: ${process_id}`
              }),
              is_error: false
            };
          }

          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: true,
              action: 'get',
              process
            })
          };

        case 'kill':
          if (!process_id) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'process_id required for kill action'
              }),
              is_error: true
            };
          }

          const killed = await processManager.kill(process_id, options?.signal || 'SIGTERM');
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: killed,
              action: 'kill',
              process_id
            })
          };

        case 'output':
          if (!process_id) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'process_id required for output action'
              }),
              is_error: true
            };
          }

          const output = processManager.getOutput(process_id);
          if (!output) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: `Process not found: ${process_id}`
              }),
              is_error: false
            };
          }

          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: true,
              action: 'output',
              output
            })
          };

        case 'stats':
          const stats = processManager.getStats();
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: true,
              action: 'stats',
              stats
            })
          };

        default:
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: false,
              error: `Unknown process action: ${action}`
            }),
            is_error: true
          };
      }
    } catch (error: any) {
      console.error('❌ Process management error:', error.message);
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: false,
          action,
          error: error.message
        }),
        is_error: false
      };
    }
  }

  /**
   * Handle web_search tool
   */
  private async handleWebSearch(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { query, count, offset, safesearch, freshness, country } = toolCall.input;

    try {
      const searchProvider = getSearchProvider();

      if (!searchProvider.isAvailable()) {
        return {
          tool_use_id: toolCall.id,
          content: JSON.stringify({
            results: [],
            error: 'Web search not available. Set BRAVE_API_KEY environment variable.',
            query
          }),
          is_error: false
        };
      }

      const response = await searchProvider.search(query, {
        count,
        offset,
        safesearch,
        freshness,
        country
      });

      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          results: response.results,
          totalResults: response.totalResults,
          hasMore: response.hasMore,
          query: response.query
        })
      };
    } catch (error: any) {
      console.error('❌ Web search error:', error.message);
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          results: [],
          error: error.message,
          query
        }),
        is_error: false
      };
    }
  }

  /**
   * Handle web_fetch tool
   */
  private async handleWebFetch(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { url, timeout, max_retries, extract_mode } = toolCall.input;

    try {
      const fetcher = getWebFetcher();

      const result = await fetcher.fetch(url, {
        timeout,
        maxRetries: max_retries,
        extractMode: extract_mode || 'readability'
      });

      if (!result.success) {
        return {
          tool_use_id: toolCall.id,
          content: JSON.stringify({
            success: false,
            url,
            error: result.error || 'Failed to fetch content'
          }),
          is_error: false
        };
      }

      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: true,
          url: result.url,
          title: result.title,
          content: result.content,
          excerpt: result.excerpt,
          byline: result.byline,
          length: result.length,
          siteName: result.siteName
        })
      };
    } catch (error: any) {
      console.error('❌ Web fetch error:', error.message);
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: false,
          url,
          error: error.message
        }),
        is_error: false
      };
    }
  }

  /**
   * Handle browser tool
   */
  private async handleBrowser(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { action, tab_id, url, selector, script, screenshot_options } = toolCall.input;

    try {
      const browserManager = getBrowserManager();

      switch (action) {
        case 'open':
          const newTabId = await browserManager.open();
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: true,
              action: 'open',
              tab_id: newTabId
            })
          };

        case 'navigate':
          if (!tab_id) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'tab_id required for navigate action'
              }),
              is_error: true
            };
          }
          const navResult = await browserManager.navigate(tab_id, url);
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify(navResult)
          };

        case 'screenshot':
          if (!tab_id) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'tab_id required for screenshot action'
              }),
              is_error: true
            };
          }
          const screenshotResult = await browserManager.screenshot(tab_id, screenshot_options || {});
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify(screenshotResult)
          };

        case 'evaluate':
          if (!tab_id || !script) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'tab_id and script required for evaluate action'
              }),
              is_error: true
            };
          }
          const evalResult = await browserManager.evaluate(tab_id, script);
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify(evalResult)
          };

        case 'click':
          if (!tab_id || !selector) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'tab_id and selector required for click action'
              }),
              is_error: true
            };
          }
          const clicked = await browserManager.click(tab_id, selector);
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: clicked,
              action: 'click',
              selector
            })
          };

        case 'fill':
          if (!tab_id || !selector || !toolCall.input.value) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'tab_id, selector, and value required for fill action'
              }),
              is_error: true
            };
          }
          const filled = await browserManager.fill(tab_id, selector, toolCall.input.value);
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: filled,
              action: 'fill',
              selector
            })
          };

        case 'get_content':
          if (!tab_id) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'tab_id required for get_content action'
              }),
              is_error: true
            };
          }
          const content = await browserManager.getContent(tab_id);
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: true,
              content
            })
          };

        case 'close':
          if (!tab_id) {
            return {
              tool_use_id: toolCall.id,
              content: JSON.stringify({
                success: false,
                error: 'tab_id required for close action'
              }),
              is_error: true
            };
          }
          const closed = await browserManager.close(tab_id);
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: closed,
              action: 'close',
              tab_id
            })
          };

        case 'list_tabs':
          const tabs = browserManager.listTabs();
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: true,
              tabs
            })
          };

        case 'status':
          const status = browserManager.getStatus();
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: true,
              status
            })
          };

        default:
          return {
            tool_use_id: toolCall.id,
            content: JSON.stringify({
              success: false,
              error: `Unknown browser action: ${action}`
            }),
            is_error: true
          };
      }
    } catch (error: any) {
      console.error('❌ Browser automation error:', error.message);
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: false,
          action,
          error: error.message
        }),
        is_error: false
      };
    }
  }

  /**
   * Handle tts tool - TODO: Implement later
   */
  private async handleTTS(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        success: false,
        message: 'TTS will be implemented in a future phase',
        text: toolCall.input.text?.substring(0, 50) + '...'
      })
    };
  }

  /**
   * Handle sessions_list tool
   */
  private async handleSessionsList(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    // TODO: Implement proper session listing from transcript manager
    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        sessions: [
          {
            session_key: sessionKey,
            message_count: 0,
            active: true
          }
        ],
        message: 'Full session listing will be enhanced in Week 7-8'
      })
    };
  }

  /**
   * Handle sessions_spawn tool
   */
  private async handleSessionsSpawn(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { task, model, system_prompt, tools, max_tokens, temperature, timeout } = toolCall.input;

    try {
      const spawner = getSubAgentSpawner(this.config);

      const result = await spawner.spawn(task, {
        parentSessionKey: sessionKey,
        model,
        systemPrompt: system_prompt,
        tools,
        maxTokens: max_tokens,
        temperature,
        timeout
      });

      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: result.success,
          agentId: result.agentId,
          jobId: result.jobId,
          status: result.status,
          error: result.error
        })
      };
    } catch (error: any) {
      console.error('❌ Sub-agent spawn error:', error.message);
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          success: false,
          error: error.message,
          task
        }),
        is_error: false
      };
    }
  }

  /**
   * Handle sessions_history tool
   */
  private async handleSessionsHistory(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    // TODO: Load from transcript manager
    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        messages: [],
        message: 'Session history loading will be implemented in Week 7-8',
        session_key: toolCall.input.session_key
      })
    };
  }

  /**
   * Handle session_status tool
   */
  private async handleSessionStatus(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        session_key: toolCall.input.session_key || sessionKey,
        status: 'active',
        message: 'Full session status will be enhanced in Week 7-8'
      })
    };
  }

  /**
   * Handle memory_get tool
   */
  private async handleMemoryGet(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { path: filePath, offset = 0, limit = 100 } = toolCall.input;

    console.log(`📄 [memory_get] File: ${filePath}, Lines: ${offset}-${offset + limit}`);

    try {
      // Get workspace directory
      const workspaceDir = this.config.storage.workspace || path.join(process.cwd(), 'workspace');
      const fullPath = filePath.startsWith('/')
        ? filePath
        : path.join(workspaceDir, filePath);

      // Check if file exists
      if (!await fs.pathExists(fullPath)) {
        return {
          tool_use_id: toolCall.id,
          content: JSON.stringify({
            content: '',
            error: `File not found: ${filePath}`,
            path: filePath
          }),
          is_error: true
        };
      }

      // Read file content
      const content = await fs.readFile(fullPath, 'utf-8');
      const lines = content.split('\n');

      // Paginate lines
      const paginatedLines = lines.slice(offset, offset + limit);
      const paginatedContent = paginatedLines.join('\n');

      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          path: filePath,
          content: paginatedContent,
          total_lines: lines.length,
          offset,
          limit,
          returned_lines: paginatedLines.length
        })
      };
    } catch (error: any) {
      console.error('❌ Memory get error:', error.message);
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          content: '',
          error: error.message,
          path: filePath
        }),
        is_error: true
      };
    }
  }

  /**
   * Handle gateway tool
   */
  private async handleGateway(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    const { action } = toolCall.input;

    // Only allow safe actions
    if (action === 'config.get') {
      return {
        tool_use_id: toolCall.id,
        content: JSON.stringify({
          config: this.config,
          message: 'Gateway configuration'
        })
      };
    }

    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        success: false,
        message: `Gateway action '${action}' requires administrator approval`,
        action
      })
    };
  }

  /**
   * Handle cron tool - TODO: Implement scheduling
   */
  private async handleCron(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        success: false,
        message: 'Scheduled jobs will be implemented in a future phase',
        action: toolCall.input.action
      })
    };
  }

  /**
   * Handle image tool - TODO: Implement vision
   */
  private async handleImage(toolCall: ToolCall, sessionKey: string): Promise<ToolResult> {
    return {
      tool_use_id: toolCall.id,
      content: JSON.stringify({
        success: false,
        message: 'Image analysis will be implemented in a future phase',
        prompt: toolCall.input.prompt
      })
    };
  }

  /**
   * Format tool result for display
   */
  private formatToolResult(toolName: string, result: any): string {
    // If result has a specific format, use it
    if (typeof result === 'string') {
      return result;
    }

    // For document creation tools, highlight the output file
    if (result.content && Array.isArray(result.content)) {
      // MCP format: [{ type: 'text', text: '...' }]
      const textContent = result.content
        .filter((c: any) => c.type === 'text')
        .map((c: any) => c.text)
        .join('\n');

      return textContent;
    }

    // For file generation tools
    if (result.output_file || result.file_path || result.files) {
      const files = result.files || [result.output_file || result.file_path];
      return JSON.stringify({
        success: true,
        files,
        message: result.message || `Created ${files.length} file(s)`,
        ...result
      });
    }

    // Default: stringify the whole result
    return JSON.stringify(result);
  }
}
