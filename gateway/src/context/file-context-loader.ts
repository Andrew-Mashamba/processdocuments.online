/**
 * File Context Loader
 * Loads and summarizes uploaded files for context (ZIMA approach)
 */

import * as fs from 'fs-extra';
import * as path from 'path';
import { ContextTier } from '../types';

export interface SessionFile {
  name: string;
  path: string;
  size: number;
  mimeType: string;
  content?: string;
  summary?: string;
}

export class FileContextLoader {
  private filesDir: string;
  private maxFullFileSize: number = 50000; // 50KB
  private maxPreviewSize: number = 200000; // 200KB

  constructor(filesDir: string) {
    this.filesDir = filesDir;
    this.ensureFilesDir();
  }

  /**
   * Load session files
   */
  async loadSessionFiles(sessionKey: string): Promise<SessionFile[]> {
    const sessionFilesPath = path.join(this.filesDir, sessionKey, 'uploads');

    if (!await fs.pathExists(sessionFilesPath)) {
      return [];
    }

    const filenames = await fs.readdir(sessionFilesPath);
    const files: SessionFile[] = [];

    for (const filename of filenames) {
      const filePath = path.join(sessionFilesPath, filename);
      const stats = await fs.stat(filePath);

      if (!stats.isFile()) continue;

      const file: SessionFile = {
        name: filename,
        path: filePath,
        size: stats.size,
        mimeType: this.getMimeType(filename)
      };

      // Load content for text files under size limit
      if (this.isTextFile(filename) && stats.size < this.maxPreviewSize) {
        try {
          file.content = await fs.readFile(filePath, 'utf-8');
        } catch (error) {
          console.error(`Error reading file ${filename}:`, error);
        }
      }

      files.push(file);
    }

    return files;
  }

  /**
   * Build file context for system prompt based on tier
   */
  buildFileContext(files: SessionFile[], tier: ContextTier): string {
    if (files.length === 0) {
      return '';
    }

    let context = '## Session Files\n\n';

    for (const file of files) {
      switch (tier) {
        case 0:
          // Tier 0: Full file content for small files
          if (file.content && file.size < this.maxFullFileSize) {
            context += `### ${file.name}\n${file.content}\n\n`;
          } else if (file.content) {
            context += `### ${file.name} (${this.formatSize(file.size)})\n`;
            context += file.content.substring(0, 1000) + '...\n\n';
          } else {
            context += `- ${file.name} (${this.formatSize(file.size)}, ${file.mimeType})\n`;
          }
          break;

        case 1:
          // Tier 1: Preview of file content
          if (file.content) {
            context += `### ${file.name} (${this.formatSize(file.size)})\n`;
            context += file.content.substring(0, 500) + '...\n\n';
          } else {
            context += `- ${file.name} (${this.formatSize(file.size)}, ${file.mimeType})\n`;
          }
          break;

        case 2:
        case 3:
          // Tier 2-3: Just list files with metadata
          context += `- ${file.name} (${this.formatSize(file.size)}, ${file.mimeType})\n`;
          break;
      }
    }

    return context;
  }

  /**
   * Save uploaded file
   */
  async saveUploadedFile(
    sessionKey: string,
    filename: string,
    content: Buffer | string
  ): Promise<string> {
    const sessionFilesPath = path.join(this.filesDir, sessionKey, 'uploads');
    await fs.ensureDir(sessionFilesPath);

    const filePath = path.join(sessionFilesPath, filename);
    await fs.writeFile(filePath, content);

    console.log(`💾 Saved uploaded file: ${filename} (${this.formatSize(Buffer.byteLength(content))})`);

    return filePath;
  }

  /**
   * Get file by name
   */
  async getFile(sessionKey: string, filename: string): Promise<SessionFile | null> {
    const files = await this.loadSessionFiles(sessionKey);
    return files.find(f => f.name === filename) || null;
  }

  /**
   * Delete file
   */
  async deleteFile(sessionKey: string, filename: string): Promise<boolean> {
    const filePath = path.join(this.filesDir, sessionKey, 'uploads', filename);

    if (await fs.pathExists(filePath)) {
      await fs.unlink(filePath);
      console.log(`🗑️  Deleted file: ${filename}`);
      return true;
    }

    return false;
  }

  /**
   * Get total size of session files
   */
  async getSessionFilesSize(sessionKey: string): Promise<number> {
    const files = await this.loadSessionFiles(sessionKey);
    return files.reduce((total, file) => total + file.size, 0);
  }

  /**
   * Clean up old files (optional maintenance)
   */
  async cleanupOldFiles(maxAgeMs: number = 7 * 24 * 60 * 60 * 1000): Promise<number> {
    // Clean up files older than 7 days by default
    console.log('🧹 Cleaning up old session files...');

    const sessionDirs = await fs.readdir(this.filesDir);
    const now = Date.now();
    let cleaned = 0;

    for (const sessionDir of sessionDirs) {
      const sessionPath = path.join(this.filesDir, sessionDir, 'uploads');

      if (!await fs.pathExists(sessionPath)) continue;

      const files = await fs.readdir(sessionPath);

      for (const file of files) {
        const filePath = path.join(sessionPath, file);
        const stats = await fs.stat(filePath);
        const age = now - stats.mtimeMs;

        if (age > maxAgeMs) {
          await fs.unlink(filePath);
          cleaned++;
        }
      }
    }

    if (cleaned > 0) {
      console.log(`✓ Cleaned up ${cleaned} old files`);
    }

    return cleaned;
  }

  /**
   * Check if file is a text file
   */
  private isTextFile(filename: string): boolean {
    const textExtensions = [
      '.txt', '.md', '.json', '.xml', '.html', '.css', '.js', '.ts',
      '.csv', '.log', '.yaml', '.yml', '.ini', '.conf', '.sh'
    ];

    const ext = path.extname(filename).toLowerCase();
    return textExtensions.includes(ext);
  }

  /**
   * Get MIME type from filename
   */
  private getMimeType(filename: string): string {
    const ext = path.extname(filename).toLowerCase();
    const mimeTypes: Record<string, string> = {
      '.txt': 'text/plain',
      '.md': 'text/markdown',
      '.json': 'application/json',
      '.xml': 'application/xml',
      '.html': 'text/html',
      '.css': 'text/css',
      '.js': 'application/javascript',
      '.ts': 'application/typescript',
      '.csv': 'text/csv',
      '.pdf': 'application/pdf',
      '.xlsx': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      '.docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      '.pptx': 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
      '.png': 'image/png',
      '.jpg': 'image/jpeg',
      '.jpeg': 'image/jpeg',
      '.gif': 'image/gif'
    };

    return mimeTypes[ext] || 'application/octet-stream';
  }

  /**
   * Format file size for display
   */
  private formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  /**
   * Ensure files directory exists
   */
  private ensureFilesDir(): void {
    fs.ensureDirSync(this.filesDir);
  }
}
