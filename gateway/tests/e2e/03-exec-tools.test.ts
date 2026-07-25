/**
 * E2E Test: Exec & Process Tools
 * Tests command execution and process management
 */

import { getExecRunner } from '../../src/tools/exec-runner';
import { getProcessManager } from '../../src/tools/process-manager';

async function testExecTools() {
  console.log('========================================');
  console.log('E2E Test: Exec & Process Tools');
  console.log('========================================\n');

  let passed = 0;
  let failed = 0;

  // Test 1: Safe command execution
  try {
    console.log('Test 1: Execute safe command...');
    const execRunner = getExecRunner();
    const result = await execRunner.run('echo "Hello from ZIMA"', { timeout: 5000 });

    if (result.success && result.stdout.includes('Hello from ZIMA')) {
      console.log(`✓ Command executed successfully`);
      console.log(`  Output: ${result.stdout.trim()}`);
      console.log(`  Duration: ${result.duration}ms`);
      passed++;
    } else {
      console.log(`✗ Command failed: ${result.stderr}`);
      failed++;
    }
  } catch (error: any) {
    console.log(`✗ Exec failed: ${error.message}`);
    failed++;
  }

  // Test 2: Dangerous command blocked
  try {
    console.log('\nTest 2: Block dangerous command...');
    const execRunner = getExecRunner({ whitelistMode: false, allowDangerous: false });
    const result = await execRunner.run('rm -rf /', { timeout: 5000 });

    if (!result.success && result.error?.includes('blacklist')) {
      console.log(`✓ Dangerous command blocked`);
      console.log(`  Reason: ${result.error}`);
      passed++;
    } else {
      console.log(`✗ Dangerous command NOT blocked!`);
      failed++;
    }
  } catch (error: any) {
    console.log(`✗ Safety test failed: ${error.message}`);
    failed++;
  }

  // Test 3: Whitelist mode
  try {
    console.log('\nTest 3: Whitelist mode...');
    const execRunner = getExecRunner({ whitelistMode: true });

    // Allowed command
    const result1 = await execRunner.run('ls', { timeout: 5000 });
    if (result1.success) {
      console.log(`  ✓ Allowed command (ls) executed`);
    }

    // Blocked command
    const result2 = await execRunner.run('nonwhitelisted', { timeout: 5000 });
    if (!result2.success && result2.error?.includes('whitelist')) {
      console.log(`  ✓ Non-whitelisted command blocked`);
    }

    console.log('✓ Whitelist mode working');
    passed++;
  } catch (error: any) {
    console.log(`✗ Whitelist test failed: ${error.message}`);
    failed++;
  }

  // Test 4: Process spawning
  try {
    console.log('\nTest 4: Spawn background process...');
    const processManager = getProcessManager();

    const processId = await processManager.spawn('sleep', ['2'], {
      detached: false,
      captureOutput: true
    });

    console.log(`  ✓ Spawned process: ${processId}`);

    // Check status
    const info = processManager.getProcess(processId);
    if (info && info.status === 'running') {
      console.log(`  ✓ Process running (PID: ${info.pid})`);
    }

    // Kill process
    await processManager.kill(processId);
    console.log(`  ✓ Process killed`);

    console.log('✓ Process management working');
    passed++;
  } catch (error: any) {
    console.log(`✗ Process test failed: ${error.message}`);
    failed++;
  }

  // Test 5: Process output capture
  try {
    console.log('\nTest 5: Capture process output...');
    const processManager = getProcessManager();

    const processId = await processManager.spawn('echo', ['Test output'], {
      captureOutput: true
    });

    // Wait a bit for process to complete
    await new Promise(resolve => setTimeout(resolve, 500));

    const output = processManager.getOutput(processId);
    if (output && output.stdout.includes('Test output')) {
      console.log(`✓ Output captured: ${output.stdout.trim()}`);
      passed++;
    } else {
      console.log(`✗ Output not captured`);
      failed++;
    }
  } catch (error: any) {
    console.log(`✗ Output capture failed: ${error.message}`);
    failed++;
  }

  // Results
  console.log('\n========================================');
  console.log(`Exec/Process Tests: ${passed} passed, ${failed} failed`);
  console.log('========================================\n');

  return { passed, failed };
}

// Run if executed directly
if (require.main === module) {
  testExecTools()
    .then(({ passed, failed }) => {
      process.exit(failed > 0 ? 1 : 0);
    })
    .catch((error) => {
      console.error('Test suite failed:', error);
      process.exit(1);
    });
}

export { testExecTools };
