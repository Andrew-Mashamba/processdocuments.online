#!/usr/bin/env node

import * as fs from 'fs/promises';
import * as path from 'path';
import inquirer from 'inquirer';
import ora from 'ora';
import chalk from 'chalk';
import { execSync } from 'child_process';
import { VisualEditorConfig } from '../types';

export async function init(): Promise<void> {
  console.log(chalk.bold.blue('\n🔍 ZIMA Visual Code Editor Setup\n'));

  // Check prerequisites
  await checkPrerequisites();

  // Detect project
  const projectInfo = await detectProject();

  // Ask configuration questions
  const answers = await inquirer.prompt([
    {
      type: 'confirm',
      name: 'confirmFramework',
      message: `Framework detected: ${chalk.green(projectInfo.framework)}. Is this correct?`,
      default: true,
    },
    {
      type: 'list',
      name: 'buildTool',
      message: 'Select build tool:',
      choices: ['vite', 'webpack', 'laravel-mix'],
      default: projectInfo.buildTool,
    },
    {
      type: 'input',
      name: 'componentPaths',
      message: 'Component paths (comma-separated):',
      default: projectInfo.componentPaths.join(', '),
    },
    {
      type: 'input',
      name: 'zimaFileService',
      message: 'ZIMA file service URL (optional):',
      default: 'http://localhost:5000',
    },
    {
      type: 'number',
      name: 'port',
      message: 'Agent server port:',
      default: 9876,
    },
    {
      type: 'confirm',
      name: 'enableMemory',
      message: 'Enable memory system?',
      default: true,
    },
  ]);

  // Create config
  const config = await createConfig(answers, projectInfo);

  // Create workspace
  await createWorkspace(config);

  // Update build config
  await updateBuildConfig(answers.buildTool);

  // Success
  console.log(chalk.green.bold('\n✅ Setup complete!\n'));
  console.log('Next steps:');
  console.log(chalk.cyan('  1. Start agent server: ') + chalk.bold('npx visual-editor start'));
  console.log(chalk.cyan('  2. Start dev server: ') + chalk.bold('npm run dev'));
  console.log(chalk.cyan('  3. Press ') + chalk.bold('Cmd+Shift+D') + chalk.cyan(' in browser to activate inspector\n'));
}

async function checkPrerequisites(): Promise<void> {
  const spinner = ora('Checking prerequisites...').start();

  try {
    // Check Node.js version
    const nodeVersion = process.version;
    const majorVersion = parseInt(nodeVersion.slice(1).split('.')[0]);
    if (majorVersion < 18) {
      spinner.fail('Node.js 18+ required');
      process.exit(1);
    }

    // Check Claude CLI
    try {
      execSync('claude --version', { stdio: 'pipe' });
      spinner.succeed('Claude CLI found');
    } catch {
      spinner.fail('Claude CLI not found');
      console.log('\nPlease install Claude CLI first:');
      console.log(chalk.cyan('  npm install -g @anthropic-ai/claude-cli'));
      console.log(chalk.cyan('  OR'));
      console.log(chalk.cyan('  brew install claude-cli'));
      console.log('\nThen authenticate:');
      console.log(chalk.cyan('  claude auth login\n'));
      process.exit(1);
    }

    // Check Anthropic API key
    try {
      execSync('claude config get api_key', { stdio: 'pipe' });
      spinner.text = 'Anthropic API key configured';
    } catch {
      spinner.warn('Anthropic API key not configured');
      console.log('\nPlease authenticate with Claude CLI:');
      console.log(chalk.cyan('  claude auth login\n'));
    }

    spinner.succeed('Prerequisites checked');

  } catch (error) {
    spinner.fail('Prerequisites check failed');
    throw error;
  }
}

async function detectProject(): Promise<any> {
  const spinner = ora('Detecting project...').start();

  try {
    const cwd = process.cwd();
    let framework = 'unknown';
    let buildTool = 'unknown';
    let componentPaths: string[] = [];

    // Read package.json
    try {
      const packageJsonPath = path.join(cwd, 'package.json');
      const packageJson = JSON.parse(await fs.readFile(packageJsonPath, 'utf-8'));

      // Detect framework
      if (packageJson.dependencies?.['@livewire/livewire']) {
        framework = 'laravel-livewire';
        componentPaths = ['app/Livewire', 'resources/views/livewire'];
      } else if (packageJson.dependencies?.react) {
        framework = 'react';
        componentPaths = ['src/components'];
      } else if (packageJson.dependencies?.vue) {
        framework = 'vue';
        componentPaths = ['src/components'];
      } else if (packageJson.dependencies?.['@angular/core']) {
        framework = 'angular';
        componentPaths = ['src/app'];
      }

      // Detect build tool
      if (packageJson.devDependencies?.vite) {
        buildTool = 'vite';
      } else if (packageJson.devDependencies?.webpack) {
        buildTool = 'webpack';
      } else if (packageJson.devDependencies?.['laravel-mix']) {
        buildTool = 'laravel-mix';
      }

    } catch {
      // No package.json
    }

    spinner.succeed(`Project detected: ${framework} with ${buildTool}`);

    return { framework, buildTool, componentPaths };

  } catch (error) {
    spinner.fail('Project detection failed');
    throw error;
  }
}

async function createConfig(answers: any, projectInfo: any): Promise<VisualEditorConfig> {
  const cwd = process.cwd();

  const config: VisualEditorConfig = {
    version: '1.0.0',
    project: {
      framework: projectInfo.framework,
      buildTool: answers.buildTool,
      root: cwd,
      componentPaths: answers.componentPaths.split(',').map((p: string) => p.trim()),
      assetPaths: ['resources/css', 'resources/js'],
    },
    agent: {
      port: answers.port,
      model: 'claude-sonnet-4-5-20250514',
      temperature: 0.7,
      maxTokens: 8192,
      contextOptimization: 'adaptive',
      memoryEnabled: answers.enableMemory,
      streamingEnabled: true,
    },
    visual: {
      hotkey: 'Cmd+Shift+D',
      highlightColor: '#3b82f6',
      overlayZIndex: 999999,
      screenshotQuality: 0.8,
      ocrEnabled: true,
    },
    tools: {
      zimaFileService: answers.zimaFileService || undefined,
      enableDocumentTools: true,
      enableWebTools: true,
      enableMemoryTools: true,
    },
    performance: {
      cacheEnabled: true,
      cacheTTL: 3600,
      promptCaching: true,
      tierOptimization: true,
    },
    security: {
      allowedOrigins: ['http://localhost:8000', 'http://localhost:3000'],
      rateLimit: 100,
    },
    logging: {
      level: 'info',
      console: true,
    },
  };

  // Save config
  const configPath = path.join(cwd, '.visual-editor', 'config.json');
  await fs.mkdir(path.dirname(configPath), { recursive: true });
  await fs.writeFile(configPath, JSON.stringify(config, null, 2));

  console.log(chalk.green(`\n✓ Created: ${configPath}`));

  return config;
}

async function createWorkspace(config: VisualEditorConfig): Promise<void> {
  const spinner = ora('Creating workspace...').start();

  try {
    const workspaceDir = path.join(config.project.root, '.visual-editor');

    // Create directories
    await fs.mkdir(path.join(workspaceDir, 'sessions'), { recursive: true });
    await fs.mkdir(path.join(workspaceDir, 'agent-workspace'), { recursive: true });

    spinner.succeed('Workspace created');

    console.log(chalk.green(`✓ Created: ${workspaceDir}/sessions/`));
    console.log(chalk.green(`✓ Created: ${workspaceDir}/agent-workspace/`));
    if (config.agent.memoryEnabled) {
      console.log(chalk.green(`✓ Will create: ${workspaceDir}/memory.db (on first run)`));
    }

  } catch (error) {
    spinner.fail('Workspace creation failed');
    throw error;
  }
}

async function updateBuildConfig(buildTool: string): Promise<void> {
  const spinner = ora(`Updating ${buildTool} configuration...`).start();

  try {
    const cwd = process.cwd();

    if (buildTool === 'vite') {
      const viteConfigPath = path.join(cwd, 'vite.config.js');

      // Check if file exists
      let configExists = false;
      try {
        await fs.access(viteConfigPath);
        configExists = true;
      } catch {
        // File doesn't exist
      }

      if (configExists) {
        // Read existing config
        let config = await fs.readFile(viteConfigPath, 'utf-8');

        // Add plugin import if not exists
        if (!config.includes('@zima/visual-code-editor/vite')) {
          const importLine = "import visualEditor from '@zima/visual-code-editor/vite';\n";
          config = importLine + config;

          // Add plugin to plugins array
          config = config.replace(
            /plugins:\s*\[/,
            `plugins: [\n    visualEditor({ enabled: process.env.NODE_ENV === 'development' }),`
          );

          await fs.writeFile(viteConfigPath, config);
          spinner.succeed(`Updated ${viteConfigPath}`);
        } else {
          spinner.info('Vite config already updated');
        }
      } else {
        spinner.info('No vite.config.js found - skipping');
      }
    }

  } catch (error) {
    spinner.warn('Build config update skipped');
  }
}

// Run if called directly
if (require.main === module) {
  init().catch((error) => {
    console.error(chalk.red('\nError:'), error.message);
    process.exit(1);
  });
}
