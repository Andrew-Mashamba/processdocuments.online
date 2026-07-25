/**
 * Session Key Builder
 * Implements OpenClaw's session key format: agent:{id}:{channel}:{type}:{account}:{thread}
 */

import { SessionKeyParams, SessionKeyComponents } from '../types';

export class SessionKeyBuilder {
  /**
   * Build a session key from components
   * Format: agent:{agentId}:{channel}:{chatType}:{accountId}:{threadId?}
   */
  build(params: SessionKeyParams): string {
    const parts = [
      'agent',
      this.sanitize(params.agentId),
      this.sanitize(params.channel),
      this.sanitize(params.chatType),
      this.sanitize(params.accountId)
    ];

    if (params.threadId) {
      parts.push(this.sanitize(params.threadId));
    }

    return parts.join(':');
  }

  /**
   * Parse a session key into components
   */
  parse(sessionKey: string): SessionKeyComponents {
    const parts = sessionKey.split(':');

    if (parts.length < 5) {
      throw new Error(`Invalid session key format: ${sessionKey}`);
    }

    return {
      prefix: parts[0],
      agentId: parts[1],
      channel: parts[2],
      chatType: parts[3],
      accountId: parts[4],
      threadId: parts[5]
    };
  }

  /**
   * Sanitize a component to be safe in session keys
   */
  private sanitize(value: string): string {
    return value
      .replace(/:/g, '-')  // Replace colons
      .replace(/\//g, '-') // Replace slashes
      .replace(/\s+/g, '_') // Replace spaces
      .toLowerCase();
  }

  /**
   * Extract agent ID from session key
   */
  extractAgentId(sessionKey: string): string {
    const components = this.parse(sessionKey);
    return components.agentId;
  }

  /**
   * Extract channel from session key
   */
  extractChannel(sessionKey: string): string {
    const components = this.parse(sessionKey);
    return components.channel;
  }

  /**
   * Check if session key is valid
   */
  isValid(sessionKey: string): boolean {
    try {
      const components = this.parse(sessionKey);
      return (
        components.prefix === 'agent' &&
        components.agentId.length > 0 &&
        components.channel.length > 0 &&
        components.chatType.length > 0 &&
        components.accountId.length > 0
      );
    } catch {
      return false;
    }
  }

  /**
   * Generate a safe filename from session key
   */
  toFilename(sessionKey: string): string {
    return sessionKey.replace(/:/g, '-');
  }

  /**
   * Generate session key from filename
   */
  fromFilename(filename: string): string {
    return filename.replace(/-/g, ':').replace(/\.jsonl$/, '');
  }
}
