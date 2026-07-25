# Memory System Fix - Implementation Plan

**Date:** 2026-01-31
**Status:** Ready to implement
**Estimated effort:** 4-6 hours

---

## Problem Summary

The memory system (database, search, indexing) is **100% implemented and working**, but the `memory_search` tool is **not available** to Claude because:

**Root Cause:** We use `claude` CLI which doesn't support custom tool definitions.

**Solution:** Switch to Anthropic SDK which has native tool support.

---

## Implementation Steps

### Step 1: Install Anthropic SDK

```bash
npm install @anthropic-ai/sdk
```

**File:** `package.json`
```json
{
  "dependencies": {
    "@anthropic-ai/sdk": "^0.38.0"
  }
}
```

---

### Step 2: Create Anthropic SDK Runtime

**New file:** `src/agent/anthropic-sdk-runtime.ts`

```typescript
import Anthropic from '@anthropic-ai/sdk';
import { GatewayConfig, AgentContext, MessageResponse } from '../types';
import { UnifiedToolRegistry, Tool } from './tool-registry';
import { ToolExecutor, ToolCall } from './tool-executor';

export interface StreamChunk {
  type: string;
  event?: any;
  delta?: any;
  content?: any;
}

export type StreamCallback = (chunk: StreamChunk) => void;

export class AnthropicSdkRuntime {
  private client: Anthropic;
  private config: GatewayConfig;
  private registry: UnifiedToolRegistry;
  private executor: ToolExecutor;

  constructor(config: GatewayConfig) {
    this.config = config;
    this.client = new Anthropic({
      apiKey: process.env.ANTHROPIC_API_KEY || ''
    });
    this.registry = new UnifiedToolRegistry(config);
    this.executor = new ToolExecutor(config, this.registry);
  }

  async execute(context: AgentContext): Promise<MessageResponse> {
    console.log('🤖 Starting Anthropic SDK runtime...');

    // Load tools
    await this.registry.refresh();
    const tools = await this.registry.getTools();
    console.log(`✓ Loaded ${tools.length} tools`);

    // Convert messages to Anthropic format
    const messages = this.convertMessages(context);

    // Initial request
    let response = await this.client.messages.create({
      model: context.model || 'claude-sonnet-4-20250514',
      max_tokens: context.maxTokens || 8192,
      system: context.systemPrompt || undefined,
      messages: messages,
      tools: this.convertTools(tools)
    });

    // Tool execution loop
    while (this.hasToolUse(response)) {
      console.log('🔧 Tool calls detected, executing...');

      const toolResults = await this.executeTools(response, context.sessionKey);

      // Continue conversation with tool results
      messages.push({
        role: 'assistant',
        content: response.content
      });

      messages.push({
        role: 'user',
        content: toolResults
      });

      response = await this.client.messages.create({
        model: context.model || 'claude-sonnet-4-20250514',
        max_tokens: context.maxTokens || 8192,
        system: context.systemPrompt || undefined,
        messages: messages,
        tools: this.convertTools(tools)
      });
    }

    // Extract final text response
    const textContent = response.content
      .filter(block => block.type === 'text')
      .map(block => block.text)
      .join('\n');

    return {
      output: textContent,
      usage: {
        inputTokens: response.usage.input_tokens,
        outputTokens: response.usage.output_tokens,
        cost: this.calculateCost(response.usage)
      },
      model: response.model,
      files: []
    };
  }

  async executeStream(
    context: AgentContext,
    onChunk: StreamCallback
  ): Promise<MessageResponse> {
    console.log('🤖 Starting Anthropic SDK runtime (streaming)...');

    await this.registry.refresh();
    const tools = await this.registry.getTools();
    console.log(`✓ Loaded ${tools.length} tools`);

    const messages = this.convertMessages(context);

    const stream = await this.client.messages.stream({
      model: context.model || 'claude-sonnet-4-20250514',
      max_tokens: context.maxTokens || 8192,
      system: context.systemPrompt || undefined,
      messages: messages,
      tools: this.convertTools(tools)
    });

    let fullText = '';
    let totalUsage = {
      inputTokens: 0,
      outputTokens: 0,
      cost: 0
    };

    stream.on('text', (text) => {
      fullText += text;
      onChunk({
        type: 'content_block_delta',
        delta: { type: 'text_delta', text }
      });
    });

    stream.on('message', (message) => {
      totalUsage.inputTokens = message.usage.input_tokens;
      totalUsage.outputTokens = message.usage.output_tokens;
      totalUsage.cost = this.calculateCost(message.usage);
    });

    const finalMessage = await stream.finalMessage();

    // Handle tool use if present
    if (this.hasToolUse(finalMessage)) {
      // TODO: Implement tool execution in streaming mode
      // For now, just complete without tools
      console.log('⚠️  Tool use detected in streaming mode (not yet supported)');
    }

    return {
      output: fullText,
      usage: totalUsage,
      model: finalMessage.model,
      files: []
    };
  }

  private convertMessages(context: AgentContext): Anthropic.MessageParam[] {
    return context.messages.map(msg => ({
      role: msg.role as 'user' | 'assistant',
      content: msg.content
    }));
  }

  private convertTools(tools: Tool[]): Anthropic.Tool[] {
    return tools.map(tool => ({
      name: tool.name,
      description: tool.description,
      input_schema: tool.input_schema as Anthropic.Tool.InputSchema
    }));
  }

  private hasToolUse(message: Anthropic.Message): boolean {
    return message.content.some(block => block.type === 'tool_use');
  }

  private async executeTools(
    message: Anthropic.Message,
    sessionKey: string
  ): Promise<Anthropic.ToolResultBlockParam[]> {
    const toolUseBlocks = message.content.filter(
      (block): block is Anthropic.ToolUseBlock => block.type === 'tool_use'
    );

    const results: Anthropic.ToolResultBlockParam[] = [];

    for (const block of toolUseBlocks) {
      console.log(`🔧 Executing tool: ${block.name}`);

      const toolCall: ToolCall = {
        id: block.id,
        name: block.name,
        input: block.input as Record<string, any>
      };

      const result = await this.executor.execute(toolCall, sessionKey);

      results.push({
        type: 'tool_result',
        tool_use_id: block.id,
        content: typeof result.content === 'string'
          ? result.content
          : JSON.stringify(result.content),
        is_error: result.is_error
      });
    }

    return results;
  }

  private calculateCost(usage: Anthropic.Usage): number {
    // Sonnet 4: $3 per MTok input, $15 per MTok output
    const inputCost = (usage.input_tokens / 1_000_000) * 3;
    const outputCost = (usage.output_tokens / 1_000_000) * 15;
    return inputCost + outputCost;
  }
}
```

---

### Step 3: Update Server to Use New Runtime

**File:** `src/server.ts`

**Before:**
```typescript
import { ClaudeCliRuntime } from './agent/claude-cli-runtime';

const runtime = new ClaudeCliRuntime(config);
```

**After:**
```typescript
import { AnthropicSdkRuntime } from './agent/anthropic-sdk-runtime';

const runtime = new AnthropicSdkRuntime(config);
```

---

### Step 4: Add Environment Variable

**File:** `.env`

```bash
ANTHROPIC_API_KEY=sk-ant-api03-...your-key-here...
```

**File:** `.env.example`

```bash
# Anthropic API Key (required for SDK runtime)
ANTHROPIC_API_KEY=sk-ant-api03-xxxxx
```

---

### Step 5: Update Configuration

**File:** `src/types/index.ts`

```typescript
export interface AgentContext {
  systemPrompt?: string;
  messages: Message[];
  newMessage: string;
  sessionKey: string;
  model?: string;
  maxTokens?: number;
}

export interface Message {
  role: 'user' | 'assistant';
  content: string | any[];
}
```

---

### Step 6: Test Memory Search

**Test script:** `test-memory-search.sh`

```bash
#!/bin/bash

# Ensure API key is set
export ANTHROPIC_API_KEY="sk-ant-api03-..."

# Test 1: Search for Alice
curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Search your memory for information about Alice",
    "channel": "webchat",
    "senderId": "test",
    "sessionKey": "test-memory-'$(date +%s)'"
  }'

echo ""
echo "---"
echo ""

# Test 2: Ask about preferences
curl -X POST http://localhost:18790/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What do you know about my preferences from memory?",
    "channel": "webchat",
    "senderId": "test",
    "sessionKey": "test-memory-2-'$(date +%s)'"
  }'
```

---

## Expected Results

### Before Fix:
```json
{
  "output": "The memory_search tool is not currently available...",
  "model": "claude-3-5-haiku-20241022"
}
```

### After Fix:
```json
{
  "output": "Based on memory search results:\n\n**Name**: Alice\n**Preferences**: Likes cats\n...",
  "model": "claude-sonnet-4-20250514"
}
```

**Tool execution logs:**
```
✓ Loaded 216 tools
🔧 Tool calls detected, executing...
🔧 Executing tool: memory_search
🔍 [memory_search] Query: "Alice", Limit: 5
🔍 Hybrid search completed in 45ms (1 results)
✓ Tool execution complete
```

---

## Verification Checklist

- [ ] Anthropic SDK installed
- [ ] `ANTHROPIC_API_KEY` set in environment
- [ ] `AnthropicSdkRuntime` created
- [ ] Server updated to use new runtime
- [ ] Gateway starts without errors
- [ ] memory_search tool appears in logs
- [ ] Test request finds Alice in memory
- [ ] Tool execution logs show search
- [ ] Response includes memory results

---

## Rollback Plan

If issues occur:

1. Revert server.ts to use `ClaudeCliRuntime`
2. Remove `@anthropic-ai/sdk` dependency
3. System will work as before (without custom tools)

---

## Performance Considerations

### API Costs:
- Input: $3 per 1M tokens
- Output: $15 per 1M tokens

**Example costs:**
- Simple query (1K input, 200 output): ~$0.003
- Complex with tools (5K input, 1K output): ~$0.03
- Heavy usage (100K tokens/day): ~$3/day

### Rate Limits:
- Tier 1: 50 requests/min, 40K tokens/min
- Tier 2: 1000 requests/min, 80K tokens/min
- Tier 3: 2000 requests/min, 160K tokens/min

**Mitigation:**
- Implement request queuing
- Add retry logic with exponential backoff
- Cache common queries

---

## Alternative: Keep Both Runtimes

**Option:** Support both Claude CLI and Anthropic SDK

```typescript
const runtime = process.env.USE_ANTHROPIC_SDK === 'true'
  ? new AnthropicSdkRuntime(config)
  : new ClaudeCliRuntime(config);
```

**Benefits:**
- Flexible deployment
- Can switch based on environment
- Keep "no API key" option

**Config:**
```bash
# Use SDK (tools available, requires key)
USE_ANTHROPIC_SDK=true
ANTHROPIC_API_KEY=sk-ant-api03-...

# Use CLI (no tools, no key required)
USE_ANTHROPIC_SDK=false
```

---

## Next Steps

1. **Implement:** Create `anthropic-sdk-runtime.ts` (4 hours)
2. **Test:** Verify memory_search works (1 hour)
3. **Document:** Update README with SDK requirements (30 min)
4. **Deploy:** Update production with API key (30 min)

**Total: ~6 hours**

---

**Status:** Ready to implement
**Priority:** High (blocks memory system usage)
**Risk:** Low (SDK is stable and well-documented)
