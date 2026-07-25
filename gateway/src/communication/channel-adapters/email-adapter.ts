/**
 * Email Channel Adapter
 * Sends messages via SMTP using Nodemailer
 */

import * as nodemailer from 'nodemailer';
import { SendOptions, SendResult, ChannelAdapter } from '../message-sender';

export class EmailAdapter implements ChannelAdapter {
  name: string = 'email';
  private transporter: nodemailer.Transporter | null = null;
  private enabled: boolean;
  private from: string;

  constructor() {
    const smtpHost = process.env.SMTP_HOST;
    const smtpPort = parseInt(process.env.SMTP_PORT || '587');
    const smtpUser = process.env.SMTP_USER;
    const smtpPass = process.env.SMTP_PASS;
    this.from = process.env.SMTP_FROM || 'noreply@zima.ai';

    this.enabled = !!(smtpHost && smtpUser && smtpPass);

    if (!this.enabled) {
      console.warn('⚠️  Email adapter disabled (missing SMTP config)');
    } else {
      this.initializeTransporter(smtpHost!, smtpPort, smtpUser!, smtpPass!);
    }
  }

  /**
   * Initialize nodemailer transporter
   */
  private initializeTransporter(
    host: string,
    port: number,
    user: string,
    pass: string
  ): void {
    try {
      this.transporter = nodemailer.createTransport({
        host,
        port,
        secure: port === 465, // true for 465, false for other ports
        auth: {
          user,
          pass
        },
        tls: {
          rejectUnauthorized: process.env.SMTP_TLS_REJECT_UNAUTHORIZED !== 'false'
        }
      });

      console.log('✓ Email adapter initialized');

      // Verify connection
      this.transporter.verify((error, success) => {
        if (error) {
          console.error('❌ SMTP verification failed:', error.message);
          this.enabled = false;
        } else {
          console.log('✓ SMTP connection verified');
        }
      });
    } catch (error: any) {
      console.error('❌ Email adapter initialization failed:', error.message);
      this.enabled = false;
    }
  }

  /**
   * Send email
   */
  async send(to: string, message: string, options?: SendOptions): Promise<SendResult> {
    if (!this.enabled || !this.transporter) {
      return {
        success: false,
        channel: this.name,
        to,
        error: 'Email not configured or SMTP verification failed',
        timestamp: Date.now()
      };
    }

    try {
      // Extract subject from metadata or use default
      const subject = options?.metadata?.subject || 'Message from ZIMA';
      const html = options?.metadata?.html || this.textToHtml(message);
      const priority = options?.priority || 'normal';

      const mailOptions: nodemailer.SendMailOptions = {
        from: this.from,
        to,
        subject,
        text: message,
        html,
        priority: priority === 'high' ? 'high' : priority === 'low' ? 'low' : 'normal'
      };

      // Add attachments if provided
      if (options?.metadata?.attachments) {
        mailOptions.attachments = options.metadata.attachments;
      }

      const info = await this.transporter.sendMail(mailOptions);

      return {
        success: true,
        messageId: info.messageId,
        channel: this.name,
        to,
        timestamp: Date.now()
      };
    } catch (error: any) {
      return {
        success: false,
        channel: this.name,
        to,
        error: error.message,
        timestamp: Date.now()
      };
    }
  }

  /**
   * Convert plain text to HTML
   */
  private textToHtml(text: string): string {
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
      </head>
      <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
        <div style="background: #f4f4f4; border-left: 4px solid #007bff; padding: 15px; margin-bottom: 20px;">
          ${text.split('\n').map(line => `<p style="margin: 0 0 10px 0;">${this.escapeHtml(line)}</p>`).join('')}
        </div>
        <footer style="margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666;">
          <p>Sent by ZIMA AI Assistant</p>
        </footer>
      </body>
      </html>
    `.trim();
  }

  /**
   * Escape HTML special characters
   */
  private escapeHtml(text: string): string {
    const map: Record<string, string> = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
  }

  /**
   * Check if email is available
   */
  isAvailable(): boolean {
    return this.enabled && this.transporter !== null;
  }

  /**
   * Get status
   */
  getStatus(): { available: boolean; connected: boolean } {
    return {
      available: this.enabled,
      connected: this.transporter !== null
    };
  }

  /**
   * Close transporter
   */
  async close(): Promise<void> {
    if (this.transporter) {
      this.transporter.close();
      this.transporter = null;
    }
  }
}
