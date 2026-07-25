/**
 * Hybrid Context Manager
 * Combines ZIMA's intelligence with OpenClaw's session management
 */

import { v4 as uuidv4 } from 'uuid';
import axios from 'axios';
import { getLockManager } from './session-lock-manager';
import { TranscriptManager } from './transcript-manager';
import { TaskClassifier } from './task-classifier';
import { getResponseCache } from './response-cache';
import { ContextTierOptimizer } from './context-tier-optimizer';
import { FileContextLoader } from './file-context-loader';
import { ClaudeCliRuntime } from '../agent/claude-cli-runtime';
import { OpenClawSystemPromptBuilder, PromptMode, RuntimeContext } from '../agent/openclaw-system-prompt';
import { TokenProcessor } from '../agent/token-processor';
import { getRequestLogger } from '../observability/request-logger';
import {
  GatewayConfig,
  MessageRequest,
  MessageResponse,
  TranscriptMessage,
  AgentContext,
  TaskComplexity,
  ContextTier
} from '../types';

export class HybridContextManager {
  private config: GatewayConfig;
  private transcriptManager: TranscriptManager;
  private taskClassifier: TaskClassifier;
  private tierOptimizer: ContextTierOptimizer;
  private fileLoader: FileContextLoader;
  private agentRuntime: ClaudeCliRuntime | null = null;
  private promptBuilder: OpenClawSystemPromptBuilder;

  constructor(config: GatewayConfig) {
    this.config = config;
    this.transcriptManager = new TranscriptManager(config.storage.transcripts);
    this.taskClassifier = new TaskClassifier(config);
    this.tierOptimizer = new ContextTierOptimizer({
      useLLMSummarization: true,
      apiKey: process.env.ANTHROPIC_API_KEY
    });
    this.fileLoader = new FileContextLoader(config.storage.files);
    this.promptBuilder = new OpenClawSystemPromptBuilder(config);

    // Initialize Claude CLI runtime
    try {
      this.agentRuntime = new ClaudeCliRuntime(config);
      console.log('✓ Claude CLI Runtime initialized');
      console.log('✓ OpenClaw System Prompt Builder initialized');
    } catch (error: any) {
      console.warn('⚠️  Could not initialize Claude CLI runtime:', error.message);
      console.log('✓ OpenClaw System Prompt Builder initialized (fallback mode)');
    }
  }

  /**
   * Process incoming message through hybrid context pipeline
   */
  async processMessage(request: MessageRequest): Promise<MessageResponse> {
    console.log('\n=== Hybrid Context Manager: Processing Message ===');
    console.log('Session:', request.sessionKey);
    console.log('Channel:', request.channel);
    console.log('Message:', request.message.substring(0, 100));

    // ========================================
    // STEP 1: ACQUIRE SESSION LOCK (OpenClaw)
    // ========================================
    const lockManager = getLockManager();
    const lock = await lockManager.acquireSessionWriteLock(request.sessionKey);

    try {
      // ========================================
      // STEP 2: LOAD SESSION TRANSCRIPT (OpenClaw)
      // ========================================
      const messages = await this.transcriptManager.readTranscript(request.sessionKey);
      const messageCount = messages.filter(m => m.type === 'message').length;

      console.log(`📜 Loaded ${messageCount} messages from transcript`);

      // ========================================
      // STEP 3: TASK CLASSIFICATION (ZIMA)
      // ========================================
      const complexity = this.taskClassifier.classifyTask(
        request.message,
        messageCount,
        request.attachments.length
      );

      const complexityStats = this.taskClassifier.getComplexityStats(complexity);
      console.log(`🎯 Task Complexity: ${complexity.toUpperCase()} (${complexityStats.costSavings})`);

      // ========================================
      // STEP 4: MODEL SELECTION (ZIMA)
      // ========================================
      const selectedModel = this.taskClassifier.selectModel(complexity);
      console.log(`🤖 Selected Model: ${selectedModel}`);

      // ========================================
      // STEP 5: RESPONSE CACHE CHECK (ZIMA)
      // ========================================
      const responseCache = getResponseCache();
      const cacheKey = responseCache.generateCacheKey(
        request.message,
        request.sessionKey,
        messageCount,
        request.attachments.length
      );

      const cachedResponse = responseCache.get(cacheKey);
      if (cachedResponse) {
        console.log('=== Context Manager: Returning Cached Response ===\n');
        return cachedResponse;
      }

      // ========================================
      // STEP 6: CONTEXT TIER OPTIMIZATION (ZIMA)
      // ========================================
      const tier = this.tierOptimizer.determineContextTier(messageCount, complexity);
      const optimizedMessages = await this.tierOptimizer.filterMessagesByTier(messages, tier);

      const tierStats = this.tierOptimizer.getTierStats(tier, messages.length, optimizedMessages.length);
      console.log(`📊 Context Tier: ${tier} - ${tierStats.description}`);
      console.log(`   Messages: ${tierStats.originalCount} → ${tierStats.filteredCount} (${tierStats.reduction} reduction)`);

      // ========================================
      // STEP 7: LOAD SESSION FILES (ZIMA)
      // ========================================
      const sessionFiles = await this.fileLoader.loadSessionFiles(request.sessionKey);
      const fileContext = this.fileLoader.buildFileContext(sessionFiles, tier);

      if (sessionFiles.length > 0) {
        const totalSize = sessionFiles.reduce((sum, f) => sum + f.size, 0);
        console.log(`📁 Loaded ${sessionFiles.length} files (${this.formatSize(totalSize)})`);
      }

      // ========================================
      // STEP 8: LOAD OR CREATE SESSION ENTRY
      // ========================================
      const sessionEntry = await this.transcriptManager.loadSessionEntry(request.sessionKey);

      // ========================================
      // STEP 8.5: IDENTIFY USER & UPDATE USER.md (Dynamic)
      // ========================================
      await this.identifyAndUpdateUser(request.sessionKey, request.sender);

      // ========================================
      // STEP 9: BUILD SYSTEM PROMPT (OpenClaw-style)
      // ========================================
      const systemPrompt = await this.buildOpenClawSystemPrompt({
        sessionKey: request.sessionKey,
        channel: request.channel,
        sender: request.sender,
        model: selectedModel,
        complexity,
        tier,
        messageCount
      });

      // ========================================
      // STEP 10: PREPARE AGENT CONTEXT
      // ========================================
      const agentContext: AgentContext = {
        sessionKey: request.sessionKey,
        model: selectedModel,
        systemPrompt,
        messages: optimizedMessages,
        newMessage: request.message,
        attachments: request.attachments,
        runId: uuidv4(),
        complexity,
        tier,
        metadata: {
          channel: request.channel,
          sender: request.sender,
          fileCount: sessionFiles.length,
          memoryCount: 0, // Will be populated in Week 7-8
          messageCount
        }
      };

      // ========================================
      // STEP 11: INVOKE HYBRID AGENT RUNTIME
      // ========================================
      console.log('🚀 Invoking Hybrid Agent Runtime...');
      const result = await this.invokeAgent(agentContext);

      // ========================================
      // STEP 12: PROCESS SPECIAL TOKENS (OpenClaw)
      // ========================================
      const processed = TokenProcessor.processResponse(result.output);

      if (processed.isSilentReply) {
        console.log('🔇 Silent reply detected (HEARTBEAT_OK)');
      }

      if (processed.replyTarget) {
        console.log(`📍 Reply target: ${processed.replyTarget}`);
      }

      if (processed.tokens.length > 0) {
        console.log(`🏷️  Special tokens detected: ${processed.tokens.join(', ')}`);
      }

      // ========================================
      // STEP 13: POST-PROCESSING
      // ========================================

      // Save user message to transcript
      const userMessage: TranscriptMessage = {
        type: 'message',
        role: 'user',
        content: request.message,
        timestamp: Date.now()
      };
      await this.transcriptManager.appendToTranscript(request.sessionKey, userMessage);

      // Save assistant response to transcript (with tokens stripped)
      const assistantMessage: TranscriptMessage = {
        type: 'message',
        role: 'assistant',
        content: processed.output, // Use processed output with tokens stripped
        timestamp: Date.now(),
        usage: result.usage,
        model: selectedModel
      };
      await this.transcriptManager.appendToTranscript(request.sessionKey, assistantMessage);

      // Cache response (with tokens stripped)
      const response: MessageResponse = {
        output: processed.output, // Use processed output
        usage: result.usage,
        model: selectedModel,
        files: result.files,
        complexity,
        tier
      };

      responseCache.set(cacheKey, response);

      // Check if compaction is needed
      const newMessageCount = messageCount + 2; // user + assistant
      if (newMessageCount > 100) {
        console.log('🗜️  Session needs compaction (>100 messages)');
        // Compact in background (don't wait)
        this.transcriptManager.compactTranscript(request.sessionKey, 20).catch(err => {
          console.error('Error compacting transcript:', err);
        });
      }

      console.log('=== Context Manager: Complete ===\n');
      return response;

    } finally {
      await lock.release();
    }
  }

  /**
   * Process incoming message through hybrid context pipeline with streaming
   */
  async processMessageStream(
    request: MessageRequest,
    onChunk: (chunk: any) => void
  ): Promise<MessageResponse> {
    const reqLogger = getRequestLogger();

    console.log('\n=== Hybrid Context Manager: Processing Message (Streaming) ===');
    console.log('Session:', request.sessionKey);
    console.log('Channel:', request.channel);
    console.log('Message:', request.message.substring(0, 100));

    // ========================================
    // STEP 1: ACQUIRE SESSION LOCK (OpenClaw)
    // ========================================
    const lockManager = getLockManager();

    reqLogger.log('ACQUIRING_LOCK', 'LockManager', {
      sessionKey: request.sessionKey,
      lockType: 'write'
    }, request.sessionKey);

    const lock = await lockManager.acquireSessionWriteLock(request.sessionKey);

    reqLogger.log('LOCK_ACQUIRED', 'LockManager', {
      sessionKey: request.sessionKey
    }, request.sessionKey);

    try {
      // ========================================
      // STEP 2: LOAD SESSION TRANSCRIPT (OpenClaw)
      // ========================================
      const messages = await this.transcriptManager.readTranscript(request.sessionKey);
      const messageCount = messages.filter(m => m.type === 'message').length;

      console.log(`📜 Loaded ${messageCount} messages from transcript`);

      reqLogger.log('TRANSCRIPT_LOADED', 'TranscriptManager', {
        sessionKey: request.sessionKey,
        totalMessages: messages.length,
        messageCount,
        lastMessage: messages.length > 0 ? messages[messages.length - 1] : null
      }, request.sessionKey);

      // ========================================
      // STEP 3: TASK CLASSIFICATION (ZIMA)
      // ========================================
      const complexity = this.taskClassifier.classifyTask(
        request.message,
        messageCount,
        request.attachments.length
      );

      const complexityStats = this.taskClassifier.getComplexityStats(complexity);
      console.log(`🎯 Task Complexity: ${complexity.toUpperCase()} (${complexityStats.costSavings})`);

      reqLogger.log('TASK_CLASSIFIED', 'TaskClassifier', {
        complexity,
        stats: complexityStats,
        messageCount,
        attachmentsCount: request.attachments.length
      }, request.sessionKey);

      // ========================================
      // STEP 4: MODEL SELECTION (ZIMA)
      // ========================================
      const selectedModel = this.taskClassifier.selectModel(complexity);
      console.log(`🤖 Selected Model: ${selectedModel}`);

      reqLogger.log('MODEL_SELECTED', 'TaskClassifier', {
        model: selectedModel,
        complexity
      }, request.sessionKey);

      // Skip cache for streaming - always fresh

      // ========================================
      // STEP 5: CONTEXT TIER OPTIMIZATION (ZIMA)
      // ========================================
      const tier = this.tierOptimizer.determineContextTier(messageCount, complexity);
      const optimizedMessages = await this.tierOptimizer.filterMessagesByTier(messages, tier);

      const tierStats = this.tierOptimizer.getTierStats(tier, messages.length, optimizedMessages.length);
      console.log(`📊 Context Tier: ${tier} - ${tierStats.description}`);

      reqLogger.log('CONTEXT_OPTIMIZED', 'TierOptimizer', {
        tier,
        stats: tierStats,
        originalMessagesCount: messages.length,
        optimizedMessagesCount: optimizedMessages.length
      }, request.sessionKey);

      // ========================================
      // STEP 6: LOAD SESSION FILES (ZIMA)
      // ========================================
      const sessionFiles = await this.fileLoader.loadSessionFiles(request.sessionKey);

      reqLogger.log('FILES_LOADED', 'FileLoader', {
        sessionKey: request.sessionKey,
        filesCount: sessionFiles.length,
        files: sessionFiles.map(f => ({ name: f.name, size: f.size }))
      }, request.sessionKey);

      // ========================================
      // STEP 7: LOAD OR CREATE SESSION ENTRY
      // ========================================
      await this.transcriptManager.loadSessionEntry(request.sessionKey);

      // ========================================
      // STEP 7.5: IDENTIFY USER & UPDATE USER.md (Dynamic)
      // ========================================
      await this.identifyAndUpdateUser(request.sessionKey, request.sender);

      // ========================================
      // STEP 8: BUILD SYSTEM PROMPT (OpenClaw-style)
      // ========================================
      const systemPrompt = await this.buildOpenClawSystemPrompt({
        sessionKey: request.sessionKey,
        channel: request.channel,
        sender: request.sender,
        model: selectedModel,
        complexity,
        tier,
        messageCount
      });

      reqLogger.log('SYSTEM_PROMPT_BUILT', 'OpenClawSystemPromptBuilder', {
        promptLength: systemPrompt.length,
        model: selectedModel,
        complexity,
        tier
      }, request.sessionKey);

      // ========================================
      // STEP 9: PREPARE AGENT CONTEXT
      // ========================================
      const agentContext: AgentContext = {
        sessionKey: request.sessionKey,
        model: selectedModel,
        systemPrompt,
        messages: optimizedMessages,
        newMessage: request.message,
        attachments: request.attachments,
        runId: uuidv4(),
        complexity,
        tier,
        metadata: {
          channel: request.channel,
          sender: request.sender,
          fileCount: sessionFiles.length,
          memoryCount: 0,
          messageCount
        }
      };

      reqLogger.log('AGENT_CONTEXT_PREPARED', 'HybridContextManager', {
        runId: agentContext.runId,
        model: agentContext.model,
        complexity: agentContext.complexity,
        tier: agentContext.tier,
        systemPromptLength: agentContext.systemPrompt.length,
        messagesCount: agentContext.messages.length,
        newMessage: agentContext.newMessage
      }, request.sessionKey);

      // ========================================
      // STEP 10: INVOKE AGENT WITH STREAMING
      // ========================================
      console.log('🚀 Invoking Hybrid Agent Runtime (streaming)...');

      reqLogger.log('INVOKING_AGENT', 'HybridContextManager', {
        runId: agentContext.runId,
        streaming: true
      }, request.sessionKey);

      const result = await this.invokeAgentStream(agentContext, onChunk);

      reqLogger.log('AGENT_COMPLETED', 'HybridContextManager', {
        runId: agentContext.runId,
        outputLength: result.output.length,
        usage: result.usage,
        filesCount: result.files.length
      }, request.sessionKey);

      // ========================================
      // STEP 11: PROCESS SPECIAL TOKENS
      // ========================================
      const processed = TokenProcessor.processResponse(result.output);

      reqLogger.log('TOKENS_PROCESSED', 'TokenProcessor', {
        originalLength: result.output.length,
        processedLength: processed.output.length,
        tokensFound: processed.output !== result.output
      }, request.sessionKey);

      // ========================================
      // STEP 12: POST-PROCESSING
      // ========================================
      const userMessage: TranscriptMessage = {
        type: 'message',
        role: 'user',
        content: request.message,
        timestamp: Date.now()
      };
      await this.transcriptManager.appendToTranscript(request.sessionKey, userMessage);

      const assistantMessage: TranscriptMessage = {
        type: 'message',
        role: 'assistant',
        content: processed.output,
        timestamp: Date.now(),
        usage: result.usage,
        model: selectedModel
      };
      await this.transcriptManager.appendToTranscript(request.sessionKey, assistantMessage);

      reqLogger.log('TRANSCRIPT_SAVED', 'TranscriptManager', {
        sessionKey: request.sessionKey,
        userMessageLength: userMessage.content?.length || 0,
        assistantMessageLength: assistantMessage.content?.length || 0,
        usage: result.usage,
        model: selectedModel
      }, request.sessionKey);

      const response: MessageResponse = {
        output: processed.output,
        usage: result.usage,
        model: selectedModel,
        files: result.files,
        complexity,
        tier
      };

      console.log('=== Context Manager: Complete (Streaming) ===\n');
      return response;

    } finally {
      await lock.release();
    }
  }

  /**
   * Identify user from session and update USER.md
   */
  private async identifyAndUpdateUser(sessionKey: string, sender: any): Promise<void> {
    try {
      // Extract user ID from session key (format: agent:main:webchat:direct:{userId})
      const parts = sessionKey.split(':');
      const userId = parts.length >= 5 ? parts[4] : null;

      // Check if it's a numeric user ID (not "unknown" or other placeholder)
      const isNumericId = userId && /^\d+$/.test(userId);

      if (!isNumericId) {
        console.log('👤 Guest user detected (no numeric user_id in session)');
        await this.updateUserMdForGuest();
        return;
      }

      console.log(`🔎 Identifying user ${userId} from database...`);

      // Query user from Laravel database
      const Database = require('better-sqlite3');
      const path = require('path');
      const fs = require('fs');

      const dbPath = path.resolve(__dirname, '../../zima-frontend/database/database.sqlite');

      if (!fs.existsSync(dbPath)) {
        console.error(`Database not found at: ${dbPath}`);
        await this.updateUserMdForGuest();
        return;
      }

      const db = new Database(dbPath, { readonly: true });

      const user = db.prepare(`
        SELECT
          id,
          name,
          email,
          gender,
          phone,
          country,
          timezone,
          bio,
          is_admin,
          preferences
        FROM users
        WHERE id = ?
      `).get(userId);

      db.close();

      if (!user) {
        console.error(`❌ User ${userId} not found in database`);
        await this.updateUserMdForGuest();
        return;
      }

      console.log(`✅ Found user: ${user.name} (${user.email})`);
      await this.updateUserMdForUser(user);
    } catch (error) {
      console.error('Error identifying user:', error);
      await this.updateUserMdForGuest();
    }
  }

  /**
   * Update USER.md for authenticated user
   */
  private async updateUserMdForUser(user: any): Promise<void> {
    const fs = require('fs/promises');
    const path = require('path');

    // Parse preferences if it's a JSON string
    let preferences: Record<string, any> = {};
    if (user.preferences) {
      try {
        preferences = typeof user.preferences === 'string'
          ? JSON.parse(user.preferences)
          : user.preferences;
      } catch {
        preferences = {};
      }
    }

    const genderLabel = user.gender
      ? user.gender.replace('_', ' ').replace(/\b\w/g, (l: string) => l.toUpperCase())
      : 'Not specified';

    const content = `# USER.md - Who I'm Helping

## ${user.name}

**User ID**: ${user.id}
**Email**: ${user.email}
**Gender**: ${genderLabel}
${user.phone ? `**Phone**: ${user.phone}` : ''}
${user.country ? `**Country**: ${user.country}` : ''}
${user.timezone ? `**Timezone**: ${user.timezone}` : ''}
**Admin**: ${user.is_admin ? 'Yes' : 'No'}

${user.bio ? `## Bio\n\n${user.bio}\n` : ''}

## Preferences

${Object.keys(preferences).length > 0
  ? Object.entries(preferences)
      .map(([key, value]) => `- **${key}**: ${value}`)
      .join('\n')
  : 'No preferences set yet.'
}

## Context

This user is registered and authenticated. Treat them as a valued returning user.
Personalize interactions based on their profile information.

---

*This file is auto-generated. Last updated: ${new Date().toISOString()}*
`;

    const userMdPath = path.resolve(this.config.storage.workspace || path.join(process.cwd(), 'workspace'), 'USER.md');
    await fs.writeFile(userMdPath, content, 'utf-8');
    console.log(`✅ USER.md updated: ${userMdPath}`);
  }

  /**
   * Update USER.md for guest user
   */
  private async updateUserMdForGuest(): Promise<void> {
    const fs = require('fs/promises');
    const path = require('path');

    const content = `# USER.md - Who I'm Helping

## Guest User

**Status**: Anonymous Guest
**Name**: Guest
**Context**: No user profile available (guest session)

## Notes

This is an anonymous session. The user has not logged in or registered.
Once they register or log in, this file will be updated with their profile information.

---

*This file is auto-generated. Last updated: ${new Date().toISOString()}*
`;

    const userMdPath = path.resolve(this.config.storage.workspace || path.join(process.cwd(), 'workspace'), 'USER.md');
    await fs.writeFile(userMdPath, content, 'utf-8');
    console.log(`✅ USER.md updated for guest: ${userMdPath}`);
  }

  /**
   * Build OpenClaw-style system prompt with workspace context
   */
  private async buildOpenClawSystemPrompt(params: {
    sessionKey: string;
    channel: string;
    sender: any;
    model: string;
    complexity: TaskComplexity;
    tier: ContextTier;
    messageCount: number;
  }): Promise<string> {
    // Get tools from agent runtime (or empty array if not available)
    let tools: any[] = [];
    if (this.agentRuntime) {
      try {
        const registry = this.agentRuntime.getRegistry();
        if (registry) {
          tools = await registry.getTools();
        }
      } catch (error) {
        console.warn('Could not load tools for system prompt:', error);
      }
    }

    // Build runtime context
    const runtime: RuntimeContext = {
      sessionKey: params.sessionKey,
      channel: params.channel,
      senderId: params.sender?.id || 'unknown',
      model: params.model,
      tools,
      capabilities: [
        'document_processing',
        'web_research',
        'file_operations',
        'session_management',
        'memory_system'
      ],
      timestamp: new Date(),
      agentId: 'main'
    };

    // Determine prompt mode based on context tier and complexity
    let mode: PromptMode = 'full';
    if (params.tier >= 3 || params.messageCount < 3) {
      mode = 'minimal'; // Use minimal for early conversations or heavily optimized contexts
    }

    // Build the complete system prompt using OpenClaw architecture
    const systemPrompt = await this.promptBuilder.buildSystemPrompt(runtime, mode);

    // Add context tier info if optimized
    if (params.tier > 0) {
      const tierDesc = this.tierOptimizer.getTierDescription(params.tier);
      return systemPrompt + `\n\n<context_optimization>\nContext has been optimized (${tierDesc}) for efficiency. Message count: ${params.messageCount}\n</context_optimization>`;
    }

    return systemPrompt;
  }

  /**
   * Invoke Hybrid Agent Runtime with tool calling
   */
  private async invokeAgent(context: AgentContext): Promise<{
    output: string;
    usage: any;
    files: any[];
  }> {
    // Use agent runtime if available
    if (this.agentRuntime) {
      try {
        const result = await this.agentRuntime.execute(context);
        return {
          output: result.output,
          usage: result.usage,
          files: result.files
        };
      } catch (error: any) {
        console.error('❌ Error in Claude CLI runtime:', error.message);

        // Fallback to direct ZIMA Core call if agent runtime fails
        return await this.fallbackToZimaCore(context);
      }
    }

    // Fallback if agent runtime not available
    return await this.fallbackToZimaCore(context);
  }

  /**
   * Invoke agent with streaming support
   */
  private async invokeAgentStream(
    context: AgentContext,
    onChunk: (chunk: any) => void
  ): Promise<{
    output: string;
    usage: any;
    files: any[];
  }> {
    // Use agent runtime if available with streaming
    if (this.agentRuntime) {
      try {
        const result = await this.agentRuntime.executeStream(context, (streamChunk) => {
          // Transform Claude CLI stream chunk to SSE format
          if (streamChunk.type === 'stream_event' && streamChunk.event) {
            if (streamChunk.event.type === 'content_block_delta' && streamChunk.event.delta?.text) {
              onChunk({
                type: 'content',
                content: streamChunk.event.delta.text,
                timestamp: Date.now()
              });
            }
          }
        });
        return {
          output: result.output,
          usage: result.usage,
          files: result.files
        };
      } catch (error: any) {
        console.error('❌ Error in Claude CLI runtime (streaming):', error.message);
        // Fallback to non-streaming ZIMA Core
        return await this.fallbackToZimaCore(context);
      }
    }

    // Fallback if agent runtime not available
    return await this.fallbackToZimaCore(context);
  }

  /**
   * Fallback: Direct ZIMA Core call (no tool calling)
   */
  private async fallbackToZimaCore(context: AgentContext): Promise<{
    output: string;
    usage: any;
    files: any[];
  }> {
    try {
      // Build messages array for ZIMA Core
      const messages = context.messages
        .filter(m => m.type === 'message')
        .map(m => ({
          role: m.role,
          content: m.content
        }));

      // Add new user message
      messages.push({
        role: 'user',
        content: context.newMessage
      });

      // Call ZIMA Core
      const response = await axios.post(
        `${this.config.zima.apiUrl}/api/generate`,
        {
          prompt: context.newMessage,
          messages,
          sessionId: context.sessionKey,
          model: context.model,
          systemPrompt: context.systemPrompt
        },
        {
          timeout: this.config.zima.timeout
        }
      );

      return {
        output: response.data.output || response.data.response || 'No response from ZIMA Core',
        usage: response.data.usage || { inputTokens: 0, outputTokens: 0, cost: 0 },
        files: response.data.files || []
      };
    } catch (error: any) {
      console.error('❌ Error calling ZIMA Core:', error.message);

      // Return error response
      return {
        output: `I processed your request but encountered an issue.\n\n` +
                `**Your request:** "${context.newMessage}"\n\n` +
                `**Session:** ${context.sessionKey}\n` +
                `**Model:** ${context.model}\n` +
                `**Complexity:** ${context.complexity}\n` +
                `**Context Tier:** ${context.tier}\n\n` +
                `Error: ${error.message}\n\n` +
                `Please ensure the agent runtime is configured with ANTHROPIC_API_KEY ` +
                `or ZIMA Core is running on ${this.config.zima.apiUrl}.`,
        usage: { inputTokens: 0, outputTokens: 0, cost: 0 },
        files: []
      };
    }
  }

  /**
   * Format file size
   */
  private formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  /**
   * Get context manager statistics
   */
  async getStats(): Promise<any> {
    const lockManager = getLockManager();
    const responseCache = getResponseCache();

    return {
      locks: {
        active: lockManager.getActiveLockCount()
      },
      cache: responseCache.getStats()
    };
  }
}
