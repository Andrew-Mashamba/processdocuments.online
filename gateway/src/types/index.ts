/**
 * ZIMA Hybrid Gateway - Type Definitions
 * Combining OpenClaw and ZIMA architectures
 */

// ========================================
// Core Message Types
// ========================================

export interface RawChannelMessage {
  message?: string;
  text?: string;
  body?: string;
  sender?: {
    id?: string;
    name?: string;
  };
  from?: string;
  senderId?: string;
  channel?: string;
  chatType?: 'direct' | 'group' | 'channel';
  attachments?: any[];
  files?: any[];
  messageId?: string;
  id?: string;
  threadId?: string;
  agentId?: string;
  metadata?: any;
}

export interface NormalizedMessage {
  text: string;
  senderId: string;
  channel: string;
  chatType: 'direct' | 'group' | 'channel';
  attachments: Attachment[];
  messageId?: string;
  threadId?: string;
  agentId?: string;
  sender: SenderInfo;
  metadata: MessageMetadata;
}

export interface SenderInfo {
  channel: string;
  channelUserId: string;
  displayName: string;
}

export interface MessageMetadata {
  timestamp: number;
  [key: string]: any;
}

export interface Attachment {
  type: string;
  url?: string;
  mimeType?: string;
  fileName?: string;
  content?: any;
}

// ========================================
// Session Key Types (OpenClaw format)
// ========================================

export interface SessionKeyParams {
  agentId: string;
  channel: string;
  chatType: string;
  accountId: string;
  threadId?: string;
}

export interface SessionKeyComponents {
  prefix: string;
  agentId: string;
  channel: string;
  chatType: string;
  accountId: string;
  threadId?: string;
}

export interface SessionEntry {
  sessionId: string;
  sessionKey: string;
  channel: string;
  chatType: 'direct' | 'group' | 'channel';
  accountId: string;
  threadId?: string;
  displayName?: string;
  subject?: string;
  lastChannel?: string;
  model?: string;
  sendPolicy?: 'allow' | 'deny';
  customThinking?: string;
  metadata: {
    createdAt: number;
    updatedAt?: number;
    firstMessage?: string;
    preview?: string;
  };
}

// ========================================
// Context Manager Types (ZIMA + OpenClaw)
// ========================================

export type TaskComplexity = 'simple' | 'standard' | 'complex';
export type ContextTier = 0 | 1 | 2 | 3;

export interface MessageRequest {
  sessionKey: string;
  message: string;
  attachments: Attachment[];
  sender: SenderInfo;
  channel: string;
  metadata: MessageMetadata;
}

export interface MessageResponse {
  output: string;
  usage: UsageInfo;
  model: string;
  files: GeneratedFile[];
  fromCache?: boolean;
  complexity?: TaskComplexity;
  tier?: ContextTier;
}

export interface UsageInfo {
  inputTokens?: number;
  outputTokens?: number;
  cost?: number;
  cacheRead?: number;
  cacheWrite?: number;
  fromCache?: boolean;
}

export interface GeneratedFile {
  name: string;
  url: string;
  path: string;
  size: number;
  mimeType: string;
}

// ========================================
// Agent Runtime Types
// ========================================

export interface AgentContext {
  sessionKey: string;
  model: string;
  systemPrompt: string;
  messages: TranscriptMessage[];
  newMessage: string;
  attachments: Attachment[];
  runId: string;
  complexity: TaskComplexity;
  tier: ContextTier;
  metadata: {
    channel: string;
    sender: SenderInfo;
    fileCount: number;
    memoryCount: number;
    messageCount: number;
  };
}

export interface AgentResult {
  output: string;
  usage: UsageInfo;
  model: string;
  files: GeneratedFile[];
  toolResults?: ToolResult[];
}

// ========================================
// Tool Types
// ========================================

export interface Tool {
  name: string;
  description: string;
  inputSchema: {
    type: string;
    properties: Record<string, any>;
    required?: string[];
  };
}

export interface ToolCall {
  name: string;
  input: Record<string, any>;
}

export interface ToolResult {
  success: boolean;
  output: string;
  files?: GeneratedFile[];
  error?: string;
}

// ========================================
// Transcript Types (OpenClaw format)
// ========================================

export interface TranscriptMessage {
  type: 'message' | 'tool_call' | 'tool_result' | 'summary';
  id?: string;
  timestamp: number;
  role?: 'user' | 'assistant' | 'system';
  content?: string;
  usage?: UsageInfo;
  model?: string;
  tool?: string;
  input?: any;
  result?: any;
  messageCount?: number;
  importance?: number;
}

// ========================================
// Memory Types (OpenClaw - LanceDB)
// ========================================

export interface Memory {
  id: string;
  text: string;
  vector?: number[];
  importance: number;
  category: string;
  createdAt: number;
}

export interface MemorySearchParams {
  query: string;
  sessionKey?: string;
  limit?: number;
  importance?: number;
}

// ========================================
// Configuration Types
// ========================================

export interface GatewayConfig {
  gateway: {
    port: number;
    bind: string;
    cors: {
      origins: string[];
    };
  };
  channels: {
    webchat: ChannelConfig;
    whatsapp: ChannelConfig;
    email: ChannelConfig;
  };
  zima: {
    apiUrl: string;
    timeout: number;
  };
  storage: {
    root: string;
    transcripts: string;
    files: string;
    memory?: string;
    workspace?: string; // OpenClaw-style workspace (SOUL.md, AGENTS.md, TOOLS.md, memory/)
  };
  models?: {
    simple: string;
    standard: string;
    complex: string;
  };
  cache?: {
    ttl: number;
    maxSize: number;
  };
  memory?: {
    enabled: boolean;
    database: string;
    embeddingProvider: 'openai';
    embeddingModel: string;
    chunkSize: number;
    chunkOverlap: number;
    syncInterval: number;
    vectorWeight: number; // Default: 0.7 (70% vector, 30% BM25)
  };
}

export interface ChannelConfig {
  enabled: boolean;
  [key: string]: any;
}

// ========================================
// Lock Types
// ========================================

export interface Lock {
  release: () => Promise<void>;
}

// ========================================
// Cache Types
// ========================================

export interface CachedResponse {
  response: MessageResponse;
  timestamp: number;
  cacheKey: string;
}

// ========================================
// WebSocket Types
// ========================================

export interface WebSocketMessage {
  type: string;
  payload: any;
  timestamp?: number;
}

export interface ChatDelta {
  runId: string;
  sessionKey: string;
  seq?: number;
  state: 'running' | 'streaming' | 'final' | 'error';
  delta?: string;
  output?: string;
  error?: string;
  usage?: UsageInfo;
  files?: GeneratedFile[];
  model?: string;
}

// ========================================
// Outbound Delivery Types
// ========================================

export interface OutboundSendParams {
  channel: string;
  to: string;
  message: string;
  media?: GeneratedFile[];
  metadata?: any;
}

export interface SendResult {
  success: boolean;
  channel: string;
  messageId?: string;
  error?: string;
}

// ========================================
// Skills Types
// ========================================

export interface Skill {
  name: string;
  description: string;
  category: string;
  steps: SkillStep[];
}

export interface SkillStep {
  tool: string;
  args: Record<string, any>;
  description?: string;
}

// ========================================
// Memory System Types (Week 7-8)
// ========================================

export interface FileRecord {
  id: number;
  path: string;
  hash: string;
  size: number;
  mtime: number;
  indexed_at: number;
}

export interface ChunkRecord {
  id: number;
  file_id: number;
  chunk_index: number;
  content: string;
  tokens: number;
  start_line: number;
  end_line: number;
}

export interface EmbeddingRecord {
  chunk_id: number;
  embedding: Float32Array;
  model: string;
  created_at: number;
}

export interface SearchResult {
  chunk_id: number;
  file_path: string;
  content: string;
  score: number;
  rank: number;
  start_line: number;
  end_line: number;
  tokens: number;
}

export interface SearchOptions {
  limit?: number;
  vectorWeight?: number;
  filePattern?: string;
  minScore?: number;
}

export interface IndexStats {
  totalFiles: number;
  totalChunks: number;
  totalEmbeddings: number;
  databaseSize: number;
  lastSync: number;
}

export interface MemoryStats {
  indexStats: IndexStats;
  searchLatency?: number;
  embeddingCoverage: number;
}
