/**
 * ZIMA Hybrid Gateway Server
 * Main entry point - combines OpenClaw routing with ZIMA intelligence
 */

import * as http from 'http';
import express from 'express';
import cors from 'cors';
import { WebSocketServer, WebSocket } from 'ws';
import { loadConfig, getConfigLoader } from './config/config';
import { HybridMessageRouter } from './router/message-router';
import { GatewayConfig, WebSocketMessage, ChatDelta } from './types';
import { AdminRoutes } from './admin/admin-routes';
import { getLogger } from './observability/logger';
import { getTracer, tracingMiddleware } from './observability/tracer';
import { getRequestLogger } from './observability/request-logger';
import { v4 as uuidv4 } from 'uuid';
import { BackgroundJobProcessor } from './agent/background-job-processor';

export class GatewayServer {
  private app: express.Application;
  private server: http.Server;
  private wss: WebSocketServer;
  private config: GatewayConfig | null = null;
  private router: HybridMessageRouter | null = null;
  private clients: Set<WebSocket> = new Set();
  private logger = getLogger({ service: 'zima-gateway' });
  private tracer = getTracer('zima-gateway');
  private adminRoutes: AdminRoutes | null = null;
  private backgroundJobProcessor: BackgroundJobProcessor | null = null;

  constructor() {
    this.app = express();
    this.server = http.createServer(this.app);
    this.wss = new WebSocketServer({ server: this.server });
  }

  /**
   * Initialize and start the gateway server
   */
  async start(): Promise<void> {
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║      ZIMA Hybrid Gateway - Multi-Channel Router       ║');
    console.log('║   Combining OpenClaw Architecture + ZIMA Intelligence  ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');

    // Load configuration
    console.log('⚙️  Loading configuration...');
    this.config = await loadConfig();
    console.log(`✓ Configuration loaded from: ${getConfigLoader().getConfigPath()}`);

    // Ensure storage directories exist
    console.log('📁 Ensuring storage directories...');
    await getConfigLoader().ensureStorageDirectories();
    console.log('✓ Storage directories ready');

    // Initialize router
    console.log('🔀 Initializing message router...');
    this.router = new HybridMessageRouter(this.config);
    console.log('✓ Message router initialized');

    // Initialize admin routes (optional - don't crash server if it fails)
    console.log('👤 Initializing admin routes...');
    try {
      this.adminRoutes = new AdminRoutes(this.router, this.config);
      console.log('✓ Admin routes initialized');
    } catch (error: any) {
      console.warn('⚠️  Admin routes failed to initialize:', error.message);
      console.warn('   Server will continue without admin dashboard');
      this.adminRoutes = null;
    }

    // Initialize and start background job processor
    console.log('⚙️  Starting background job processor...');
    try {
      this.backgroundJobProcessor = new BackgroundJobProcessor(this.config);
      await this.backgroundJobProcessor.startProcessing(5); // 5 concurrent jobs
      console.log('✓ Background job processor started (concurrency: 5)');
    } catch (error: any) {
      console.warn('⚠️  Background job processor failed to start:', error.message);
      console.warn('   Server will continue without background agent spawning');
      this.backgroundJobProcessor = null;
    }

    // Setup Express middleware
    this.setupMiddleware();

    // Setup HTTP routes
    this.setupHttpRoutes();

    // Setup WebSocket
    this.setupWebSocket();

    // Start server
    const port = this.config.gateway.port;
    const bind = this.config.gateway.bind;

    this.server.listen(port, bind, () => {
      console.log('\n╔════════════════════════════════════════════════════════╗');
      console.log(`║  Gateway Server Running                                ║`);
      console.log(`║  WebSocket: ws://${bind}:${port}                          ║`);
      console.log(`║  HTTP API:  http://${bind}:${port}                        ║`);
      console.log('╚════════════════════════════════════════════════════════╝\n');

      console.log('📡 Channels:');
      Object.entries(this.config!.channels).forEach(([name, config]) => {
        const status = config.enabled ? '✓ Enabled' : '✗ Disabled';
        console.log(`   - ${name.padEnd(10)}: ${status}`);
      });

      console.log('\n🔧 Configuration:');
      console.log(`   - ZIMA Core: ${this.config!.zima.apiUrl}`);
      console.log(`   - Storage:   ${this.config!.storage.root}`);
      console.log(`   - Models:    ${this.config!.models?.simple || 'haiku'} / ${this.config!.models?.standard || 'sonnet'} / ${this.config!.models?.complex || 'opus'}`);

      console.log('\n✅ Gateway ready to route messages!\n');
    });

    // Graceful shutdown
    this.setupGracefulShutdown();
  }

  /**
   * Setup Express middleware
   */
  private setupMiddleware(): void {
    if (!this.config) throw new Error('Config not loaded');

    // CORS
    this.app.use(cors({
      origin: this.config.gateway.cors.origins,
      credentials: true
    }));

    // JSON parser
    this.app.use(express.json({ limit: '50mb' }));

    // Distributed tracing middleware
    this.app.use(tracingMiddleware(this.tracer));

    // Request logging with structured logger
    this.app.use((req, res, next) => {
      this.logger.info('HTTP Request', {
        method: req.method,
        path: req.path,
        trace_id: (req as any).trace_id,
        user_agent: req.get('user-agent')
      });
      next();
    });
  }

  /**
   * Setup HTTP routes
   */
  private setupHttpRoutes(): void {
    // Health check
    this.app.get('/health', (req, res) => {
      res.json({
        status: 'healthy',
        version: '1.0.0',
        gateway: 'ZIMA Hybrid',
        uptime: process.uptime(),
        timestamp: Date.now()
      });
    });

    // Chat endpoint (HTTP alternative to WebSocket)
    this.app.post('/api/chat', async (req, res) => {
      try {
        if (!this.router) {
          return res.status(503).json({ error: 'Router not initialized' });
        }

        const result = await this.router.routeMessage(req.body);
        res.json(result);
      } catch (error: any) {
        console.error('Error in /api/chat:', error);
        res.status(500).json({
          error: error.message,
          stack: process.env.NODE_ENV === 'development' ? error.stack : undefined
        });
      }
    });

    // Chat streaming endpoint (SSE)
    this.app.post('/api/chat/stream', async (req, res) => {
      const requestId = uuidv4();
      const reqLogger = getRequestLogger();

      try {
        // Start request logging
        reqLogger.startRequest(requestId);
        reqLogger.log('HTTP_REQUEST_RECEIVED', 'Server', {
          method: 'POST',
          path: '/api/chat/stream',
          body: req.body,
          requestId
        }, req.body.sessionKey);

        if (!this.router) {
          return res.status(503).json({ error: 'Router not initialized' });
        }

        // Set SSE headers
        res.setHeader('Content-Type', 'text/event-stream');
        res.setHeader('Cache-Control', 'no-cache');
        res.setHeader('Connection', 'keep-alive');
        res.setHeader('X-Accel-Buffering', 'no'); // Disable nginx buffering

        // Send start event
        res.write(`event: start\ndata: ${JSON.stringify({ timestamp: Date.now(), requestId })}\n\n`);

        // Stream the response
        const result = await this.router.routeMessageStream(req.body, (chunk: any) => {
          // Log streaming chunks
          reqLogger.logStreamChunk(req.body.sessionKey, chunk);

          // Send each chunk as SSE
          res.write(`event: content\ndata: ${JSON.stringify(chunk)}\n\n`);
        });

        // Log final result
        reqLogger.log('HTTP_RESPONSE_COMPLETE', 'Server', {
          result,
          requestId
        }, req.body.sessionKey);

        // Send final result
        res.write(`event: complete\ndata: ${JSON.stringify(result)}\n\n`);
        res.end();

        // End request logging
        reqLogger.endRequest(req.body.sessionKey, true);

      } catch (error: any) {
        console.error('Error in /api/chat/stream:', error);

        reqLogger.log('HTTP_ERROR', 'Server', {
          error: error.message,
          stack: error.stack,
          requestId
        }, req.body.sessionKey);

        res.write(`event: error\ndata: ${JSON.stringify({ error: error.message })}\n\n`);
        res.end();

        // End request logging with error
        reqLogger.endRequest(req.body.sessionKey, false, error);
      }
    });

    // Gateway stats
    this.app.get('/api/stats', async (req, res) => {
      if (!this.router) {
        return res.status(503).json({ error: 'Router not initialized' });
      }

      try {
        const contextStats = await this.router.getContextStats();

        res.json({
          clients: this.clients.size,
          dedup: this.router.getCacheStats(),
          context: contextStats,
          uptime: process.uptime(),
          memory: process.memoryUsage()
        });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // Configuration info (safe version)
    this.app.get('/api/config', (req, res) => {
      if (!this.config) {
        return res.status(503).json({ error: 'Config not loaded' });
      }

      res.json({
        gateway: {
          port: this.config.gateway.port,
          channels: Object.keys(this.config.channels).map(name => ({
            name,
            enabled: (this.config!.channels as any)[name].enabled
          }))
        },
        storage: {
          root: this.config.storage.root
        },
        models: this.config.models
      });
    });

    // Admin routes
    if (this.adminRoutes) {
      this.app.use('/api/admin', this.adminRoutes.getRouter());
    }

    // 404 handler
    this.app.use((req, res) => {
      res.status(404).json({
        error: 'Not found',
        path: req.path,
        available: ['/health', '/api/chat', '/api/stats', '/api/config', '/api/admin/*']
      });
    });
  }

  /**
   * Setup WebSocket server
   */
  private setupWebSocket(): void {
    this.wss.on('connection', (ws: WebSocket) => {
      console.log('🔌 WebSocket client connected');
      this.clients.add(ws);

      // Send welcome message
      this.sendToClient(ws, {
        type: 'connected',
        payload: {
          message: 'Connected to ZIMA Hybrid Gateway',
          version: '1.0.0',
          timestamp: Date.now()
        }
      });

      // Handle messages
      ws.on('message', async (data: Buffer) => {
        try {
          const message: WebSocketMessage = JSON.parse(data.toString());
          await this.handleWebSocketMessage(ws, message);
        } catch (error: any) {
          console.error('Error handling WebSocket message:', error);
          this.sendToClient(ws, {
            type: 'error',
            payload: {
              error: error.message
            }
          });
        }
      });

      // Handle disconnect
      ws.on('close', () => {
        console.log('🔌 WebSocket client disconnected');
        this.clients.delete(ws);
      });

      // Handle errors
      ws.on('error', (error) => {
        console.error('WebSocket error:', error);
        this.clients.delete(ws);
      });
    });
  }

  /**
   * Handle incoming WebSocket message
   */
  private async handleWebSocketMessage(ws: WebSocket, message: WebSocketMessage): Promise<void> {
    console.log(`[WS] Received: ${message.type}`);

    switch (message.type) {
      case 'chat':
        await this.handleChatMessage(ws, message.payload);
        break;

      case 'ping':
        this.sendToClient(ws, { type: 'pong', payload: { timestamp: Date.now() } });
        break;

      default:
        this.sendToClient(ws, {
          type: 'error',
          payload: { error: `Unknown message type: ${message.type}` }
        });
    }
  }

  /**
   * Handle chat message via WebSocket
   */
  private async handleChatMessage(ws: WebSocket, payload: any): Promise<void> {
    if (!this.router) {
      this.sendToClient(ws, {
        type: 'error',
        payload: { error: 'Router not initialized' }
      });
      return;
    }

    try {
      // Send acknowledgment
      this.sendToClient(ws, {
        type: 'chat:start',
        payload: {
          sessionKey: payload.sessionKey || 'pending',
          timestamp: Date.now()
        }
      });

      // Route message
      const result = await this.router.routeMessage(payload);

      // Send result
      this.sendToClient(ws, {
        type: 'chat:complete',
        payload: result
      });
    } catch (error: any) {
      this.sendToClient(ws, {
        type: 'chat:error',
        payload: {
          error: error.message
        }
      });
    }
  }

  /**
   * Send message to specific client
   */
  private sendToClient(ws: WebSocket, message: WebSocketMessage): void {
    if (ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify(message));
    }
  }

  /**
   * Broadcast message to all connected clients
   */
  public broadcast(message: WebSocketMessage): void {
    const json = JSON.stringify(message);
    this.clients.forEach(client => {
      if (client.readyState === WebSocket.OPEN) {
        client.send(json);
      }
    });
  }

  /**
   * Broadcast chat delta (streaming)
   */
  public broadcastChatDelta(delta: ChatDelta): void {
    this.broadcast({
      type: 'chat:delta',
      payload: delta,
      timestamp: Date.now()
    });
  }

  /**
   * Setup graceful shutdown
   */
  private setupGracefulShutdown(): void {
    const shutdown = async (signal: string) => {
      console.log(`\n${signal} received, shutting down gracefully...`);

      // Stop background job processor
      if (this.backgroundJobProcessor) {
        console.log('Stopping background job processor...');
        // Note: BackgroundJobProcessor doesn't have a stop method yet, but we're marking it for cleanup
        this.backgroundJobProcessor = null;
      }

      // Close WebSocket server
      console.log('Closing WebSocket connections...');
      this.wss.clients.forEach(client => {
        client.close(1000, 'Server shutting down');
      });

      // Close HTTP server
      console.log('Closing HTTP server...');
      this.server.close(() => {
        console.log('✓ Server closed');
        process.exit(0);
      });

      // Force exit after 10 seconds
      setTimeout(() => {
        console.error('Force shutdown after timeout');
        process.exit(1);
      }, 10000);
    };

    process.on('SIGTERM', () => shutdown('SIGTERM'));
    process.on('SIGINT', () => shutdown('SIGINT'));
  }

  /**
   * Get router instance (for testing)
   */
  getRouter(): HybridMessageRouter | null {
    return this.router;
  }
}

// Start server if run directly
if (require.main === module) {
  const server = new GatewayServer();
  server.start().catch(error => {
    console.error('Fatal error starting gateway:', error);
    process.exit(1);
  });
}

export default GatewayServer;
