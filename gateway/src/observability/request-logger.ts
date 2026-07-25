/**
 * Request Logger - Detailed file logging for request/response tracking
 * Logs the complete flow from HTTP request to Claude CLI response
 */

import * as fs from 'fs';
import * as path from 'path';

export interface RequestLog {
  timestamp: string;
  requestId: string;
  sessionKey: string;
  phase: string;
  service: string;
  data: any;
}

export class RequestLogger {
  private logDir: string;
  private currentRequestId: string | null = null;
  private requestStartTime: number = 0;

  constructor(logDir: string = './logs/requests') {
    this.logDir = logDir;
    this.ensureLogDirectory();
  }

  private ensureLogDirectory() {
    if (!fs.existsSync(this.logDir)) {
      fs.mkdirSync(this.logDir, { recursive: true });
    }
  }

  /**
   * Start tracking a new request
   */
  startRequest(requestId: string): void {
    this.currentRequestId = requestId;
    this.requestStartTime = Date.now();
  }

  /**
   * Log a phase in the request processing
   */
  log(phase: string, service: string, data: any, sessionKey?: string): void {
    const logEntry: RequestLog = {
      timestamp: new Date().toISOString(),
      requestId: this.currentRequestId || 'unknown',
      sessionKey: sessionKey || 'unknown',
      phase,
      service,
      data
    };

    // Write to file
    this.writeToFile(logEntry);

    // Also console log for immediate visibility
    console.log(`\n📝 [${service}] ${phase}`);
    if (data.message) console.log(`   Message: ${data.message.substring(0, 100)}...`);
    if (data.prompt) console.log(`   Prompt Length: ${data.prompt.length} chars`);
    if (data.response) console.log(`   Response Length: ${data.response.length} chars`);
  }

  /**
   * Log the full prompt being sent to Claude
   */
  logPrompt(sessionKey: string, prompt: string, context: any): void {
    const logEntry = {
      timestamp: new Date().toISOString(),
      requestId: this.currentRequestId || 'unknown',
      sessionKey,
      phase: 'CLAUDE_PROMPT',
      service: 'ClaudeCliRuntime',
      data: {
        prompt,
        promptLength: prompt.length,
        context: {
          model: context.model,
          complexity: context.complexity,
          tier: context.tier,
          messageCount: context.messages?.length || 0
        }
      }
    };

    this.writeToFile(logEntry);
    this.writePromptToSeparateFile(sessionKey, prompt);
  }

  /**
   * Log the full response from Claude
   */
  logResponse(sessionKey: string, response: string, usage: any): void {
    const logEntry = {
      timestamp: new Date().toISOString(),
      requestId: this.currentRequestId || 'unknown',
      sessionKey,
      phase: 'CLAUDE_RESPONSE',
      service: 'ClaudeCliRuntime',
      data: {
        response,
        responseLength: response.length,
        usage
      }
    };

    this.writeToFile(logEntry);
    this.writeResponseToSeparateFile(sessionKey, response);
  }

  /**
   * Log streaming chunks
   */
  logStreamChunk(sessionKey: string, chunk: any): void {
    const logEntry = {
      timestamp: new Date().toISOString(),
      requestId: this.currentRequestId || 'unknown',
      sessionKey,
      phase: 'STREAM_CHUNK',
      service: 'ClaudeCliRuntime',
      data: chunk
    };

    this.writeToFile(logEntry);
  }

  /**
   * End request logging
   */
  endRequest(sessionKey: string, success: boolean, error?: any): void {
    const duration = Date.now() - this.requestStartTime;

    const logEntry = {
      timestamp: new Date().toISOString(),
      requestId: this.currentRequestId || 'unknown',
      sessionKey,
      phase: 'REQUEST_END',
      service: 'Server',
      data: {
        success,
        duration: `${duration}ms`,
        error: error ? {
          message: error.message,
          stack: error.stack
        } : null
      }
    };

    this.writeToFile(logEntry);

    console.log(`\n✅ Request ${this.currentRequestId} completed in ${duration}ms`);

    this.currentRequestId = null;
    this.requestStartTime = 0;
  }

  /**
   * Write log entry to main log file (JSONL format)
   */
  private writeToFile(logEntry: RequestLog): void {
    const logFile = path.join(this.logDir, 'requests.jsonl');
    const logLine = JSON.stringify(logEntry) + '\n';

    fs.appendFileSync(logFile, logLine, 'utf8');
  }

  /**
   * Write full prompt to separate file for easy reading
   */
  private writePromptToSeparateFile(sessionKey: string, prompt: string): void {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const filename = `prompt_${sessionKey.replace(/:/g, '_')}_${timestamp}.txt`;
    const filepath = path.join(this.logDir, 'prompts', filename);

    // Ensure prompts directory exists
    const promptsDir = path.join(this.logDir, 'prompts');
    if (!fs.existsSync(promptsDir)) {
      fs.mkdirSync(promptsDir, { recursive: true });
    }

    const content = `=== PROMPT ===
Request ID: ${this.currentRequestId}
Session Key: ${sessionKey}
Timestamp: ${new Date().toISOString()}
Length: ${prompt.length} characters

${prompt}
`;

    fs.writeFileSync(filepath, content, 'utf8');
    console.log(`   📄 Prompt saved to: ${filename}`);
  }

  /**
   * Write full response to separate file for easy reading
   */
  private writeResponseToSeparateFile(sessionKey: string, response: string): void {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const filename = `response_${sessionKey.replace(/:/g, '_')}_${timestamp}.txt`;
    const filepath = path.join(this.logDir, 'responses', filename);

    // Ensure responses directory exists
    const responsesDir = path.join(this.logDir, 'responses');
    if (!fs.existsSync(responsesDir)) {
      fs.mkdirSync(responsesDir, { recursive: true });
    }

    const content = `=== RESPONSE ===
Request ID: ${this.currentRequestId}
Session Key: ${sessionKey}
Timestamp: ${new Date().toISOString()}
Length: ${response.length} characters

${response}
`;

    fs.writeFileSync(filepath, content, 'utf8');
    console.log(`   📄 Response saved to: ${filename}`);
  }

  /**
   * Get current request ID
   */
  getCurrentRequestId(): string | null {
    return this.currentRequestId;
  }
}

// Singleton instance
let requestLogger: RequestLogger | null = null;

export function getRequestLogger(logDir?: string): RequestLogger {
  if (!requestLogger) {
    requestLogger = new RequestLogger(logDir);
  }
  return requestLogger;
}
