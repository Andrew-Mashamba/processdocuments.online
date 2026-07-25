import * as fs from 'fs';
import * as path from 'path';
import { exec } from 'child_process';
import { promisify } from 'util';
import { ToolFailure } from './tool-failure-logger';

const execAsync = promisify(exec);

export interface ToolTemplate {
  name: string;
  description: string;
  category: string;
  generateCode: (toolName: string, originalTool: any, failure?: ToolFailure) => string;
  generateDefinition: (toolName: string, originalTool: any) => any;
}

export interface GeneratedTool {
  name: string;
  filePath: string;
  definition: any;
  implementationType: string;
}

export class ToolGenerator {
  private templates: Map<string, ToolTemplate>;
  private generatedToolsDir: string;

  constructor(generatedToolsDir: string = './src/tools/generated') {
    this.templates = new Map();
    this.generatedToolsDir = generatedToolsDir;

    this.ensureGeneratedToolsDirectory();
    this.registerDefaultTemplates();
  }

  private ensureGeneratedToolsDirectory(): void {
    if (!fs.existsSync(this.generatedToolsDir)) {
      fs.mkdirSync(this.generatedToolsDir, { recursive: true });
    }
  }

  private registerDefaultTemplates(): void {
    // Template 1: Excel creation using exceljs (Node.js library)
    this.registerTemplate({
      name: 'excel_creator_exceljs',
      description: 'Create Excel files using exceljs library',
      category: 'document',
      generateCode: (toolName: string, originalTool: any, failure?: ToolFailure) => {
        return `import * as ExcelJS from 'exceljs';
import * as fs from 'fs';
import * as path from 'path';

export async function ${toolName}_generated(input: any): Promise<any> {
  const { filename, data, sessionKey } = input;

  // Determine output path
  const outputDir = process.env.STORAGE_ROOT || './generated_files';
  const sessionDir = sessionKey ? path.join(outputDir, sessionKey) : outputDir;

  if (!fs.existsSync(sessionDir)) {
    fs.mkdirSync(sessionDir, { recursive: true });
  }

  const filePath = path.join(sessionDir, filename);

  // Create workbook
  const workbook = new ExcelJS.Workbook();
  const worksheet = workbook.addWorksheet('Sheet1');

  // Add data
  if (Array.isArray(data)) {
    data.forEach(row => {
      if (Array.isArray(row)) {
        worksheet.addRow(row);
      } else if (typeof row === 'object') {
        worksheet.addRow(Object.values(row));
      }
    });
  }

  // Auto-fit columns
  worksheet.columns.forEach(column => {
    if (column) {
      let maxLength = 0;
      column.eachCell?.({ includeEmpty: true }, cell => {
        const cellValue = cell.value ? cell.value.toString() : '';
        maxLength = Math.max(maxLength, cellValue.length);
      });
      column.width = Math.min(maxLength + 2, 50);
    }
  });

  // Save file
  await workbook.xlsx.writeFile(filePath);

  return {
    success: true,
    file_path: filePath,
    file_name: filename,
    message: \`Excel file created successfully using exceljs: \${filename}\`
  };
}
`;
      },
      generateDefinition: (toolName: string, originalTool: any) => ({
        name: `${toolName}_generated`,
        description: `${originalTool.description} (Generated alternative implementation using exceljs)`,
        input_schema: originalTool.input_schema
      })
    });

    // Template 2: Python-based Excel creation
    this.registerTemplate({
      name: 'excel_creator_python',
      description: 'Create Excel files using Python openpyxl',
      category: 'document',
      generateCode: (toolName: string, originalTool: any, failure?: ToolFailure) => {
        return `import { exec } from 'child_process';
import { promisify } from 'util';
import * as fs from 'fs';
import * as path from 'path';

const execAsync = promisify(exec);

export async function ${toolName}_generated(input: any): Promise<any> {
  const { filename, data, sessionKey } = input;

  // Determine output path
  const outputDir = process.env.STORAGE_ROOT || './generated_files';
  const sessionDir = sessionKey ? path.join(outputDir, sessionKey) : outputDir;

  if (!fs.existsSync(sessionDir)) {
    fs.mkdirSync(sessionDir, { recursive: true });
  }

  const filePath = path.join(sessionDir, filename);

  // Create Python script
  const pythonScript = \`
import openpyxl
from openpyxl import Workbook
import json
import sys

data = json.loads(sys.argv[1])
output_path = sys.argv[2]

wb = Workbook()
ws = wb.active

for row in data:
    if isinstance(row, list):
        ws.append(row)
    elif isinstance(row, dict):
        ws.append(list(row.values()))

wb.save(output_path)
print(f"Excel file created: {output_path}")
\`;

  const scriptPath = path.join(sessionDir, 'temp_excel_script.py');
  fs.writeFileSync(scriptPath, pythonScript);

  try {
    const dataJson = JSON.stringify(data);
    const { stdout, stderr } = await execAsync(
      \`python3 "\${scriptPath}" '\${dataJson.replace(/'/g, "\\\\'")}' "\${filePath}"\`
    );

    // Clean up script
    fs.unlinkSync(scriptPath);

    return {
      success: true,
      file_path: filePath,
      file_name: filename,
      message: \`Excel file created successfully using Python: \${filename}\`,
      stdout: stdout.trim()
    };
  } catch (error) {
    // Clean up script
    if (fs.existsSync(scriptPath)) {
      fs.unlinkSync(scriptPath);
    }

    throw error;
  }
}
`;
      },
      generateDefinition: (toolName: string, originalTool: any) => ({
        name: `${toolName}_generated`,
        description: `${originalTool.description} (Generated alternative implementation using Python)`,
        input_schema: originalTool.input_schema
      })
    });

    // Template 3: Direct API wrapper with retry logic
    this.registerTemplate({
      name: 'api_wrapper_retry',
      description: 'Wrap API calls with retry logic',
      category: 'api',
      generateCode: (toolName: string, originalTool: any, failure?: ToolFailure) => {
        return `import fetch from 'node-fetch';

async function retryWithBackoff<T>(
  fn: () => Promise<T>,
  maxRetries: number = 3,
  baseDelay: number = 1000
): Promise<T> {
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      return await fn();
    } catch (error) {
      if (attempt === maxRetries) {
        throw error;
      }

      const delay = baseDelay * Math.pow(2, attempt - 1);
      console.log(\`Retry attempt \${attempt}/\${maxRetries} after \${delay}ms\`);
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }

  throw new Error('Max retries exceeded');
}

export async function ${toolName}_generated(input: any): Promise<any> {
  const apiUrl = process.env.ZIMA_API_URL || 'http://localhost:5000';
  const endpoint = '/api/generate'; // Adjust based on tool

  return await retryWithBackoff(async () => {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 60000); // 60s timeout

    try {
      const response = await fetch(\`\${apiUrl}\${endpoint}\`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input),
        signal: controller.signal
      });

      clearTimeout(timeout);

      if (!response.ok) {
        throw new Error(\`API error: \${response.status} \${response.statusText}\`);
      }

      return await response.json();
    } catch (error) {
      clearTimeout(timeout);
      throw error;
    }
  }, 3, 2000);
}
`;
      },
      generateDefinition: (toolName: string, originalTool: any) => ({
        name: `${toolName}_generated`,
        description: `${originalTool.description} (Generated with retry logic)`,
        input_schema: originalTool.input_schema
      })
    });

    // Template 4: Generic file operation tool
    this.registerTemplate({
      name: 'file_operation',
      description: 'Generic file operation tool',
      category: 'file',
      generateCode: (toolName: string, originalTool: any, failure?: ToolFailure) => {
        return `import * as fs from 'fs';
import * as path from 'path';

export async function ${toolName}_generated(input: any): Promise<any> {
  const { operation, filepath, content, sessionKey } = input;

  const outputDir = process.env.STORAGE_ROOT || './generated_files';
  const sessionDir = sessionKey ? path.join(outputDir, sessionKey) : outputDir;

  if (!fs.existsSync(sessionDir)) {
    fs.mkdirSync(sessionDir, { recursive: true });
  }

  const fullPath = path.join(sessionDir, filepath);

  switch (operation) {
    case 'write':
      fs.writeFileSync(fullPath, content);
      return { success: true, message: \`File written: \${filepath}\` };

    case 'read':
      const data = fs.readFileSync(fullPath, 'utf-8');
      return { success: true, content: data };

    case 'delete':
      fs.unlinkSync(fullPath);
      return { success: true, message: \`File deleted: \${filepath}\` };

    case 'exists':
      const exists = fs.existsSync(fullPath);
      return { success: true, exists };

    default:
      throw new Error(\`Unknown operation: \${operation}\`);
  }
}
`;
      },
      generateDefinition: (toolName: string, originalTool: any) => ({
        name: `${toolName}_generated`,
        description: `Generic file operation tool (auto-generated)`,
        input_schema: {
          type: 'object',
          properties: {
            operation: { type: 'string', enum: ['write', 'read', 'delete', 'exists'] },
            filepath: { type: 'string' },
            content: { type: 'string' },
            sessionKey: { type: 'string' }
          },
          required: ['operation', 'filepath']
        }
      })
    });
  }

  registerTemplate(template: ToolTemplate): void {
    this.templates.set(template.name, template);
  }

  async generateTool(
    originalToolName: string,
    originalTool: any,
    templateName?: string,
    failure?: ToolFailure
  ): Promise<GeneratedTool> {
    console.log(`\n🔨 Generating new tool implementation for: ${originalToolName}`);

    // Auto-select template if not specified
    if (!templateName) {
      templateName = this.selectTemplate(originalToolName, originalTool, failure);
      console.log(`   Auto-selected template: ${templateName}`);
    }

    const template = this.templates.get(templateName);
    if (!template) {
      throw new Error(`Template not found: ${templateName}`);
    }

    // Generate code
    const code = template.generateCode(originalToolName, originalTool, failure);
    const definition = template.generateDefinition(originalToolName, originalTool);

    // Write to file
    const filename = `${originalToolName}_generated.ts`;
    const filePath = path.join(this.generatedToolsDir, filename);

    fs.writeFileSync(filePath, code);

    console.log(`   ✅ Generated tool code: ${filePath}`);

    // Also write definition
    const defPath = path.join(this.generatedToolsDir, `${originalToolName}_generated.json`);
    fs.writeFileSync(defPath, JSON.stringify(definition, null, 2));

    console.log(`   ✅ Generated tool definition: ${defPath}`);

    // Compile TypeScript to JavaScript
    let jsFilePath: string | undefined;
    try {
      jsFilePath = await this.compileToJavaScript(filePath);
    } catch (error) {
      console.log(`   ⚠️  Compilation failed, tool will require runtime TS execution`);
      console.log(`   Error: ${error instanceof Error ? error.message : String(error)}`);
    }

    return {
      name: definition.name,
      filePath: jsFilePath || filePath, // Prefer compiled JS
      definition,
      implementationType: template.name
    };
  }

  private selectTemplate(
    toolName: string,
    originalTool: any,
    failure?: ToolFailure
  ): string {
    // Excel tools
    if (toolName.includes('excel') || toolName.startsWith('create_excel')) {
      // If API failure, try Node.js library approach
      if (failure?.failureType === 'api_error') {
        return 'excel_creator_exceljs';
      }
      // If timeout, try Python approach (might be faster)
      if (failure?.failureType === 'timeout') {
        return 'excel_creator_python';
      }
      // Default to exceljs
      return 'excel_creator_exceljs';
    }

    // PDF, Word, or other document tools - try API wrapper with retry
    if (toolName.includes('pdf') || toolName.includes('word') || toolName.startsWith('create_')) {
      return 'api_wrapper_retry';
    }

    // File operations
    if (toolName.includes('file') || toolName.includes('read') || toolName.includes('write')) {
      return 'file_operation';
    }

    // Default to API wrapper with retry
    return 'api_wrapper_retry';
  }

  /**
   * Compile generated TypeScript to JavaScript
   */
  private async compileToJavaScript(tsFilePath: string): Promise<string> {
    const jsFilePath = tsFilePath.replace('.ts', '.js');

    try {
      console.log(`   🔨 Compiling: ${path.basename(tsFilePath)} → ${path.basename(jsFilePath)}`);

      // Compile using tsc with inline config
      const { stdout, stderr } = await execAsync(
        `npx tsc "${tsFilePath}" --module commonjs --target es2020 --esModuleInterop --skipLibCheck`,
        { cwd: process.cwd() }
      );

      if (stderr && !stderr.includes('Debugger')) {
        console.log(`   ⚠️  Compilation warnings: ${stderr}`);
      }

      if (!fs.existsSync(jsFilePath)) {
        throw new Error('Compilation failed - JS file not created');
      }

      console.log(`   ✅ Compiled to: ${path.basename(jsFilePath)}`);

      return jsFilePath;
    } catch (error) {
      console.error(`   ❌ Compilation failed:`, error instanceof Error ? error.message : String(error));
      throw error;
    }
  }

  listTemplates(): ToolTemplate[] {
    return Array.from(this.templates.values());
  }

  async generateToolFromPrompt(prompt: string): Promise<GeneratedTool> {
    // This would use an LLM to generate a tool from a natural language description
    // For now, throw an error indicating this requires LLM integration
    throw new Error('Generating tools from prompts requires LLM integration (not yet implemented)');
  }
}
