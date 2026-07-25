/**
 * Claude CLI Runtime
 * Uses the Claude CLI instead of Anthropic API
 * Modeled after ZIMA's AgentService.cs ExecuteClaudeAsync pattern
 */

import { spawn } from 'child_process';
import { GatewayConfig, AgentContext, MessageResponse } from '../types';
import { UnifiedToolRegistry } from './tool-registry';
import { ToolExecutor } from './tool-executor';
import * as readline from 'readline';
import { getRequestLogger } from '../observability/request-logger';

export interface StreamChunk {
  type: string;
  event?: any;
  delta?: any;
  content?: any;
}

export type StreamCallback = (chunk: StreamChunk) => void;

export class ClaudeCliRuntime {
  private config: GatewayConfig;
  private registry: UnifiedToolRegistry;
  private executor: ToolExecutor;

  constructor(config: GatewayConfig) {
    this.config = config;
    this.registry = new UnifiedToolRegistry(config);
    this.executor = new ToolExecutor(config, this.registry);
  }

  /**
   * Execute agent using Claude CLI (non-streaming)
   */
  async execute(context: AgentContext): Promise<MessageResponse> {
    console.log('🤖 Starting Claude CLI runtime (non-streaming)...');

    // Load tools
    await this.registry.refresh();
    const tools = await this.registry.getTools();
    console.log(`✓ Loaded ${tools.length} tools`);

    // Build the full prompt
    const prompt = this.buildPrompt(context);

    return new Promise((resolve, reject) => {
      const proc = spawn('claude', [
        '--print',
        '--output-format', 'json',
        '--dangerously-skip-permissions'
      ], {
        cwd: this.config.storage?.workspace || process.cwd(),
        stdio: ['pipe', 'pipe', 'pipe']
      });

      let output = '';
      let errors = '';

      proc.stdout.on('data', (data) => {
        output += data.toString();
      });

      proc.stderr.on('data', (data) => {
        errors += data.toString();
      });

      proc.on('close', (code) => {
        if (code !== 0) {
          console.error('❌ Claude CLI error:', errors);
          reject(new Error(`Claude CLI exited with code ${code}: ${errors}`));
          return;
        }

        try {
          // Parse JSON output from Claude CLI
          const result = JSON.parse(output);

          resolve({
            output: result.content?.[0]?.text || output,
            usage: {
              inputTokens: result.usage?.input_tokens || 0,
              outputTokens: result.usage?.output_tokens || 0,
              cost: 0
            },
            model: 'claude',
            files: []
          });
        } catch (error: any) {
          console.error('❌ Failed to parse Claude CLI output:', error.message);
          resolve({
            output: output,
            usage: { inputTokens: 0, outputTokens: 0, cost: 0 },
            model: 'claude',
            files: []
          });
        }
      });

      proc.on('error', (error) => {
        reject(new Error(`Failed to spawn claude CLI: ${error.message}`));
      });

      // Send prompt via stdin
      proc.stdin.write(prompt);
      proc.stdin.end();
    });
  }

  /**
   * Execute agent using Claude CLI with streaming
   * Modeled after ZIMA's ExecuteClaudeStreamAsync
   */
  async executeStream(context: AgentContext, onStream?: StreamCallback): Promise<MessageResponse> {
    const reqLogger = getRequestLogger();

    console.log('🤖 Starting Claude CLI runtime (streaming)...');

    reqLogger.log('CLAUDE_CLI_START', 'ClaudeCliRuntime', {
      runId: context.runId,
      model: context.model,
      complexity: context.complexity,
      tier: context.tier
    }, context.sessionKey);

    // Load tools
    await this.registry.refresh();
    const tools = await this.registry.getTools();
    console.log(`✓ Loaded ${tools.length} tools`);

    reqLogger.log('TOOLS_LOADED', 'UnifiedToolRegistry', {
      toolsCount: tools.length,
      tools: tools.map(t => ({ name: t.name, description: t.description }))
    }, context.sessionKey);

    // Build the full prompt
    const prompt = this.buildPrompt(context);

    // Log the complete prompt being sent to Claude
    reqLogger.logPrompt(context.sessionKey, prompt, context);

    return new Promise((resolve, reject) => {
      const cliArgs = [
        '--print',
        '--output-format', 'stream-json',
        '--verbose',
        '--include-partial-messages',
        '--dangerously-skip-permissions'
      ];

      reqLogger.log('CLAUDE_CLI_SPAWN', 'ClaudeCliRuntime', {
        command: 'claude',
        args: cliArgs,
        cwd: this.config.storage?.workspace || process.cwd(),
        promptLength: prompt.length
      }, context.sessionKey);

      const proc = spawn('claude', cliArgs, {
        cwd: this.config.storage?.workspace || process.cwd(),
        stdio: ['pipe', 'pipe', 'pipe'],
        env: process.env // Use default environment (Claude CLI manages API key)
      });

      let fullResponse = '';
      let totalUsage = {
        inputTokens: 0,
        outputTokens: 0,
        cost: 0
      };

      // Create readline interface for line-by-line processing
      const rl = readline.createInterface({
        input: proc.stdout,
        crlfDelay: Infinity
      });

      rl.on('line', (line) => {
        if (!line.trim()) return;

        try {
          const event = JSON.parse(line);

          // Stream event to callback
          if (onStream) {
            onStream(event);
          }

          // Handle different event types (matching ZIMA's pattern)
          if (event.type === 'stream_event' && event.event) {
            const eventType = event.event.type;

            // Extract streaming text deltas
            if (eventType === 'content_block_delta' && event.event.delta?.text) {
              fullResponse += event.event.delta.text;
            }

            // Extract usage information
            if (eventType === 'message_stop' && event.event.message?.usage) {
              const usage = event.event.message.usage;
              totalUsage.inputTokens = usage.input_tokens || 0;
              totalUsage.outputTokens = usage.output_tokens || 0;
            }
          }

          // Handle final result format
          if (event.type === 'result' && event.content) {
            for (const block of event.content) {
              if (block.type === 'text') {
                fullResponse += block.text;
              }
            }

            if (event.usage) {
              totalUsage.inputTokens = event.usage.input_tokens || 0;
              totalUsage.outputTokens = event.usage.output_tokens || 0;
            }
          }

        } catch (error: any) {
          // Skip invalid JSON lines
          console.warn('⚠️  Failed to parse stream line:', error.message);
        }
      });

      let errors = '';
      proc.stderr.on('data', (data) => {
        errors += data.toString();
        // Some warnings/info go to stderr, log them but don't fail
        const errMsg = data.toString().trim();
        if (errMsg && !errMsg.includes('deprecated')) {
          console.error('  [claude stderr]:', errMsg);
        }
      });

      proc.on('close', (code) => {
        if (code !== 0 && code !== null) {
          console.error('❌ Claude CLI error:', errors);

          reqLogger.log('CLAUDE_CLI_ERROR', 'ClaudeCliRuntime', {
            exitCode: code,
            errors,
            runId: context.runId
          }, context.sessionKey);

          reject(new Error(`Claude CLI exited with code ${code}: ${errors}`));
          return;
        }

        // Log the complete response
        reqLogger.logResponse(context.sessionKey, fullResponse, totalUsage);

        reqLogger.log('CLAUDE_CLI_COMPLETE', 'ClaudeCliRuntime', {
          exitCode: code,
          responseLength: fullResponse.length,
          usage: totalUsage,
          runId: context.runId
        }, context.sessionKey);

        resolve({
          output: fullResponse,
          usage: totalUsage,
          model: 'claude',
          files: []
        });
      });

      proc.on('error', (error) => {
        reject(new Error(`Failed to spawn claude CLI: ${error.message}`));
      });

      // Send prompt via stdin
      proc.stdin.write(prompt);
      proc.stdin.end();
    });
  }

  /**
   * Build the prompt for Claude CLI
   * Combines system prompt, conversation history, and current message
   */
  private buildPrompt(context: AgentContext): string {
    let prompt = '';

    // Add system prompt
    if (context.systemPrompt) {
      prompt += context.systemPrompt + '\n\n';
    }

    // Add conversation history
    for (const msg of context.messages) {
      if (msg.type === 'message') {
        const role = msg.role === 'user' ? 'Human' : 'Assistant';
        prompt += `${role}: ${msg.content}\n\n`;
      }
    }

    // Add current message
    prompt += `Human: ${context.newMessage}\n\nAssistant:`;

    return prompt;
  }

  /**
   * Get tool registry
   */
  getRegistry(): UnifiedToolRegistry {
    return this.registry;
  }

  /**
   * Get tool executor
   */
  getExecutor(): ToolExecutor {
    return this.executor;
  }
}
