import express, { Express, Request, Response, NextFunction } from 'express';
import cors from 'cors';
import bodyParser from 'body-parser';
import rateLimit from 'express-rate-limit';
import { VisualEditorConfig } from '../types';
import { ClaudeCliRuntime } from '../agent/claude-cli-runtime';
import { ToolRegistry } from '../agent/tool-registry';
import { TranscriptManager } from '../context/transcript-manager';
import { MemoryService } from '../memory/memory-service';
import { HybridContextManager } from '../context/hybrid-context-manager';
import * as path from 'path';

export class ExpressApp {
  private app: Express;
  private config: VisualEditorConfig;
  private runtime: ClaudeCliRuntime;
  private toolRegistry: ToolRegistry;
  private transcriptManager: TranscriptManager;
  private memoryService?: MemoryService;
  private contextManager: HybridContextManager;

  constructor(config: VisualEditorConfig, workspaceRoot: string) {
    this.config = config;
    this.app = express();

    // Initialize services
    const sessionsDir = path.join(workspaceRoot, '.visual-editor', 'sessions');
    const memoryDbPath = path.join(workspaceRoot, '.visual-editor', 'memory.db');

    this.transcriptManager = new TranscriptManager(sessionsDir);

    if (config.agent.memoryEnabled) {
      this.memoryService = new MemoryService(memoryDbPath);
    }

    this.contextManager = new HybridContextManager(
      this.transcriptManager,
      this.memoryService
    );

    this.toolRegistry = new ToolRegistry({
      zimaFileServiceUrl: config.tools.zimaFileService,
    });

    this.runtime = new ClaudeCliRuntime({
      workspacePath: workspaceRoot,
      model: config.agent.model,
      temperature: config.agent.temperature,
      maxTokens: config.agent.maxTokens,
    });

    this.setupMiddleware();
    this.setupRoutes();
  }

  /**
   * Setup Express middleware
   */
  private setupMiddleware(): void {
    // CORS
    this.app.use(cors({
      origin: this.config.security.allowedOrigins,
      credentials: true,
    }));

    // Body parsers
    this.app.use(bodyParser.json({ limit: this.config.security.maxUploadSize || '10mb' }));
    this.app.use(bodyParser.urlencoded({ extended: true, limit: '10mb' }));

    // Rate limiting
    const limiter = rateLimit({
      windowMs: 60 * 60 * 1000, // 1 hour
      max: this.config.security.rateLimit,
      message: 'Too many requests, please try again later.',
    });
    this.app.use(limiter);

    // Logging
    this.app.use((req: Request, res: Response, next: NextFunction) => {
      if (this.config.logging.level === 'debug') {
        console.log(`${new Date().toISOString()} ${req.method} ${req.path}`);
      }
      next();
    });

    // Error handler
    this.app.use((err: Error, req: Request, res: Response, next: NextFunction) => {
      console.error('Express error:', err);
      res.status(500).json({ error: err.message });
    });
  }

  /**
   * Setup routes
   */
  private setupRoutes(): void {
    // Health check
    this.app.get('/health', (req: Request, res: Response) => {
      res.json({ status: 'ok', timestamp: new Date().toISOString() });
    });

    // Chat endpoint (non-streaming)
    this.app.post('/api/chat', async (req: Request, res: Response) => {
      try {
        const { sessionKey, message, files } = req.body;

        if (!sessionKey || !message) {
          return res.status(400).json({ error: 'sessionKey and message are required' });
        }

        // Get context
        const messages = await this.contextManager.getContext({
          sessionKey,
          optimization: this.config.agent.contextOptimization,
          memoryEnabled: this.config.agent.memoryEnabled,
        });

        // Add user message
        const userMessage = {
          role: 'user' as const,
          content: message,
          timestamp: new Date().toISOString(),
        };

        // Append to transcript
        await this.transcriptManager.append(sessionKey, userMessage);

        // Build prompt
        const systemPrompt = this.buildSystemPrompt();
        const prompt = this.runtime.buildPrompt(systemPrompt, [
          ...messages,
          userMessage,
        ]);

        // Execute
        const response = await this.runtime.run(prompt, { streaming: false });

        // Save assistant message
        const assistantMessage = {
          role: 'assistant' as const,
          content: response.content,
          timestamp: new Date().toISOString(),
          metadata: {
            model: response.model,
            usage: response.usage,
          },
        };

        await this.transcriptManager.append(sessionKey, assistantMessage);

        // Return response
        res.json({
          content: response.content,
          usage: response.usage,
          model: response.model,
        });

      } catch (error: any) {
        console.error('Chat error:', error);
        res.status(500).json({ error: error.message });
      }
    });

    // Chat endpoint (streaming)
    this.app.post('/api/chat/stream', async (req: Request, res: Response) => {
      try {
        const { sessionKey, message } = req.body;

        if (!sessionKey || !message) {
          return res.status(400).json({ error: 'sessionKey and message are required' });
        }

        // Set SSE headers
        res.setHeader('Content-Type', 'text/event-stream');
        res.setHeader('Cache-Control', 'no-cache');
        res.setHeader('Connection', 'keep-alive');
        res.flushHeaders();

        // Get context
        const messages = await this.contextManager.getContext({
          sessionKey,
          optimization: this.config.agent.contextOptimization,
          memoryEnabled: this.config.agent.memoryEnabled,
        });

        // Add user message
        const userMessage = {
          role: 'user' as const,
          content: message,
          timestamp: new Date().toISOString(),
        };

        await this.transcriptManager.append(sessionKey, userMessage);

        // Build prompt
        const systemPrompt = this.buildSystemPrompt();
        const prompt = this.runtime.buildPrompt(systemPrompt, [
          ...messages,
          userMessage,
        ]);

        // Send start event
        res.write(`event: start\ndata: ${JSON.stringify({ sessionKey })}\n\n`);

        // Execute streaming
        let fullContent = '';
        const response = await this.runtime.run(prompt, {
          streaming: true,
          onChunk: (chunk: any) => {
            if (chunk.type === 'content') {
              fullContent += chunk.content;
              res.write(`event: content\ndata: ${JSON.stringify({ content: chunk.content })}\n\n`);
            }
          },
        });

        // Save assistant message
        const assistantMessage = {
          role: 'assistant' as const,
          content: fullContent,
          timestamp: new Date().toISOString(),
          metadata: {
            model: response.model,
            usage: response.usage,
          },
        };

        await this.transcriptManager.append(sessionKey, assistantMessage);

        // Send complete event
        res.write(`event: complete\ndata: ${JSON.stringify({
          usage: response.usage,
          model: response.model,
        })}\n\n`);

        res.end();

      } catch (error: any) {
        console.error('Stream error:', error);
        res.write(`event: error\ndata: ${JSON.stringify({ error: error.message })}\n\n`);
        res.end();
      }
    });

    // Sessions API
    this.app.get('/api/sessions', async (req: Request, res: Response) => {
      try {
        const sessions = await this.transcriptManager.listSessions();
        const sessionsWithInfo = await Promise.all(
          sessions.map(async (key) => {
            const info = await this.transcriptManager.getSessionInfo(key);
            return { key, ...info };
          })
        );

        res.json({ sessions: sessionsWithInfo });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // Memory search API
    this.app.post('/api/memory/search', async (req: Request, res: Response) => {
      try {
        if (!this.memoryService) {
          return res.status(400).json({ error: 'Memory system not enabled' });
        }

        const { query, limit } = req.body;
        const results = await this.memoryService.search({ query, limit });

        res.json({ results });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // Tools API
    this.app.get('/api/tools', (req: Request, res: Response) => {
      const tools = this.toolRegistry.getAll().map(t => ({
        name: t.name,
        description: t.description,
        category: t.category,
        provider: t.provider,
      }));

      res.json({ tools, count: tools.length });
    });

    // Execute tool
    this.app.post('/api/tools/:toolName', async (req: Request, res: Response) => {
      try {
        const { toolName } = req.params;
        const input = req.body;

        if (!this.toolRegistry.has(toolName)) {
          return res.status(404).json({ error: `Tool not found: ${toolName}` });
        }

        const result = await this.toolRegistry.execute(toolName, input);
        res.json(result);

      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // Stats endpoint
    this.app.get('/api/stats', async (req: Request, res: Response) => {
      try {
        const sessions = await this.transcriptManager.listSessions();
        const totalSize = await this.transcriptManager.getTotalSize();
        const memoryCount = this.memoryService?.count() || 0;

        res.json({
          sessions: sessions.length,
          totalTranscriptSize: totalSize,
          memoryEntries: memoryCount,
          toolsAvailable: this.toolRegistry.getCount(),
        });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });
  }

  /**
   * Build system prompt
   */
  private buildSystemPrompt(): string {
    const toolDefinitions = this.toolRegistry.getToolDefinitions();

    return `You are a visual code editor AI assistant powered by ZIMA.

You have access to ${toolDefinitions.length} tools to help users edit their web applications.

**Available Tools:**
${toolDefinitions.map(t => `- ${t.name}: ${t.description}`).join('\n')}

**Your Capabilities:**
- Read and edit source files (read, write, edit)
- Execute shell commands (exec)
- Generate documents (Excel, PDF, Word, PowerPoint)
- Analyze visual context and screenshots
- Map UI components to source files
- Match design patterns and styles

**Guidelines:**
- Make minimal changes (only what's requested)
- Preserve existing functionality
- Follow framework best practices
- Provide clear explanations
- Use appropriate tools for each task

Begin by understanding the user's request, then use the available tools to complete the task.
`;
  }

  /**
   * Get Express app instance
   */
  getApp(): Express {
    return this.app;
  }

  /**
   * Start server
   */
  listen(port: number): void {
    this.app.listen(port, () => {
      console.log(`\n✓ Visual Editor Agent Server running on http://localhost:${port}`);
      console.log(`  - Chat API: http://localhost:${port}/api/chat`);
      console.log(`  - Stream API: http://localhost:${port}/api/chat/stream`);
      console.log(`  - Tools: ${this.toolRegistry.getCount()} available`);
      console.log(`  - Memory: ${this.config.agent.memoryEnabled ? 'Enabled' : 'Disabled'}`);
      console.log(`  - Optimization: ${this.config.agent.contextOptimization}`);
      console.log();
    });
  }

  /**
   * Cleanup resources
   */
  close(): void {
    if (this.memoryService) {
      this.memoryService.close();
    }
  }
}
