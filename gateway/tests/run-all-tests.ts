/**
 * Master Test Runner
 * Runs all test suites and generates comprehensive report
 */

import { testMemorySystem } from './e2e/01-memory-system.test';
import { testWebTools } from './e2e/02-web-tools.test';
import { testExecTools } from './e2e/03-exec-tools.test';
import { testMemoryLoad } from './performance/memory-load.test';
import * as fs from 'fs-extra';
import * as path from 'path';

interface TestResult {
  suite: string;
  passed: number;
  failed: number;
  duration: number;
  details?: any;
}

async function runAllTests() {
  console.log('\n');
  console.log('╔════════════════════════════════════════════════════════╗');
  console.log('║     ZIMA Gateway - Comprehensive Test Suite           ║');
  console.log('║     Weeks 7-10 Complete Implementation Tests           ║');
  console.log('╚════════════════════════════════════════════════════════╝');
  console.log('\n');

  const results: TestResult[] = [];
  let totalPassed = 0;
  let totalFailed = 0;

  // E2E Tests
  console.log('═══════════════════════════════════════════════════════');
  console.log('  PHASE 1: END-TO-END TESTS');
  console.log('═══════════════════════════════════════════════════════\n');

  // Test 1: Memory System
  try {
    const start = Date.now();
    const result = await testMemorySystem();
    const duration = Date.now() - start;

    results.push({
      suite: 'Memory System (E2E)',
      passed: result.passed,
      failed: result.failed,
      duration
    });

    totalPassed += result.passed;
    totalFailed += result.failed;
  } catch (error: any) {
    console.error(`Memory System test suite crashed: ${error.message}`);
    results.push({
      suite: 'Memory System (E2E)',
      passed: 0,
      failed: 1,
      duration: 0
    });
    totalFailed++;
  }

  // Test 2: Web Tools
  try {
    const start = Date.now();
    const result = await testWebTools();
    const duration = Date.now() - start;

    results.push({
      suite: 'Web Tools (E2E)',
      passed: result.passed,
      failed: result.failed,
      duration
    });

    totalPassed += result.passed;
    totalFailed += result.failed;
  } catch (error: any) {
    console.error(`Web Tools test suite crashed: ${error.message}`);
    results.push({
      suite: 'Web Tools (E2E)',
      passed: 0,
      failed: 1,
      duration: 0
    });
    totalFailed++;
  }

  // Test 3: Exec & Process Tools
  try {
    const start = Date.now();
    const result = await testExecTools();
    const duration = Date.now() - start;

    results.push({
      suite: 'Exec & Process Tools (E2E)',
      passed: result.passed,
      failed: result.failed,
      duration
    });

    totalPassed += result.passed;
    totalFailed += result.failed;
  } catch (error: any) {
    console.error(`Exec Tools test suite crashed: ${error.message}`);
    results.push({
      suite: 'Exec & Process Tools (E2E)',
      passed: 0,
      failed: 1,
      duration: 0
    });
    totalFailed++;
  }

  // Performance Tests
  console.log('═══════════════════════════════════════════════════════');
  console.log('  PHASE 2: PERFORMANCE TESTS');
  console.log('═══════════════════════════════════════════════════════\n');

  try {
    const start = Date.now();
    const result = await testMemoryLoad();
    const duration = Date.now() - start;

    results.push({
      suite: 'Memory System (Performance)',
      passed: result.passed ? 1 : 0,
      failed: result.passed ? 0 : 1,
      duration,
      details: result
    });

    if (result.passed) {
      totalPassed++;
    } else {
      totalFailed++;
    }
  } catch (error: any) {
    console.error(`Performance test suite crashed: ${error.message}`);
    results.push({
      suite: 'Memory System (Performance)',
      passed: 0,
      failed: 1,
      duration: 0
    });
    totalFailed++;
  }

  // Generate Report
  console.log('═══════════════════════════════════════════════════════');
  console.log('  TEST SUMMARY');
  console.log('═══════════════════════════════════════════════════════\n');

  console.log('Test Suite Results:\n');

  results.forEach((result) => {
    const status = result.failed === 0 ? '✓ PASS' : '✗ FAIL';
    const color = result.failed === 0 ? '' : '';
    console.log(`  ${status} ${result.suite}`);
    console.log(`       ${result.passed} passed, ${result.failed} failed (${result.duration}ms)`);
  });

  console.log('\n═══════════════════════════════════════════════════════');
  console.log('  FINAL RESULTS');
  console.log('═══════════════════════════════════════════════════════\n');

  const overallStatus = totalFailed === 0 ? '✓ ALL TESTS PASSED' : '✗ SOME TESTS FAILED';
  console.log(`  ${overallStatus}`);
  console.log(`  Total: ${totalPassed} passed, ${totalFailed} failed`);
  console.log(`  Success Rate: ${((totalPassed / (totalPassed + totalFailed)) * 100).toFixed(1)}%`);

  console.log('\n═══════════════════════════════════════════════════════\n');

  // Save report to file
  const reportPath = path.join(__dirname, '../TEST_REPORT.md');
  const report = generateMarkdownReport(results, totalPassed, totalFailed);
  await fs.writeFile(reportPath, report);
  console.log(`📄 Detailed report saved to: ${reportPath}\n`);

  return {
    results,
    totalPassed,
    totalFailed,
    success: totalFailed === 0
  };
}

function generateMarkdownReport(results: TestResult[], totalPassed: number, totalFailed: number): string {
  const timestamp = new Date().toISOString();

  let report = `# ZIMA Gateway - Test Report\n\n`;
  report += `**Generated:** ${timestamp}\n`;
  report += `**Status:** ${totalFailed === 0 ? '✅ PASS' : '❌ FAIL'}\n\n`;

  report += `## Summary\n\n`;
  report += `- **Total Tests:** ${totalPassed + totalFailed}\n`;
  report += `- **Passed:** ${totalPassed}\n`;
  report += `- **Failed:** ${totalFailed}\n`;
  report += `- **Success Rate:** ${((totalPassed / (totalPassed + totalFailed)) * 100).toFixed(1)}%\n\n`;

  report += `## Test Suites\n\n`;
  report += `| Suite | Status | Passed | Failed | Duration |\n`;
  report += `|-------|--------|--------|--------|----------|\n`;

  results.forEach((result) => {
    const status = result.failed === 0 ? '✅' : '❌';
    report += `| ${result.suite} | ${status} | ${result.passed} | ${result.failed} | ${result.duration}ms |\n`;
  });

  report += `\n## Details\n\n`;

  results.forEach((result) => {
    report += `### ${result.suite}\n\n`;
    report += `- **Status:** ${result.failed === 0 ? '✅ PASS' : '❌ FAIL'}\n`;
    report += `- **Tests:** ${result.passed} passed, ${result.failed} failed\n`;
    report += `- **Duration:** ${result.duration}ms\n`;

    if (result.details) {
      report += `\n**Performance Metrics:**\n\n`;
      if (result.details.sequential) {
        report += `- Sequential: ${result.details.sequential.avg.toFixed(2)}ms avg, `;
        report += `${result.details.sequential.p95.toFixed(2)}ms P95\n`;
      }
      if (result.details.parallel) {
        report += `- Parallel: ${result.details.parallel.avg.toFixed(2)}ms avg, `;
        report += `${result.details.parallel.p95.toFixed(2)}ms P95\n`;
      }
    }

    report += `\n`;
  });

  report += `---\n\n`;
  report += `*Generated by ZIMA Gateway Test Suite*\n`;

  return report;
}

// Run tests
if (require.main === module) {
  runAllTests()
    .then((summary) => {
      process.exit(summary.success ? 0 : 1);
    })
    .catch((error) => {
      console.error('Test runner crashed:', error);
      process.exit(1);
    });
}

export { runAllTests };
