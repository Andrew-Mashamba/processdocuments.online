/**
 * Webchat Channel Adapter
 * Sends messages to Laravel webchat via HTTP POST
 */

import axios from 'axios';
import { SendOptions, SendResult, ChannelAdapter } from '../message-sender';

export class WebchatAdapter implements ChannelAdapter {
  name: string = 'webchat';
  private apiUrl: string;
  private apiKey: string | null;
  private enabled: boolean;

  constructor() {
    this.apiUrl = process.env.WEBCHAT_API_URL || 'http://localhost:8000/api/webchat/messages';
    this.apiKey = process.env.WEBCHAT_API_KEY || null;
    this.enabled = !!this.apiUrl;

    if (!this.enabled) {
      console.warn('⚠️  Webchat adapter disabled (no WEBCHAT_API_URL)');
    } else {
      console.log('✓ Webchat adapter initialized');
    }
  }

  /**
   * Send message via Laravel webchat API
   */
  async send(to: string, message: string, options?: SendOptions): Promise<SendResult> {
    if (!this.enabled) {
      return {
        success: false,
        channel: this.name,
        to,
        error: 'Webchat not configured',
        timestamp: Date.now()
      };
    }

    try {
      const response = await axios.post(
        this.apiUrl,
        {
          session_key: to,
          message,
          priority: options?.priority || 'normal',
          metadata: options?.metadata || {}
        },
        {
          headers: {
            'Content-Type': 'application/json',
            ...(this.apiKey && { 'Authorization': `Bearer ${this.apiKey}` })
          },
          timeout: options?.timeout || 10000
        }
      );

      return {
        success: true,
        messageId: response.data.message_id || response.data.id,
        channel: this.name,
        to,
        timestamp: Date.now()
      };
    } catch (error: any) {
      return {
        success: false,
        channel: this.name,
        to,
        error: error.response?.data?.message || error.message,
        timestamp: Date.now()
      };
    }
  }

  /**
   * Check if webchat is available
   */
  isAvailable(): boolean {
    return this.enabled;
  }

  /**
   * Get status
   */
  getStatus(): { available: boolean; connected: boolean } {
    return {
      available: this.enabled,
      connected: this.enabled // Simple check, could ping API endpoint
    };
  }
}
