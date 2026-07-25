import { WebSocketServer, WebSocket } from 'ws';
import { v4 as uuidv4 } from 'uuid';
import { ProjectInfo, ElementContext, WSMessage, CodeChange } from '../types';
import { ClaudeCliRuntime } from '../agent/claude-cli-runtime';
import { ToolRegistry } from '../agent/tool-registry';

export class VisualEditorWebSocketServer {
  private wss: WebSocketServer;
  private projectContext: Map<string, ProjectInfo> = new Map();
  private componentFileMap: Map<string, any> = new Map();
  private runtime: ClaudeCliRuntime;
  private toolRegistry: ToolRegistry;

  constructor(
    port: number,
    runtime: ClaudeCliRuntime,
    toolRegistry: ToolRegistry
  ) {
    this.runtime = runtime;
    this.toolRegistry = toolRegistry;
    this.wss = new WebSocketServer({ port });
    this.setupHandlers();

    console.log(`✓ Visual Editor WebSocket Server running on ws://localhost:${port}`);
  }

  /**
   * Setup WebSocket handlers
   */
  private setupHandlers(): void {
    this.wss.on('connection', (ws: WebSocket, req) => {
      const sessionId = uuidv4();
      console.log(`WebSocket client connected: ${sessionId}`);

      ws.on('message', async (data: Buffer) => {
        try {
          const message: WSMessage = JSON.parse(data.toString());
          await this.handleMessage(sessionId, message, ws);
        } catch (error: any) {
          console.error('WebSocket message error:', error);
          this.sendError(ws, error.message);
        }
      });

      ws.on('close', () => {
        console.log(`WebSocket client disconnected: ${sessionId}`);
        this.projectContext.delete(sessionId);
      });

      ws.on('error', (error) => {
        console.error('WebSocket error:', error);
      });
    });
  }

  /**
   * Handle incoming WebSocket message
   */
  private async handleMessage(
    sessionId: string,
    message: WSMessage,
    ws: WebSocket
  ): Promise<void> {
    switch (message.type) {
      case 'init':
        await this.handleInit(sessionId, message.projectInfo, ws);
        break;

      case 'element_selected':
        await this.handleElementSelection(sessionId, message.context, ws);
        break;

      case 'command_submitted':
        await this.handleCommand(
          sessionId,
          message.contexts || [message.context], // Support both plural and singular
          message.command,
          ws
        );
        break;

      case 'code_preview_accepted':
        await this.handleCodeAccepted(sessionId, message.changes, ws);
        break;

      case 'code_preview_rejected':
        this.send(ws, { type: 'ready' });
        break;

      case 'dom_tool_result':
        await this.handleDOMToolResult(sessionId, message.tool, message.result, ws);
        break;

      case 'persist_changes':
        await this.handlePersistChanges(sessionId, message.changeLog, message.summary, ws);
        break;

      case 'generate_component':
        await this.handleGenerateComponent(
          sessionId,
          message.componentType,
          message.instructions,
          message.pageContext,
          message.insertionPoint,
          ws
        );
        break;

      case 'refine_component':
        await this.handleRefineComponent(
          sessionId,
          message.instructions,
          message.currentHtml,
          message.insertionPoint,
          ws
        );
        break;

      default:
        console.warn('Unknown message type:', message.type);
    }
  }

  /**
   * Handle project initialization
   */
  private async handleInit(
    sessionId: string,
    projectInfo: ProjectInfo,
    ws: WebSocket
  ): Promise<void> {
    this.projectContext.set(sessionId, projectInfo);

    // Map components to files
    await this.mapProjectFiles(projectInfo);

    this.send(ws, {
      type: 'ready',
      message: 'Visual editor initialized',
    });
  }

  /**
   * Map project components to files
   */
  private async mapProjectFiles(projectInfo: ProjectInfo): Promise<void> {
    // Framework-specific mapping
    // For now, placeholder logic
    for (const component of projectInfo.components) {
      const files = await this.mapComponentToFiles(component.name, projectInfo.framework);
      this.componentFileMap.set(component.name, files);
    }
  }

  /**
   * Map component name to source files
   */
  private async mapComponentToFiles(
    componentName: string,
    framework: ProjectInfo['framework']
  ): Promise<any> {
    // Placeholder: Would use framework-specific mappers
    return {
      class: null,
      view: null,
      styles: null,
    };
  }

  /**
   * Handle element selection
   */
  private async handleElementSelection(
    sessionId: string,
    context: ElementContext,
    ws: WebSocket
  ): Promise<void> {
    this.send(ws, {
      type: 'ready',
      context,
    } as any);
  }

  /**
   * Handle user command
   */
  private async handleCommand(
    sessionId: string,
    contexts: ElementContext[],
    command: string,
    ws: WebSocket
  ): Promise<void> {
    const projectInfo = this.projectContext.get(sessionId);
    if (!projectInfo) {
      throw new Error('Project context not found');
    }

    // Send thinking status
    this.send(ws, {
      type: 'ai_thinking',
      content: 'Analyzing your request...',
    });

    try {
      // Build visual edit prompt
      const prompt = this.buildVisualEditPrompt(projectInfo, contexts, command);

      // Execute Claude CLI
      let fullContent = '';
      const response = await this.runtime.run(prompt, {
        streaming: true,
        onChunk: (chunk: any) => {
          if (chunk.type === 'content') {
            fullContent += chunk.content;
            this.send(ws, {
              type: 'ai_thinking',
              content: chunk.content,
            });
          }
        },
      });

      // Extract code changes from tool uses
      const changes = await this.extractCodeChanges(response.toolUses || []);

      // Send preview
      this.send(ws, {
        type: 'code_preview',
        changes,
        explanation: fullContent,
      });

    } catch (error: any) {
      this.sendError(ws, error.message);
    }
  }

  /**
   * Build visual edit prompt
   */
  private buildVisualEditPrompt(
    projectInfo: ProjectInfo,
    contexts: ElementContext[],
    command: string
  ): any {
    const elementsInfo = contexts.map((ctx, idx) => `
**Element ${idx + 1}:**
- Tag: ${ctx.element.tag}
- ID: ${ctx.element.id || 'none'}
- Classes: ${ctx.element.classes.join(', ') || 'none'}
- Selector: ${ctx.selector}
- Component: ${ctx.component?.name || 'none'}
- Current Content: ${ctx.element.innerHTML.substring(0, 200)}${ctx.element.innerHTML.length > 200 ? '...' : ''}
`).join('\n');

    const systemPrompt = `You are a visual code editor agent for a ${projectInfo.framework} project.

**Your Task:**
The user has selected ${contexts.length} UI element${contexts.length > 1 ? 's' : ''} and wants to: "${command}"

**Project Context:**
- Framework: ${projectInfo.framework}
- CSS Framework: ${projectInfo.cssFramework}
- Build Tool: ${projectInfo.buildTool}
- Language: ${projectInfo.page.lang}

**Selected Elements:**
${elementsInfo}

**IMPORTANT - TWO-PHASE WORKFLOW:**

**PHASE 1: DOM Manipulation (Instant Preview)**
Use the DOM manipulation tools to make changes directly in the browser:
- dom_set_style: Change CSS styles instantly
- dom_add_class / dom_remove_class: Add/remove CSS classes
- dom_set_text / dom_set_html: Change content
- dom_set_attribute: Change element attributes
- dom_append_child / dom_remove: Add/remove elements
- dom_show / dom_hide: Show/hide elements

These changes happen INSTANTLY for the user to preview.

**Instructions:**
1. Use DOM tools to manipulate the selected elements
2. Apply changes to ALL selected elements consistently
3. Use ${projectInfo.cssFramework} classes if available
4. Make minimal changes (only what's requested)
5. When done, the user will see changes live
6. If the user likes the preview, they'll click "Save" to persist to code

**DO NOT:**
- Use file reading/writing tools (read, write, edit)
- Try to access source files
- Generate code yet (that happens when user clicks "Save")

Begin by using DOM tools to manipulate the page.
`;

    const userContent: any[] = [
      {
        type: 'text',
        text: `User command: "${command}"\n\nNumber of elements: ${contexts.length}`,
      },
    ];

    // Add screenshot from first context if available
    if (contexts[0]?.visual?.screenshot) {
      userContent.push({
        type: 'image',
        source: {
          type: 'base64',
          media_type: 'image/png',
          data: contexts[0].visual.screenshot.replace(/^data:image\/png;base64,/, ''),
        },
      });
    }

    return this.runtime.buildPrompt(systemPrompt, [
      {
        role: 'user',
        content: userContent,
      },
    ]);
  }

  /**
   * Extract code changes from tool uses
   */
  private async extractCodeChanges(toolUses: any[]): Promise<CodeChange[]> {
    const changes: CodeChange[] = [];

    for (const toolUse of toolUses) {
      if (toolUse.name === 'write' || toolUse.name === 'edit') {
        changes.push({
          file: toolUse.input.file_path,
          oldContent: '',
          newContent: toolUse.input.content || toolUse.input.new_string || '',
          diff: '', // Would calculate actual diff
          language: this.detectLanguage(toolUse.input.file_path),
        });
      }
    }

    return changes;
  }

  /**
   * Detect file language from extension
   */
  private detectLanguage(filePath: string): string {
    const ext = filePath.split('.').pop()?.toLowerCase();
    const langMap: Record<string, string> = {
      'php': 'php',
      'js': 'javascript',
      'ts': 'typescript',
      'jsx': 'javascript',
      'tsx': 'typescript',
      'vue': 'vue',
      'blade.php': 'blade',
      'css': 'css',
      'scss': 'scss',
      'html': 'html',
    };

    return langMap[ext || ''] || 'text';
  }

  /**
   * Handle code accepted
   */
  private async handleCodeAccepted(
    sessionId: string,
    changes: CodeChange[],
    ws: WebSocket
  ): Promise<void> {
    try {
      // Changes already applied by Claude CLI
      // Just confirm to client
      this.send(ws, {
        type: 'changes_applied',
        message: 'Code changes have been applied. Page will reload via HMR.',
      });
    } catch (error: any) {
      this.sendError(ws, error.message);
    }
  }

  /**
   * Handle DOM tool result from browser
   */
  private async handleDOMToolResult(
    sessionId: string,
    tool: string,
    result: any,
    ws: WebSocket
  ): Promise<void> {
    // Tool result received, AI can continue
    console.log(`✓ DOM tool result: ${tool}`, result);
  }

  /**
   * Handle persist changes request
   */
  private async handlePersistChanges(
    sessionId: string,
    changeLog: any[],
    summary: string,
    ws: WebSocket
  ): Promise<void> {
    const projectInfo = this.projectContext.get(sessionId);
    if (!projectInfo) {
      throw new Error('Project context not found');
    }

    this.send(ws, {
      type: 'ai_thinking',
      content: 'Analyzing DOM changes and generating code...',
    });

    try {
      // Build prompt to generate code from DOM changes
      const prompt = this.buildCodeGenerationPrompt(projectInfo, changeLog, summary);

      // Execute Claude CLI
      let fullContent = '';
      const response = await this.runtime.run(prompt, {
        streaming: true,
        onChunk: (chunk: any) => {
          if (chunk.type === 'content') {
            fullContent += chunk.content;
          }
        },
      });

      // Extract code changes from tool uses
      const changes = await this.extractCodeChanges(response.toolUses || []);

      // Send code preview to user
      this.send(ws, {
        type: 'code_preview',
        changes,
        explanation: fullContent,
      });

    } catch (error: any) {
      this.sendError(ws, error.message);
    }
  }

  /**
   * Handle component generation request
   */
  private async handleGenerateComponent(
    sessionId: string,
    componentType: string,
    instructions: string,
    pageContext: any,
    insertionPoint: string,
    ws: WebSocket
  ): Promise<void> {
    const projectInfo = this.projectContext.get(sessionId);
    if (!projectInfo) {
      throw new Error('Project context not found');
    }

    this.send(ws, {
      type: 'ai_thinking',
      content: `Generating ${componentType} component...`,
    });

    try {
      const prompt = this.buildComponentGenerationPrompt(
        projectInfo,
        componentType,
        instructions,
        pageContext,
        insertionPoint
      );

      const response = await this.runtime.run(prompt, {
        streaming: false,
      });

      // Extract HTML from response
      const html = this.extractHtmlFromResponse(response.content);

      this.send(ws, {
        type: 'component_generated',
        html,
        explanation: response.content,
      });

    } catch (error: any) {
      this.sendError(ws, error.message);
    }
  }

  /**
   * Handle component refinement request
   */
  private async handleRefineComponent(
    sessionId: string,
    instructions: string,
    currentHtml: string,
    insertionPoint: string,
    ws: WebSocket
  ): Promise<void> {
    const projectInfo = this.projectContext.get(sessionId);
    if (!projectInfo) {
      throw new Error('Project context not found');
    }

    this.send(ws, {
      type: 'ai_thinking',
      content: 'Refining component...',
    });

    try {
      const prompt = this.buildComponentRefinementPrompt(
        projectInfo,
        instructions,
        currentHtml,
        insertionPoint
      );

      const response = await this.runtime.run(prompt, {
        streaming: false,
      });

      // Extract HTML from response
      const html = this.extractHtmlFromResponse(response.content);

      this.send(ws, {
        type: 'component_refined',
        html,
        explanation: response.content,
      });

    } catch (error: any) {
      this.sendError(ws, error.message);
    }
  }

  /**
   * Build component generation prompt
   */
  private buildComponentGenerationPrompt(
    projectInfo: ProjectInfo,
    componentType: string,
    instructions: string,
    pageContext: any,
    insertionPoint: string
  ): any {
    const systemPrompt = `You are a Tailwind CSS component generator for a ${projectInfo.framework} project.

**Your Task:**
Generate a complete, production-ready Tailwind CSS component based on the user's request.

**Component Type:** ${componentType}
**Additional Instructions:** ${instructions || 'None - use best practices'}

**Page Context:**
- URL: ${pageContext.url}
- Title: ${pageContext.title}
- Framework: ${pageContext.framework}
- CSS Framework: ${projectInfo.cssFramework}
- Insertion Point: ${insertionPoint}
- Existing Classes (for reference): ${pageContext.existingClasses?.slice(0, 5).join(', ') || 'None'}

**IMPORTANT REQUIREMENTS:**

1. **Use Tailwind CSS utility classes** (v3.x)
2. **Return ONLY the HTML** - no explanations before or after the code
3. **Make it responsive** with Tailwind breakpoints (sm, md, lg, xl)
4. **Use semantic HTML5** elements (section, article, header, etc.)
5. **Include proper accessibility** (ARIA labels, roles, alt text)
6. **Follow modern design principles** (proper spacing, hierarchy, contrast)
7. **Make it interactive** if applicable (hover states, focus states)
8. **Use Tailwind colors** from the default palette
9. **Include transitions** where appropriate (transition-all, duration-300)
10. **Match the design aesthetic** of similar sites (clean, modern, professional)

**Example Structure:**
For a "Primary Button":
\`\`\`html
<button class="px-6 py-3 bg-blue-600 text-white font-medium rounded-lg shadow-md hover:bg-blue-700 hover:shadow-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-all duration-200 active:scale-95">
  Click Me
</button>
\`\`\`

**DO NOT:**
- Include \`\`\`html or \`\`\` markers
- Add explanatory text before or after
- Use inline styles (use Tailwind classes only)
- Use custom CSS classes that don't exist in Tailwind

Generate the ${componentType} component now.`;

    return this.runtime.buildPrompt(systemPrompt, [
      {
        role: 'user',
        content: [
          {
            type: 'text',
            text: `Generate a ${componentType} component${instructions ? ` with these requirements: ${instructions}` : ''}`,
          },
        ],
      },
    ]);
  }

  /**
   * Build component refinement prompt
   */
  private buildComponentRefinementPrompt(
    projectInfo: ProjectInfo,
    instructions: string,
    currentHtml: string,
    insertionPoint: string
  ): any {
    const systemPrompt = `You are refining a Tailwind CSS component.

**Current Component HTML:**
\`\`\`html
${currentHtml}
\`\`\`

**Refinement Request:**
${instructions}

**Project Context:**
- Framework: ${projectInfo.framework}
- CSS Framework: ${projectInfo.cssFramework}
- Insertion Point: ${insertionPoint}

**IMPORTANT:**
1. Keep the same component structure unless the refinement requires major changes
2. Use only Tailwind CSS utility classes
3. Return ONLY the updated HTML - no code blocks or explanations
4. Make sure the component remains responsive and accessible
5. Apply the requested changes precisely

Generate the refined component now.`;

    return this.runtime.buildPrompt(systemPrompt, [
      {
        role: 'user',
        content: [
          {
            type: 'text',
            text: `Refine the component: ${instructions}`,
          },
        ],
      },
    ]);
  }

  /**
   * Extract HTML from Claude's response
   */
  private extractHtmlFromResponse(content: string): string {
    // Remove markdown code blocks if present
    let html = content.trim();

    // Remove ```html and ``` markers
    html = html.replace(/^```html\n?/i, '');
    html = html.replace(/^```\n?/, '');
    html = html.replace(/\n?```$/,'');

    // If response has explanatory text, try to extract just the HTML
    const htmlMatch = html.match(/<[^>]+>[\s\S]*<\/[^>]+>/);
    if (htmlMatch) {
      html = htmlMatch[0];
    }

    return html.trim();
  }

  /**
   * Build code generation prompt from DOM changes
   */
  private buildCodeGenerationPrompt(
    projectInfo: ProjectInfo,
    changeLog: any[],
    summary: string
  ): any {
    const changeDetails = changeLog.map((change, idx) => `
${idx + 1}. ${change.description}
   Tool: ${change.tool}
   Params: ${JSON.stringify(change.params, null, 2)}
`).join('\n');

    const systemPrompt = `You are a code generator for a ${projectInfo.framework} project.

**Your Task:**
The user made visual changes to the page using DOM manipulation. Now you need to generate the actual source code to persist these changes.

**Project Context:**
- Framework: ${projectInfo.framework}
- CSS Framework: ${projectInfo.cssFramework}
- Build Tool: ${projectInfo.buildTool}
- Language: ${projectInfo.page.lang}

**DOM Changes Made:**
${changeDetails}

**Summary:**
${summary}

**Instructions:**
1. Use the 'read' tool to read the relevant source files
2. Analyze the current code structure
3. Generate code that implements the same changes permanently
4. Use the 'write' or 'edit' tool to update source files
5. Explain what you did

**Important:**
- Match the DOM changes exactly
- Preserve existing functionality
- Use ${projectInfo.cssFramework} for styling
- Follow ${projectInfo.framework} best practices
- Make minimal changes

Begin by reading the source files to understand the current structure.
`;

    const userContent: any[] = [
      {
        type: 'text',
        text: `Generate code for these ${changeLog.length} DOM changes:\n\n${summary}`,
      },
    ];

    return this.runtime.buildPrompt(systemPrompt, [
      {
        role: 'user',
        content: userContent,
      },
    ]);
  }

  /**
   * Send message to WebSocket client
   */
  private send(ws: WebSocket, message: WSMessage): void {
    if (ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify(message));
    }
  }

  /**
   * Send error message
   */
  private sendError(ws: WebSocket, error: string): void {
    this.send(ws, {
      type: 'error',
      error,
    });
  }

  /**
   * Close server
   */
  close(): void {
    this.wss.close();
  }
}
