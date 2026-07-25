/**
 * Transcript Manager
 * Manages session transcripts in JSONL format (OpenClaw approach)
 */

import * as fs from 'fs-extra';
import * as path from 'path';
import { TranscriptMessage, SessionEntry } from '../types';
import { SessionKeyBuilder } from '../router/session-key-builder';

export class TranscriptManager {
  private transcriptDir: string;
  private sessionKeyBuilder: SessionKeyBuilder;

  constructor(transcriptDir: string) {
    this.transcriptDir = transcriptDir;
    this.sessionKeyBuilder = new SessionKeyBuilder();
    this.ensureTranscriptDir();
  }

  /**
   * Get transcript path for session
   */
  getTranscriptPath(sessionKey: string): string {
    const filename = this.sessionKeyBuilder.toFilename(sessionKey) + '.jsonl';
    return path.join(this.transcriptDir, filename);
  }

  /**
   * Read transcript for session
   */
  async readTranscript(sessionKey: string, limit?: number): Promise<TranscriptMessage[]> {
    const transcriptPath = this.getTranscriptPath(sessionKey);

    if (!await fs.pathExists(transcriptPath)) {
      return [];
    }

    try {
      const content = await fs.readFile(transcriptPath, 'utf-8');
      const lines = content.split('\n').filter(line => line.trim());
      const messages = lines.map(line => JSON.parse(line) as TranscriptMessage);

      // Apply limit if specified
      if (limit && limit > 0) {
        return messages.slice(-limit);
      }

      return messages;
    } catch (error: any) {
      console.error(`Error reading transcript for ${sessionKey}:`, error.message);
      return [];
    }
  }

  /**
   * Append message to transcript
   */
  async appendToTranscript(sessionKey: string, message: TranscriptMessage): Promise<void> {
    const transcriptPath = this.getTranscriptPath(sessionKey);

    // Ensure directory exists
    await fs.ensureDir(path.dirname(transcriptPath));

    // Append as JSONL (one JSON object per line)
    const line = JSON.stringify(message) + '\n';
    await fs.appendFile(transcriptPath, line, 'utf-8');
  }

  /**
   * Write entire transcript (used for compaction)
   */
  async writeTranscript(sessionKey: string, messages: TranscriptMessage[]): Promise<void> {
    const transcriptPath = this.getTranscriptPath(sessionKey);

    // Ensure directory exists
    await fs.ensureDir(path.dirname(transcriptPath));

    // Write all messages as JSONL
    const content = messages.map(msg => JSON.stringify(msg)).join('\n') + '\n';
    await fs.writeFile(transcriptPath, content, 'utf-8');
  }

  /**
   * Get message count for session
   */
  async getMessageCount(sessionKey: string): Promise<number> {
    const messages = await this.readTranscript(sessionKey);
    return messages.filter(m => m.type === 'message').length;
  }

  /**
   * Get transcript size in bytes
   */
  async getTranscriptSize(sessionKey: string): Promise<number> {
    const transcriptPath = this.getTranscriptPath(sessionKey);

    if (!await fs.pathExists(transcriptPath)) {
      return 0;
    }

    const stats = await fs.stat(transcriptPath);
    return stats.size;
  }

  /**
   * Compact transcript (remove old messages, keep important ones)
   */
  async compactTranscript(sessionKey: string, keepCount: number = 20): Promise<void> {
    console.log(`🗜️  Compacting transcript for session: ${sessionKey}`);

    const messages = await this.readTranscript(sessionKey);

    if (messages.length <= keepCount) {
      console.log(`  No compaction needed (${messages.length} messages)`);
      return;
    }

    // Keep:
    // 1. Last N messages
    // 2. Messages with high importance
    // 3. Messages with tool calls
    // Summarize the rest

    const recent = messages.slice(-keepCount);
    const old = messages.slice(0, -keepCount);

    const important = old.filter(m => m.importance && m.importance > 0.8);
    const withTools = old.filter(m => m.type === 'tool_call' || m.type === 'tool_result');

    // Create summary of old messages
    const summary: TranscriptMessage = {
      type: 'summary',
      timestamp: Date.now(),
      content: `Summarized ${old.length - important.length - withTools.length} older messages`,
      messageCount: old.length
    };

    // Build compacted transcript
    const compacted = [summary, ...important, ...withTools, ...recent];

    // Write compacted version
    await this.writeTranscript(sessionKey, compacted);

    console.log(`  ✓ Compacted ${messages.length} → ${compacted.length} messages`);
  }

  /**
   * Load or create session entry
   */
  async loadSessionEntry(sessionKey: string): Promise<SessionEntry> {
    const sessionsPath = path.join(path.dirname(this.transcriptDir), 'sessions.json');

    // Read existing sessions
    let sessionsData: { sessions: SessionEntry[] } = { sessions: [] };

    if (await fs.pathExists(sessionsPath)) {
      sessionsData = await fs.readJSON(sessionsPath);
    }

    // Find existing session
    let session = sessionsData.sessions.find(s => s.sessionKey === sessionKey);

    if (!session) {
      // Create new session entry
      const components = this.sessionKeyBuilder.parse(sessionKey);

      session = {
        sessionId: sessionKey,
        sessionKey,
        channel: components.channel,
        chatType: components.chatType as any,
        accountId: components.accountId,
        threadId: components.threadId,
        displayName: components.accountId,
        metadata: {
          createdAt: Date.now()
        }
      };

      sessionsData.sessions.push(session);
      await fs.writeJSON(sessionsPath, sessionsData, { spaces: 2 });

      console.log(`✓ Created new session entry: ${sessionKey}`);
    }

    return session;
  }

  /**
   * Update session entry
   */
  async updateSessionEntry(session: SessionEntry): Promise<void> {
    const sessionsPath = path.join(path.dirname(this.transcriptDir), 'sessions.json');

    let sessionsData: { sessions: SessionEntry[] } = { sessions: [] };

    if (await fs.pathExists(sessionsPath)) {
      sessionsData = await fs.readJSON(sessionsPath);
    }

    // Find and update session
    const index = sessionsData.sessions.findIndex(s => s.sessionKey === session.sessionKey);

    if (index >= 0) {
      sessionsData.sessions[index] = {
        ...sessionsData.sessions[index],
        ...session,
        metadata: {
          ...sessionsData.sessions[index].metadata,
          ...session.metadata,
          updatedAt: Date.now()
        }
      };
    } else {
      sessionsData.sessions.push(session);
    }

    await fs.writeJSON(sessionsPath, sessionsData, { spaces: 2 });
  }

  /**
   * Ensure transcript directory exists
   */
  private ensureTranscriptDir(): void {
    fs.ensureDirSync(this.transcriptDir);
  }
}
