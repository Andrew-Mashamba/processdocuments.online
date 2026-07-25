/**
 * Resource Pooling
 * Week 12: Production Polish - Optimization
 */

export interface Resource<T> {
  id: string;
  resource: T;
  inUse: boolean;
  createdAt: number;
  lastUsedAt: number;
  useCount: number;
}

export interface PoolOptions<T> {
  min: number;
  max: number;
  createResource: () => Promise<T>;
  destroyResource?: (resource: T) => Promise<void>;
  validateResource?: (resource: T) => Promise<boolean>;
  maxIdleTime?: number; // ms
  maxLifetime?: number; // ms
}

export class ResourcePool<T> {
  private resources: Map<string, Resource<T>> = new Map();
  private options: Required<PoolOptions<T>>;
  private idCounter: number = 0;
  private cleanupInterval: NodeJS.Timeout | null = null;

  constructor(options: PoolOptions<T>) {
    this.options = {
      min: options.min,
      max: options.max,
      createResource: options.createResource,
      destroyResource: options.destroyResource || (async () => {}),
      validateResource: options.validateResource || (async () => true),
      maxIdleTime: options.maxIdleTime || 60000, // 1 minute
      maxLifetime: options.maxLifetime || 3600000 // 1 hour
    };

    // Start cleanup task
    this.cleanupInterval = setInterval(() => this.cleanup(), 30000); // Every 30 seconds
  }

  /**
   * Initialize pool with minimum resources
   */
  async initialize(): Promise<void> {
    const promises: Promise<void>[] = [];

    for (let i = 0; i < this.options.min; i++) {
      promises.push(this.createNewResource());
    }

    await Promise.all(promises);
  }

  /**
   * Acquire a resource from the pool
   */
  async acquire(): Promise<{ resource: T; release: () => void }> {
    // Try to find an available resource
    for (const [id, res] of this.resources.entries()) {
      if (!res.inUse) {
        // Validate resource
        const isValid = await this.options.validateResource(res.resource);

        if (isValid) {
          res.inUse = true;
          res.lastUsedAt = Date.now();
          res.useCount++;

          return {
            resource: res.resource,
            release: () => this.release(id)
          };
        } else {
          // Remove invalid resource
          await this.destroyResource(id);
        }
      }
    }

    // No available resource, create new one if under max
    if (this.resources.size < this.options.max) {
      await this.createNewResource();
      return this.acquire(); // Retry
    }

    // Pool exhausted, wait for a resource
    return this.waitForResource();
  }

  /**
   * Release a resource back to the pool
   */
  private release(id: string): void {
    const resource = this.resources.get(id);
    if (resource) {
      resource.inUse = false;
      resource.lastUsedAt = Date.now();
    }
  }

  /**
   * Create a new resource
   */
  private async createNewResource(): Promise<void> {
    const id = `resource-${++this.idCounter}`;
    const resource = await this.options.createResource();

    this.resources.set(id, {
      id,
      resource,
      inUse: false,
      createdAt: Date.now(),
      lastUsedAt: Date.now(),
      useCount: 0
    });
  }

  /**
   * Wait for a resource to become available
   */
  private async waitForResource(): Promise<{ resource: T; release: () => void }> {
    return new Promise((resolve) => {
      const checkInterval = setInterval(() => {
        for (const [id, res] of this.resources.entries()) {
          if (!res.inUse) {
            clearInterval(checkInterval);

            res.inUse = true;
            res.lastUsedAt = Date.now();
            res.useCount++;

            resolve({
              resource: res.resource,
              release: () => this.release(id)
            });
            return;
          }
        }
      }, 100); // Check every 100ms
    });
  }

  /**
   * Destroy a resource
   */
  private async destroyResource(id: string): Promise<void> {
    const resource = this.resources.get(id);
    if (resource) {
      await this.options.destroyResource(resource.resource);
      this.resources.delete(id);
    }
  }

  /**
   * Cleanup idle and expired resources
   */
  private async cleanup(): Promise<void> {
    const now = Date.now();
    const toRemove: string[] = [];

    for (const [id, res] of this.resources.entries()) {
      if (res.inUse) continue;

      // Check idle time
      const idleTime = now - res.lastUsedAt;
      if (idleTime > this.options.maxIdleTime && this.resources.size > this.options.min) {
        toRemove.push(id);
        continue;
      }

      // Check lifetime
      const lifetime = now - res.createdAt;
      if (lifetime > this.options.maxLifetime) {
        toRemove.push(id);
      }
    }

    // Remove expired resources
    for (const id of toRemove) {
      await this.destroyResource(id);
    }

    // Ensure minimum resources
    while (this.resources.size < this.options.min) {
      await this.createNewResource();
    }
  }

  /**
   * Get pool stats
   */
  getStats(): {
    total: number;
    inUse: number;
    idle: number;
    min: number;
    max: number;
  } {
    const inUse = Array.from(this.resources.values()).filter(r => r.inUse).length;

    return {
      total: this.resources.size,
      inUse,
      idle: this.resources.size - inUse,
      min: this.options.min,
      max: this.options.max
    };
  }

  /**
   * Drain pool (close all resources)
   */
  async drain(): Promise<void> {
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
      this.cleanupInterval = null;
    }

    const destroyPromises: Promise<void>[] = [];

    for (const id of this.resources.keys()) {
      destroyPromises.push(this.destroyResource(id));
    }

    await Promise.all(destroyPromises);
  }
}
