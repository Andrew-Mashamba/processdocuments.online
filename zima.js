#!/usr/bin/env node

/**
 * ZIMA - Interactive AI Coding Assistant
 * Powered by Qwen2.5-Coder-14B via Ollama
 * Claude Code-style TUI interface
 */

const blessed = require('blessed');
const chalk = require('chalk');
const fs = require('fs');
const path = require('path');
const { exec, spawn } = require('child_process');
const { promisify } = require('util');
const { glob } = require('glob');

const execAsync = promisify(exec);

// Background shell tracking
const backgroundShells = new Map();
let shellIdCounter = 0;

// Configuration
const OLLAMA_URL = 'http://localhost:11434/api/generate';
const MODEL = 'qwen2.5-coder:14b';
const WORKSPACE = process.cwd();
const HISTORY_FILE = path.join(require('os').homedir(), '.zima_history.json');

// Chat history
let chatHistory = [];
let currentContext = [];
let currentStreamingMessage = null; // Track streaming message line

// Pending suggestions awaiting approval
let pendingSuggestion = null;

// Todo list for task management
let todoList = [];

// Session context for tracking (P0 - Context Tracking)
const sessionContext = {
  filesRead: new Map(),  // filepath -> {content, timestamp, hash}
  toolCalls: [],
  errors: [],
  checkpointCount: 0,
  lastEditedFiles: [],  // Track files edited in current session (for auto-test)
  mode: 'execute'  // 'planning' or 'execute' (P2 - Planning Mode)
};

// Planning mode state (P2 - Planning Mode)
const planningState = {
  pendingPlan: null,  // Current plan awaiting approval
  currentTask: null   // Active task being executed
};

// Memory system (P3 - Memory System)
const MEMORY_FILE = path.join(require('os').homedir(), '.zima_memory.json');
let memoryStore = {
  userPreferences: {},
  projectPatterns: {},
  codeConventions: {},
  learnings: []
};

// Error handling wrapper (P0 - Error Handling)
async function wrapToolCall(toolName, toolFn, ...args) {
  const startTime = Date.now();
  sessionContext.toolCalls.push({ toolName, timestamp: startTime, args });
  
  try {
    const result = await toolFn(...args);
    return { success: true, data: result, duration: Date.now() - startTime };
  } catch (error) {
    const errorInfo = {
      toolName,
      error: error.message,
      stack: error.stack,
      timestamp: Date.now(),
      args
    };
    sessionContext.errors.push(errorInfo);
    
    appendMessage('error', `⚠️ Tool ${toolName} failed: ${error.message}`);
    
    // Auto-retry for retryable errors
    if (isRetryableError(error) && !args.__retryAttempt) {
      appendMessage('system', `🔄 Retrying ${toolName}...`);
      await sleep(1000);
      return wrapToolCall(toolName, toolFn, ...args, { __retryAttempt: true });
    }
    
    return { success: false, error: error.message, duration: Date.now() - startTime };
  }
}

function isRetryableError(error) {
  const retryablePatterns = [
    /ECONNREFUSED/,
    /ETIMEDOUT/,
    /ENOTFOUND/,
    /rate limit/i
  ];
  return retryablePatterns.some(pattern => pattern.test(error.message));
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

// Status update helper (P1 - Status Updates)
function statusUpdate(message, type = 'info') {
  const timestamp = new Date().toLocaleTimeString();
  appendMessage('status', `[${type.toUpperCase()}] ${message}`);
}

// Checkpoint system (P1 - Checkpoints)
const CHECKPOINT_INTERVAL = 5;

function maybeCheckpoint() {
  sessionContext.toolCalls.length % CHECKPOINT_INTERVAL === 0;
  
  if (sessionContext.toolCalls.length > 0 && sessionContext.toolCalls.length % CHECKPOINT_INTERVAL === 0) {
    const recentTools = sessionContext.toolCalls.slice(-CHECKPOINT_INTERVAL);
    const toolNames = recentTools.map(t => t.toolName).join(', ');
    const avgDuration = recentTools.reduce((sum, t) => sum + (t.duration || 0), 0) / CHECKPOINT_INTERVAL;
    
    const summary = `Completed ${sessionContext.toolCalls.length} tool calls (recent: ${toolNames}). Avg: ${Math.round(avgDuration)}ms`;
    appendMessage('checkpoint', summary);
    sessionContext.checkpointCount++;
  }
}

// Memory system helpers (P3 - Memory System)
function loadMemory() {
  try {
    if (fs.existsSync(MEMORY_FILE)) {
      const data = fs.readFileSync(MEMORY_FILE, 'utf8');
      memoryStore = JSON.parse(data);
    }
  } catch (error) {
    // Silent fail - start with empty memory
  }
}

function saveMemory() {
  try {
    fs.writeFileSync(MEMORY_FILE, JSON.stringify(memoryStore, null, 2), 'utf8');
  } catch (error) {
    // Silent fail
  }
}

function addLearning(category, key, value) {
  memoryStore.learnings.push({
    category,
    key,
    value,
    timestamp: Date.now(),
    count: 1
  });
  
  // Keep only last 100 learnings
  if (memoryStore.learnings.length > 100) {
    memoryStore.learnings = memoryStore.learnings.slice(-100);
  }
  
  saveMemory();
}

// Testing protocol (P1 - Testing Protocols)
async function autoTestAfterEdit(editedFiles) {
  // Only auto-test if files were actually modified
  if (!editedFiles || editedFiles.length === 0) {
    return;
  }
  
  statusUpdate(`Auto-testing after editing ${editedFiles.length} file(s)...`, 'info');
  
  try {
    const result = await TOOLS.verify_changes(editedFiles);
    
    if (result.includes('✓') || result.includes('passed')) {
      statusUpdate('Auto-test passed', 'success');
    } else if (result.includes('✗') || result.includes('failed')) {
      statusUpdate('Auto-test failed - review needed', 'warning');
    }
    
    return result;
  } catch (error) {
    // Silent fail - verification not available
    return null;
  }
}

// Safety validation system (P0 - Bolt-inspired, P2 - Complete)
const SAFETY_RULES = {
  database: {
    forbidden: [
      'DROP TABLE',
      'DROP DATABASE',
      'TRUNCATE',
      'DELETE FROM users',
      'ALTER TABLE DROP COLUMN',  // Data loss risk
      'DROP COLUMN',                // Data loss risk
      'DISABLE TRIGGER',            // Security bypass
      'GRANT ALL'                   // Over-permissive
    ],
    requiresConfirmation: [
      'ALTER TABLE',
      'UPDATE users',
      'DELETE FROM',
      'CREATE USER',
      'GRANT',
      'REVOKE'
    ],
    migrationPatterns: {
      // Two-action migration: create + execute separately
      requiresTwoSteps: ['ALTER TABLE', 'CREATE INDEX', 'DROP INDEX'],
      rlsRequired: ['CREATE TABLE', 'ALTER TABLE ADD COLUMN']  // Require RLS for new tables/columns
    }
  },
  filesystem: {
    forbidden: [
      'rm -rf /',
      'rm -rf ~',
      'rm -rf *',
      'format',
      'dd if=',
      '> /dev/',
      'mkfs.'
    ],
    requiresConfirmation: [
      'rm -r',
      'rm -rf',
      'git reset --hard',
      'git clean -fd'
    ]
  },
  code: {
    forbiddenPatterns: [
      /API_KEY\s*=\s*["'][^"']{20,}["']/,  // Hardcoded API keys
      /password\s*=\s*["'][^"']+["']/i,     // Hardcoded passwords
      /SECRET\s*=\s*["'][^"']{10,}["']/i    // Hardcoded secrets
    ]
  }
};

function validateOperation(type, operation) {
  const rules = SAFETY_RULES[type];
  if (!rules) return { allowed: true };
  
  // Check forbidden operations
  if (rules.forbidden) {
    for (const forbidden of rules.forbidden) {
      if (operation.includes(forbidden)) {
        return {
          allowed: false,
          reason: `Forbidden ${type} operation: ${forbidden}`,
          suggestion: `This operation is blocked for safety. Please verify your intent.`
        };
      }
    }
  }
  
  // Check confirmation-required operations
  if (rules.requiresConfirmation) {
    for (const needsConfirm of rules.requiresConfirmation) {
      if (operation.includes(needsConfirm)) {
        return {
          allowed: true,
          needsConfirmation: true,
          reason: `Potentially destructive ${type} operation: ${needsConfirm}`
        };
      }
    }
  }
  
  // Check forbidden patterns (for code)
  if (rules.forbiddenPatterns) {
    for (const pattern of rules.forbiddenPatterns) {
      if (pattern.test(operation)) {
        return {
          allowed: false,
          reason: `Security risk detected: Hardcoded credential pattern`,
          suggestion: `Use environment variables instead of hardcoding secrets.`
        };
      }
    }
  }
  
  return { allowed: true };
}

// Claude-inspired tools for ZIMA
const TOOLS = {
  // File operations
  create_file: async (filepath, content = '') => {
    // Safety check for hardcoded secrets
    const validation = validateOperation('code', content);
    if (!validation.allowed) {
      return `❌ ${validation.reason}\n${validation.suggestion}`;
    }
    
    const fullPath = path.isAbsolute(filepath) ? filepath : path.join(WORKSPACE, filepath);
    const dir = path.dirname(fullPath);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(fullPath, content, 'utf8');
    return `Created: ${fullPath}`;
  },
  
  read_file: async (filepath, offset, limit) => {
    const fullPath = path.isAbsolute(filepath) ? filepath : path.join(WORKSPACE, filepath);
    
    // Context tracking: Check cache first
    if (sessionContext.filesRead.has(fullPath)) {
      const cached = sessionContext.filesRead.get(fullPath);
      const stats = fs.statSync(fullPath);
      const currentMtime = stats.mtimeMs;
      
      // If file hasn't changed, use cache
      if (cached.mtime === currentMtime) {
        const content = cached.content;
        if (offset !== undefined || limit !== undefined) {
          const lines = content.split('\n');
          const start = offset || 0;
          const end = limit ? start + limit : lines.length;
          return lines.slice(start, end).map((line, i) => `${start + i + 1}: ${line}`).join('\n');
        }
        return content;
      }
    }
    
    // Read file and cache
    const content = fs.readFileSync(fullPath, 'utf8');
    const stats = fs.statSync(fullPath);
    sessionContext.filesRead.set(fullPath, {
      content,
      mtime: stats.mtimeMs,
      timestamp: Date.now()
    });
    
    if (offset !== undefined || limit !== undefined) {
      const lines = content.split('\n');
      const start = offset || 0;
      const end = limit ? start + limit : lines.length;
      return lines.slice(start, end).map((line, i) => `${start + i + 1}: ${line}`).join('\n');
    }
    return content;
  },
  
  edit_file: async (filepath, old_string, new_string, replace_all = false) => {
    const fullPath = path.isAbsolute(filepath) ? filepath : path.join(WORKSPACE, filepath);
    let content = fs.readFileSync(fullPath, 'utf8');
    
    if (replace_all) {
      const regex = new RegExp(old_string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'g');
      content = content.replace(regex, new_string);
    } else {
      content = content.replace(old_string, new_string);
    }
    
    fs.writeFileSync(fullPath, content, 'utf8');
    
    // Invalidate cache after edit
    sessionContext.filesRead.delete(fullPath);
    
    // Track edited file for auto-testing (P1 - Testing Protocols)
    if (!sessionContext.lastEditedFiles.includes(fullPath)) {
      sessionContext.lastEditedFiles.push(fullPath);
    }
    
    // Auto-test after edit (if verify_changes is available)
    setTimeout(() => autoTestAfterEdit([fullPath]), 500);
    
    return `Edited: ${fullPath}`;
  },
  
  multi_edit: async (filepath, edits) => {
    const fullPath = path.isAbsolute(filepath) ? filepath : path.join(WORKSPACE, filepath);
    let content = fs.readFileSync(fullPath, 'utf8');
    
    for (const edit of edits) {
      if (edit.replace_all) {
        const regex = new RegExp(edit.old_string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'g');
        content = content.replace(regex, edit.new_string);
      } else {
        content = content.replace(edit.old_string, edit.new_string);
      }
    }
    
    fs.writeFileSync(fullPath, content, 'utf8');
    
    // Invalidate cache after edit
    sessionContext.filesRead.delete(fullPath);
    
    // Track edited file for auto-testing (P1 - Testing Protocols)
    if (!sessionContext.lastEditedFiles.includes(fullPath)) {
      sessionContext.lastEditedFiles.push(fullPath);
    }
    
    // Auto-test after edit (if verify_changes is available)
    setTimeout(() => autoTestAfterEdit([fullPath]), 500);
    
    return `Applied ${edits.length} edits to: ${fullPath}`;
  },
  
  // Directory operations
  list_files: async (dirpath = '.', ignore = []) => {
    const fullPath = path.isAbsolute(dirpath) ? dirpath : path.join(WORKSPACE, dirpath);
    const files = fs.readdirSync(fullPath);
    const filtered = files.filter(f => {
      return !ignore.some(pattern => {
        const regex = new RegExp(pattern.replace(/\*/g, '.*'));
        return regex.test(f);
      });
    });
    return filtered.join('\n');
  },
  
  glob_files: async (pattern, search_path) => {
    const searchDir = search_path 
      ? (path.isAbsolute(search_path) ? search_path : path.join(WORKSPACE, search_path))
      : WORKSPACE;
    const files = await glob(pattern, { cwd: searchDir });
    return files.join('\n');
  },
  
  grep: async (pattern, search_path, case_insensitive = false) => {
    const searchPath = search_path 
      ? (path.isAbsolute(search_path) ? search_path : path.join(WORKSPACE, search_path))
      : WORKSPACE;
    
    try {
      // Use ripgrep (rg) if available, fallback to grep
      const rgAvailable = await execAsync('which rg').then(() => true).catch(() => false);
      
      if (rgAvailable) {
        // Use ripgrep with better defaults
        const flags = case_insensitive ? '-i' : '';
        const { stdout } = await execAsync(`rg ${flags} --color never -n "${pattern}" "${searchPath}" 2>/dev/null || true`);
        return stdout || 'No matches found';
      } else {
        // Fallback to standard grep
        const flags = case_insensitive ? '-ri' : '-r';
        const { stdout } = await execAsync(`grep ${flags} "${pattern}" "${searchPath}" 2>/dev/null || true`);
        return stdout || 'No matches found';
      }
    } catch (error) {
      return 'No matches found';
    }
  },
  
  // Shell operations
  run_command: async (command, run_in_background = false) => {
    // Safety validation for dangerous commands
    const validation = validateOperation('filesystem', command);
    if (!validation.allowed) {
      return `🚫 BLOCKED: ${validation.reason}\n${validation.suggestion}`;
    }
    if (validation.needsConfirmation) {
      return `⚠️ CONFIRMATION NEEDED: ${validation.reason}\nThis command requires explicit user approval. Please confirm.`;
    }
    
    if (run_in_background) {
      const shellId = `shell_${shellIdCounter++}`;
      const shell = spawn('bash', ['-c', command], {
        cwd: WORKSPACE,
        stdio: ['pipe', 'pipe', 'pipe']
      });
      
      let output = '';
      shell.stdout.on('data', (data) => output += data.toString());
      shell.stderr.on('data', (data) => output += data.toString());
      
      backgroundShells.set(shellId, { shell, output });
      return `Background shell started: ${shellId}\nUse bash_output("${shellId}") to check output.`;
    }
    
    const { stdout, stderr } = await execAsync(command, { cwd: WORKSPACE, timeout: 120000 });
    return stdout || stderr;
  },
  
  bash_output: async (bash_id) => {
    const shellData = backgroundShells.get(bash_id);
    if (!shellData) return `Shell ${bash_id} not found`;
    return shellData.output || 'No output yet';
  },
  
  kill_bash: async (bash_id) => {
    const shellData = backgroundShells.get(bash_id);
    if (!shellData) return `Shell ${bash_id} not found`;
    shellData.shell.kill();
    backgroundShells.delete(bash_id);
    return `Killed shell: ${bash_id}`;
  },
  
  // Verification & Quality Assurance (P1 - Full Implementation)
  verify_changes: async (files = []) => {
    const results = {
      lint: { available: false, passed: true, output: '' },
      typecheck: { available: false, passed: true, output: '' },
      test: { available: false, passed: true, output: '' },
      build: { available: false, passed: true, output: '' }
    };
    
    // Detect project type
    let projectType = 'unknown';
    let packageJson = null;
    
    if (fs.existsSync(path.join(WORKSPACE, 'package.json'))) {
      projectType = 'node';
      packageJson = JSON.parse(fs.readFileSync(path.join(WORKSPACE, 'package.json'), 'utf8'));
    } else if (fs.existsSync(path.join(WORKSPACE, 'requirements.txt'))) {
      projectType = 'python';
    } else if (fs.existsSync(path.join(WORKSPACE, 'Cargo.toml'))) {
      projectType = 'rust';
    } else if (fs.existsSync(path.join(WORKSPACE, 'go.mod'))) {
      projectType = 'go';
    }
    
    // Node.js project verification
    if (projectType === 'node' && packageJson) {
      const scripts = packageJson.scripts || {};
      
      // Lint
      if (scripts.lint) {
        results.lint.available = true;
        try {
          const { stdout } = await execAsync('npm run lint', { cwd: WORKSPACE, timeout: 30000 });
          results.lint.passed = true;
          results.lint.output = stdout.substring(0, 500);
        } catch (error) {
          results.lint.passed = false;
          results.lint.output = (error.stdout || error.stderr || error.message).substring(0, 500);
        }
      }
      
      // Typecheck
      if (scripts.typecheck || scripts['type-check'] || scripts.tsc) {
        results.typecheck.available = true;
        const cmd = scripts.typecheck ? 'typecheck' : (scripts['type-check'] ? 'type-check' : 'tsc');
        try {
          const { stdout } = await execAsync(`npm run ${cmd}`, { cwd: WORKSPACE, timeout: 30000 });
          results.typecheck.passed = true;
          results.typecheck.output = stdout.substring(0, 500);
        } catch (error) {
          results.typecheck.passed = false;
          results.typecheck.output = (error.stdout || error.stderr || error.message).substring(0, 500);
        }
      }
      
      // Tests
      if (scripts.test) {
        results.test.available = true;
        try {
          const { stdout } = await execAsync('npm test', { cwd: WORKSPACE, timeout: 60000 });
          results.test.passed = true;
          results.test.output = stdout.substring(0, 500);
        } catch (error) {
          results.test.passed = false;
          results.test.output = (error.stdout || error.stderr || error.message).substring(0, 500);
        }
      }
      
      // Build
      if (scripts.build) {
        results.build.available = true;
        try {
          const { stdout } = await execAsync('npm run build', { cwd: WORKSPACE, timeout: 120000 });
          results.build.passed = true;
          results.build.output = stdout.substring(0, 500);
        } catch (error) {
          results.build.passed = false;
          results.build.output = (error.stdout || error.stderr || error.message).substring(0, 500);
        }
      }
    }
    
    // Python project verification
    if (projectType === 'python') {
      try {
        await execAsync('which flake8', { cwd: WORKSPACE });
        results.lint.available = true;
        try {
          const { stdout } = await execAsync('flake8 .', { cwd: WORKSPACE, timeout: 30000 });
          results.lint.passed = true;
          results.lint.output = stdout.substring(0, 500);
        } catch (error) {
          results.lint.passed = false;
          results.lint.output = (error.stdout || error.stderr || error.message).substring(0, 500);
        }
      } catch (e) {}
      
      try {
        await execAsync('which mypy', { cwd: WORKSPACE });
        results.typecheck.available = true;
        try {
          const { stdout } = await execAsync('mypy .', { cwd: WORKSPACE, timeout: 30000 });
          results.typecheck.passed = true;
          results.typecheck.output = stdout.substring(0, 500);
        } catch (error) {
          results.typecheck.passed = false;
          results.typecheck.output = (error.stdout || error.stderr || error.message).substring(0, 500);
        }
      } catch (e) {}
      
      try {
        await execAsync('which pytest', { cwd: WORKSPACE });
        results.test.available = true;
        try {
          const { stdout } = await execAsync('pytest', { cwd: WORKSPACE, timeout: 60000 });
          results.test.passed = true;
          results.test.output = stdout.substring(0, 500);
        } catch (error) {
          results.test.passed = false;
          results.test.output = (error.stdout || error.stderr || error.message).substring(0, 500);
        }
      } catch (e) {}
    }
    
    // Rust project verification
    if (projectType === 'rust') {
      results.lint.available = true;
      try {
        const { stdout } = await execAsync('cargo clippy', { cwd: WORKSPACE, timeout: 60000 });
        results.lint.passed = true;
        results.lint.output = stdout.substring(0, 500);
      } catch (error) {
        results.lint.passed = false;
        results.lint.output = (error.stdout || error.stderr || error.message).substring(0, 500);
      }
      
      results.test.available = true;
      try {
        const { stdout } = await execAsync('cargo test', { cwd: WORKSPACE, timeout: 120000 });
        results.test.passed = true;
        results.test.output = stdout.substring(0, 500);
      } catch (error) {
        results.test.passed = false;
        results.test.output = (error.stdout || error.stderr || error.message).substring(0, 500);
      }
    }
    
    // Format summary
    const summary = [];
    summary.push(`Project: ${projectType}`);
    
    if (results.lint.available) {
      summary.push(`Lint: ${results.lint.passed ? '✅ PASSED' : '❌ FAILED'}`);
    }
    if (results.typecheck.available) {
      summary.push(`Typecheck: ${results.typecheck.passed ? '✅ PASSED' : '❌ FAILED'}`);
    }
    if (results.test.available) {
      summary.push(`Tests: ${results.test.passed ? '✅ PASSED' : '❌ FAILED'}`);
    }
    if (results.build.available) {
      summary.push(`Build: ${results.build.passed ? '✅ PASSED' : '❌ FAILED'}`);
    }
    
    if (!results.lint.available && !results.typecheck.available && !results.test.available) {
      return `Project: ${projectType}\n\nNo verification tools detected.\nConsider adding lint, typecheck, or test scripts to package.json.`;
    }
    
    const allPassed = Object.values(results).every(r => !r.available || r.passed);
    const header = allPassed ? '✅ All Checks Passed' : '❌ Some Checks Failed';
    
    let output = `${header}\n\n${summary.join('\n')}`;
    
    // Add failure details
    for (const [key, result] of Object.entries(results)) {
      if (result.available && !result.passed) {
        output += `\n\n${key.toUpperCase()} OUTPUT:\n${result.output}`;
      }
    }
    
    return output;
  },
  
  // Database Safety Patterns (P2 - Bolt-inspired, Complete)
  create_migration: async (name, sql, rollback_sql = '') => {
    const timestamp = Date.now();
    const migrationFile = `migration_${timestamp}_${name}.sql`;
    const migrationPath = path.join(WORKSPACE, 'migrations', migrationFile);
    
    // Validate SQL for forbidden operations
    const validation = validateOperation('database', sql);
    if (!validation.allowed) {
      return `❌ ${validation.reason}\n${validation.suggestion}`;
    }
    
    // Check if RLS is required
    const requiresRLS = SAFETY_RULES.database.migrationPatterns.rlsRequired.some(pattern => 
      sql.toUpperCase().includes(pattern)
    );
    
    if (requiresRLS && !sql.toUpperCase().includes('ROW LEVEL SECURITY')) {
      return `⚠️ WARNING: This migration creates/modifies a table but doesn't include Row Level Security (RLS).\n\nRecommendation:\nALTER TABLE your_table ENABLE ROW LEVEL SECURITY;\nCREATE POLICY ... ON your_table ...\n\nProceed only if you understand the security implications.`;
    }
    
    // Create migration file
    const migrationContent = `-- Migration: ${name}
-- Created: ${new Date().toISOString()}
-- Status: PENDING (use execute_migration to run)

-- UP Migration
${sql}

-- DOWN Migration (Rollback)
${rollback_sql || '-- No rollback SQL provided'}

-- Security Notes:
-- - Review this migration before execution
-- - Ensure proper RLS policies are in place
-- - Test rollback in development first
`;
    
    const dir = path.dirname(migrationPath);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(migrationPath, migrationContent, 'utf8');
    
    return `✅ Migration created: ${migrationFile}

NEXT STEPS:
1. Review the migration file: migrations/${migrationFile}
2. Test in development environment
3. Use execute_migration("${migrationFile}") to apply

This is a TWO-STEP process for safety:
- Step 1: CREATE migration (done ✅)
- Step 2: EXECUTE migration (requires explicit approval)`;
  },
  
  execute_migration: async (migration_file) => {
    const migrationPath = path.join(WORKSPACE, 'migrations', migration_file);
    
    if (!fs.existsSync(migrationPath)) {
      return `❌ Migration file not found: ${migration_file}\n\nAvailable migrations:\n${
        fs.existsSync(path.join(WORKSPACE, 'migrations'))
          ? fs.readdirSync(path.join(WORKSPACE, 'migrations')).filter(f => f.endsWith('.sql')).join('\n')
          : 'No migrations directory found'
      }`;
    }
    
    const content = fs.readFileSync(migrationPath, 'utf8');
    
    return `⚠️ MIGRATION EXECUTION BLOCKED

Migration file: ${migration_file}

SAFETY PROTOCOL:
ZIMA does not execute SQL directly for safety reasons.

To execute this migration:
1. Review the migration file carefully
2. Run it through your database client (psql, mysql, etc.)
3. Update migration status manually

Migration content preview:
${content.substring(0, 500)}...

If you need to execute via command line, use run_command with explicit approval.`;
  },
  
  validate_rls: async (table_name) => {
    return `Row Level Security (RLS) Validation

To validate RLS for table "${table_name}":

1. Check if RLS is enabled:
   SELECT tablename, rowsecurity FROM pg_tables WHERE tablename = '${table_name}';

2. List RLS policies:
   SELECT * FROM pg_policies WHERE tablename = '${table_name}';

3. Enable RLS:
   ALTER TABLE ${table_name} ENABLE ROW LEVEL SECURITY;

4. Create policy example:
   CREATE POLICY user_isolation ON ${table_name}
   FOR ALL
   USING (user_id = current_user_id());

RLS Best Practices:
- Enable RLS on all tables with user data
- Create policies for SELECT, INSERT, UPDATE, DELETE separately
- Test policies with different user roles
- Use functions for complex authorization logic

Would you like help creating RLS policies?`;
  },
  
  // Task management (Claude Code-inspired)
  todo_write: async (todos) => {
    // Validate: only ONE task can be in_progress
    const inProgressCount = todos.filter(t => t.status === 'in_progress').length;
    if (inProgressCount > 1) {
      return `Error: Only ONE task can be "in_progress" at a time. Found ${inProgressCount}.`;
    }
    
    // Validate: each todo has required fields
    for (const todo of todos) {
      if (!todo.content || !todo.status || !todo.activeForm) {
        return `Error: Each todo must have content, status, and activeForm`;
      }
      if (!['pending', 'in_progress', 'completed'].includes(todo.status)) {
        return `Error: Status must be pending, in_progress, or completed`;
      }
    }
    
    todoList = todos;
    const summary = `Tasks: ${todos.filter(t => t.status === 'pending').length} pending, ${todos.filter(t => t.status === 'in_progress').length} in progress, ${todos.filter(t => t.status === 'completed').length} completed`;
    return `Todo list updated. ${summary}`;
  },
  
  todo_read: async () => {
    if (todoList.length === 0) {
      return `No tasks in todo list`;
    }
    return JSON.stringify(todoList, null, 2);
  },
  
  // P2 - Planning Mode (Devin AI-inspired)
  create_plan: async (tasks) => {
    if (!Array.isArray(tasks) || tasks.length === 0) {
      return `Error: tasks must be a non-empty array`;
    }
    
    // Validate task structure
    for (const task of tasks) {
      if (!task.description || !task.complexity) {
        return `Error: Each task must have description and complexity (low/medium/high)`;
      }
    }
    
    planningState.pendingPlan = tasks;
    sessionContext.mode = 'planning';
    
    const planText = tasks.map((t, i) => 
      `${i + 1}. ${t.description} (${t.complexity} complexity)${t.estimate ? ` - ${t.estimate}` : ''}`
    ).join('\n');
    
    return `📋 Implementation Plan Created:\n\n${planText}\n\nTotal tasks: ${tasks.length}\n\nThis plan is awaiting your approval. Respond with "approve" to proceed with execution.`;
  },
  
  approve_plan: async () => {
    if (!planningState.pendingPlan) {
      return `No pending plan to approve`;
    }
    
    const tasks = planningState.pendingPlan;
    sessionContext.mode = 'execute';
    planningState.currentTask = tasks[0];
    planningState.pendingPlan = null;
    
    return `✅ Plan approved. Starting execution with task 1: ${tasks[0].description}`;
  },
  
  // P3 - Contracts.md System (Emergent-inspired)
  create_contracts: async (apiSpecs) => {
    if (!Array.isArray(apiSpecs) || apiSpecs.length === 0) {
      return `Error: apiSpecs must be a non-empty array`;
    }
    
    // Validate spec structure
    for (const spec of apiSpecs) {
      if (!spec.endpoint || !spec.method) {
        return `Error: Each spec must have endpoint and method`;
      }
    }
    
    const contractMd = `# API Contracts

${apiSpecs.map(spec => `
## ${spec.endpoint}
**Method**: ${spec.method}
**Auth**: ${spec.auth || 'None'}
**Description**: ${spec.description || 'No description provided'}

**Request**:
\`\`\`json
${JSON.stringify(spec.request || {}, null, 2)}
\`\`\`

**Response**:
\`\`\`json
${JSON.stringify(spec.response || {}, null, 2)}
\`\`\`

**Error Codes**:
${(spec.errors || []).map(e => `- ${e.code}: ${e.message}`).join('\n') || '- 500: Internal Server Error'}
`).join('\n---\n')}

## Implementation Notes

This contract file defines the API interface. Implement the backend according to these specifications.

**Generated**: ${new Date().toISOString()}
**Endpoints**: ${apiSpecs.length}
`;
    
    const contractPath = path.join(WORKSPACE, 'contracts.md');
    fs.writeFileSync(contractPath, contractMd, 'utf8');
    
    return `✅ API contracts created: contracts.md\n\nEndpoints defined: ${apiSpecs.length}\nBackend can now implement from these specifications.`;
  },
  
  // P3 - Memory System
  remember: async (category, key, value) => {
    if (!category || !key || !value) {
      return `Error: category, key, and value are required`;
    }
    
    const validCategories = ['userPreferences', 'projectPatterns', 'codeConventions'];
    if (!validCategories.includes(category)) {
      return `Error: category must be one of: ${validCategories.join(', ')}`;
    }
    
    memoryStore[category][key] = value;
    addLearning(category, key, value);
    
    return `✅ Remembered: ${category}.${key} = ${JSON.stringify(value)}`;
  },
  
  recall: async (category, key = null) => {
    if (!category) {
      return `Error: category is required`;
    }
    
    if (key) {
      const value = memoryStore[category]?.[key];
      if (value === undefined) {
        return `No memory found for ${category}.${key}`;
      }
      return JSON.stringify(value, null, 2);
    } else {
      const categoryData = memoryStore[category];
      if (!categoryData || Object.keys(categoryData).length === 0) {
        return `No memories found in category: ${category}`;
      }
      return JSON.stringify(categoryData, null, 2);
    }
  },
  
  list_learnings: async (limit = 10) => {
    const recent = memoryStore.learnings.slice(-limit).reverse();
    if (recent.length === 0) {
      return `No learnings recorded yet`;
    }
    
    const formatted = recent.map((l, i) => 
      `${i + 1}. [${l.category}] ${l.key}: ${JSON.stringify(l.value)} (${new Date(l.timestamp).toLocaleString()})`
    ).join('\n');
    
    return `Recent learnings (${recent.length}):\n\n${formatted}`;
  },
  
  // P3 - Semantic Search (RAG with embeddings)
  semantic_search: async (query, filePattern = '**/*.{js,ts,py,go,rs,java,c,cpp}') => {
    if (!query || query.trim().length === 0) {
      return `Error: query is required`;
    }
    
    statusUpdate('Performing semantic code search...', 'info');
    
    // Find all matching files
    const files = await glob(filePattern, { cwd: WORKSPACE, ignore: ['node_modules/**', '.git/**', 'dist/**', 'build/**'] });
    
    if (files.length === 0) {
      return `No files found matching pattern: ${filePattern}`;
    }
    
    // For now, use grep-based search (embedding model would require additional dependencies)
    // This is a simplified version - full RAG would use embeddings
    const results = [];
    const queryLower = query.toLowerCase();
    const keywords = queryLower.split(/\s+/).filter(k => k.length > 2);
    
    for (const file of files.slice(0, 100)) {  // Limit to 100 files
      const fullPath = path.join(WORKSPACE, file);
      try {
        const content = fs.readFileSync(fullPath, 'utf8');
        const lines = content.split('\n');
        
        let score = 0;
        const matches = [];
        
        lines.forEach((line, idx) => {
          const lineLower = line.toLowerCase();
          let lineScore = 0;
          
          keywords.forEach(keyword => {
            if (lineLower.includes(keyword)) {
              lineScore += 1;
            }
          });
          
          if (lineScore > 0) {
            score += lineScore;
            matches.push({ line: idx + 1, text: line.trim(), score: lineScore });
          }
        });
        
        if (score > 0) {
          results.push({
            file,
            score,
            matches: matches.slice(0, 3)  // Top 3 matches per file
          });
        }
      } catch (error) {
        // Skip files that can't be read
      }
    }
    
    // Sort by relevance score
    results.sort((a, b) => b.score - a.score);
    
    const topResults = results.slice(0, 10);
    
    if (topResults.length === 0) {
      return `No semantic matches found for: "${query}"`;
    }
    
    const formatted = topResults.map((r, i) => 
      `${i + 1}. ${r.file} (relevance: ${r.score})\n${r.matches.map(m => 
        `   Line ${m.line}: ${m.text}`
      ).join('\n')}`
    ).join('\n\n');
    
    return `🔍 Semantic Search Results for "${query}":\n\nFound ${results.length} files with matches, showing top ${topResults.length}:\n\n${formatted}`;
  }
};

// Load history
try {
  if (fs.existsSync(HISTORY_FILE)) {
    const data = JSON.parse(fs.readFileSync(HISTORY_FILE, 'utf8'));
    chatHistory = data.history || [];
  }
} catch (e) {
  // Ignore errors loading history
}

// Load memory (P3 - Memory System)
loadMemory();

// Save history
function saveHistory() {
  try {
    fs.writeFileSync(HISTORY_FILE, JSON.stringify({ history: chatHistory.slice(-50) }, null, 2));
  } catch (e) {
    // Ignore save errors
  }
}

// Create screen
const screen = blessed.screen({
  smartCSR: true,
  title: 'ZIMA - AI Coding Assistant',
  cursor: {
    artificial: true,
    shape: 'line',
    blink: true,
    color: 'white'
  }
});

// Header
const header = blessed.box({
  top: 0,
  left: 0,
  width: '100%',
  height: 3,
  content: `${chalk.cyan.bold('⚡ ZIMA')} ${chalk.gray('- Qwen2.5-Coder-14B')} ${chalk.yellow('|')} ${chalk.gray('Workspace:')} ${chalk.green(WORKSPACE)}`,
  tags: true,
  border: {
    type: 'line',
    fg: 'cyan'
  },
  style: {
    fg: 'white',
    border: {
      fg: 'cyan'
    }
  }
});

// Chat area (scrollable)
const chatBox = blessed.box({
  top: 3,
  left: 0,
  width: '100%',
  height: '100%-6',
  scrollable: true,
  alwaysScroll: true,
  scrollbar: {
    ch: '█',
    style: {
      fg: 'cyan'
    }
  },
  mouse: true,
  keys: true,
  vi: true,
  tags: true,
  border: {
    type: 'line',
    fg: 'white'
  },
  style: {
    fg: 'white',
    scrollbar: {
      bg: 'blue'
    }
  },
  interactive: true,
  input: true
});

// Input box
const inputBox = blessed.textarea({
  bottom: 0,
  left: 0,
  width: '100%',
  height: 3,
  inputOnFocus: true,
  border: {
    type: 'line',
    fg: 'green'
  },
  style: {
    fg: 'white',
    border: {
      fg: 'green'
    },
    focus: {
      border: {
        fg: 'cyan'
      }
    }
  },
  keys: true,
  mouse: true
});

// Loading indicator
const loader = blessed.loading({
  top: 'center',
  left: 'center',
  width: '50%',
  height: 5,
  border: {
    type: 'line',
    fg: 'yellow'
  },
  style: {
    fg: 'yellow',
    border: {
      fg: 'yellow'
    }
  },
  hidden: true
});

// Append to screen
screen.append(header);
screen.append(chatBox);
screen.append(inputBox);
screen.append(loader);

// Focus input
inputBox.focus();

// Add welcome message
function addWelcomeMessage() {
  const welcome = `
${chalk.cyan.bold('Welcome to ZIMA!')}
${chalk.gray('Your local AI coding assistant powered by Qwen2.5-Coder-14B')}

${chalk.yellow('Commands:')}
  ${chalk.green('/help')}    - Show this help
  ${chalk.green('/clear')}   - Clear chat history
  ${chalk.green('/tools')}   - List available tools
  ${chalk.green('/exit')}    - Exit ZIMA

${chalk.yellow('Keyboard shortcuts:')}
  ${chalk.green('Ctrl+C')}   - Exit
  ${chalk.green('Enter')}    - Send message
  ${chalk.green('↑/↓')}      - Scroll chat
  ${chalk.green('Click & drag')} - Select text to copy

${chalk.cyan('Ready! I can read/write files, run commands, and help with your code.')}
${chalk.gray('Try: "create a file called test.md" or "list files in this directory"')}

${chalk.yellow('💡 Smart Suggestions:')} I can suggest actions and execute them with your approval!
${chalk.gray('Just say "yes", "ok", or "do it" to approve, or "no" to decline.')}
`;
  
  appendMessage('system', welcome);
}

// Append message to chat
function appendMessage(role, content, streaming = false) {
  const timestamp = new Date().toLocaleTimeString();
  let prefix = '';
  let color = 'white';
  
  if (role === 'user') {
    prefix = chalk.green.bold('You');
    color = 'green';
  } else if (role === 'assistant') {
    prefix = chalk.cyan.bold('ZIMA');
    color = 'cyan';
  } else if (role === 'system') {
    prefix = chalk.yellow.bold('System');
    color = 'yellow';
  } else if (role === 'tool') {
    prefix = chalk.magenta.bold('Tool');
    color = 'magenta';
  } else if (role === 'status') {
    prefix = chalk.blue.bold('Status');
    color = 'blue';
  } else if (role === 'error') {
    prefix = chalk.red.bold('Error');
    color = 'red';
  } else if (role === 'checkpoint') {
    prefix = chalk.cyan.bold('Progress');
    color = 'cyan';
  }
  
  const message = `${chalk.gray(timestamp)} ${prefix}: ${content}\n`;
  
  chatBox.pushLine(message);
  chatBox.setScrollPerc(100);
  screen.render();
}

// Call Ollama API
async function callOllama(prompt, streaming = true) {
  const response = await fetch(OLLAMA_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      model: MODEL,
      prompt: prompt,
      stream: streaming,
      context: currentContext,
      options: {
        temperature: 0.3,  // Lower temperature for faster, more focused responses
        top_p: 0.9,
        top_k: 40,
        num_predict: 2048  // Max tokens to generate
      }
    })
  });
  
  if (streaming) {
    let fullResponse = '';
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    const timestamp = new Date().toLocaleTimeString();
    const prefix = 'ZIMA';
    
    // Get current chat content before streaming
    const currentContent = chatBox.getContent();
    const formattedPrefix = `{gray-fg}${timestamp}{/gray-fg} {cyan-fg}{bold}${prefix}{/bold}{/cyan-fg}`;
    
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      
      const chunk = decoder.decode(value);
      const lines = chunk.split('\n').filter(line => line.trim());
      
      for (const line of lines) {
        try {
          const data = JSON.parse(line);
          if (data.response) {
            fullResponse += data.response;
            
            // Update by replacing entire content with previous + streaming message
            chatBox.setContent(currentContent + `\n${formattedPrefix}: ${fullResponse}`);
            chatBox.setScrollPerc(100);
            screen.render();
          }
          if (data.context) {
            currentContext = data.context;
          }
        } catch (e) {
          // Ignore JSON parse errors
        }
      }
    }
    
    return fullResponse;
  } else {
    const data = await response.json();
    if (data.context) {
      currentContext = data.context;
    }
    return data.response;
  }
}

// Helper function to parse individual arguments (global scope)
function parseArgument(arg) {
  arg = arg.trim();
  
  // String (remove quotes and handle escape sequences)
  if ((arg.startsWith('"') && arg.endsWith('"')) || (arg.startsWith("'") && arg.endsWith("'"))) {
    return arg.slice(1, -1)
      .replace(/\\n/g, '\n')
      .replace(/\\t/g, '\t')
      .replace(/\\"/g, '"')
      .replace(/\\'/g, "'");
  }
  
  // Boolean
  if (arg === 'true') return true;
  if (arg === 'false') return false;
  
  // Null/undefined
  if (arg === 'null') return null;
  if (arg === 'undefined') return undefined;
  
  // Number
  if (/^-?\d+\.?\d*$/.test(arg)) {
    return parseFloat(arg);
  }
  
  // Array or Object (parse as JSON)
  if ((arg.startsWith('[') && arg.endsWith(']')) || (arg.startsWith('{') && arg.endsWith('}'))) {
    try {
      return JSON.parse(arg);
    } catch (e) {
      return arg;
    }
  }
  
  // Default: return as string
  return arg;
}

// Handle user input
async function handleInput(input) {
  const message = input.trim();
  
  if (!message) return;
  
  // Handle commands
  if (message.startsWith('/')) {
    const command = message.toLowerCase();
    
    if (command === '/exit' || command === '/quit') {
      saveHistory();
      process.exit(0);
    }
    
    if (command === '/clear') {
      chatBox.setContent('');
      chatHistory = [];
      currentContext = [];
      addWelcomeMessage();
      return;
    }
    
    if (command === '/help') {
      addWelcomeMessage();
      return;
    }
    
    if (command === '/tools') {
      const toolsInfo = `
${chalk.cyan.bold('Available Tools:')} (10 total)

${chalk.green('File Operations:')}
  • create_file(filepath, content) - Create new file
  • read_file(filepath, offset, limit) - Read file
  • edit_file(filepath, old, new, replace_all) - Replace text
  • multi_edit(filepath, edits) - Multiple edits
  
${chalk.green('Directory Operations:')}
  • list_files(dirpath, ignore) - List files
  • glob_files(pattern, path) - Find by pattern (*.js)
  • grep(pattern, path, case_insensitive) - Search contents
  
${chalk.green('Shell Operations:')}
  • run_command(command, background) - Execute commands
  • bash_output(bash_id) - Get background output
  • kill_bash(bash_id) - Kill background shell

${chalk.yellow('Usage:')} ZIMA automatically calls these when needed
${chalk.yellow('Examples:')}
  "create test.md with hello world"
  "find all JavaScript files"
  "search for 'function' in this directory"
  "edit config.json to change port 3000 to 8080"
`;
      appendMessage('system', toolsInfo);
      return;
    }
    
    appendMessage('system', `Unknown command: ${message}\nType /help for available commands`);
    return;
  }
  
  // Check if user is approving a pending suggestion
  const approvalWords = ['yes', 'approve', 'ok', 'do it', 'sure', 'go ahead', 'proceed'];
  if (pendingSuggestion && approvalWords.some(word => message.toLowerCase().includes(word))) {
    appendMessage('user', message);
    appendMessage('system', chalk.green(`✓ Executing suggestion: ${pendingSuggestion.description}`));
    
    try {
      // Parse and execute the pending suggestion
      const args = [];
      let currentArg = '';
      let inString = false;
      let inArray = false;
      let bracketCount = 0;
      let stringChar = null;
      const argsStr = pendingSuggestion.toolArgs;
      
      for (let i = 0; i < argsStr.length; i++) {
        const char = argsStr[i];
        const prevChar = i > 0 ? argsStr[i - 1] : null;
        
        if ((char === '"' || char === "'") && prevChar !== '\\') {
          if (!inString) {
            inString = true;
            stringChar = char;
          } else if (char === stringChar) {
            inString = false;
            stringChar = null;
          }
          currentArg += char;
        }
        else if ((char === '[' || char === '{') && !inString) {
          bracketCount++;
          inArray = true;
          currentArg += char;
        }
        else if ((char === ']' || char === '}') && !inString) {
          bracketCount--;
          if (bracketCount === 0) inArray = false;
          currentArg += char;
        }
        else if (char === ',' && !inString && !inArray) {
          args.push(parseArgument(currentArg.trim()));
          currentArg = '';
        }
        else {
          currentArg += char;
        }
      }
      
      if (currentArg.trim()) {
        args.push(parseArgument(currentArg.trim()));
      }
      
      // Execute the approved tool
      const result = await TOOLS[pendingSuggestion.toolName](...args);
      appendMessage('tool', `Result: ${result}`);
      pendingSuggestion = null;
      return;
      
    } catch (error) {
      appendMessage('system', chalk.red(`Error executing suggestion: ${error.message}`));
      pendingSuggestion = null;
      return;
    }
  }
  
  // Clear pending suggestion if user said no or asks something else
  if (pendingSuggestion) {
    const declineWords = ['no', 'cancel', 'skip', 'ignore', 'nope', 'don\'t'];
    if (declineWords.some(word => message.toLowerCase().includes(word))) {
      appendMessage('user', message);
      appendMessage('system', chalk.gray('Suggestion cancelled.'));
      pendingSuggestion = null;
      return;
    }
  }
  
  // Add user message to chat
  appendMessage('user', message);
  chatHistory.push({ role: 'user', content: message });
  
  // Show loading
  loader.load('Thinking...');
  
  try {
    // Build conversation context
    // Only include last 3 messages to reduce context size (speed optimization)
    const conversationHistory = chatHistory.slice(-3).map(h => 
      `${h.role === 'user' ? 'User' : 'Assistant'}: ${h.content.substring(0, 500)}`  // Truncate long messages
    ).join('\n');
    
    // Get project context including git status
    let projectContext = '';
    try {
      const files = fs.readdirSync(WORKSPACE);
      const relevantFiles = files.filter(f => 
        !f.startsWith('.') && 
        !['node_modules', 'vendor', 'dist', 'build'].includes(f)
      ).slice(0, 20);
      projectContext = `\n\nProject files in ${WORKSPACE}:\n${relevantFiles.join(', ')}`;
      
      // Check for package.json or README
      if (files.includes('package.json')) {
        const pkg = JSON.parse(fs.readFileSync(path.join(WORKSPACE, 'package.json'), 'utf8'));
        projectContext += `\n\nProject: ${pkg.name || 'Node.js project'}`;
        if (pkg.description) projectContext += ` - ${pkg.description}`;
      }
      if (files.includes('README.md')) {
        const readme = fs.readFileSync(path.join(WORKSPACE, 'README.md'), 'utf8');
        projectContext += `\n\nREADME summary: ${readme.substring(0, 300)}...`;
      }
      
      // Check for git repository and get status
      if (files.includes('.git')) {
        try {
          const { stdout: branch } = await execAsync('git branch --show-current', { cwd: WORKSPACE });
          const { stdout: status } = await execAsync('git status --short', { cwd: WORKSPACE });
          projectContext += `\n\nGit status:`;
          projectContext += `\nCurrent branch: ${branch.trim()}`;
          if (status.trim()) {
            projectContext += `\nModified files:\n${status.trim()}`;
          } else {
            projectContext += `\nWorking tree clean`;
          }
        } catch (e) {
          // Git not available or not a repo
        }
      }
    } catch (e) {
      // Ignore errors reading project context
    }
    
    const systemPrompt = `You are ZIMA, a fast AI coding assistant (Qwen2.5-Coder-14B).

STYLE: Concise, direct. No preambles. One-word answers when possible. Code refs: filepath:line.

COMPLEX TASKS: Brief outline (under 4 lines), then execute immediately.
Example: "I'll search auth.js, read login(), add validation, test. Starting..."
Simple tasks: Skip outline, execute directly.

SECURITY: Defensive only. Never expose secrets/keys. No malicious code.

TASKS: Use todo_write for multi-step. ONE in_progress max. Mark completed immediately.

TOOLS (use format: USE_TOOL: name("arg1", "arg2")):

FILES: create_file, read_file, edit_file, multi_edit
DIRS: list_files, glob_files, grep
SHELL: run_command, bash_output, kill_bash
TASKS: todo_write, todo_read
VERIFY: verify_changes (auto-detects: npm/python/rust)
DB: create_migration, execute_migration, validate_rls (2-step safety, RLS required)
PLAN: create_plan, approve_plan (for 5+ step tasks)
CONTRACTS: create_contracts (generates contracts.md)
MEMORY: remember, recall, list_learnings (persistent across sessions)
SEARCH: semantic_search (finds code by meaning)

EXAMPLES:
USE_TOOL: read_file("package.json")
USE_TOOL: edit_file("config.js", "3000", "8080", false)
USE_TOOL: glob_files("**/*.js", ".")
USE_TOOL: run_command("npm test", false)
USE_TOOL: verify_changes(["src/auth.ts"])

RULES:
- Use tools proactively. Don't ask permission.
- PARALLELIZE: Read 5 files → 1 response with 5 calls. NEVER sequential unless output A needed for input B.
- Follow existing code style. Check imports/neighbors before adding libs.
- After edits: verify_changes() runs (auto-triggered after 500ms).
- DB changes: create_migration (2-step), enable RLS, never DROP TABLE.
- Complex tasks (5+ steps): create_plan first.
- Memory: remember() user preferences, recall() for personalization.
- Git: NEVER commit unless explicitly asked.

Current workspace: ${WORKSPACE}${projectContext}

Recent conversation:
${conversationHistory}

User: ${message}

Now respond. If the user wants file operations, use the tools above.`;
    
    // Call Ollama (streaming will handle the message display)
    const response = await callOllama(systemPrompt, true);
    
    // Check if response contains tool calls (support multiple)
    if (response.includes('USE_TOOL:')) {
      // Match ALL tool calls in the response
      const toolMatches = [...response.matchAll(/USE_TOOL:\s*(\w+)\((.*?)\)(?=\s*(?:USE_TOOL:|$))/gs)];
      
      if (toolMatches.length > 0) {
        // Execute all tool calls in parallel when possible
        const toolExecutions = [];
        
        for (const toolMatch of toolMatches) {
          const toolName = toolMatch[1];
          const argsStr = toolMatch[2];
          
          try {
            // Enhanced argument parser for complex types
            const args = [];
            let currentArg = '';
            let inString = false;
            let inArray = false;
            let bracketCount = 0;
            let stringChar = null;
            
            for (let i = 0; i < argsStr.length; i++) {
              const char = argsStr[i];
              const prevChar = i > 0 ? argsStr[i - 1] : null;
              
              // Handle string delimiters
              if ((char === '"' || char === "'") && prevChar !== '\\') {
                if (!inString) {
                  inString = true;
                  stringChar = char;
                } else if (char === stringChar) {
                  inString = false;
                  stringChar = null;
                }
                currentArg += char;
              }
              // Handle arrays and objects
              else if ((char === '[' || char === '{') && !inString) {
                bracketCount++;
                inArray = true;
                currentArg += char;
              }
              else if ((char === ']' || char === '}') && !inString) {
                bracketCount--;
                if (bracketCount === 0) inArray = false;
                currentArg += char;
              }
              // Handle argument separator
              else if (char === ',' && !inString && !inArray) {
                args.push(parseArgument(currentArg.trim()));
                currentArg = '';
              }
              else {
                currentArg += char;
              }
            }
            
            // Add last argument
            if (currentArg.trim()) {
              args.push(parseArgument(currentArg.trim()));
            }
            
            // Add tool execution to batch (execute in parallel)
            if (TOOLS[toolName]) {
              toolExecutions.push({
                toolName,
                args,
                execute: async () => {
                  appendMessage('tool', `Executing: ${toolName}(...)`);
                  const result = await TOOLS[toolName](...args);
                  appendMessage('tool', `Result: ${result}`);
                  return { toolName, result };
                }
              });
            } else {
              appendMessage('system', chalk.red(`Unknown tool: ${toolName}`));
            }
          } catch (error) {
            appendMessage('system', chalk.red(`Tool error: ${error.message}`));
          }
        }
        
        // Execute all tools in parallel
        if (toolExecutions.length > 0) {
          const results = await Promise.all(toolExecutions.map(t => t.execute()));
          
          // Feed combined results back to ZIMA for follow-up
          const combinedResults = results.map(r => `${r.toolName}: ${r.result}`).join('\n');
          const followUpPrompt = `The following tools were executed:\n${combinedResults}\n\nBased on these results, provide your response to the user. 

If any result is empty or indicates something doesn't exist, you can SUGGEST an action by using this format:
SUGGEST: <brief description of what you'll do>
USE_TOOL: tool_name("arg1", "arg2")

The user can then approve by typing "yes", "approve", "ok", or "do it".`;
          
          const followUpResponse = await callOllama(followUpPrompt, true);
          
          // Check if response contains a suggestion
          if (followUpResponse.includes('SUGGEST:')) {
            const suggestMatch = followUpResponse.match(/SUGGEST:\s*(.+?)(?=\n|$)/);
            const toolMatch = followUpResponse.match(/USE_TOOL:\s*(\w+)\((.*)\)/s);
            
            if (suggestMatch && toolMatch) {
              pendingSuggestion = {
                description: suggestMatch[1].trim(),
                toolName: toolMatch[1],
                toolArgs: toolMatch[2]
              };
              appendMessage('system', chalk.yellow(`💡 Suggestion pending: ${pendingSuggestion.description}`));
              appendMessage('system', chalk.gray(`Type "yes", "approve", "ok", or "do it" to execute`));
            }
          }
          
          chatHistory.push({ role: 'assistant', content: followUpResponse });
        }
      }
    }
    
    // Save to history
    chatHistory.push({ role: 'assistant', content: response });
    saveHistory();
    
  } catch (error) {
    appendMessage('system', chalk.red(`Error: ${error.message}`));
  } finally {
    loader.stop();
  }
}

// Input handling
inputBox.key('enter', function() {
  const input = inputBox.getValue();
  inputBox.clearValue();
  handleInput(input);
});

// Quit handlers
screen.key(['C-c'], function() {
  saveHistory();
  return process.exit(0);
});

// Scroll handlers
chatBox.key(['up'], function() {
  chatBox.scroll(-1);
  screen.render();
});

chatBox.key(['down'], function() {
  chatBox.scroll(1);
  screen.render();
});

// Mouse wheel scrolling
chatBox.on('wheelup', function() {
  chatBox.scroll(-3);
  screen.render();
});

chatBox.on('wheeldown', function() {
  chatBox.scroll(3);
  screen.render();
});

// Initial render
addWelcomeMessage();
screen.render();