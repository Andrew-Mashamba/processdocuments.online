/**
 * Sub-Agent Spawner - Background agent execution
 * Week 9-10: Communication - Phase C
 */

import { EventEmitter } from 'events';
import { v4 as uuidv4 } from 'uuid';
import { GatewayConfig } from '../types';
import { getJobQueueManager } from '../queue/job-queue';

export interface SpawnOptions {
  parentSessionKey?: string;
  timeout?: number;
  priority?: number;
  model?: string;
  systemPrompt?: string;
  tools?: string[];
  maxTokens?: number;
  temperature?: number;
}

export interface SpawnResult {
  success: boolean;
  agentId: string;
  jobId?: string;
  status: 'queued' | 'running' | 'completed' | 'failed';
  error?: string;
}

export interface AgentStatus {
  agentId: string;
  status: 'queued' | 'running' | 'completed' | 'failed';
  progress: number;
  jobId?: string;
  startedAt?: number;
  completedAt?: number;
  result?: any;
  error?: string;
}

export class SubAgentSpawner extends EventEmitter {
  private config: GatewayConfig;
  private agents: Map<string, AgentStatus> = new Map();

  constructor(config: GatewayConfig) {
    super();
    this.config = config;
  }

  /**
   * Spawn a new background agent
   */
  async spawn(
    task: string,
    options: SpawnOptions = {}
  ): Promise<SpawnResult> {
    const agentId = uuidv4();

    console.log(`🤖 Spawning sub-agent: ${agentId}`);
    console.log(`   Task: ${task.substring(0, 100)}...`);

    try {
      // Get job queue manager
      const queueManager = getJobQueueManager();

      // Create agent queue if it doesn't exist
      await queueManager.createQueue('agent-jobs');

      // Create agent status
      const status: AgentStatus = {
        agentId,
        status: 'queued',
        progress: 0
      };

      this.agents.set(agentId, status);

      // Queue the agent job
      const job = await queueManager.addJob('agent-jobs', {
        agentId,
        task,
        parentSessionKey: options.parentSessionKey,
        model: options.model || 'claude-3-5-sonnet-20241022',
        systemPrompt: options.systemPrompt,
        tools: options.tools,
        maxTokens: options.maxTokens || 4096,
        temperature: options.temperature || 1.0,
        timeout: options.timeout || 300000 // 5 minutes default
      }, {
        priority: options.priority || 1,
        attempts: 3,
        backoff: {
          type: 'exponential',
          delay: 5000
        }
      });

      // Update status with job ID
      status.jobId = job.id as string;

      console.log(`✓ Sub-agent queued: ${agentId} (job: ${job.id})`);

      this.emit('agent-spawn', { agentId, jobId: job.id });

      return {
        success: true,
        agentId,
        jobId: job.id as string,
        status: 'queued'
      };

    } catch (error: any) {
      console.error(`❌ Failed to spawn sub-agent:`, error.message);

      return {
        success: false,
        agentId,
        status: 'failed',
        error: error.message
      };
    }
  }

  /**
   * Get agent status
   */
  async getStatus(agentId: string): Promise<AgentStatus | null> {
    const status = this.agents.get(agentId);

    if (!status) {
      return null;
    }

    // If job is queued/running, check job status
    if (status.jobId && (status.status === 'queued' || status.status === 'running')) {
      try {
        const queueManager = getJobQueueManager();
        const job = await queueManager.getJob('agent-jobs', status.jobId);

        if (job) {
          const jobState = await job.getState();
          const progress = await job.progress();

          // Update status based on job state
          if (jobState === 'active') {
            status.status = 'running';
            status.progress = progress as number;
            if (!status.startedAt) {
              status.startedAt = Date.now();
            }
          } else if (jobState === 'completed') {
            status.status = 'completed';
            status.progress = 100;
            status.completedAt = Date.now();
            status.result = await job.returnvalue;
          } else if (jobState === 'failed') {
            status.status = 'failed';
            status.completedAt = Date.now();
            status.error = job.failedReason || 'Unknown error';
          }
        }
      } catch (error: any) {
        console.error(`Error checking agent status: ${error.message}`);
      }
    }

    return { ...status };
  }

  /**
   * Wait for agent to complete
   */
  async waitForCompletion(
    agentId: string,
    timeout?: number
  ): Promise<AgentStatus | null> {
    const startTime = Date.now();
    const maxWait = timeout || 300000; // 5 minutes default

    while (Date.now() - startTime < maxWait) {
      const status = await this.getStatus(agentId);

      if (!status) {
        return null;
      }

      if (status.status === 'completed' || status.status === 'failed') {
        return status;
      }

      // Wait 1 second before checking again
      await new Promise(resolve => setTimeout(resolve, 1000));
    }

    throw new Error(`Timeout waiting for agent ${agentId}`);
  }

  /**
   * Cancel a running agent
   */
  async cancel(agentId: string): Promise<boolean> {
    const status = this.agents.get(agentId);

    if (!status || !status.jobId) {
      return false;
    }

    try {
      const queueManager = getJobQueueManager();
      const job = await queueManager.getJob('agent-jobs', status.jobId);

      if (job) {
        await job.remove();
        status.status = 'failed';
        status.error = 'Cancelled by user';
        status.completedAt = Date.now();

        console.log(`✓ Agent cancelled: ${agentId}`);
        this.emit('agent-cancel', { agentId });

        return true;
      }

      return false;
    } catch (error: any) {
      console.error(`Error cancelling agent: ${error.message}`);
      return false;
    }
  }

  /**
   * List all agents
   */
  listAgents(): AgentStatus[] {
    return Array.from(this.agents.values());
  }

  /**
   * Clean up completed/failed agents
   */
  cleanup(olderThan?: number): number {
    const threshold = olderThan || 3600000; // 1 hour default
    const now = Date.now();
    let count = 0;

    for (const [agentId, status] of this.agents.entries()) {
      if (
        (status.status === 'completed' || status.status === 'failed') &&
        status.completedAt &&
        now - status.completedAt > threshold
      ) {
        this.agents.delete(agentId);
        count++;
      }
    }

    if (count > 0) {
      console.log(`✓ Cleaned up ${count} old agents`);
    }

    return count;
  }

  /**
   * Get statistics
   */
  getStats(): {
    total: number;
    queued: number;
    running: number;
    completed: number;
    failed: number;
  } {
    const stats = {
      total: this.agents.size,
      queued: 0,
      running: 0,
      completed: 0,
      failed: 0
    };

    for (const status of this.agents.values()) {
      switch (status.status) {
        case 'queued':
          stats.queued++;
          break;
        case 'running':
          stats.running++;
          break;
        case 'completed':
          stats.completed++;
          break;
        case 'failed':
          stats.failed++;
          break;
      }
    }

    return stats;
  }
}

// Global instance
let subAgentSpawner: SubAgentSpawner | null = null;

/**
 * Get global sub-agent spawner instance
 */
export function getSubAgentSpawner(config?: GatewayConfig): SubAgentSpawner {
  if (!subAgentSpawner && config) {
    subAgentSpawner = new SubAgentSpawner(config);
  }

  if (!subAgentSpawner) {
    throw new Error('SubAgentSpawner not initialized. Provide config on first call.');
  }

  return subAgentSpawner;
}
