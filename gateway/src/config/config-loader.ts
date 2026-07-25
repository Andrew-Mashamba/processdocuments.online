/**
 * Config Loader - Simple configuration loader for tests
 */

import * as path from 'path';
import * as fs from 'fs-extra';
import { GatewayConfig } from '../types';

export async function loadConfig(): Promise<GatewayConfig> {
  const workspaceDir = path.join(process.cwd(), 'workspace');
  const storageDir = path.join(process.cwd(), 'storage');
  const memoryDb = path.join(storageDir, 'memory.db');

  // Ensure directories exist
  await fs.ensureDir(workspaceDir);
  await fs.ensureDir(storageDir);
  await fs.ensureDir(path.join(storageDir, 'transcripts'));
  await fs.ensureDir(path.join(storageDir, 'files'));

  const config: GatewayConfig = {
    gateway: {
      port: parseInt(process.env.PORT || '5555'),
      bind: process.env.BIND || '0.0.0.0',
      cors: {
        origins: process.env.CORS_ORIGINS?.split(',') || ['*']
      }
    },
    channels: {
      webchat: {
        enabled: process.env.WEBCHAT_ENABLED === 'true'
      },
      whatsapp: {
        enabled: process.env.WHATSAPP_ENABLED === 'true'
      },
      email: {
        enabled: process.env.SMTP_HOST !== undefined
      }
    },
    zima: {
      apiUrl: process.env.ZIMA_API_URL || 'http://localhost:5000',
      timeout: parseInt(process.env.ZIMA_TIMEOUT || '30000')
    },
    storage: {
      root: storageDir,
      transcripts: path.join(storageDir, 'transcripts'),
      files: path.join(storageDir, 'files'),
      workspace: workspaceDir,
      memory: memoryDb
    },
    memory: {
      enabled: process.env.MEMORY_ENABLED !== 'false',
      database: memoryDb,
      embeddingProvider: 'openai',
      embeddingModel: process.env.EMBEDDING_MODEL || 'text-embedding-3-small',
      chunkSize: parseInt(process.env.MEMORY_CHUNK_SIZE || '400'),
      chunkOverlap: parseInt(process.env.MEMORY_CHUNK_OVERLAP || '80'),
      syncInterval: parseInt(process.env.MEMORY_SYNC_INTERVAL || '3600000'),
      vectorWeight: parseFloat(process.env.MEMORY_VECTOR_WEIGHT || '0.7')
    }
  };

  return config;
}
