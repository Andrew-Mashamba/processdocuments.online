/**
 * Admin Dashboard Routes
 * Week 12: Production Polish - Admin Dashboard API
 */

import { Router, Request, Response } from 'express';
import { HybridMessageRouter } from '../router/message-router';
import { ToolAnalyticsService } from '../analytics/tool-analytics-service';
import { FactService } from '../memory/fact-service';
import { SubAgentSpawner } from '../agent/sub-agent-spawner';
import { getQueueManager } from '../queue/job-queue';
import { TranscriptManager } from '../context/transcript-manager';
import { MemoryService } from '../memory/memory-service';
import * as path from 'path';
import * as fs from 'fs-extra';

export class AdminRoutes {
  private router: Router;
  private messageRouter: HybridMessageRouter;
  private analytics: ToolAnalyticsService;
  private factService: FactService;
  private agentSpawner: SubAgentSpawner;
  private transcriptManager: TranscriptManager;
  private memoryService: MemoryService;

  constructor(messageRouter: HybridMessageRouter, config: any) {
    this.router = Router();
    this.messageRouter = messageRouter;

    // Initialize services
    this.analytics = new ToolAnalyticsService(
      path.join(config.storage.root, 'analytics/tools')
    );

    this.factService = new FactService({
      storageDir: path.join(config.storage.root, 'memory/facts'),
      apiKey: process.env.ANTHROPIC_API_KEY
    });

    this.agentSpawner = new SubAgentSpawner(config);
    this.transcriptManager = new TranscriptManager(config.storage.transcripts);
    this.memoryService = new MemoryService({
      dbPath: path.join(config.storage.root, 'memory/memory.db')
    });

    this.setupRoutes();
  }

  private setupRoutes(): void {
    // ========================================
    // DASHBOARD OVERVIEW
    // ========================================

    this.router.get('/dashboard', async (req: Request, res: Response) => {
      try {
        const [
          contextStats,
          cacheStats,
          analyticsStats,
          factStats,
          agentStats,
          memoryStats
        ] = await Promise.all([
          this.messageRouter.getContextStats(),
          this.getCacheStats(),
          this.analytics.getAllMetrics(),
          this.factService.getStats(),
          this.getAgentStats(),
          this.memoryService.getStats()
        ]);

        res.json({
          status: 'operational',
          uptime: process.uptime(),
          memory: process.memoryUsage(),
          timestamp: new Date().toISOString(),
          stats: {
            context: contextStats,
            cache: cacheStats,
            analytics: {
              totalTools: analyticsStats.length,
              topTools: analyticsStats.slice(0, 5).map(m => ({
                name: m.toolName,
                executions: m.totalExecutions,
                successRate: (m.successRate * 100).toFixed(1) + '%'
              }))
            },
            facts: factStats,
            agents: agentStats,
            memory: memoryStats
          }
        });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // ========================================
    // TOOL ANALYTICS
    // ========================================

    this.router.get('/analytics/tools', async (req: Request, res: Response) => {
      try {
        const metrics = this.analytics.getAllMetrics();
        res.json({ tools: metrics });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.get('/analytics/tools/:toolName', async (req: Request, res: Response) => {
      try {
        const { toolName } = req.params;
        const metrics = this.analytics.getToolMetrics(toolName);

        if (!metrics) {
          return res.status(404).json({ error: 'Tool not found' });
        }

        const trends = this.analytics.getUsageTrends(toolName, 'day');

        res.json({
          metrics,
          trends
        });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.get('/analytics/recommendations', async (req: Request, res: Response) => {
      try {
        const { sessionKey, recentTools, taskDescription } = req.query;

        const recommendations = this.analytics.getRecommendations({
          sessionKey: sessionKey as string,
          recentTools: recentTools ? JSON.parse(recentTools as string) : undefined,
          taskDescription: taskDescription as string
        });

        res.json({ recommendations });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // ========================================
    // AGENT STATUS
    // ========================================

    this.router.get('/agents', async (req: Request, res: Response) => {
      try {
        const agents = this.agentSpawner.getAllAgents();
        res.json({ agents });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.get('/agents/:agentId', async (req: Request, res: Response) => {
      try {
        const { agentId } = req.params;
        const status = this.agentSpawner.getStatus(agentId);

        if (!status) {
          return res.status(404).json({ error: 'Agent not found' });
        }

        res.json({ agent: status });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.post('/agents/:agentId/stop', async (req: Request, res: Response) => {
      try {
        const { agentId } = req.params;
        const result = await this.agentSpawner.stop(agentId);
        res.json({ success: result });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // ========================================
    // MEMORY BROWSER
    // ========================================

    this.router.get('/memory/search', async (req: Request, res: Response) => {
      try {
        const { query, limit = 10 } = req.query;

        if (!query) {
          return res.status(400).json({ error: 'Query parameter required' });
        }

        const results = await this.memoryService.search(query as string, {
          limit: parseInt(limit as string)
        });

        res.json({ results });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.get('/memory/sessions', async (req: Request, res: Response) => {
      try {
        const sessions = await this.transcriptManager.listSessions();
        res.json({ sessions });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.get('/memory/sessions/:sessionKey', async (req: Request, res: Response) => {
      try {
        const { sessionKey } = req.params;
        const messages = await this.transcriptManager.readTranscript(sessionKey);
        res.json({ sessionKey, messages, count: messages.length });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.get('/memory/facts', async (req: Request, res: Response) => {
      try {
        const { sessionKey, type, limit = 50 } = req.query;

        const facts = await this.factService.searchFacts({
          sessionKey: sessionKey as string,
          type: type as string,
          limit: parseInt(limit as string)
        });

        res.json({ facts });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.get('/memory/facts/summary/:sessionKey', async (req: Request, res: Response) => {
      try {
        const { sessionKey } = req.params;
        const summary = await this.factService.getSessionFactSummary(sessionKey);
        res.json(summary);
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // ========================================
    // JOB QUEUE MANAGER
    // ========================================

    this.router.get('/jobs/queues', async (req: Request, res: Response) => {
      try {
        const queueManager = getQueueManager();
        const queues = await queueManager.getQueueStats();
        res.json({ queues });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.get('/jobs/:queueName', async (req: Request, res: Response) => {
      try {
        const { queueName } = req.params;
        const { status = 'active', limit = 50 } = req.query;

        const queueManager = getQueueManager();
        const jobs = await queueManager.getJobs(
          queueName,
          status as any,
          parseInt(limit as string)
        );

        res.json({ queueName, status, jobs });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.post('/jobs/:jobId/retry', async (req: Request, res: Response) => {
      try {
        const { jobId } = req.params;
        const queueManager = getQueueManager();
        await queueManager.retryJob(jobId);
        res.json({ success: true });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    this.router.delete('/jobs/:jobId', async (req: Request, res: Response) => {
      try {
        const { jobId } = req.params;
        const queueManager = getQueueManager();
        await queueManager.removeJob(jobId);
        res.json({ success: true });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // ========================================
    // SYSTEM HEALTH
    // ========================================

    this.router.get('/health/detailed', async (req: Request, res: Response) => {
      try {
        const health = await this.getDetailedHealth();
        res.json(health);
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });

    // ========================================
    // LOGS
    // ========================================

    this.router.get('/logs/tool-failures', async (req: Request, res: Response) => {
      try {
        const { limit = 50 } = req.query;
        const logsPath = path.join(process.cwd(), 'logs/tools/failures.jsonl');

        if (!await fs.pathExists(logsPath)) {
          return res.json({ failures: [] });
        }

        const lines = (await fs.readFile(logsPath, 'utf-8'))
          .split('\n')
          .filter(l => l.trim());

        const failures = lines
          .slice(-parseInt(limit as string))
          .map(line => JSON.parse(line))
          .reverse();

        res.json({ failures });
      } catch (error: any) {
        res.status(500).json({ error: error.message });
      }
    });
  }

  private async getCacheStats() {
    return this.messageRouter.getCacheStats();
  }

  private async getAgentStats() {
    const agents = this.agentSpawner.getAllAgents();
    const byStatus = agents.reduce((acc: any, agent) => {
      acc[agent.status] = (acc[agent.status] || 0) + 1;
      return acc;
    }, {});

    return {
      total: agents.length,
      byStatus,
      recent: agents.slice(-5)
    };
  }

  private async getDetailedHealth() {
    const queueManager = getQueueManager();

    return {
      status: 'healthy',
      timestamp: new Date().toISOString(),
      uptime: process.uptime(),
      process: {
        pid: process.pid,
        memory: process.memoryUsage(),
        cpu: process.cpuUsage()
      },
      queues: await queueManager.getQueueStats(),
      agents: {
        active: this.agentSpawner.getAllAgents().filter(a => a.status === 'running').length,
        total: this.agentSpawner.getAllAgents().length
      }
    };
  }

  getRouter(): Router {
    return this.router;
  }
}
