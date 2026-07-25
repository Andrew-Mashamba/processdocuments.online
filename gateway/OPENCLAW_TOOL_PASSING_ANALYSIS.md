# OpenClaw Tool-Passing Mechanism - Complete Analysis

**Date:** 2026-01-31
**Investigation:** How OpenClaw passes tools to Claude (vs our current approach)

---

## Executive Summary

**Critical Finding:** OpenClaw does **NOT** use Claude CLI. They use the **Anthropic SDK** (`@mariozechner/pi-ai` + `@mariozechner/pi-coding-agent`) which has **native tool support built-in**.

Our current implementation tries to use `claude` CLI directly, which **does not support custom tool definitions**.

---

## OpenClaw's Architecture

### File: `src/agents/pi-embedded-runner/run/attempt.ts`

**Lines 1-7:**
```typescript
import { streamSimple } from "@mariozechner/pi-ai";
import { createAgentSession, SessionManager, SettingsManager }
  from "@mariozechner/pi-coding-agent";
```

**Lines 201-234: Create Tools**
```typescript
const toolsRaw = params.disableTools
  ? []
  : createOpenClawCodingTools({
      exec: { ...params.execOverrides, elevated: params.bashElevated },
      sandbox,
      messageProvider: params.messageChannel ?? params.messageProvider,
      agentAccountId: params.agentAccountId,
      messageTo: params.messageTo,
      messageThreadId: params.messageThreadId,
      // ... 20+ more parameters
      sessionKey: params.sessionKey ?? params.sessionId,
      agentDir,
      workspaceDir: effectiveWorkspace,
      config: params.config,
      abortSignal: runAbortController.signal,
      modelProvider: params.model.provider,
      modelId: params.modelId,
      modelAuthMode: resolveModelAuthMode(params.model.provider, params.config),
      modelHasVision,
    });
```

**Lines 435-438: Split Tools**
```typescript
const { builtInTools, customTools } = splitSdkTools({
  tools,
  sandboxEnabled: !!sandbox?.enabled,
});
```

**Lines 450-465: Pass Tools to SDK**
```typescript
({ session } = await createAgentSession({
  cwd: resolvedWorkspace,
  agentDir,
  authStorage: params.authStorage,
  modelRegistry: params.modelRegistry,
  model: params.model,
  thinkingLevel: mapThinkingLevel(params.thinkLevel),
  systemPrompt,
  tools: builtInTools,           // ← SDK built-in tools
  customTools: allCustomTools,   // ← Custom OpenClaw tools
  sessionManager,
  settingsManager,
  skills: [],
  contextFiles: [],
  additionalExtensionPaths,
}));
```

**Line 493: Stream Function**
```typescript
activeSession.agent.streamFn = streamSimple;
```

---

## The Key Difference

### OpenClaw Approach (Working):
```
createOpenClawCodingTools() → tools[]
         ↓
splitSdkTools() → { builtInTools, customTools }
         ↓
createAgentSession({ tools, customTools }) → session with SDK
         ↓
SDK handles tool execution internally
         ↓
streamSimple() sends tools to Anthropic API
```

### Our Current Approach (Broken):
```
UnifiedToolRegistry.getTools() → 216 tools
         ↓
spawn('claude', [...]) ← NO TOOLS PASSED
         ↓
Claude CLI has only built-in tools
         ↓
memory_search NOT available
```

---

## What OpenClaw Uses

### NPM Packages:
- `@mariozechner/pi-ai` - Anthropic SDK wrapper with streaming
- `@mariozechner/pi-coding-agent` - Agent session management + tool support
- Both packages support **Anthropic's native tool calling API**

### Tool Structure:
```typescript
interface AnyAgentTool {
  name: string;
  description: string;
  input_schema: {
    type: "object";
    properties: Record<string, any>;
    required?: string[];
  };
  execute: (input: any) => Promise<any>;
}
```

Tools are defined with:
1. **Schema** (name, description, input_schema)
2. **Executor function** (async function that runs the tool)

---

## How OpenClaw Creates Tools

### File: `src/agents/pi-tools.ts`

**createOpenClawCodingTools() returns:**

```typescript
export function createOpenClawCodingTools(options): AnyAgentTool[] {
  // Built-in coding tools from SDK
  const codingToolsBase = [
    createReadTool(...),
    createWriteTool(...),
    createEditTool(...),
    // ...
  ];

  // OpenClaw custom tools
  const customTools = [
    createExecTool(...),           // bash execution
    createProcessTool(...),         // process management
    ...createOpenClawTools(...),   // memory_search, sessions, etc.
    ...listChannelAgentTools(...), // messaging tools
  ];

  // Filter by policy
  return filterToolsByPolicy([...codingToolsBase, ...customTools], ...);
}
```

**createOpenClawTools() includes:**
- `memory_search` - Semantic workspace search
- `memory_get` - Read specific memory files
- `sessions_spawn` - Spawn sub-agents
- `sessions_history` - Read session history
- `session_status` - Get session info
- `camera` - Camera access
- `gateway` - Gateway management

---

## How SDK Passes Tools to Anthropic

### File: `@mariozechner/pi-ai/dist/index.d.ts` (inferred)

```typescript
export function streamSimple(params: {
  model: string;
  systemPrompt: string;
  messages: Message[];
  tools?: Tool[];           // ← Tools passed here
  temperature?: number;
  maxTokens?: number;
  apiKey: string;
}): AsyncIterator<StreamEvent>;
```

The SDK:
1. Takes tool definitions with schemas
2. Converts them to Anthropic's tool format
3. Includes them in the API request
4. Handles tool_use blocks from Claude
5. Calls the executor functions
6. Returns tool results to Claude

---

## Why Claude CLI Doesn't Work

### Claude CLI (`claude` binary):

**Designed for:**
- Interactive terminal use
- Built-in tools only (Read, Write, Bash, Grep, etc.)
- File-based workflows

**NOT designed for:**
- Custom tool injection
- Programmatic tool registration
- External tool executors

**No API for:**
```bash
claude --tools /path/to/tools.json  # ← Doesn't exist
claude --custom-tools-file ...       # ← Doesn't exist
```

The CLI is meant to be used **as-is** with its predefined tools.

---

## Our Implementation Gap

### Current Code: `src/agent/claude-cli-runtime.ts`

**Lines 38-54:**
```typescript
// Load tools ✅
await this.registry.refresh();
const tools = await this.registry.getTools(); // 216 tools loaded
console.log(`✓ Loaded ${tools.length} tools`);

// Spawn Claude CLI ❌ (tools not used!)
const proc = spawn('claude', [
  '--print',
  '--output-format', 'json',
  '--dangerously-skip-permissions'
  // ← NO --tools argument
  // ← NO way to pass custom tools
], {
  cwd: this.config.storage?.workspace || process.cwd(),
  stdio: ['pipe', 'pipe', 'pipe']
});
```

**buildPrompt() - Lines 238-258:**
```typescript
private buildPrompt(context: AgentContext): string {
  let prompt = '';

  // Add system prompt
  if (context.systemPrompt) {
    prompt += context.systemPrompt + '\n\n';
  }

  // Add conversation history
  for (const msg of context.messages) {
    // ...
  }

  // ← Tools NOT included in prompt
  // ← No tool definitions sent

  return prompt;
}
```

**The tools are loaded but never used!**

---

## Solution Options

### Option 1: Switch to Anthropic SDK (Recommended)

**Implementation:**
```typescript
import Anthropic from '@anthropic-ai/sdk';

class AnthropicSdkRuntime {
  private client: Anthropic;

  async execute(context: AgentContext): Promise<MessageResponse> {
    const tools = await this.registry.getTools();

    const response = await this.client.messages.create({
      model: 'claude-sonnet-4-20250514',
      max_tokens: 8192,
      system: context.systemPrompt,
      messages: context.messages,
      tools: tools.map(tool => ({
        name: tool.name,
        description: tool.description,
        input_schema: tool.input_schema
      }))
    });

    // Handle tool_use blocks
    for (const block of response.content) {
      if (block.type === 'tool_use') {
        const result = await this.executor.execute({
          id: block.id,
          name: block.name,
          input: block.input
        }, sessionKey);
        // Return result to Claude
      }
    }
  }
}
```

**Pros:**
- ✅ Native tool support
- ✅ Streaming built-in
- ✅ Official Anthropic SDK
- ✅ Well-documented

**Cons:**
- ❌ Requires API key (credits/usage limits)
- ❌ Rate limiting applies
- ❌ Cost per request

---

### Option 2: Implement Tool Interception Layer

**Concept:** Modify Claude CLI prompts to include tool schemas, parse tool calls from output.

```typescript
class ClaudeCliWithToolsRuntime {
  async execute(context: AgentContext): Promise<MessageResponse> {
    const tools = await this.registry.getTools();

    // Inject tool schemas into system prompt
    const systemPromptWithTools = `
${context.systemPrompt}

## Available Tools

You have access to the following tools:

${tools.map(t => `
### ${t.name}
${t.description}

Input schema:
${JSON.stringify(t.input_schema, null, 2)}

To use this tool, output:
<tool_use>
<tool_name>${t.name}</tool_name>
<tool_input>${'{...}'}</tool_input>
</tool_use>
`).join('\n')}
`;

    // Run Claude CLI
    const response = await this.runCli({ systemPrompt: systemPromptWithTools, ... });

    // Parse <tool_use> blocks from response
    const toolCalls = this.extractToolCalls(response);

    // Execute tools
    for (const call of toolCalls) {
      const result = await this.executor.execute(call, sessionKey);
      // Continue conversation with tool result
    }
  }
}
```

**Pros:**
- ✅ No API key required
- ✅ No rate limits
- ✅ Keeps Claude CLI

**Cons:**
- ❌ Fragile (parsing text output)
- ❌ Not official tool protocol
- ❌ May confuse Claude
- ❌ Requires multiple CLI invocations per turn

---

### Option 3: Use OpenClaw's SDK Packages

**Implementation:**
```typescript
import { createAgentSession } from '@mariozechner/pi-coding-agent';
import { streamSimple } from '@mariozechner/pi-ai';

class OpenClawSdkRuntime {
  async execute(context: AgentContext): Promise<MessageResponse> {
    const tools = await this.registry.getTools();
    const { builtInTools, customTools } = this.splitTools(tools);

    const { session } = await createAgentSession({
      cwd: this.config.storage.workspace,
      agentDir: this.config.storage.agentDir,
      model: this.resolveModel(),
      systemPrompt: context.systemPrompt,
      tools: builtInTools,
      customTools: customTools,
      sessionManager: this.sessionManager,
      // ...
    });

    // Stream response
    for await (const event of session.agent.stream(userMessage)) {
      // Handle events
    }
  }
}
```

**Pros:**
- ✅ Proven solution (OpenClaw uses it)
- ✅ Tool support built-in
- ✅ Session management included

**Cons:**
- ❌ Adds dependency
- ❌ Still requires API key
- ❌ Less control over implementation

---

## Recommended Path Forward

### Immediate (Fix memory_search):

**Option 1A: Switch to Anthropic SDK**

1. Install: `npm install @anthropic-ai/sdk`
2. Create new runtime: `src/agent/anthropic-sdk-runtime.ts`
3. Implement tool execution loop
4. Update `src/server.ts` to use new runtime
5. Add `ANTHROPIC_API_KEY` requirement

**Estimated effort:** 4-6 hours
**Risk:** Low (official SDK)

### Future (Explore alternatives):

1. Research if Claude CLI will add tool support
2. Consider Claude Desktop integration
3. Evaluate cost vs. performance tradeoffs

---

## Comparison: Claude CLI vs Anthropic SDK

| Feature | Claude CLI | Anthropic SDK |
|---------|-----------|---------------|
| **Custom tools** | ❌ No | ✅ Yes |
| **Streaming** | ✅ Yes | ✅ Yes |
| **API key required** | ❌ No | ✅ Yes |
| **Rate limits** | ❌ No | ✅ Yes (tier-based) |
| **Cost** | Free | $3-15 per 1M tokens |
| **Built-in tools** | ✅ 20+ | ❌ None (must implement) |
| **Session management** | ✅ Auto | ⚠️ Manual |
| **Official support** | ✅ Yes | ✅ Yes |
| **Tool execution** | Auto | Manual (must handle) |

---

## Code Examples from OpenClaw

### Memory Search Tool Definition

**File:** `src/agents/memory-search.ts`

```typescript
export function createMemorySearchTool(params: {
  agentDir: string;
  workspaceDir: string;
}): AnyAgentTool {
  return {
    name: 'memory_search',
    description: 'Semantically search MEMORY.md and memory/*.md files for prior work, decisions, preferences',
    input_schema: {
      type: 'object',
      properties: {
        query: {
          type: 'string',
          description: 'Search query'
        },
        limit: {
          type: 'number',
          description: 'Max results',
          default: 5
        }
      },
      required: ['query']
    },
    async execute(input) {
      const { query, limit = 5 } = input;

      // Actual implementation
      const results = await searchMemoryFiles({
        agentDir: params.agentDir,
        workspaceDir: params.workspaceDir,
        query,
        limit
      });

      return results;
    }
  };
}
```

### Tool Registration

**File:** `src/agents/pi-tools.ts` (lines 200+)

```typescript
const memoryTools = [
  createMemorySearchTool({ agentDir, workspaceDir }),
  createMemoryGetTool({ agentDir, workspaceDir }),
];

const allTools = [
  ...codingTools,
  ...memoryTools,
  ...execTools,
  ...messagingTools,
  // ...
];

return filterToolsByPolicy(allTools, policyConfig);
```

---

## Memory Search Implementation Details

**File:** `src/agents/memory-search.ts` (lines 200+)

```typescript
async function searchMemoryFiles(params: {
  agentDir: string;
  workspaceDir: string;
  query: string;
  limit: number;
}): Promise<MemorySearchResult[]> {
  const memoryDir = path.join(params.workspaceDir, 'memory');
  const memoryFile = path.join(params.workspaceDir, 'MEMORY.md');

  // Read all memory files
  const files = await glob('**/*.md', { cwd: memoryDir });
  files.push(memoryFile);

  // Semantic search using embeddings
  const results = await semanticSearch({
    files,
    query: params.query,
    limit: params.limit,
    embedder: getEmbedder() // OpenAI or local
  });

  return results.map(r => ({
    file: r.file,
    content: r.chunk,
    score: r.similarity
  }));
}
```

**OpenClaw uses:**
- File-based memory (MEMORY.md + memory/*.md)
- Embeddings for semantic search (OpenAI API)
- No SQLite vector database (simpler approach)

---

## Conclusion

**The memory system in our gateway is fully implemented and working**, but it's **not connected** to the runtime because:

1. We use Claude CLI (no custom tool support)
2. OpenClaw uses Anthropic SDK (native tool support)
3. Tools are loaded but never passed to Claude

**To fix:** Switch from Claude CLI to Anthropic SDK, implementing the tool execution loop as OpenClaw does.

**Trade-off:** Gain tool support, lose "no API key" benefit of Claude CLI.

---

**Investigation completed:** 2026-01-31 14:30
**Key files analyzed:**
- `/reference/openclaw-main/src/agents/pi-embedded-runner/run/attempt.ts`
- `/reference/openclaw-main/src/agents/pi-tools.ts`
- `/reference/openclaw-main/src/agents/memory-search.ts`
- `/gateway/src/agent/claude-cli-runtime.ts`
