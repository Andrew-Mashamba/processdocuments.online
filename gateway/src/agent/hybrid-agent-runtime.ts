/**
 * Hybrid Agent Runtime
 * Combines Anthropic SDK with ZIMA tools and OpenClaw session management
 */

import Anthropic from '@anthropic-ai/sdk';
import { GatewayConfig, AgentContext, MessageResponse } from '../types';
import { UnifiedToolRegistry } from './tool-registry';
import { ToolExecutor } from './tool-executor';

export interface StreamChunk {
  type: 'content_block_start' | 'content_block_delta' | 'content_block_stop' | 'message_stop' | 'message_delta';
  delta?: {
    type: 'text_delta' | 'input_json_delta';
    text?: string;
    partial_json?: string;
  };
  content_block?: any;
  index?: number;
}

export type StreamCallback = (chunk: StreamChunk) => void;

export class HybridAgentRuntime {
  private anthropic: Anthropic;
  private config: GatewayConfig;
  private registry: UnifiedToolRegistry;
  private executor: ToolExecutor;

  constructor(config: GatewayConfig) {
    this.config = config;

    // Initialize Anthropic client
    const apiKey = process.env.ANTHROPIC_API_KEY;
    if (!apiKey) {
      throw new Error('ANTHROPIC_API_KEY environment variable not set');
    }

    this.anthropic = new Anthropic({
      apiKey
    });

    // Initialize tool system
    this.registry = new UnifiedToolRegistry(config);
    this.executor = new ToolExecutor(config, this.registry);
  }

  /**
   * Execute agent with tool calling support
   */
  async execute(context: AgentContext, onStream?: StreamCallback): Promise<MessageResponse> {
    console.log('🤖 Starting hybrid agent runtime...');

    // Load tools
    await this.registry.refresh();
    const tools = await this.registry.getTools();

    console.log(`✓ Loaded ${tools.length} tools`);

    // Build messages array
    const messages: any[] = [
      ...context.messages
        .filter(m => m.type === 'message')
        .map(m => ({
          role: m.role as 'user' | 'assistant',
          content: m.content
        })),
      {
        role: 'user' as const,
        content: context.newMessage
      }
    ];

    // Execute agent loop with tool calling
    let fullResponse = '';
    let totalUsage = {
      inputTokens: 0,
      outputTokens: 0,
      cacheCreationTokens: 0,
      cacheReadTokens: 0,
      cost: 0
    };
    const generatedFiles: any[] = [];
    let conversationMessages = messages;
    let maxIterations = 10; // Prevent infinite loops
    let iteration = 0;

    while (iteration < maxIterations) {
      iteration++;
      console.log(`\n=== Agent Loop Iteration ${iteration} ===`);

      // Call Claude with tools
      const response = await this.callClaudeWithTools(
        conversationMessages,
        context.systemPrompt,
        context.model,
        tools,
        onStream
      );

      // Update usage
      if (response.usage) {
        totalUsage.inputTokens += response.usage.input_tokens || 0;
        totalUsage.outputTokens += response.usage.output_tokens || 0;
        totalUsage.cacheCreationTokens += (response.usage as any).cache_creation_input_tokens || 0;
        totalUsage.cacheReadTokens += (response.usage as any).cache_read_input_tokens || 0;
      }

      // Process response blocks
      const textBlocks: string[] = [];
      const toolUses: any[] = [];

      for (const block of response.content) {
        if (block.type === 'text') {
          textBlocks.push(block.text);
        } else if (block.type === 'tool_use') {
          toolUses.push(block);
        }
      }

      // Collect text response
      if (textBlocks.length > 0) {
        fullResponse += textBlocks.join('\n');
      }

      // If no tool calls, we're done
      if (toolUses.length === 0) {
        console.log('✓ Agent loop complete (no more tool calls)');
        break;
      }

      // Execute all tool calls
      console.log(`🔧 Executing ${toolUses.length} tool call(s)...`);
      const toolResults = await Promise.all(
        toolUses.map(toolUse =>
          this.executor.execute(
            {
              id: toolUse.id,
              name: toolUse.name,
              input: toolUse.input
            },
            context.sessionKey
          )
        )
      );

      // Extract generated files from tool results
      for (const result of toolResults) {
        if (!result.is_error && typeof result.content === 'string') {
          try {
            const parsed = JSON.parse(result.content);
            if (parsed.files) {
              generatedFiles.push(...parsed.files);
            } else if (parsed.file_path || parsed.output_file) {
              generatedFiles.push(parsed.file_path || parsed.output_file);
            }
          } catch (e) {
            // Not JSON, ignore
          }
        }
      }

      // Add assistant's tool use and tool results to conversation
      conversationMessages = [
        ...conversationMessages,
        {
          role: 'assistant' as const,
          content: response.content
        },
        {
          role: 'user' as const,
          content: toolResults.map(result => ({
            type: 'tool_result' as const,
            tool_use_id: result.tool_use_id,
            content: result.content,
            is_error: result.is_error
          }))
        }
      ];

      // If we got a stop reason other than tool_use, we're done
      if (response.stop_reason !== 'tool_use' && response.stop_reason !== null) {
        console.log(`✓ Agent loop complete (${response.stop_reason})`);
        break;
      }
    }

    // Calculate cost
    totalUsage.cost = this.calculateCost(context.model, totalUsage);

    console.log('✓ Agent execution complete');
    console.log(`  Input tokens: ${totalUsage.inputTokens}`);
    console.log(`  Output tokens: ${totalUsage.outputTokens}`);
    console.log(`  Cost: $${totalUsage.cost.toFixed(4)}`);
    console.log(`  Files generated: ${generatedFiles.length}`);

    return {
      output: fullResponse || 'Task completed successfully.',
      usage: totalUsage,
      model: context.model,
      files: generatedFiles,
      complexity: context.complexity,
      tier: context.tier
    };
  }

  /**
   * Call Claude API with tools
   */
  private async callClaudeWithTools(
    messages: any[],
    systemPrompt: string,
    model: string,
    tools: any[],
    onStream?: StreamCallback
  ): Promise<any> {
    const params: any = {
      model,
      max_tokens: 4096,
      system: systemPrompt,
      messages,
      tools: tools.map(t => ({
        name: t.name,
        description: t.description,
        input_schema: t.input_schema
      }))
    };

    // Use streaming if callback provided
    if (onStream) {
      return await this.streamWithTools(params, onStream);
    } else {
      return await this.anthropic.messages.create(params);
    }
  }

  /**
   * Stream Claude response with tools
   */
  private async streamWithTools(params: any, onStream: StreamCallback): Promise<any> {
    const stream = await this.anthropic.messages.stream(params);

    // Collect response as we stream
    let response: any = null;

    stream.on('message', (message) => {
      response = message;
    });

    stream.on('contentBlock', (block) => {
      onStream({
        type: 'content_block_start',
        content_block: block,
        index: 0
      });
    });

    stream.on('text', (text, snapshot) => {
      onStream({
        type: 'content_block_delta',
        delta: {
          type: 'text_delta',
          text
        },
        index: 0
      });
    });

    stream.on('end', () => {
      onStream({
        type: 'message_stop'
      });
    });

    // Wait for completion
    await stream.finalMessage();

    return response || stream.finalMessage();
  }

  /**
   * Calculate cost based on model and usage
   */
  private calculateCost(model: string, usage: any): number {
    const costs: Record<string, { input: number; output: number; cacheWrite: number; cacheRead: number }> = {
      'claude-3-5-haiku-20241022': {
        input: 0.25,
        output: 1.25,
        cacheWrite: 0.30,
        cacheRead: 0.03
      },
      'claude-sonnet-4-20250514': {
        input: 3.00,
        output: 15.00,
        cacheWrite: 3.75,
        cacheRead: 0.30
      },
      'claude-opus-4-20250514': {
        input: 15.00,
        output: 75.00,
        cacheWrite: 18.75,
        cacheRead: 1.50
      }
    };

    const modelCost = costs[model] || costs['claude-sonnet-4-20250514'];

    const inputCost = (usage.inputTokens / 1_000_000) * modelCost.input;
    const outputCost = (usage.outputTokens / 1_000_000) * modelCost.output;
    const cacheWriteCost = (usage.cacheCreationTokens / 1_000_000) * modelCost.cacheWrite;
    const cacheReadCost = (usage.cacheReadTokens / 1_000_000) * modelCost.cacheRead;

    return inputCost + outputCost + cacheWriteCost + cacheReadCost;
  }

  /**
   * Get registry stats
   */
  getRegistryStats() {
    return this.registry.getStats();
  }
}
