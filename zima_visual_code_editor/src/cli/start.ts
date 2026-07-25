#!/usr/bin/env node

import * as fs from 'fs/promises';
import * as path from 'path';
import chalk from 'chalk';
import ora from 'ora';
import { ExpressApp } from '../server/express-app';
import { VisualEditorWebSocketServer } from '../server/websocket-server';
import { ClaudeCliRuntime } from '../agent/claude-cli-runtime';
import { ToolRegistry } from '../agent/tool-registry';
import { VisualEditorConfig } from '../types';

export async function start(): Promise<void> {
  console.log(chalk.bold.blue('\n🚀 Starting ZIMA Visual Code Editor\n'));

  const cwd = process.cwd();
  const configPath = path.join(cwd, '.visual-editor', 'config.json');

  // Load config
  const spinner = ora('Loading configuration...').start();
  let config: VisualEditorConfig;

  try {
    const configData = await fs.readFile(configPath, 'utf-8');
    config = JSON.parse(configData);
    spinner.succeed(`Configuration loaded from: ${configPath}`);
  } catch (error) {
    spinner.fail('Configuration not found');
    console.log(chalk.yellow('\nRun setup first:'));
    console.log(chalk.cyan('  npx visual-editor init\n'));
    process.exit(1);
  }

  // Check Claude CLI
  spinner.start('Checking Claude CLI...');
  const cliAvailable = await ClaudeCliRuntime.checkAvailability();
  if (!cliAvailable) {
    spinner.fail('Claude CLI not found');
    console.log(chalk.yellow('\nInstall Claude CLI:'));
    console.log(chalk.cyan('  npm install -g @anthropic-ai/claude-cli'));
    console.log(chalk.cyan('  claude auth login\n'));
    process.exit(1);
  }

  const version = await ClaudeCliRuntime.getVersion();
  spinner.succeed(`Claude CLI ready: ${version}`);

  // Initialize services
  spinner.start('Initializing services...');

  const runtime = new ClaudeCliRuntime({
    workspacePath: cwd,
    model: config.agent.model,
    temperature: config.agent.temperature,
    maxTokens: config.agent.maxTokens,
  });

  const toolRegistry = new ToolRegistry({
    zimaFileServiceUrl: config.tools.zimaFileService,
  });

  spinner.succeed(`Services initialized (${toolRegistry.getCount()} tools available)`);

  // Start servers
  try {
    // HTTP/REST API server
    const expressApp = new ExpressApp(config, cwd);
    expressApp.listen(config.agent.port);

    // WebSocket server for visual editor
    const wsPort = config.agent.port + 1;
    const wsServer = new VisualEditorWebSocketServer(wsPort, runtime, toolRegistry);

    console.log(chalk.green.bold('\n✅ All systems ready!\n'));

    console.log(chalk.cyan('Visual Editor:'));
    console.log(`  Press ${chalk.bold('Cmd+Shift+D')} in your browser to activate inspector\n`);

    console.log(chalk.cyan('API Endpoints:'));
    console.log(`  Chat API:   http://localhost:${config.agent.port}/api/chat`);
    console.log(`  Stream API: http://localhost:${config.agent.port}/api/chat/stream`);
    console.log(`  Tools API:  http://localhost:${config.agent.port}/api/tools`);
    console.log(`  WebSocket:  ws://localhost:${wsPort}\n`);

    console.log(chalk.cyan('Configuration:'));
    console.log(`  Framework:     ${config.project.framework}`);
    console.log(`  Model:         ${config.agent.model}`);
    console.log(`  Memory:        ${config.agent.memoryEnabled ? 'Enabled' : 'Disabled'}`);
    console.log(`  Optimization:  ${config.agent.contextOptimization}`);
    console.log(`  Tools:         ${toolRegistry.getCount()} available\n`);

    if (config.tools.zimaFileService) {
      console.log(chalk.cyan('ZIMA File Service:'));
      console.log(`  URL: ${config.tools.zimaFileService}\n`);
    }

    console.log(chalk.gray('Press Ctrl+C to stop\n'));

    // Handle shutdown
    process.on('SIGINT', () => {
      console.log(chalk.yellow('\n\nShutting down...'));
      expressApp.close();
      wsServer.close();
      process.exit(0);
    });

  } catch (error: any) {
    console.error(chalk.red('\nFailed to start servers:'), error.message);
    process.exit(1);
  }
}

// Run if called directly
if (require.main === module) {
  start().catch((error) => {
    console.error(chalk.red('\nError:'), error.message);
    process.exit(1);
  });
}
