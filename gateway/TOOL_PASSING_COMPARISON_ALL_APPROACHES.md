# Tool-Passing Comparison: Three Approaches

**Date:** 2026-01-31
**Analysis:** Complete comparison of tool integration methods

---

## Executive Summary

We studied three different approaches to passing custom tools to Claude/LLMs:

1. **Current Gateway**: Claude CLI (❌ no custom tools)
2. **OpenClaw**: Anthropic SDK via `@mariozechner/pi-coding-agent` (✅ Anthropic-only)
3. **OpenCode**: Vercel AI SDK (`ai` package) (✅ multi-provider)

**Key Finding:** Our current implementation uses Claude CLI which **cannot accept custom tool definitions**. Both OpenClaw and OpenCode solve this by using SDKs that support the tool calling API.

---

## Approach 1: Current Gateway (Claude CLI)

### Architecture

**File:** `src/agent/claude-cli-runtime.ts`

```typescript
// Lines 38-54: Load tools but don't use them
await this.registry.refresh();
const tools = await this.registry.getTools(); // 216 tools loaded ✅
console.log(`✓ Loaded ${tools.length} tools`);

// Spawn Claude CLI ❌
const proc = spawn('claude', [
  '--print',
  '--output-format', 'json',
  '--dangerously-skip-permissions'
  // ← NO --tools parameter
  // ← NO way to pass custom tools
], {
  cwd: this.config.storage?.workspace || process.cwd(),
  stdio: ['pipe', 'pipe', 'pipe']
});

// Send text prompt only (no tool definitions)
proc.stdin.write(this.buildPrompt(context));
```

### Tool Flow

```
UnifiedToolRegistry.getTools() → 216 tools loaded
         ↓
spawn('claude', [...]) → Tools NOT passed ❌
         ↓
Claude CLI → Only built-in tools available
         ↓
memory_search NOT available
```

### What Works

✅ **Claude CLI built-in tools:**
- Read, Write, Edit, Bash, Grep, Glob
- Task, WebSearch, WebFetch
- AskUserQuestion, EnterPlanMode
- ~20 total built-in tools

✅ **Session transcripts:**
- Saved to `~/.zima/agents/main/sessions/*.jsonl`
- Claude recalls context within session perfectly

### What Doesn't Work

❌ **Custom gateway tools:**
- `memory_search` - Semantic workspace search
- `memory_get` - Read memory files
- 196 ZIMA document tools (CreateExcel, CreatePDF, etc.)
- All custom tools from UnifiedToolRegistry

### Why It Fails

Claude CLI is designed for:
- Interactive terminal use
- Built-in tools only
- File-based workflows

Claude CLI does NOT support:
- Custom tool injection
- Programmatic tool registration
- External tool executors

**No CLI API for:**
```bash
claude --tools /path/to/tools.json      # ❌ Doesn't exist
claude --custom-tools memory_search     # ❌ Doesn't exist
```

---

## Approach 2: OpenClaw (Anthropic SDK)

### Architecture

**File:** `src/agents/pi-embedded-runner/run/attempt.ts`

```typescript
// Lines 1-7: Imports
import { streamSimple } from "@mariozechner/pi-ai";
import { createAgentSession } from "@mariozechner/pi-coding-agent";

// Lines 201-234: Create tools
const toolsRaw = createOpenClawCodingTools({
  exec: { ...params.execOverrides },
  sandbox,
  sessionKey: params.sessionKey,
  agentDir,
  workspaceDir: effectiveWorkspace,
  config: params.config,
  modelProvider: params.model.provider,
  // ... 20+ parameters
});

// Lines 435-438: Split tools
const { builtInTools, customTools } = splitSdkTools({
  tools,
  sandboxEnabled: !!sandbox?.enabled,
});

// Lines 450-465: Pass to SDK
const { session } = await createAgentSession({
  cwd: resolvedWorkspace,
  agentDir,
  model: params.model,
  systemPrompt,
  tools: builtInTools,           // ← SDK built-in tools
  customTools: allCustomTools,   // ← Custom OpenClaw tools
  sessionManager,
  settingsManager,
  // ...
});

// Line 493: Stream function uses Anthropic SDK
activeSession.agent.streamFn = streamSimple;
```

### Tool Creation

**File:** `src/agents/pi-tools.ts`

```typescript
export function createOpenClawCodingTools(options): AnyAgentTool[] {
  // Built-in coding tools
  const codingToolsBase = [
    createReadTool(...),
    createWriteTool(...),
    createEditTool(...),
    createGrepTool(...),
    createBashTool(...),
    // ...
  ];

  // Custom OpenClaw tools
  const customTools = [
    createExecTool(...),           // bash execution
    createProcessTool(...),         // process management
    ...createOpenClawTools(...),   // memory_search, sessions, etc.
    ...listChannelAgentTools(...), // messaging tools
  ];

  return filterToolsByPolicy([...codingToolsBase, ...customTools], ...);
}
```

### Tool Definition Structure

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

// Example: memory_search
{
  name: 'memory_search',
  description: 'Semantically search MEMORY.md and memory/*.md files',
  input_schema: {
    type: 'object',
    properties: {
      query: { type: 'string', description: 'Search query' },
      limit: { type: 'number', description: 'Max results', default: 5 }
    },
    required: ['query']
  },
  async execute(input) {
    const { query, limit = 5 } = input;
    const results = await searchMemoryFiles({ query, limit, ... });
    return results;
  }
}
```

### Tool Flow

```
createOpenClawCodingTools() → tools[]
         ↓
splitSdkTools() → { builtInTools, customTools }
         ↓
createAgentSession({ tools, customTools }) → session with SDK
         ↓
streamSimple() → Calls Anthropic API with tools
         ↓
Tool execution handled by SDK internally
         ↓
All tools available to Claude ✅
```

### SDK Implementation

**File:** `@mariozechner/pi-ai` (inferred from usage)

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
4. Handles `tool_use` blocks from Claude
5. Calls the executor functions
6. Returns tool results to Claude

### Pros & Cons

**Pros:**
- ✅ Native tool support (Anthropic's official API)
- ✅ Streaming built-in
- ✅ Proven solution (OpenClaw uses it)
- ✅ Tool execution handled by SDK

**Cons:**
- ❌ Requires `ANTHROPIC_API_KEY`
- ❌ API costs: $3/MTok input, $15/MTok output
- ❌ Rate limits (tier-based)
- ❌ Anthropic-only (can't use other providers)

---

## Approach 3: OpenCode (Vercel AI SDK)

### Architecture

**Dependencies** (`packages/opencode/package.json`):

```json
{
  "dependencies": {
    "@ai-sdk/anthropic": "2.0.57",
    "@ai-sdk/openai": "2.0.89",
    "@ai-sdk/google": "2.0.27",
    "@ai-sdk/mistral": "1.0.20",
    "@modelcontextprotocol/sdk": "1.25.2",
    "ai": "catalog:",  // Vercel AI SDK
    // ... 15+ provider SDKs
  }
}
```

### Tool Registry

**File:** `src/tool/registry.ts`

```typescript
async function all(): Promise<Tool.Info[]> {
  return [
    BashTool,
    ReadTool,
    GrepTool,
    EditTool,
    WriteTool,
    GlobTool,
    WebSearchTool,
    WebFetchTool,
    AskUserQuestionTool,
    // ... more built-in tools
    ...custom, // loaded from plugins/MCP
  ];
}

export async function tools(model, agent?) {
  const tools = await all();
  return tools.map(async (t) => ({
    id: t.id,
    ...(await t.init({ agent }))
  }));
}
```

### MCP Integration

**File:** `src/mcp/index.ts`

```typescript
// Lines 119-147: Convert MCP tools to AI SDK format
async function convertMcpTool(mcpTool: MCPToolDef, client: MCPClient): Promise<Tool> {
  return dynamicTool({
    description: mcpTool.description ?? "",
    inputSchema: jsonSchema(mcpTool.inputSchema),
    execute: async (args) => {
      return client.callTool({
        name: mcpTool.name,
        arguments: args
      });
    }
  });
}

// Lines 728-817: Load MCP tools
for (const [key, item] of Object.entries(await MCP.tools())) {
  const execute = item.execute;
  if (!execute) continue;

  // Wrap execute to add plugin hooks and format output
  item.execute = async (args, opts) => {
    const ctx = context(args, opts);

    // Before hook
    await Plugin.trigger("tool.execute.before", { tool: key, ... }, { args });

    // Permission check
    await ctx.ask({
      permission: key,
      metadata: {},
      patterns: ["*"],
      always: ["*"],
    });

    // Execute tool
    const result = await execute(args, opts);

    // After hook
    await Plugin.trigger("tool.execute.after", { tool: key, ... }, result);

    // Format result (handle text, images, resources)
    const textParts: string[] = [];
    const attachments: MessageV2.FilePart[] = [];

    for (const contentItem of result.content) {
      if (contentItem.type === "text") {
        textParts.push(contentItem.text);
      } else if (contentItem.type === "image") {
        attachments.push({ /* base64 image */ });
      } else if (contentItem.type === "resource") {
        // Handle resource attachments
      }
    }

    return {
      title: "",
      metadata: result.metadata ?? {},
      output: textParts.join("\n\n"),
      attachments,
      content: result.content,
    };
  };

  tools[key] = item;
}
```

### Tool Resolution

**File:** `src/session/prompt.ts`

```typescript
// Lines 646-820: Resolve all tools for session
async function resolveTools(input: {
  agent: Agent.Info
  model: Provider.Model
  session: Session.Info
  tools?: Record<string, boolean>
  processor: SessionProcessor.Info
  bypassAgentCheck: boolean
}) {
  using _ = log.time("resolveTools");
  const tools: Record<string, AITool> = {};

  // Create context for tool execution
  const context = (args: any, options: ToolCallOptions): Tool.Context => ({
    sessionID: input.session.id,
    abort: options.abortSignal!,
    messageID: input.processor.message.id,
    callID: options.toolCallId,
    extra: { model: input.model, bypassAgentCheck: input.bypassAgentCheck },
    agent: input.agent.name,
    metadata: async (val: { title?: string; metadata?: any }) => {
      // Update part metadata during execution
    },
    async ask(req) {
      // Permission check
      await PermissionNext.ask({ ...req, ... });
    },
  });

  // Load built-in tools from registry
  for (const item of await ToolRegistry.tools(
    { modelID: input.model.api.id, providerID: input.model.providerID },
    input.agent,
  )) {
    const schema = ProviderTransform.schema(input.model, z.toJSONSchema(item.parameters));
    tools[item.id] = tool({
      id: item.id as any,
      description: item.description,
      inputSchema: jsonSchema(schema as any),
      async execute(args, options) {
        const ctx = context(args, options);

        // Before hook
        await Plugin.trigger("tool.execute.before", { tool: item.id, ... }, { args });

        // Execute
        const result = await item.execute(args, ctx);

        // After hook
        await Plugin.trigger("tool.execute.after", { tool: item.id, ... }, result);

        return result;
      },
    });
  }

  // Load MCP tools
  for (const [key, item] of Object.entries(await MCP.tools())) {
    // (MCP tool wrapping - see above)
    tools[key] = item;
  }

  return tools;
}
```

### Streaming with Tools

**File:** `src/session/llm.ts`

```typescript
// Lines 162-229: Stream with tools
const tools = await resolveTools(input);

// LiteLLM compatibility: add dummy tool if needed
const isLiteLLMProxy =
  provider.options?.["litellmProxy"] === true ||
  input.model.providerID.toLowerCase().includes("litellm");

if (isLiteLLMProxy && Object.keys(tools).length === 0 && hasToolCalls(input.messages)) {
  tools["_noop"] = tool({
    description: "Placeholder for LiteLLM/Anthropic proxy compatibility",
    inputSchema: jsonSchema({ type: "object", properties: {} }),
    execute: async () => ({ output: "", title: "", metadata: {} }),
  });
}

return streamText({
  onError(error) {
    l.error("stream error", { error });
  },
  async experimental_repairToolCall(failed) {
    // Auto-fix tool name case mismatches
    const lower = failed.toolCall.toolName.toLowerCase();
    if (lower !== failed.toolCall.toolName && tools[lower]) {
      return { ...failed.toolCall, toolName: lower };
    }
    return { ...failed.toolCall, toolName: "invalid" };
  },
  temperature: params.temperature,
  topP: params.topP,
  topK: params.topK,
  providerOptions: ProviderTransform.providerOptions(input.model, params.options),
  activeTools: Object.keys(tools).filter((x) => x !== "invalid"),
  tools,                    // ← Tools passed here
  maxOutputTokens,
  abortSignal: input.abort,
  headers: { /* custom headers */ },
  messages: [
    ...system.map(createSystemMessage),
    ...input.messages,
  ],
  model: wrapLanguageModel({ /* model config */ }),
});
```

### Tool Flow

```
ToolRegistry.tools() → built-in tools
         ↓
MCP.tools() → external MCP tools
         ↓
resolveTools() → combines all + wraps with hooks
         ↓
streamText({ tools, ... }) → AI SDK handles tool calling
         ↓
AI SDK detects tool_use → calls execute()
         ↓
Tool result → returned to LLM
         ↓
All tools available to Claude ✅
```

### Pros & Cons

**Pros:**
- ✅ Multi-provider support (Anthropic, OpenAI, Google, Mistral, etc.)
- ✅ MCP (Model Context Protocol) integration
- ✅ Plugin system with hooks (before/after execution)
- ✅ Permission management built-in
- ✅ Tool result formatting (text, images, resources)
- ✅ Auto-repair tool calls (case mismatch fixes)
- ✅ Active community and updates

**Cons:**
- ❌ Requires API keys for providers
- ❌ API costs (vary by provider)
- ❌ Rate limits (provider-specific)
- ❌ More complex setup

---

## Side-by-Side Comparison

| Feature | Claude CLI | OpenClaw (Anthropic SDK) | OpenCode (Vercel AI SDK) |
|---------|-----------|--------------------------|---------------------------|
| **Custom tools** | ❌ No | ✅ Yes | ✅ Yes |
| **Built-in tools** | ✅ 20+ | ⚠️ Must implement | ⚠️ Must implement |
| **Streaming** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Multi-provider** | ❌ Anthropic only | ❌ Anthropic only | ✅ 15+ providers |
| **MCP support** | ❌ No | ❌ No | ✅ Yes |
| **API key required** | ❌ No | ✅ Yes | ✅ Yes |
| **Rate limits** | ❌ No | ✅ Tier-based | ✅ Provider-specific |
| **Cost per request** | Free | $3-15/MTok | Varies by provider |
| **Tool execution** | Auto | Manual loop | Auto (SDK handles) |
| **Session management** | ✅ Auto | ⚠️ Manual | ⚠️ Manual |
| **Permission system** | ✅ Built-in | ⚠️ Must implement | ✅ Built-in |
| **Plugin hooks** | ❌ No | ❌ No | ✅ Yes |
| **Official support** | ✅ Anthropic | ⚠️ Community | ✅ Vercel |
| **Tool result formatting** | Auto | Manual | ✅ Auto (text/images) |
| **Tool call repair** | N/A | ❌ No | ✅ Yes (auto-fix) |

---

## Implementation Complexity

### Claude CLI (Current)
```
Complexity: ⭐ (Simplest)
Code: ~300 lines
Dependencies: 0 (just spawn)
Setup: None
```

### OpenClaw (Anthropic SDK)
```
Complexity: ⭐⭐⭐ (Medium)
Code: ~500 lines (runtime + tool loop)
Dependencies: @anthropic-ai/sdk
Setup: ANTHROPIC_API_KEY
```

### OpenCode (Vercel AI SDK)
```
Complexity: ⭐⭐⭐⭐ (Complex)
Code: ~800 lines (runtime + registry + MCP)
Dependencies: ai, @ai-sdk/*, @modelcontextprotocol/sdk
Setup: Provider API keys, MCP servers (optional)
```

---

## Cost Analysis

### Claude CLI
- **Cost:** $0
- **Limits:** None

### OpenClaw (Anthropic SDK)
- **Model:** Claude Sonnet 4
- **Input:** $3 per 1M tokens
- **Output:** $15 per 1M tokens

**Example costs:**
- Simple query (1K input, 200 output): ~$0.003
- With tools (5K input, 1K output): ~$0.03
- Heavy usage (100K tokens/day): ~$3/day

**Rate limits:**
- Tier 1: 50 req/min, 40K tokens/min
- Tier 2: 1000 req/min, 80K tokens/min
- Tier 3: 2000 req/min, 160K tokens/min

### OpenCode (Vercel AI SDK)
- **Cost:** Varies by provider
- **Anthropic:** Same as above
- **OpenAI GPT-4:** $2.50-10/MTok (input), $10-30/MTok (output)
- **OpenRouter:** Variable pricing
- **Local models:** Free (if self-hosted)

---

## Recommendation

### For Our Gateway: **Hybrid Approach**

**Implement both runtimes with config-based switching:**

```typescript
// src/server.ts
import { ClaudeCliRuntime } from './agent/claude-cli-runtime';
import { AnthropicSdkRuntime } from './agent/anthropic-sdk-runtime';

const runtime = process.env.USE_ANTHROPIC_SDK === 'true'
  ? new AnthropicSdkRuntime(config)
  : new ClaudeCliRuntime(config);
```

**Why Hybrid:**

1. **Keep Claude CLI for basic use** (no API key needed)
2. **Add Anthropic SDK for memory_search** (when API key available)
3. **Easy switching** via environment variable
4. **Gradual migration** - no breaking changes

**Configuration:**

```bash
# .env

# Use SDK (tools available, requires key)
USE_ANTHROPIC_SDK=true
ANTHROPIC_API_KEY=sk-ant-api03-...

# OR use CLI (no tools, no key required)
USE_ANTHROPIC_SDK=false
```

### Implementation Steps

**Step 1: Create Anthropic SDK runtime**
- File: `src/agent/anthropic-sdk-runtime.ts`
- Based on implementation plan in `MEMORY_FIX_IMPLEMENTATION_PLAN.md`
- Handles tool execution loop

**Step 2: Update server to support both**
- Conditional runtime selection
- Keep existing ClaudeCliRuntime

**Step 3: Test with memory_search**
- Verify tool is available
- Validate search results
- Monitor performance

**Step 4 (Future): Consider Vercel AI SDK**
- If multi-provider support needed
- If MCP integration desired
- More complex but more flexible

---

## Conclusion

**Current State:**
- ❌ Claude CLI: No custom tools (memory_search unavailable)
- ✅ Infrastructure: Fully implemented (database, search, indexing)
- ⚠️ Gap: Tools not connected to runtime

**Solution:**
- **Short-term:** Add Anthropic SDK runtime (4-6 hours)
- **Medium-term:** Hybrid approach (both CLI and SDK)
- **Long-term:** Evaluate Vercel AI SDK for multi-provider support

**Critical Finding:**
Both OpenClaw and OpenCode solve the same problem (custom tools) using SDKs instead of CLI. The choice between Anthropic SDK (simpler, Anthropic-only) vs Vercel AI SDK (complex, multi-provider) depends on:
- Budget constraints
- Provider flexibility needs
- MCP integration requirements
- Development time available

**Recommended Path:** Start with Anthropic SDK (simpler, proven), migrate to Vercel AI SDK later if multi-provider support becomes necessary.

---

**Analysis completed:** 2026-01-31
**Files analyzed:**
- `/gateway/src/agent/claude-cli-runtime.ts`
- `/reference/openclaw-main/src/agents/pi-embedded-runner/run/attempt.ts`
- `/reference/openclaw-main/src/agents/pi-tools.ts`
- `/reference/opencode/packages/opencode/src/tool/registry.ts`
- `/reference/opencode/packages/opencode/src/mcp/index.ts`
- `/reference/opencode/packages/opencode/src/session/prompt.ts`
- `/reference/opencode/packages/opencode/src/session/llm.ts`
