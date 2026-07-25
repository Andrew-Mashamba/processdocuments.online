// Core Types for @zima/visual-code-editor

export interface ClaudeCliOptions {
  workspacePath: string;
  model?: string;
  temperature?: number;
  maxTokens?: number;
  streaming?: boolean;
  verbose?: boolean;
}

export interface ClaudeCliResponse {
  content: string;
  usage: {
    input_tokens: number;
    output_tokens: number;
    cache_creation_input_tokens?: number;
    cache_read_input_tokens?: number;
  };
  model: string;
  stopReason: string;
  toolUses?: ToolUse[];
}

export interface ToolUse {
  id: string;
  name: string;
  input: any;
}

export interface Tool {
  name: string;
  description: string;
  inputSchema: any;
  category: 'document' | 'system' | 'visual' | 'web' | 'memory';
  provider: 'zima' | 'openclaw' | 'custom';
  execute?: (input: any) => Promise<any>;
}

export interface Message {
  role: 'user' | 'assistant';
  content: string | any[];
  timestamp: string;
  metadata?: {
    model?: string;
    usage?: any;
    sessionKey?: string;
    [key: string]: any;
  };
}

export interface Memory {
  id: number;
  content: string;
  embedding?: number[];
  metadata: {
    type: 'conversation' | 'file' | 'workspace' | 'custom';
    timestamp: string;
    sessionId?: string;
    tags?: string[];
    [key: string]: any;
  };
  createdAt: string;
}

export interface SearchOptions {
  query: string;
  limit?: number;
  threshold?: number;
  type?: Memory['metadata']['type'];
}

export interface SessionInfo {
  key: string;
  messageCount: number;
  firstMessageAt?: string;
  lastMessageAt?: string;
  size: number;
}

export interface ProjectInfo {
  framework: 'laravel-livewire' | 'react' | 'vue' | 'angular' | 'next' | 'nuxt' | 'unknown';
  buildTool: 'vite' | 'webpack' | 'laravel-mix' | 'turbopack' | 'unknown';
  cssFramework: 'tailwindcss' | 'bootstrap' | 'material-ui' | 'custom' | 'unknown';
  root: string;
  componentPaths: string[];
  assetPaths: string[];
  page: {
    url: string;
    title: string;
    lang: string;
    locale?: string;
  };
  components: ComponentInfo[];
}

export interface ComponentInfo {
  name: string;
  type: 'livewire' | 'react' | 'vue' | 'angular';
  id?: string;
  files?: ComponentFiles;
}

export interface ComponentFiles {
  class?: string;
  view?: string;
  styles?: string;
  tests?: string;
}

export interface ElementContext {
  visual: {
    screenshot: string;
    boundingBox: {
      x: number;
      y: number;
      width: number;
      height: number;
    };
    viewport: {
      width: number;
      height: number;
    };
    devicePixelRatio: number;
  };
  element: {
    tag: string;
    id: string;
    classes: string[];
    attributes: Record<string, string>;
    textContent: string;
    innerHTML: string;
  };
  selector: string;
  component?: ComponentInfo;
  context: {
    parent: ElementInfo;
    siblings: {
      before: ElementInfo[];
      after: ElementInfo[];
    };
    children: {
      count: number;
      types: string[];
    };
    depth: number;
  };
  styles: Record<string, string>;
  ocr?: {
    text: string;
    language: string;
    confidence: number;
  };
}

export interface ElementInfo {
  tag: string;
  id?: string;
  classes: string[];
}

export interface CodeChange {
  file: string;
  oldContent: string;
  newContent: string;
  diff: string;
  language: string;
}

export interface VisualEditorConfig {
  version: string;
  project: {
    framework: ProjectInfo['framework'];
    buildTool: ProjectInfo['buildTool'];
    root: string;
    componentPaths: string[];
    assetPaths: string[];
    testPaths?: string[];
  };
  agent: {
    port: number;
    model: string;
    temperature: number;
    maxTokens: number;
    contextOptimization: 'none' | 'adaptive' | 'aggressive';
    memoryEnabled: boolean;
    streamingEnabled: boolean;
    maxConcurrentRequests?: number;
  };
  visual: {
    hotkey: string;
    highlightColor: string;
    overlayZIndex: number;
    screenshotQuality: number;
    ocrEnabled: boolean;
    voiceInputEnabled?: boolean;
  };
  tools: {
    zimaFileService?: string;
    enableDocumentTools: boolean;
    enableWebTools: boolean;
    enableMemoryTools: boolean;
    customTools?: Tool[];
  };
  performance: {
    cacheEnabled: boolean;
    cacheTTL: number;
    promptCaching: boolean;
    tierOptimization: boolean;
    maxMemorySize?: string;
  };
  security: {
    apiKey?: string;
    allowedOrigins: string[];
    rateLimit: number;
    maxUploadSize?: string;
  };
  logging: {
    level: 'debug' | 'info' | 'warn' | 'error';
    file?: string;
    console: boolean;
  };
  advanced?: {
    customSystemPrompt?: string;
    toolTimeout?: number;
    sessionTTL?: number;
    autoSave?: boolean;
  };
}

export interface WSMessage {
  type: 'init' | 'ready' | 'element_selected' | 'command_submitted' |
        'ai_thinking' | 'code_preview' | 'code_preview_accepted' |
        'code_preview_rejected' | 'changes_applied' | 'error' |
        'dom_tool_call' | 'dom_tool_result' | 'persist_changes' | 'dom_preview_complete' |
        'generate_component' | 'component_generated' | 'refine_component' | 'component_refined';
  [key: string]: any;
}

export interface StreamChunk {
  type: 'start' | 'content' | 'files' | 'usage' | 'complete' | 'error';
  data: any;
}
