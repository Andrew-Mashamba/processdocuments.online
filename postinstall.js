#!/usr/bin/env node

/**
 * ZIMA Post-Install Script
 * Checks for Ollama and Qwen model, provides setup instructions
 */

const { exec } = require('child_process');
const chalk = require('chalk');
const os = require('os');

console.log('\n' + chalk.cyan.bold('🚀 ZIMA Installation'));
console.log(chalk.cyan('═══════════════════════════════\n'));

// Check if Ollama is installed
exec('ollama --version', (error) => {
  if (error) {
    console.log(chalk.yellow('⚠️  Ollama not found\n'));
    console.log(chalk.white('ZIMA requires Ollama to run the Qwen2.5-Coder-14B model locally.\n'));
    console.log(chalk.white('Please install Ollama:\n'));
    
    const platform = os.platform();
    if (platform === 'darwin') {
      console.log(chalk.green('  macOS:'));
      console.log(chalk.white('    brew install ollama'));
      console.log(chalk.white('    brew services start ollama\n'));
    } else if (platform === 'linux') {
      console.log(chalk.green('  Linux:'));
      console.log(chalk.white('    curl -fsSL https://ollama.com/install.sh | sh\n'));
    } else if (platform === 'win32') {
      console.log(chalk.green('  Windows:'));
      console.log(chalk.white('    Download from: https://ollama.com/download/windows\n'));
    }
    
    console.log(chalk.white('Or visit: ') + chalk.blue('https://ollama.com\n'));
  } else {
    console.log(chalk.green('✅ Ollama found\n'));
    
    // Check if Qwen model is installed
    exec('ollama list', (error, stdout) => {
      if (error) {
        console.log(chalk.yellow('⚠️  Could not check for Qwen model\n'));
      } else {
        if (stdout.includes('qwen2.5-coder:14b')) {
          console.log(chalk.green('✅ Qwen2.5-Coder-14B model found\n'));
          console.log(chalk.cyan('═══════════════════════════════'));
          console.log(chalk.green.bold('✅ ZIMA is ready to use!'));
          console.log(chalk.cyan('═══════════════════════════════\n'));
          console.log(chalk.white('Start ZIMA by typing: ') + chalk.yellow.bold('zima\n'));
        } else {
          console.log(chalk.yellow('⚠️  Qwen2.5-Coder-14B model not found\n'));
          console.log(chalk.white('Please download the model (9GB, one-time):\n'));
          console.log(chalk.yellow('  ollama pull qwen2.5-coder:14b\n'));
          console.log(chalk.gray('This may take 5-15 minutes depending on your connection.\n'));
        }
      }
    });
  }
});

// Show quick start
console.log(chalk.cyan('Quick Start Guide:'));
console.log(chalk.white('1. Ensure Ollama is running'));
console.log(chalk.white('2. Download model: ') + chalk.yellow('ollama pull qwen2.5-coder:14b'));
console.log(chalk.white('3. Start ZIMA: ') + chalk.yellow('zima'));
console.log(chalk.white('4. Type ') + chalk.yellow('/help') + chalk.white(' for commands\n'));

console.log(chalk.gray('Documentation: https://github.com/Andrew-Mashamba/zima_code\n'));
