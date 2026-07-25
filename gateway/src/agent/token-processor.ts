/**
 * Token Processor for OpenClaw-style Special Tokens
 *
 * Handles:
 * - HEARTBEAT_OK (silent reply)
 * - [[reply_to_current]] (reply tags)
 * - [[reply_to:<session_id>]] (cross-session replies)
 */

export interface ProcessedResponse {
  output: string;
  isSilentReply: boolean;
  replyTarget?: string;
  tokens: string[];
}

export class TokenProcessor {
  private static readonly SILENT_TOKEN = 'HEARTBEAT_OK';
  private static readonly REPLY_CURRENT_PATTERN = /\[\[reply_to_current\]\]/g;
  private static readonly REPLY_TO_PATTERN = /\[\[reply_to:([^\]]+)\]\]/g;

  /**
   * Process response to detect and strip special tokens
   */
  static processResponse(output: string): ProcessedResponse {
    const tokens: string[] = [];
    let processedOutput = output;
    let isSilentReply = false;
    let replyTarget: string | undefined;

    // 1. Check for HEARTBEAT_OK (silent reply)
    if (output.trim() === this.SILENT_TOKEN || output.includes(this.SILENT_TOKEN)) {
      isSilentReply = true;
      tokens.push(this.SILENT_TOKEN);
      // Remove the token from output
      processedOutput = processedOutput.replace(this.SILENT_TOKEN, '').trim();
    }

    // 2. Check for [[reply_to_current]]
    const replyCurrentMatches = output.match(this.REPLY_CURRENT_PATTERN);
    if (replyCurrentMatches) {
      replyTarget = 'current';
      tokens.push('[[reply_to_current]]');
      // Remove the token from output
      processedOutput = processedOutput.replace(this.REPLY_CURRENT_PATTERN, '').trim();
    }

    // 3. Check for [[reply_to:<session_id>]]
    const replyToMatches = output.match(this.REPLY_TO_PATTERN);
    if (replyToMatches) {
      const sessionId = replyToMatches[0].match(/\[\[reply_to:([^\]]+)\]\]/)?.[1];
      if (sessionId) {
        replyTarget = sessionId;
        tokens.push(`[[reply_to:${sessionId}]]`);
        // Remove the token from output
        processedOutput = processedOutput.replace(this.REPLY_TO_PATTERN, '').trim();
      }
    }

    return {
      output: processedOutput,
      isSilentReply,
      replyTarget,
      tokens
    };
  }

  /**
   * Check if response is a silent reply
   */
  static isSilentReply(output: string): boolean {
    return output.trim() === this.SILENT_TOKEN || output.includes(this.SILENT_TOKEN);
  }

  /**
   * Extract reply target from response
   */
  static extractReplyTarget(output: string): string | undefined {
    // Check for [[reply_to_current]]
    if (output.match(this.REPLY_CURRENT_PATTERN)) {
      return 'current';
    }

    // Check for [[reply_to:<session_id>]]
    const replyToMatches = output.match(this.REPLY_TO_PATTERN);
    if (replyToMatches) {
      const sessionId = replyToMatches[0].match(/\[\[reply_to:([^\]]+)\]\]/)?.[1];
      return sessionId;
    }

    return undefined;
  }

  /**
   * Strip all special tokens from output
   */
  static stripTokens(output: string): string {
    let processed = output;

    // Remove HEARTBEAT_OK
    processed = processed.replace(this.SILENT_TOKEN, '').trim();

    // Remove [[reply_to_current]]
    processed = processed.replace(this.REPLY_CURRENT_PATTERN, '').trim();

    // Remove [[reply_to:<session_id>]]
    processed = processed.replace(this.REPLY_TO_PATTERN, '').trim();

    return processed;
  }

  /**
   * Build example token usage documentation
   */
  static getTokenDocumentation(): string {
    return `
## Special Tokens

**Silent Reply**: Use when you have nothing substantive to say
- Token: HEARTBEAT_OK
- Example: When processing a heartbeat check with no updates

**Reply Tags**: Route replies in multi-session contexts
- [[reply_to_current]]: Reply to the current message
- [[reply_to:<session_id>]]: Reply to specific session

**Token Handling**:
- Tokens are automatically stripped before message delivery
- Use for internal signaling only
- Don't mention tokens in user-facing content
`;
  }
}
