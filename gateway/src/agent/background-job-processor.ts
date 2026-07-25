/**
 * Background Job Processor - Process sub-agent jobs
 * Week 9-10: Communication - Phase C
 */

import { Job } from 'bull';
import Anthropic from '@anthropic-ai/sdk';
import { GatewayConfig } from '../types';
import { getJobQueueManager } from '../queue/job-queue';
import { UnifiedToolRegistry } from './tool-registry';
import { ToolExecutor } from './tool-executor';
import { OpenClawSystemPromptBuilder } from './openclaw-system-prompt';
import * as fs from 'fs-extra';
import * as path from 'path';

export interface AgentJobData {
  agentId: string;
  task: string;
  parentSessionKey?: string;
  model: string;
  systemPrompt?: string;
  tools?: string[];
  maxTokens: number;
  temperature: number;
  timeout: number;
}

export interface AgentJobResult {
  agentId: string;
  status: 'success' | 'error';
  response?: string;
  error?: string;
  tokensUsed?: number;
  duration: number;
  transcript: any[];
}

export class BackgroundJobProcessor {
  private config: GatewayConfig;
  private anthropic: Anthropic;
  private registry: UnifiedToolRegistry;
  private toolExecutor: ToolExecutor;

  constructor(config: GatewayConfig) {
    this.config = config;

    const apiKey = process.env.ANTHROPIC_API_KEY;
    if (!apiKey) {
      throw new Error('ANTHROPIC_API_KEY environment variable not set');
    }

    this.anthropic = new Anthropic({
      apiKey
    });
    this.registry = new UnifiedToolRegistry(config);
    this.toolExecutor = new ToolExecutor(config, this.registry);
  }

  /**
   * Process an agent job
   */
  async processAgentJob(job: Job<AgentJobData>): Promise<AgentJobResult> {
    const startTime = Date.now();
    const { agentId, task, model, systemPrompt, tools, maxTokens, temperature, parentSessionKey } = job.data;

    console.log(`🤖 Processing agent job: ${agentId}`);
    console.log(`   Task: ${task.substring(0, 100)}...`);

    await job.progress(10);

    try {
      // Build system prompt
      let finalSystemPrompt: string;
      if (systemPrompt) {
        finalSystemPrompt = systemPrompt;
      } else {
        // Load OpenClaw-style system prompt from workspace
        const promptBuilder = new OpenClawSystemPromptBuilder(this.config);
        finalSystemPrompt = await promptBuilder.buildSystemPrompt({
          sessionKey: agentId,
          channel: 'agent',
          senderId: agentId,
          model,
          tools: [],
          capabilities: [],
          timestamp: new Date()
        }, 'full');
      }

      const systemMessages: any[] = [
        {
          type: 'text',
          text: finalSystemPrompt
        }
      ];

      // Add tool descriptions if tools are specified
      let availableTools: any[] = [];
      const allTools = await this.registry.getTools();

      if (tools && tools.length > 0) {
        // Filter tools by name
        availableTools = allTools.filter(tool => tools.includes(tool.name));
      } else {
        // Use all tools by default
        availableTools = allTools;
      }

      await job.progress(20);

      // Run conversation loop
      const transcript: any[] = [];
      let currentMessage = task;
      let iterationCount = 0;
      const maxIterations = 10;

      while (iterationCount < maxIterations) {
        iterationCount++;

        console.log(`   Iteration ${iterationCount}/${maxIterations}`);

        await job.progress(20 + (iterationCount * 6)); // Progress up to 80%

        // Create message
        const response = await this.anthropic.messages.create({
          model,
          max_tokens: maxTokens,
          temperature,
          system: systemMessages,
          messages: [
            {
              role: 'user',
              content: currentMessage
            }
          ],
          tools: availableTools.length > 0 ? availableTools : undefined
        });

        transcript.push({
          role: 'assistant',
          content: response.content
        });

        // Check stop reason
        if (response.stop_reason === 'end_turn') {
          // Agent finished
          const textContent = response.content
            .filter((c: any) => c.type === 'text')
            .map((c: any) => c.text)
            .join('\n');

          await job.progress(100);

          const duration = Date.now() - startTime;

          console.log(`✓ Agent completed: ${agentId} (${duration}ms, ${iterationCount} iterations)`);

          return {
            agentId,
            status: 'success',
            response: textContent,
            tokensUsed: response.usage.input_tokens + response.usage.output_tokens,
            duration,
            transcript
          };
        }

        // Execute tools
        if (response.stop_reason === 'tool_use') {
          const toolUses = response.content.filter((c: any) => c.type === 'tool_use') as any[];

          const toolResults = [];

          for (const toolUse of toolUses) {
            const sessionKey = parentSessionKey || agentId;

            const result = await this.toolExecutor.execute({
              id: toolUse.id as string,
              name: toolUse.name as string,
              input: toolUse.input as Record<string, any>
            }, sessionKey);

            toolResults.push(result);
          }

          // Add tool results to transcript
          transcript.push({
            role: 'user',
            content: toolResults
          });

          // Continue loop with tool results
          currentMessage = JSON.stringify(toolResults);
          continue;
        }

        // Max tokens reached
        if (response.stop_reason === 'max_tokens') {
          console.warn(`⚠️  Agent hit max tokens: ${agentId}`);

          const textContent = response.content
            .filter((c: any) => c.type === 'text')
            .map((c: any) => c.text)
            .join('\n');

          await job.progress(100);

          const duration = Date.now() - startTime;

          return {
            agentId,
            status: 'success',
            response: textContent + '\n\n(Response truncated - max tokens reached)',
            tokensUsed: response.usage.input_tokens + response.usage.output_tokens,
            duration,
            transcript
          };
        }

        // Unknown stop reason
        console.warn(`⚠️  Unknown stop reason: ${response.stop_reason}`);
        break;
      }

      // Max iterations reached
      await job.progress(100);

      const duration = Date.now() - startTime;

      return {
        agentId,
        status: 'error',
        error: `Max iterations (${maxIterations}) reached`,
        duration,
        transcript
      };

    } catch (error: any) {
      console.error(`❌ Agent job failed: ${agentId}`, error.message);

      await job.progress(100);

      const duration = Date.now() - startTime;

      return {
        agentId,
        status: 'error',
        error: error.message,
        duration,
        transcript: []
      };
    }
  }

  /**
   * Start processing agent jobs
   */
  async startProcessing(concurrency: number = 3): Promise<void> {
    console.log(`⚙️  Starting background job processor (concurrency: ${concurrency})`);

    const queueManager = getJobQueueManager();

    // Create queue if it doesn't exist
    await queueManager.createQueue('agent-jobs');

    // Process jobs
    await queueManager.processQueue('agent-jobs', async (job) => {
      return await this.processAgentJob(job);
    }, concurrency);

    console.log('✓ Background job processor started');
  }

  /**
   * Save agent transcript to file
   */
  async saveTranscript(agentId: string, transcript: any[]): Promise<string> {
    const transcriptDir = path.join(
      this.config.storage.transcripts || './transcripts',
      'agents'
    );

    await fs.ensureDir(transcriptDir);

    const filename = `${agentId}.json`;
    const filepath = path.join(transcriptDir, filename);

    await fs.writeJson(filepath, {
      agentId,
      timestamp: new Date().toISOString(),
      transcript
    }, { spaces: 2 });

    return filepath;
  }
}

// Global instance
let jobProcessor: BackgroundJobProcessor | null = null;

/**
 * Get global job processor instance
 */
export function getJobProcessor(config?: GatewayConfig): BackgroundJobProcessor {
  if (!jobProcessor && config) {
    jobProcessor = new BackgroundJobProcessor(config);
  }

  if (!jobProcessor) {
    throw new Error('BackgroundJobProcessor not initialized. Provide config on first call.');
  }

  return jobProcessor;
}

/**
 * Start background job processing
 */
export async function startBackgroundProcessing(
  config: GatewayConfig,
  concurrency: number = 3
): Promise<void> {
  const processor = getJobProcessor(config);
  await processor.startProcessing(concurrency);
}
