/**
 * Job Queue Manager - Redis + Bull integration
 * Week 9-10: Exec & Queue - Phase B
 */

import Bull, { Queue, Job, JobOptions, QueueOptions } from 'bull';
import Redis from 'ioredis';
import { EventEmitter } from 'events';

export interface JobData {
  type: string;
  payload: any;
  sessionKey?: string;
  createdAt: number;
}

export interface JobProcessor<T = any, R = any> {
  (job: Job<T>): Promise<R>;
}

export interface JobQueueConfig {
  redis: {
    host: string;
    port: number;
    password?: string;
    db?: number;
  };
  defaultJobOptions?: JobOptions;
  concurrency?: number;
}

export interface QueueInfo {
  name: string;
  waiting: number;
  active: number;
  completed: number;
  failed: number;
  delayed: number;
  paused: boolean;
}

export class JobQueueManager extends EventEmitter {
  private queues: Map<string, Queue> = new Map();
  private redis: Redis;
  private config: JobQueueConfig;
  private defaultConcurrency: number = 5;

  constructor(config: JobQueueConfig) {
    super();
    this.config = config;
    this.defaultConcurrency = config.concurrency || 5;

    // Create Redis client for health checks
    this.redis = new Redis({
      host: config.redis.host,
      port: config.redis.port,
      password: config.redis.password,
      db: config.redis.db || 0,
      retryStrategy: (times) => {
        const delay = Math.min(times * 50, 2000);
        return delay;
      }
    });

    this.redis.on('connect', () => {
      console.log('✓ Redis connected');
      this.emit('redis-connected');
    });

    this.redis.on('error', (error) => {
      console.error('❌ Redis error:', error.message);
      this.emit('redis-error', error);
    });
  }

  /**
   * Create or get a queue
   */
  async createQueue(name: string, options?: QueueOptions): Promise<Queue> {
    if (this.queues.has(name)) {
      return this.queues.get(name)!;
    }

    console.log(`📦 Creating queue: ${name}`);

    const queue = new Bull(name, {
      redis: {
        host: this.config.redis.host,
        port: this.config.redis.port,
        password: this.config.redis.password,
        db: this.config.redis.db || 0
      },
      defaultJobOptions: this.config.defaultJobOptions || {
        attempts: 3,
        backoff: {
          type: 'exponential',
          delay: 2000
        },
        removeOnComplete: 100, // Keep last 100 completed jobs
        removeOnFail: 50       // Keep last 50 failed jobs
      },
      ...options
    });

    // Setup event handlers
    queue.on('error', (error) => {
      console.error(`❌ Queue error (${name}):`, error.message);
      this.emit('queue-error', { queue: name, error });
    });

    queue.on('waiting', (jobId) => {
      this.emit('job-waiting', { queue: name, jobId });
    });

    queue.on('active', (job) => {
      console.log(`🔄 Job active: ${name}/${job.id}`);
      this.emit('job-active', { queue: name, jobId: job.id });
    });

    queue.on('completed', (job, result) => {
      console.log(`✓ Job completed: ${name}/${job.id}`);
      this.emit('job-completed', { queue: name, jobId: job.id, result });
    });

    queue.on('failed', (job, error) => {
      console.error(`❌ Job failed: ${name}/${job.id} - ${error.message}`);
      this.emit('job-failed', { queue: name, jobId: job.id, error: error.message });
    });

    queue.on('progress', (job, progress) => {
      this.emit('job-progress', { queue: name, jobId: job.id, progress });
    });

    this.queues.set(name, queue);

    console.log(`✓ Queue created: ${name}`);

    return queue;
  }

  /**
   * Add a job to a queue
   */
  async addJob<T = any>(
    queueName: string,
    data: T,
    options?: JobOptions
  ): Promise<Job<T>> {
    const queue = this.queues.get(queueName);

    if (!queue) {
      throw new Error(`Queue not found: ${queueName}. Create it first with createQueue()`);
    }

    console.log(`➕ Adding job to queue: ${queueName}`);

    const job = await queue.add(data, options);

    console.log(`✓ Job added: ${queueName}/${job.id}`);

    return job;
  }

  /**
   * Process jobs from a queue
   */
  async processQueue<T = any, R = any>(
    queueName: string,
    processor: JobProcessor<T, R>,
    concurrency?: number
  ): Promise<void> {
    const queue = this.queues.get(queueName);

    if (!queue) {
      throw new Error(`Queue not found: ${queueName}`);
    }

    const workerConcurrency = concurrency || this.defaultConcurrency;

    console.log(`⚙️  Processing queue: ${queueName} (concurrency: ${workerConcurrency})`);

    queue.process(workerConcurrency, async (job: Job<T>) => {
      console.log(`🔨 Processing job: ${queueName}/${job.id}`);

      try {
        const result = await processor(job);
        return result;
      } catch (error: any) {
        console.error(`❌ Job processing error: ${queueName}/${job.id}`, error.message);
        throw error;
      }
    });
  }

  /**
   * Get job by ID
   */
  async getJob(queueName: string, jobId: string): Promise<Job | null> {
    const queue = this.queues.get(queueName);

    if (!queue) {
      return null;
    }

    return await queue.getJob(jobId);
  }

  /**
   * Get queue statistics
   */
  async getQueueInfo(queueName: string): Promise<QueueInfo | null> {
    const queue = this.queues.get(queueName);

    if (!queue) {
      return null;
    }

    const [waiting, active, completed, failed, delayed] = await Promise.all([
      queue.getWaitingCount(),
      queue.getActiveCount(),
      queue.getCompletedCount(),
      queue.getFailedCount(),
      queue.getDelayedCount()
    ]);

    const isPaused = await queue.isPaused();

    return {
      name: queueName,
      waiting,
      active,
      completed,
      failed,
      delayed,
      paused: isPaused
    };
  }

  /**
   * Get all queue statistics
   */
  async getAllQueueInfo(): Promise<QueueInfo[]> {
    const infos: QueueInfo[] = [];

    for (const queueName of this.queues.keys()) {
      const info = await this.getQueueInfo(queueName);
      if (info) {
        infos.push(info);
      }
    }

    return infos;
  }

  /**
   * Pause a queue
   */
  async pauseQueue(queueName: string): Promise<boolean> {
    const queue = this.queues.get(queueName);

    if (!queue) {
      return false;
    }

    await queue.pause();
    console.log(`⏸️  Queue paused: ${queueName}`);

    return true;
  }

  /**
   * Resume a queue
   */
  async resumeQueue(queueName: string): Promise<boolean> {
    const queue = this.queues.get(queueName);

    if (!queue) {
      return false;
    }

    await queue.resume();
    console.log(`▶️  Queue resumed: ${queueName}`);

    return true;
  }

  /**
   * Clean old jobs from queue
   */
  async cleanQueue(
    queueName: string,
    age: number,
    status: 'completed' | 'failed' = 'completed'
  ): Promise<number> {
    const queue = this.queues.get(queueName);

    if (!queue) {
      return 0;
    }

    const jobs = await queue.clean(age, status);

    console.log(`🧹 Cleaned ${jobs.length} ${status} jobs from ${queueName}`);

    return jobs.length;
  }

  /**
   * Remove all jobs from queue
   */
  async emptyQueue(queueName: string): Promise<boolean> {
    const queue = this.queues.get(queueName);

    if (!queue) {
      return false;
    }

    await queue.empty();
    console.log(`🗑️  Queue emptied: ${queueName}`);

    return true;
  }

  /**
   * Close a specific queue
   */
  async closeQueue(queueName: string): Promise<boolean> {
    const queue = this.queues.get(queueName);

    if (!queue) {
      return false;
    }

    await queue.close();
    this.queues.delete(queueName);

    console.log(`✓ Queue closed: ${queueName}`);

    return true;
  }

  /**
   * Close all queues and Redis connection
   */
  async closeAll(): Promise<void> {
    console.log(`🔒 Closing all ${this.queues.size} queues...`);

    for (const [name, queue] of this.queues.entries()) {
      await queue.close();
      console.log(`✓ Closed queue: ${name}`);
    }

    this.queues.clear();

    await this.redis.quit();

    console.log('✓ All queues closed');
  }

  /**
   * Check Redis connection
   */
  async healthCheck(): Promise<boolean> {
    try {
      const result = await this.redis.ping();
      return result === 'PONG';
    } catch (error) {
      return false;
    }
  }

  /**
   * Get Redis info
   */
  async getRedisInfo(): Promise<string> {
    return await this.redis.info();
  }

  /**
   * List all queues
   */
  listQueues(): string[] {
    return Array.from(this.queues.keys());
  }
}

// Global instance
let jobQueueManager: JobQueueManager | null = null;

/**
 * Get global job queue manager instance
 */
export function getJobQueueManager(config?: JobQueueConfig): JobQueueManager {
  if (!jobQueueManager && config) {
    jobQueueManager = new JobQueueManager(config);
  }

  if (!jobQueueManager) {
    throw new Error('JobQueueManager not initialized. Provide config on first call.');
  }

  return jobQueueManager;
}

/**
 * Initialize job queue manager from environment
 */
export function initJobQueueFromEnv(): JobQueueManager {
  const redisUrl = process.env.REDIS_URL || 'redis://localhost:6379';
  const url = new URL(redisUrl);

  const config: JobQueueConfig = {
    redis: {
      host: url.hostname || 'localhost',
      port: parseInt(url.port) || 6379,
      password: url.password || undefined,
      db: parseInt(url.pathname.slice(1)) || 0
    },
    concurrency: parseInt(process.env.QUEUE_CONCURRENCY || '5')
  };

  return getJobQueueManager(config);
}

/**
 * Cleanup on process exit
 */
process.on('SIGINT', async () => {
  if (jobQueueManager) {
    await jobQueueManager.closeAll();
  }
  process.exit(0);
});

process.on('SIGTERM', async () => {
  if (jobQueueManager) {
    await jobQueueManager.closeAll();
  }
  process.exit(0);
});
