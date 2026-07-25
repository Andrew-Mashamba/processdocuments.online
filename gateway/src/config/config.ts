/**
 * Configuration Loader
 * Loads and manages gateway configuration
 */

import * as fs from 'fs-extra';
import * as path from 'path';
import { GatewayConfig } from '../types';

export class ConfigLoader {
  private config: GatewayConfig | null = null;
  private configPath: string;

  constructor(configPath?: string) {
    this.configPath = configPath || this.getDefaultConfigPath();
  }

  /**
   * Load configuration from file
   */
  async load(): Promise<GatewayConfig> {
    if (this.config) {
      return this.config;
    }

    // Check if config file exists
    if (!await fs.pathExists(this.configPath)) {
      console.log(`Config file not found at ${this.configPath}, creating default...`);
      await this.createDefaultConfig();
    }

    // Read and parse config
    const content = await fs.readFile(this.configPath, 'utf-8');
    this.config = JSON.parse(content) as GatewayConfig;

    // Merge with environment variables
    this.mergeEnvVars();

    // Validate config
    this.validate();

    return this.config;
  }

  /**
   * Get configuration (throws if not loaded)
   */
  get(): GatewayConfig {
    if (!this.config) {
      throw new Error('Configuration not loaded. Call load() first.');
    }
    return this.config;
  }

  /**
   * Get default config path
   */
  private getDefaultConfigPath(): string {
    // Try project root first
    const projectConfig = path.join(process.cwd(), 'config.json');
    if (fs.existsSync(projectConfig)) {
      return projectConfig;
    }

    // Try ~/.zima/config.json
    const homeConfig = path.join(process.env.HOME || '~', '.zima', 'config.json');
    return homeConfig;
  }

  /**
   * Create default configuration file
   */
  private async createDefaultConfig(): Promise<void> {
    const defaultConfig: GatewayConfig = {
      gateway: {
        port: 18789,
        bind: '0.0.0.0',
        cors: {
          origins: ['http://localhost:8000', 'http://localhost:3000']
        }
      },
      channels: {
        webchat: {
          enabled: true,
          laravelUrl: 'http://localhost:8000',
          webhookSecret: 'change-this-secret'
        },
        whatsapp: {
          enabled: false,
          sessionPath: './sessions/whatsapp',
          dmPairing: {
            mode: 'allow',
            allowedNumbers: []
          }
        },
        email: {
          enabled: false,
          imap: {
            host: 'imap.gmail.com',
            port: 993,
            user: 'your-email@gmail.com',
            password: 'your-app-password'
          },
          smtp: {
            host: 'smtp.gmail.com',
            port: 587,
            user: 'your-email@gmail.com',
            password: 'your-app-password'
          }
        }
      },
      zima: {
        apiUrl: 'http://localhost:5000',
        timeout: 120000
      },
      storage: {
        root: path.join(process.env.HOME || '~', '.zima'),
        transcripts: path.join(process.env.HOME || '~', '.zima', 'agents', 'main', 'sessions'),
        files: path.join(process.env.HOME || '~', '.zima', 'agents', 'main', 'files'),
        memory: path.join(process.env.HOME || '~', '.zima', 'agents', 'main', 'memory.db'),
        workspace: path.join(process.cwd(), 'workspace') // Project directory for git tracking
      },
      models: {
        simple: 'claude-3-5-haiku-20241022',
        standard: 'claude-sonnet-4-20250514',
        complex: 'claude-opus-4-20250514'
      },
      cache: {
        ttl: 3600000, // 1 hour
        maxSize: 1000
      },
      memory: {
        enabled: true,
        database: path.join(process.env.HOME || '~', '.zima', 'agents', 'main', 'memory.db'),
        embeddingProvider: 'openai',
        embeddingModel: 'text-embedding-3-small',
        chunkSize: 400,
        chunkOverlap: 80,
        syncInterval: 3600000, // 1 hour
        vectorWeight: 0.7 // 70% vector, 30% BM25
      }
    };

    // Ensure directory exists
    await fs.ensureDir(path.dirname(this.configPath));

    // Write config file
    await fs.writeJSON(this.configPath, defaultConfig, { spaces: 2 });

    console.log(`Created default configuration at ${this.configPath}`);
  }

  /**
   * Merge environment variables into config
   */
  private mergeEnvVars(): void {
    if (!this.config) return;

    // Gateway port
    if (process.env.GATEWAY_PORT) {
      this.config.gateway.port = parseInt(process.env.GATEWAY_PORT);
    }

    // ZIMA API URL
    if (process.env.ZIMA_API_URL) {
      this.config.zima.apiUrl = process.env.ZIMA_API_URL;
    }

    // Laravel URL
    if (process.env.LARAVEL_URL) {
      this.config.channels.webchat.laravelUrl = process.env.LARAVEL_URL;
    }

    // Storage root
    if (process.env.STORAGE_ROOT) {
      this.config.storage.root = process.env.STORAGE_ROOT;
      this.config.storage.transcripts = path.join(process.env.STORAGE_ROOT, 'agents', 'main', 'sessions');
      this.config.storage.files = path.join(process.env.STORAGE_ROOT, 'agents', 'main', 'files');
      this.config.storage.memory = path.join(process.env.STORAGE_ROOT, 'agents', 'main', 'memory.db');
    }

    // Workspace path (defaults to project directory for git tracking)
    if (process.env.WORKSPACE_PATH) {
      this.config.storage.workspace = process.env.WORKSPACE_PATH;
    }

    // Memory configuration
    if (process.env.MEMORY_ENABLED !== undefined) {
      if (this.config.memory) {
        this.config.memory.enabled = process.env.MEMORY_ENABLED === 'true';
      }
    }

    if (process.env.MEMORY_DATABASE && this.config.memory) {
      this.config.memory.database = process.env.MEMORY_DATABASE;
    }

    if (process.env.EMBEDDING_MODEL && this.config.memory) {
      this.config.memory.embeddingModel = process.env.EMBEDDING_MODEL;
    }
  }

  /**
   * Validate configuration
   */
  private validate(): void {
    if (!this.config) {
      throw new Error('Configuration is null');
    }

    // Validate required fields
    if (!this.config.gateway || !this.config.gateway.port) {
      throw new Error('Invalid configuration: gateway.port is required');
    }

    if (!this.config.zima || !this.config.zima.apiUrl) {
      throw new Error('Invalid configuration: zima.apiUrl is required');
    }

    if (!this.config.storage || !this.config.storage.root) {
      throw new Error('Invalid configuration: storage.root is required');
    }
  }

  /**
   * Ensure storage directories exist
   */
  async ensureStorageDirectories(): Promise<void> {
    if (!this.config) {
      throw new Error('Configuration not loaded');
    }

    await fs.ensureDir(this.config.storage.root);
    await fs.ensureDir(this.config.storage.transcripts);
    await fs.ensureDir(this.config.storage.files);

    // Create sessions.json if it doesn't exist
    const sessionsPath = path.join(path.dirname(this.config.storage.transcripts), 'sessions.json');
    if (!await fs.pathExists(sessionsPath)) {
      await fs.writeJSON(sessionsPath, { sessions: [] }, { spaces: 2 });
    }
  }

  /**
   * Get config file path
   */
  getConfigPath(): string {
    return this.configPath;
  }
}

// Singleton instance
let configLoader: ConfigLoader | null = null;

/**
 * Get global config loader instance
 */
export function getConfigLoader(): ConfigLoader {
  if (!configLoader) {
    configLoader = new ConfigLoader();
  }
  return configLoader;
}

/**
 * Load configuration (convenience function)
 */
export async function loadConfig(): Promise<GatewayConfig> {
  const loader = getConfigLoader();
  return await loader.load();
}
