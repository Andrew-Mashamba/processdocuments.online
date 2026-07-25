#!/usr/bin/env ts-node
/**
 * Self-Healing Tool System Test
 *
 * This script tests the self-healing capabilities by:
 * 1. Attempting to execute a tool that will fail (ZIMA API down)
 * 2. Watching the system automatically diagnose and fix
 * 3. Verifying tool generation and registration
 */

import { GatewayConfig } from './src/types';
import { UnifiedToolRegistry } from './src/agent/tool-registry';
import { ToolExecutor } from './src/agent/tool-executor';
import { SelfHealingExecutor } from './src/mcp/self-healing-executor';
import * as path from 'path';

async function main() {
  console.log('🧪 Testing Self-Healing Tool System\n');
  console.log('=' .repeat(60));

  // Configuration
  const config: GatewayConfig = {
    gateway: {
      port: 18790,
      bind: '0.0.0.0',
      cors: { origins: ['*'] }
    },
    channels: {
      webchat: { enabled: true },
      whatsapp: { enabled: false },
      email: { enabled: false }
    },
    zima: {
      apiUrl: 'http://localhost:5000', // This will fail - API is stopped
      timeout: 5000 // Short timeout to fail quickly
    },
    storage: {
      root: path.join(process.cwd(), 'storage'),
      transcripts: path.join(process.env.HOME!, '.zima/agents/main/sessions'),
      files: path.join(process.cwd(), 'generated_files'),
      workspace: path.join(process.cwd(), 'workspace'),
      memory: path.join(process.cwd(), 'storage/memory.db')
    }
  };

  console.log('\n📋 Configuration:');
  console.log(`   ZIMA API: ${config.zima.apiUrl} (Expected: DOWN)`);
  console.log(`   Timeout: ${config.zima.timeout}ms`);
  console.log(`   Storage: ${config.storage.root}`);

  // Initialize components
  console.log('\n🔧 Initializing components...');

  const registry = new UnifiedToolRegistry(config);
  await registry.refresh();

  const standardExecutor = new ToolExecutor(config, registry);

  const healingExecutor = new SelfHealingExecutor(
    config,
    registry,
    standardExecutor,
    {
      maxRetries: 3,
      enableAutoFix: true,
      enableAutoGenerate: true,
      logDir: path.join(process.cwd(), 'logs/tools'),
      configDir: path.join(process.cwd(), 'config/tools'),
      generatedToolsDir: path.join(process.cwd(), 'src/tools/generated')
    }
  );

  console.log('   ✓ Registry loaded');
  console.log('   ✓ Standard executor ready');
  console.log('   ✓ Self-healing executor ready');

  // Test tool call
  console.log('\n' + '='.repeat(60));
  console.log('TEST: Create Excel File (ZIMA API DOWN)');
  console.log('='.repeat(60));

  const toolCall = {
    id: 'test_001',
    name: 'create_excel',
    input: {
      filename: 'test_self_healing.xlsx',
      data: [
        ['Product', 'Price', 'Stock'],
        ['Apple', '$1.99', '100'],
        ['Banana', '$0.79', '150'],
        ['Orange', '$2.49', '80']
      ],
      sessionKey: 'test-self-healing'
    }
  };

  console.log('\n📝 Tool Call:');
  console.log(`   Tool: ${toolCall.name}`);
  console.log(`   File: ${toolCall.input.filename}`);
  console.log(`   Data rows: ${toolCall.input.data.length}`);

  console.log('\n🚀 Executing with self-healing...\n');
  console.log('-'.repeat(60));

  const startTime = Date.now();

  try {
    const result = await healingExecutor.execute(toolCall, 'test-self-healing');
    const duration = Date.now() - startTime;

    console.log('-'.repeat(60));
    console.log(`\n⏱️  Execution time: ${duration}ms`);

    console.log('\n📊 Result:');
    console.log(`   Success: ${!result.is_error}`);
    console.log(`   Tool ID: ${result.tool_use_id}`);

    if (result.is_error) {
      console.log(`   ❌ Error: ${result.content}`);
    } else {
      console.log(`   ✅ Content: ${typeof result.content === 'string' ? result.content.substring(0, 200) : JSON.stringify(result.content, null, 2)}`);
    }

  } catch (error) {
    const duration = Date.now() - startTime;
    console.log('-'.repeat(60));
    console.log(`\n⏱️  Execution time: ${duration}ms`);
    console.log(`\n❌ Error: ${error instanceof Error ? error.message : String(error)}`);
  }

  // Check for generated tools
  console.log('\n' + '='.repeat(60));
  console.log('VERIFICATION');
  console.log('='.repeat(60));

  console.log('\n1️⃣  Checking for generated tools...');
  const fs = await import('fs');
  const generatedDir = path.join(process.cwd(), 'src/tools/generated');

  if (fs.existsSync(generatedDir)) {
    const files = fs.readdirSync(generatedDir);
    if (files.length > 0) {
      console.log(`   ✅ Found ${files.length} generated files:`);
      files.forEach(file => console.log(`      - ${file}`));
    } else {
      console.log(`   ⚠️  No generated tools found`);
    }
  } else {
    console.log(`   ⚠️  Generated tools directory doesn't exist`);
  }

  console.log('\n2️⃣  Checking failure logs...');
  const failureLog = path.join(process.cwd(), 'logs/tools/failures.jsonl');

  if (fs.existsSync(failureLog)) {
    const content = fs.readFileSync(failureLog, 'utf-8');
    const lines = content.trim().split('\n').filter(l => l);
    console.log(`   ✅ Found ${lines.length} failure log entries`);

    if (lines.length > 0) {
      const lastFailure = JSON.parse(lines[lines.length - 1]);
      console.log(`\n   Latest failure:`);
      console.log(`      Tool: ${lastFailure.toolName}`);
      console.log(`      Type: ${lastFailure.failureType}`);
      console.log(`      Time: ${lastFailure.timestamp}`);
      console.log(`      Error: ${lastFailure.errorMessage.substring(0, 80)}...`);
    }
  } else {
    console.log(`   ℹ️  No failure log found (may not have failed yet)`);
  }

  console.log('\n3️⃣  Checking fix attempts...');
  const fixLog = path.join(process.cwd(), 'config/tools/fix-attempts.jsonl');

  if (fs.existsSync(fixLog)) {
    const content = fs.readFileSync(fixLog, 'utf-8');
    const lines = content.trim().split('\n').filter(l => l);
    console.log(`   ✅ Found ${lines.length} fix attempt entries`);

    if (lines.length > 0) {
      const lastFix = JSON.parse(lines[lines.length - 1]);
      console.log(`\n   Latest fix attempt:`);
      console.log(`      Tool: ${lastFix.toolName}`);
      console.log(`      Strategy: ${lastFix.fixStrategy}`);
      console.log(`      Success: ${lastFix.success}`);
      console.log(`      Time: ${lastFix.timestamp}`);
    }
  } else {
    console.log(`   ℹ️  No fix attempts log found`);
  }

  console.log('\n' + '='.repeat(60));
  console.log('✅ Test Complete');
  console.log('='.repeat(60));
}

main().catch(console.error);
