/**
 * Message Sender - Cross-channel messaging
 * Week 9-10: Communication - Phase C
 */

import { EventEmitter } from 'events';
import { WebchatAdapter } from './channel-adapters/webchat-adapter';
import { WhatsAppAdapter } from './channel-adapters/whatsapp-adapter';
import { EmailAdapter } from './channel-adapters/email-adapter';

export interface SendOptions {
  priority?: 'low' | 'normal' | 'high';
  metadata?: Record<string, any>;
  timeout?: number;
  retries?: number;
}

export interface SendResult {
  success: boolean;
  messageId?: string;
  channel: string;
  to: string;
  error?: string;
  timestamp: number;
}

export interface ChannelAdapter {
  name: string;
  send(to: string, message: string, options?: SendOptions): Promise<SendResult>;
  isAvailable(): boolean;
  getStatus(): { available: boolean; connected: boolean };
}

export class MessageSender extends EventEmitter {
  private adapters: Map<string, ChannelAdapter> = new Map();

  constructor() {
    super();
    this.initializeAdapters();
  }

  /**
   * Initialize all channel adapters
   */
  private initializeAdapters(): void {
    console.log('📡 Initializing channel adapters...');

    // Webchat adapter
    const webchat = new WebchatAdapter();
    this.adapters.set('webchat', webchat);
    this.adapters.set('web', webchat); // Alias

    // WhatsApp adapter
    const whatsapp = new WhatsAppAdapter();
    this.adapters.set('whatsapp', whatsapp);
    this.adapters.set('wa', whatsapp); // Alias

    // Email adapter
    const email = new EmailAdapter();
    this.adapters.set('email', email);
    this.adapters.set('mail', email); // Alias

    const available = Array.from(this.adapters.values())
      .filter(a => a.isAvailable())
      .map(a => a.name);

    console.log(`✓ Adapters initialized: ${available.join(', ') || 'none'}`);
  }

  /**
   * Send message to a specific channel
   */
  async send(
    channel: string,
    to: string,
    message: string,
    options: SendOptions = {}
  ): Promise<SendResult> {
    const normalizedChannel = channel.toLowerCase();

    console.log(`📤 Sending message via ${normalizedChannel} to ${to}`);
    this.emit('message-send-start', { channel: normalizedChannel, to, message });

    // Get adapter
    const adapter = this.adapters.get(normalizedChannel);

    if (!adapter) {
      const error = `Unknown channel: ${channel}`;
      console.error(`❌ ${error}`);

      const result: SendResult = {
        success: false,
        channel: normalizedChannel,
        to,
        error,
        timestamp: Date.now()
      };

      this.emit('message-send-error', result);
      return result;
    }

    // Check availability
    if (!adapter.isAvailable()) {
      const error = `Channel not available: ${channel}`;
      console.warn(`⚠️  ${error}`);

      const result: SendResult = {
        success: false,
        channel: normalizedChannel,
        to,
        error,
        timestamp: Date.now()
      };

      this.emit('message-send-error', result);
      return result;
    }

    // Send with retry logic
    const maxRetries = options.retries || 3;
    let lastError: Error | null = null;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        const result = await adapter.send(to, message, options);

        if (result.success) {
          console.log(`✓ Message sent: ${normalizedChannel}/${result.messageId}`);
          this.emit('message-send-success', result);
          return result;
        } else {
          lastError = new Error(result.error || 'Unknown error');
        }
      } catch (error: any) {
        lastError = error;
        console.error(`❌ Send attempt ${attempt}/${maxRetries} failed:`, error.message);

        // Wait before retry (exponential backoff)
        if (attempt < maxRetries) {
          const delay = Math.min(1000 * Math.pow(2, attempt - 1), 10000);
          await new Promise(resolve => setTimeout(resolve, delay));
        }
      }
    }

    // All retries failed
    const result: SendResult = {
      success: false,
      channel: normalizedChannel,
      to,
      error: lastError?.message || 'Send failed after retries',
      timestamp: Date.now()
    };

    this.emit('message-send-error', result);
    return result;
  }

  /**
   * Send to multiple recipients on the same channel
   */
  async sendBatch(
    channel: string,
    recipients: string[],
    message: string,
    options: SendOptions = {}
  ): Promise<SendResult[]> {
    console.log(`📤 Batch sending to ${recipients.length} recipients via ${channel}`);

    const results = await Promise.allSettled(
      recipients.map(to => this.send(channel, to, message, options))
    );

    return results.map((result, i) => {
      if (result.status === 'fulfilled') {
        return result.value;
      } else {
        return {
          success: false,
          channel,
          to: recipients[i],
          error: result.reason?.message || 'Batch send failed',
          timestamp: Date.now()
        };
      }
    });
  }

  /**
   * Send to multiple channels (broadcast)
   */
  async broadcast(
    channels: string[],
    to: string,
    message: string,
    options: SendOptions = {}
  ): Promise<SendResult[]> {
    console.log(`📡 Broadcasting to ${channels.length} channels`);

    const results = await Promise.allSettled(
      channels.map(channel => this.send(channel, to, message, options))
    );

    return results.map((result, i) => {
      if (result.status === 'fulfilled') {
        return result.value;
      } else {
        return {
          success: false,
          channel: channels[i],
          to,
          error: result.reason?.message || 'Broadcast failed',
          timestamp: Date.now()
        };
      }
    });
  }

  /**
   * Get available channels
   */
  getAvailableChannels(): string[] {
    return Array.from(this.adapters.values())
      .filter(adapter => adapter.isAvailable())
      .map(adapter => adapter.name);
  }

  /**
   * Get all channels (available + unavailable)
   */
  getAllChannels(): string[] {
    return Array.from(this.adapters.values())
      .map(adapter => adapter.name);
  }

  /**
   * Check if channel is available
   */
  isChannelAvailable(channel: string): boolean {
    const adapter = this.adapters.get(channel.toLowerCase());
    return adapter ? adapter.isAvailable() : false;
  }

  /**
   * Get channel status
   */
  getChannelStatus(channel: string): { available: boolean; connected: boolean } | null {
    const adapter = this.adapters.get(channel.toLowerCase());
    return adapter ? adapter.getStatus() : null;
  }

  /**
   * Get all channel statuses
   */
  getAllChannelStatuses(): Record<string, { available: boolean; connected: boolean }> {
    const statuses: Record<string, { available: boolean; connected: boolean }> = {};

    for (const [name, adapter] of this.adapters.entries()) {
      statuses[name] = adapter.getStatus();
    }

    return statuses;
  }
}

// Global instance
let messageSender: MessageSender | null = null;

/**
 * Get global message sender instance
 */
export function getMessageSender(): MessageSender {
  if (!messageSender) {
    messageSender = new MessageSender();
  }
  return messageSender;
}
