import * as fs from 'fs/promises';
import * as path from 'path';
import { createReadStream } from 'fs';
import * as readline from 'readline';
import { Message } from '../types';

export class TranscriptManager {
  private sessionsDir: string;

  constructor(sessionsDir: string) {
    this.sessionsDir = sessionsDir;
  }

  /**
   * Get transcript file path for session
   */
  private getTranscriptPath(sessionKey: string): string {
    // Replace colons with dashes for filesystem compatibility
    const safeKey = sessionKey.replace(/:/g, '-');
    return path.join(this.sessionsDir, `${safeKey}.jsonl`);
  }

  /**
   * Append message to transcript
   */
  async append(sessionKey: string, message: Message): Promise<void> {
    const transcriptPath = this.getTranscriptPath(sessionKey);

    // Ensure directory exists
    await fs.mkdir(path.dirname(transcriptPath), { recursive: true });

    // Append message as JSONL
    const line = JSON.stringify(message) + '\n';
    await fs.appendFile(transcriptPath, line, 'utf-8');
  }

  /**
   * Load entire transcript
   */
  async load(sessionKey: string): Promise<Message[]> {
    const transcriptPath = this.getTranscriptPath(sessionKey);

    try {
      await fs.access(transcriptPath);
    } catch {
      return []; // File doesn't exist yet
    }

    const messages: Message[] = [];
    const fileStream = createReadStream(transcriptPath);
    const rl = readline.createInterface({
      input: fileStream,
      crlfDelay: Infinity,
    });

    for await (const line of rl) {
      if (line.trim()) {
        try {
          messages.push(JSON.parse(line));
        } catch (error) {
          console.error('Failed to parse JSONL line:', error);
        }
      }
    }

    return messages;
  }

  /**
   * Load last N messages
   */
  async loadLast(sessionKey: string, count: number): Promise<Message[]> {
    const allMessages = await this.load(sessionKey);
    return allMessages.slice(-count);
  }

  /**
   * Load messages with pagination
   */
  async loadRange(sessionKey: string, offset: number, limit: number): Promise<Message[]> {
    const allMessages = await this.load(sessionKey);
    return allMessages.slice(offset, offset + limit);
  }

  /**
   * Get message count
   */
  async getCount(sessionKey: string): Promise<number> {
    const messages = await this.load(sessionKey);
    return messages.length;
  }

  /**
   * Clear transcript
   */
  async clear(sessionKey: string): Promise<void> {
    const transcriptPath = this.getTranscriptPath(sessionKey);
    try {
      await fs.unlink(transcriptPath);
    } catch {
      // File doesn't exist, ignore
    }
  }

  /**
   * List all sessions
   */
  async listSessions(): Promise<string[]> {
    try {
      await fs.mkdir(this.sessionsDir, { recursive: true });
      const files = await fs.readdir(this.sessionsDir);
      return files
        .filter((f) => f.endsWith('.jsonl'))
        .map((f) => f.replace('.jsonl', '').replace(/-/g, ':'));
    } catch {
      return [];
    }
  }

  /**
   * Get session info
   */
  async getSessionInfo(sessionKey: string): Promise<{
    messageCount: number;
    firstMessageAt?: string;
    lastMessageAt?: string;
    size: number;
  }> {
    const messages = await this.load(sessionKey);
    const transcriptPath = this.getTranscriptPath(sessionKey);

    let size = 0;
    try {
      const stat = await fs.stat(transcriptPath);
      size = stat.size;
    } catch {
      // File doesn't exist
    }

    return {
      messageCount: messages.length,
      firstMessageAt: messages[0]?.timestamp,
      lastMessageAt: messages[messages.length - 1]?.timestamp,
      size,
    };
  }

  /**
   * Check if session exists
   */
  async exists(sessionKey: string): Promise<boolean> {
    const transcriptPath = this.getTranscriptPath(sessionKey);
    try {
      await fs.access(transcriptPath);
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Export session as JSON
   */
  async export(sessionKey: string): Promise<string> {
    const messages = await this.load(sessionKey);
    return JSON.stringify({
      sessionKey,
      messages,
      exportedAt: new Date().toISOString(),
    }, null, 2);
  }

  /**
   * Import session from JSON
   */
  async import(sessionKey: string, jsonData: string): Promise<void> {
    const data = JSON.parse(jsonData);
    const transcriptPath = this.getTranscriptPath(sessionKey);

    // Clear existing transcript
    await this.clear(sessionKey);

    // Write all messages
    for (const message of data.messages) {
      await this.append(sessionKey, message);
    }
  }

  /**
   * Get total size of all transcripts
   */
  async getTotalSize(): Promise<number> {
    const sessions = await this.listSessions();
    let totalSize = 0;

    for (const session of sessions) {
      const info = await this.getSessionInfo(session);
      totalSize += info.size;
    }

    return totalSize;
  }
}
