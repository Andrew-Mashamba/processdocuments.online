import { spawn, ChildProcess } from 'child_process';
import readline from 'readline';
import { ClaudeCliOptions, ClaudeCliResponse, ToolUse } from '../types';

export class ClaudeCliRuntime {
  private workspacePath: string;
  private model: string;
  private temperature: number;
  private maxTokens: number;

  constructor(options: ClaudeCliOptions) {
    this.workspacePath = options.workspacePath;
    this.model = options.model || 'claude-sonnet-4-5-20250514';
    this.temperature = options.temperature || 0.7;
    this.maxTokens = options.maxTokens || 8192;
  }

  /**
   * Execute Claude CLI with streaming support
   */
  async run(
    prompt: any,
    options?: {
      streaming?: boolean;
      onChunk?: (chunk: any) => void;
      onProgress?: (progress: any) => void;
    }
  ): Promise<ClaudeCliResponse> {
    const args = [
      '--print',
      '--output-format', options?.streaming ? 'stream-json' : 'json',
      '--dangerously-skip-permissions',
    ];

    if (options?.streaming) {
      args.push('--include-partial-messages', '--verbose');
    }

    const proc = spawn('claude', args, {
      cwd: this.workspacePath,
      stdio: ['pipe', 'pipe', 'pipe'],
      env: process.env,
    });

    // Send prompt via stdin
    proc.stdin.write(JSON.stringify(prompt));
    proc.stdin.end();

    if (options?.streaming) {
      return this.handleStreamingResponse(proc, options);
    } else {
      return this.handleNonStreamingResponse(proc);
    }
  }

  /**
   * Handle streaming response (NDJSON)
   */
  private async handleStreamingResponse(
    proc: ChildProcess,
    options: {
      onChunk?: (chunk: any) => void;
      onProgress?: (progress: any) => void;
    }
  ): Promise<ClaudeCliResponse> {
    return new Promise((resolve, reject) => {
      let content = '';
      let usage: any = null;
      let model = this.model;
      let stopReason = '';
      const toolUses: ToolUse[] = [];

      const rl = readline.createInterface({
        input: proc.stdout!,
        crlfDelay: Infinity,
      });

      rl.on('line', (line) => {
        try {
          const event = JSON.parse(line);

          if (event.type === 'stream_event') {
            const { event: innerEvent } = event;

            switch (innerEvent.type) {
              case 'content_block_delta':
                if (innerEvent.delta.type === 'text_delta') {
                  content += innerEvent.delta.text;
                  options.onChunk?.({ type: 'content', content: innerEvent.delta.text });
                }
                break;

              case 'message_start':
                model = innerEvent.message.model;
                usage = innerEvent.message.usage;
                options.onChunk?.({ type: 'start', model, usage });
                break;

              case 'message_delta':
                stopReason = innerEvent.delta.stop_reason;
                if (innerEvent.usage) {
                  usage = { ...usage, ...innerEvent.usage };
                }
                break;

              case 'content_block_start':
                if (innerEvent.content_block.type === 'tool_use') {
                  toolUses.push({
                    id: innerEvent.content_block.id,
                    name: innerEvent.content_block.name,
                    input: {},
                  });
                }
                break;

              case 'content_block_delta':
                if (innerEvent.delta.type === 'input_json_delta') {
                  const lastTool = toolUses[toolUses.length - 1];
                  if (lastTool) {
                    try {
                      const partialInput = JSON.parse(innerEvent.delta.partial_json || '{}');
                      lastTool.input = { ...lastTool.input, ...partialInput };
                    } catch {
                      // Partial JSON, skip
                    }
                  }
                }
                break;
            }
          }
        } catch (error) {
          console.error('Error parsing stream line:', error);
        }
      });

      rl.on('close', () => {
        resolve({
          content,
          usage: usage || { input_tokens: 0, output_tokens: 0 },
          model,
          stopReason,
          toolUses,
        });
      });

      proc.on('error', reject);
      proc.stderr?.on('data', (data) => {
        console.error('Claude CLI stderr:', data.toString());
      });
    });
  }

  /**
   * Handle non-streaming response (JSON)
   */
  private async handleNonStreamingResponse(proc: ChildProcess): Promise<ClaudeCliResponse> {
    return new Promise((resolve, reject) => {
      let stdout = '';
      let stderr = '';

      proc.stdout?.on('data', (data) => {
        stdout += data.toString();
      });

      proc.stderr?.on('data', (data) => {
        stderr += data.toString();
      });

      proc.on('close', (code) => {
        if (code !== 0) {
          reject(new Error(`Claude CLI exited with code ${code}: ${stderr}`));
          return;
        }

        try {
          const response = JSON.parse(stdout);
          const content = response.content
            .filter((c: any) => c.type === 'text')
            .map((c: any) => c.text)
            .join('');

          const toolUses = response.content
            .filter((c: any) => c.type === 'tool_use')
            .map((c: any) => ({
              id: c.id,
              name: c.name,
              input: c.input,
            }));

          resolve({
            content,
            usage: response.usage,
            model: response.model,
            stopReason: response.stop_reason,
            toolUses,
          });
        } catch (error) {
          reject(new Error(`Failed to parse Claude CLI output: ${error}`));
        }
      });

      proc.on('error', reject);
    });
  }

  /**
   * Build prompt from system + messages
   */
  buildPrompt(systemPrompt: string, messages: any[]): any {
    return {
      model: this.model,
      temperature: this.temperature,
      max_tokens: this.maxTokens,
      system: [
        {
          type: 'text',
          text: systemPrompt,
          cache_control: { type: 'ephemeral' }, // Enable prompt caching
        },
      ],
      messages: messages,
    };
  }

  /**
   * Check if Claude CLI is available
   */
  static async checkAvailability(): Promise<boolean> {
    try {
      const { execSync } = require('child_process');
      execSync('claude --version', { stdio: 'pipe' });
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Get Claude CLI version
   */
  static async getVersion(): Promise<string | null> {
    try {
      const { execSync } = require('child_process');
      const output = execSync('claude --version', { encoding: 'utf-8', stdio: 'pipe' });
      return output.trim();
    } catch {
      return null;
    }
  }
}
