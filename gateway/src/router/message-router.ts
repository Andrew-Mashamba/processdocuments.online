/**
 * Hybrid Message Router
 * Combines OpenClaw's multi-channel routing with ZIMA's intelligence
 */

import { createHash } from 'crypto';
import { v4 as uuidv4 } from 'uuid';
import { SessionKeyBuilder } from './session-key-builder';
import { HybridContextManager } from '../context/hybrid-context-manager';
import { GatewayConfig, RawChannelMessage, NormalizedMessage, MessageRequest, MessageResponse } from '../types';
import { getRequestLogger } from '../observability/request-logger';

export class HybridMessageRouter {
  private sessionKeyBuilder: SessionKeyBuilder;
  private dedupCache: Map<string, any>;
  private config: GatewayConfig;
  private contextManager: HybridContextManager;

  constructor(config: GatewayConfig) {
    this.config = config;
    this.sessionKeyBuilder = new SessionKeyBuilder();
    this.dedupCache = new Map();
    this.contextManager = new HybridContextManager(config);

    // Clean up old dedup entries every 5 minutes
    setInterval(() => this.cleanupDedupCache(), 300000);
  }

  /**
   * Route incoming message from any channel
   */
  async routeMessage(rawMessage: RawChannelMessage, contextManager?: any): Promise<MessageResponse> {
    console.log('\n=== Message Router: Processing Message ===');
    console.log('Channel:', rawMessage.channel || 'webchat');
    console.log('Message:', (rawMessage.message || rawMessage.text || rawMessage.body || '').substring(0, 100));

    // 1. Normalize message from channel
    const normalized = this.normalizeChannelMessage(rawMessage);
    console.log('Session Key Format:', this.sessionKeyBuilder.build({
      agentId: normalized.agentId || 'main',
      channel: normalized.channel,
      chatType: normalized.chatType,
      accountId: normalized.senderId,
      threadId: normalized.threadId
    }));

    // 2. Build session key (OpenClaw format)
    const sessionKey = this.sessionKeyBuilder.build({
      agentId: normalized.agentId || 'main',
      channel: normalized.channel,
      chatType: normalized.chatType,
      accountId: normalized.senderId,
      threadId: normalized.threadId
    });

    console.log('Session Key:', sessionKey);

    // 3. Idempotency check
    const idemKey = normalized.messageId || this.generateIdempotencyKey(normalized);
    console.log('Idempotency Key:', idemKey);

    if (this.dedupCache.has(idemKey)) {
      console.log('✓ Returning cached response (idempotency hit)');
      return this.dedupCache.get(idemKey);
    }

    // 4. Prepare request for context manager
    const request: MessageRequest = {
      sessionKey,
      message: normalized.text,
      attachments: normalized.attachments,
      sender: normalized.sender,
      channel: normalized.channel,
      metadata: normalized.metadata
    };

    // 5. Process through hybrid context manager
    console.log('✓ Dispatching to Hybrid Context Manager');
    const result = await this.contextManager.processMessage(request);

    // 6. Cache result
    this.dedupCache.set(idemKey, result);
    console.log('✓ Cached response for future requests');

    // Schedule cache cleanup for this specific key (5 minutes)
    setTimeout(() => {
      this.dedupCache.delete(idemKey);
      console.log('✓ Cleaned up cache entry:', idemKey.substring(0, 16) + '...');
    }, 300000);

    console.log('=== Message Router: Complete ===\n');
    return result;
  }

  /**
   * Route incoming message with streaming support
   */
  async routeMessageStream(
    rawMessage: RawChannelMessage,
    onChunk: (chunk: any) => void
  ): Promise<MessageResponse> {
    const reqLogger = getRequestLogger();

    console.log('\n=== Message Router: Processing Message (Streaming) ===');
    console.log('Channel:', rawMessage.channel || 'webchat');
    console.log('Message:', (rawMessage.message || rawMessage.text || rawMessage.body || '').substring(0, 100));

    // 1. Normalize message from channel
    const normalized = this.normalizeChannelMessage(rawMessage);

    reqLogger.log('MESSAGE_NORMALIZED', 'HybridMessageRouter', {
      originalMessage: rawMessage,
      normalized: {
        text: normalized.text,
        channel: normalized.channel,
        agentId: normalized.agentId,
        senderId: normalized.senderId
      }
    });

    // 2. Build session key (OpenClaw format)
    const sessionKey = this.sessionKeyBuilder.build({
      agentId: normalized.agentId || 'main',
      channel: normalized.channel,
      chatType: normalized.chatType,
      accountId: normalized.senderId,
      threadId: normalized.threadId
    });

    console.log('Session Key:', sessionKey);

    reqLogger.log('SESSION_KEY_BUILT', 'SessionKeyBuilder', {
      sessionKey,
      components: {
        agentId: normalized.agentId || 'main',
        channel: normalized.channel,
        chatType: normalized.chatType,
        accountId: normalized.senderId,
        threadId: normalized.threadId
      }
    }, sessionKey);

    // 3. Idempotency check (skip for streaming - always fresh)
    const idemKey = normalized.messageId || this.generateIdempotencyKey(normalized);
    console.log('Idempotency Key:', idemKey, '(streaming - no cache)');

    // 4. Prepare request for context manager
    const request: MessageRequest = {
      sessionKey,
      message: normalized.text,
      attachments: normalized.attachments,
      sender: normalized.sender,
      channel: normalized.channel,
      metadata: normalized.metadata
    };

    reqLogger.log('REQUEST_PREPARED', 'HybridMessageRouter', {
      request: {
        sessionKey,
        message: request.message,
        attachmentsCount: request.attachments.length,
        channel: request.channel
      }
    }, sessionKey);

    // 5. Process through hybrid context manager with streaming
    console.log('✓ Dispatching to Hybrid Context Manager (streaming)');
    const result = await this.contextManager.processMessageStream(request, onChunk);

    // 6. Cache final result
    this.dedupCache.set(idemKey, result);
    console.log('✓ Cached final response');

    reqLogger.log('RESPONSE_CACHED', 'HybridMessageRouter', {
      cacheKey: idemKey,
      result: {
        outputLength: result.output.length,
        model: result.model,
        complexity: result.complexity,
        tier: result.tier
      }
    }, sessionKey);

    // Schedule cache cleanup
    setTimeout(() => {
      this.dedupCache.delete(idemKey);
    }, 300000);

    console.log('=== Message Router: Complete (Streaming) ===\n');
    return result;
  }

  /**
   * Normalize message from different channel formats to unified structure
   */
  private normalizeChannelMessage(raw: RawChannelMessage): NormalizedMessage {
    // Extract text from various possible fields (including 'goal' for agent mode)
    const text = raw.message || raw.text || raw.body || (raw as any).goal || '';

    // Extract sender ID
    const senderId = raw.sender?.id || raw.from || raw.senderId || 'unknown';

    // Determine channel
    const channel = raw.channel || 'webchat';

    // Determine chat type
    const chatType = raw.chatType || 'direct';

    // Extract attachments
    const attachments = raw.attachments || raw.files || [];

    // Generate or use existing message ID
    const messageId = raw.messageId || raw.id;

    // Extract thread ID
    const threadId = raw.threadId;

    // Extract agent ID
    const agentId = raw.agentId;

    // Build sender info
    const sender = {
      channel,
      channelUserId: senderId,
      displayName: raw.sender?.name || 'Unknown User'
    };

    // Build metadata
    const metadata = {
      timestamp: Date.now(),
      ...(raw.metadata || {})
    };

    return {
      text,
      senderId,
      channel,
      chatType,
      attachments,
      messageId,
      threadId,
      agentId,
      sender,
      metadata
    };
  }

  /**
   * Generate idempotency key for message
   */
  private generateIdempotencyKey(message: NormalizedMessage): string {
    // Create hash from message content + sender + timestamp (rounded to 1 second)
    const timestamp = Math.floor(message.metadata.timestamp / 1000);
    const str = `${message.channel}:${message.senderId}:${message.text}:${timestamp}`;
    return createHash('sha256').update(str).digest('hex');
  }

  /**
   * Clean up old dedup cache entries
   */
  private cleanupDedupCache(): void {
    // In production, implement TTL-based cleanup
    // For now, we use setTimeout for individual entries
    console.log(`Dedup cache size: ${this.dedupCache.size} entries`);
  }

  /**
   * Get context manager stats
   */
  async getContextStats(): Promise<any> {
    return await this.contextManager.getStats();
  }

  /**
   * Get session key builder instance
   */
  getSessionKeyBuilder(): SessionKeyBuilder {
    return this.sessionKeyBuilder;
  }

  /**
   * Get dedup cache stats
   */
  getCacheStats(): { size: number; keys: string[] } {
    return {
      size: this.dedupCache.size,
      keys: Array.from(this.dedupCache.keys()).slice(0, 10) // First 10 keys
    };
  }
}
