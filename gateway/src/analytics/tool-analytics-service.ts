/**
 * Tool Analytics Service
 * Week 11: Advanced Features - Tool Usage Analytics
 */

import * as fs from 'fs';
import * as path from 'path';

export interface ToolExecution {
  toolName: string;
  timestamp: string;
  duration: number;
  success: boolean;
  error?: string;
  sessionKey: string;
  inputSize?: number;
  outputSize?: number;
}

export interface ToolMetrics {
  toolName: string;
  totalExecutions: number;
  successCount: number;
  failureCount: number;
  successRate: number;
  averageDuration: number;
  medianDuration: number;
  minDuration: number;
  maxDuration: number;
  lastExecuted: string;
  firstExecuted: string;
  popularityScore: number;
}

export interface ToolRecommendation {
  toolName: string;
  confidence: number;
  reason: string;
  metrics: ToolMetrics;
}

export class ToolAnalyticsService {
  private analyticsDir: string;
  private executionsLog: string;
  private metricsCache: Map<string, ToolMetrics>;
  private cacheExpiry: number = 60000; // 1 minute
  private lastCacheUpdate: number = 0;

  constructor(analyticsDir: string = './analytics/tools') {
    this.analyticsDir = analyticsDir;
    this.executionsLog = path.join(analyticsDir, 'executions.jsonl');
    this.metricsCache = new Map();

    this.ensureAnalyticsDirectory();
  }

  private ensureAnalyticsDirectory(): void {
    if (!fs.existsSync(this.analyticsDir)) {
      fs.mkdirSync(this.analyticsDir, { recursive: true });
    }
  }

  /**
   * Track a tool execution
   */
  trackExecution(execution: ToolExecution): void {
    fs.appendFileSync(this.executionsLog, JSON.stringify(execution) + '\n');

    // Invalidate cache for this tool
    this.metricsCache.delete(execution.toolName);
  }

  /**
   * Get metrics for a specific tool
   */
  getToolMetrics(toolName: string): ToolMetrics | null {
    // Check cache
    if (this.metricsCache.has(toolName) && Date.now() - this.lastCacheUpdate < this.cacheExpiry) {
      return this.metricsCache.get(toolName)!;
    }

    const executions = this.getExecutions({ toolName });

    if (executions.length === 0) {
      return null;
    }

    const metrics = this.calculateMetrics(toolName, executions);
    this.metricsCache.set(toolName, metrics);

    return metrics;
  }

  /**
   * Get all tool metrics
   */
  getAllMetrics(): ToolMetrics[] {
    const executions = this.getExecutions({});
    const toolNames = new Set(executions.map(e => e.toolName));

    const allMetrics: ToolMetrics[] = [];

    toolNames.forEach(toolName => {
      const toolExecutions = executions.filter(e => e.toolName === toolName);
      const metrics = this.calculateMetrics(toolName, toolExecutions);
      allMetrics.push(metrics);
    });

    // Sort by popularity
    return allMetrics.sort((a, b) => b.popularityScore - a.popularityScore);
  }

  /**
   * Get top performing tools
   */
  getTopTools(limit: number = 10): ToolMetrics[] {
    return this.getAllMetrics().slice(0, limit);
  }

  /**
   * Get recommendations based on context
   */
  getRecommendations(context: {
    taskDescription?: string;
    recentTools?: string[];
    sessionKey?: string;
  }): ToolRecommendation[] {
    const allMetrics = this.getAllMetrics();
    const recommendations: ToolRecommendation[] = [];

    // Strategy 1: High success rate + high usage
    allMetrics.forEach(metrics => {
      if (metrics.totalExecutions >= 5 && metrics.successRate >= 0.8) {
        recommendations.push({
          toolName: metrics.toolName,
          confidence: metrics.successRate * (metrics.popularityScore / 100),
          reason: `High success rate (${(metrics.successRate * 100).toFixed(1)}%) with ${metrics.totalExecutions} uses`,
          metrics
        });
      }
    });

    // Strategy 2: Recently used successfully in this session
    if (context.sessionKey) {
      const recentSuccessful = this.getExecutions({
        sessionKey: context.sessionKey,
        success: true
      }).slice(-5);

      recentSuccessful.forEach(exec => {
        const metrics = this.getToolMetrics(exec.toolName);
        if (metrics && !recommendations.find(r => r.toolName === exec.toolName)) {
          recommendations.push({
            toolName: exec.toolName,
            confidence: 0.9,
            reason: 'Recently used successfully in this session',
            metrics
          });
        }
      });
    }

    // Strategy 3: Similar to recent tools (basic similarity)
    if (context.recentTools && context.recentTools.length > 0) {
      const recentTool = context.recentTools[context.recentTools.length - 1];
      const similarTools = this.findSimilarTools(recentTool);

      similarTools.forEach(toolName => {
        const metrics = this.getToolMetrics(toolName);
        if (metrics && !recommendations.find(r => r.toolName === toolName)) {
          recommendations.push({
            toolName,
            confidence: 0.7,
            reason: `Similar to recently used tool: ${recentTool}`,
            metrics
          });
        }
      });
    }

    // Sort by confidence
    return recommendations.sort((a, b) => b.confidence - a.confidence);
  }

  /**
   * Find similar tools (basic name-based similarity)
   */
  private findSimilarTools(toolName: string): string[] {
    const allMetrics = this.getAllMetrics();
    const similar: string[] = [];

    // Extract base name (e.g., create_excel -> excel)
    const baseName = toolName.replace(/^(create_|read_|update_|delete_|get_|set_)/, '');

    allMetrics.forEach(metrics => {
      if (metrics.toolName !== toolName && metrics.toolName.includes(baseName)) {
        similar.push(metrics.toolName);
      }
    });

    return similar;
  }

  /**
   * Get executions with filters
   */
  private getExecutions(filters: {
    toolName?: string;
    sessionKey?: string;
    success?: boolean;
    since?: number;
  }): ToolExecution[] {
    if (!fs.existsSync(this.executionsLog)) {
      return [];
    }

    const lines = fs.readFileSync(this.executionsLog, 'utf-8')
      .split('\n')
      .filter(l => l.trim());

    const executions: ToolExecution[] = [];

    lines.forEach(line => {
      try {
        const execution: ToolExecution = JSON.parse(line);

        // Apply filters
        if (filters.toolName && execution.toolName !== filters.toolName) return;
        if (filters.sessionKey && execution.sessionKey !== filters.sessionKey) return;
        if (filters.success !== undefined && execution.success !== filters.success) return;
        if (filters.since && new Date(execution.timestamp).getTime() < filters.since) return;

        executions.push(execution);
      } catch (err) {
        // Skip invalid lines
      }
    });

    return executions;
  }

  /**
   * Calculate metrics from executions
   */
  private calculateMetrics(toolName: string, executions: ToolExecution[]): ToolMetrics {
    const successCount = executions.filter(e => e.success).length;
    const failureCount = executions.length - successCount;
    const durations = executions.map(e => e.duration).sort((a, b) => a - b);

    // Calculate popularity score (recency-weighted)
    const now = Date.now();
    const popularityScore = executions.reduce((score, exec) => {
      const age = now - new Date(exec.timestamp).getTime();
      const ageInDays = age / (1000 * 60 * 60 * 24);
      const recencyWeight = Math.max(0, 1 - (ageInDays / 30)); // Decay over 30 days
      return score + recencyWeight;
    }, 0);

    return {
      toolName,
      totalExecutions: executions.length,
      successCount,
      failureCount,
      successRate: executions.length > 0 ? successCount / executions.length : 0,
      averageDuration: durations.reduce((a, b) => a + b, 0) / durations.length,
      medianDuration: durations[Math.floor(durations.length / 2)] || 0,
      minDuration: durations[0] || 0,
      maxDuration: durations[durations.length - 1] || 0,
      lastExecuted: executions[executions.length - 1].timestamp,
      firstExecuted: executions[0].timestamp,
      popularityScore
    };
  }

  /**
   * Get usage trends over time
   */
  getUsageTrends(toolName: string, bucketSize: 'hour' | 'day' | 'week' = 'day'): {
    timestamp: string;
    count: number;
    successRate: number;
  }[] {
    const executions = this.getExecutions({ toolName });

    const buckets = new Map<string, ToolExecution[]>();

    executions.forEach(exec => {
      const timestamp = new Date(exec.timestamp);
      let bucketKey: string;

      switch (bucketSize) {
        case 'hour':
          bucketKey = `${timestamp.getFullYear()}-${timestamp.getMonth()}-${timestamp.getDate()}-${timestamp.getHours()}`;
          break;
        case 'week':
          const weekNum = Math.floor(timestamp.getTime() / (1000 * 60 * 60 * 24 * 7));
          bucketKey = `week-${weekNum}`;
          break;
        case 'day':
        default:
          bucketKey = `${timestamp.getFullYear()}-${timestamp.getMonth() + 1}-${timestamp.getDate()}`;
      }

      if (!buckets.has(bucketKey)) {
        buckets.set(bucketKey, []);
      }
      buckets.get(bucketKey)!.push(exec);
    });

    const trends: { timestamp: string; count: number; successRate: number }[] = [];

    buckets.forEach((execs, key) => {
      const successCount = execs.filter(e => e.success).length;
      trends.push({
        timestamp: key,
        count: execs.length,
        successRate: successCount / execs.length
      });
    });

    return trends.sort((a, b) => a.timestamp.localeCompare(b.timestamp));
  }

  /**
   * Clear old analytics data
   */
  clearOldData(daysToKeep: number = 30): void {
    const cutoffTime = Date.now() - (daysToKeep * 24 * 60 * 60 * 1000);
    const executions = this.getExecutions({ since: cutoffTime });

    // Rewrite log with only recent data
    const tempLog = this.executionsLog + '.tmp';
    executions.forEach(exec => {
      fs.appendFileSync(tempLog, JSON.stringify(exec) + '\n');
    });

    fs.renameSync(tempLog, this.executionsLog);
    this.metricsCache.clear();
  }

  /**
   * Export metrics to JSON
   */
  exportMetrics(): string {
    const metrics = this.getAllMetrics();
    const exportData = {
      exportedAt: new Date().toISOString(),
      totalTools: metrics.length,
      totalExecutions: metrics.reduce((sum, m) => sum + m.totalExecutions, 0),
      metrics
    };

    const exportPath = path.join(this.analyticsDir, `metrics-export-${Date.now()}.json`);
    fs.writeFileSync(exportPath, JSON.stringify(exportData, null, 2));

    return exportPath;
  }
}
