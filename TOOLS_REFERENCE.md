# ZIMA Tools Reference

ZIMA has **10 powerful tools** for file operations, directory management, and shell commands.

## Tool Format

All tools follow this format:
```
USE_TOOL: tool_name("arg1", "arg2", arg3)
```

- **Strings**: Wrap in double quotes `"text"`
- **Numbers**: No quotes `42` or `3.14`
- **Booleans**: `true` or `false`
- **Arrays**: JSON format `["item1", "item2"]`
- **Objects**: JSON format `{"key": "value"}`

---

## File Operations

### 1. create_file(filepath, content)
Create a new file with specified content.

**Parameters:**
- `filepath` (string): Relative or absolute path
- `content` (string): File content (use `\n` for newlines)

**Examples:**
```
USE_TOOL: create_file("test.md", "# Hello\nWorld")
USE_TOOL: create_file("config.json", "{\"port\": 3000}")
USE_TOOL: create_file("src/utils.js", "module.exports = {}")
```

---

### 2. read_file(filepath, offset, limit)
Read file contents with optional pagination.

**Parameters:**
- `filepath` (string): Path to file
- `offset` (number, optional): Starting line number
- `limit` (number, optional): Number of lines to read

**Examples:**
```
USE_TOOL: read_file("package.json")
USE_TOOL: read_file("largefile.txt", 100, 50)
USE_TOOL: read_file("/absolute/path/file.txt")
```

---

### 3. edit_file(filepath, old_string, new_string, replace_all)
Replace text in a file.

**Parameters:**
- `filepath` (string): Path to file
- `old_string` (string): Text to find
- `new_string` (string): Replacement text
- `replace_all` (boolean): Replace all occurrences (true) or just first (false)

**Examples:**
```
USE_TOOL: edit_file("config.json", "3000", "8080", false)
USE_TOOL: edit_file("app.js", "console.log", "logger.info", true)
USE_TOOL: edit_file("README.md", "Version 1.0", "Version 2.0", false)
```

---

### 4. multi_edit(filepath, edits)
Perform multiple sequential edits on one file.

**Parameters:**
- `filepath` (string): Path to file
- `edits` (array): Array of edit objects with `old_string`, `new_string`, `replace_all`

**Examples:**
```
USE_TOOL: multi_edit("app.js", [{"old_string": "var", "new_string": "const", "replace_all": true}, {"old_string": "http", "new_string": "https", "replace_all": true}])

USE_TOOL: multi_edit("config.js", [{"old_string": "port: 3000", "new_string": "port: 8080", "replace_all": false}])
```

---

## Directory Operations

### 5. list_files(dirpath, ignore)
List files in a directory with optional ignore patterns.

**Parameters:**
- `dirpath` (string): Directory path (default: ".")
- `ignore` (array, optional): Glob patterns to ignore

**Examples:**
```
USE_TOOL: list_files(".")
USE_TOOL: list_files("src")
USE_TOOL: list_files(".", ["node_modules", ".git", "*.log"])
USE_TOOL: list_files("/absolute/path")
```

---

### 6. glob_files(pattern, search_path)
Find files matching a glob pattern.

**Parameters:**
- `pattern` (string): Glob pattern (e.g., `**/*.js`, `*.md`)
- `search_path` (string, optional): Directory to search (default: current dir)

**Examples:**
```
USE_TOOL: glob_files("**/*.js", ".")
USE_TOOL: glob_files("*.md")
USE_TOOL: glob_files("**/*.{ts,tsx}", "src")
USE_TOOL: glob_files("test/**/*.spec.js")
```

**Common Patterns:**
- `**/*.js` - All JS files recursively
- `*.md` - Markdown files in current dir
- `src/**/*.{ts,tsx}` - TypeScript files in src/
- `**/test*.js` - Test files everywhere

---

### 7. grep(pattern, search_path, case_insensitive)
Search file contents for a pattern.

**Parameters:**
- `pattern` (string): Search pattern (regex supported)
- `search_path` (string, optional): Directory or file to search
- `case_insensitive` (boolean, optional): Case-insensitive search

**Examples:**
```
USE_TOOL: grep("function", ".", false)
USE_TOOL: grep("TODO", "src", true)
USE_TOOL: grep("import.*React", ".", false)
USE_TOOL: grep("console.log", "app.js", false)
```

---

## Shell Operations

### 8. run_command(command, run_in_background)
Execute shell commands.

**Parameters:**
- `command` (string): Shell command to execute
- `run_in_background` (boolean, optional): Run in background (returns shell_id)

**Examples:**
```
USE_TOOL: run_command("npm install", false)
USE_TOOL: run_command("git status", false)
USE_TOOL: run_command("npm run dev", true)
USE_TOOL: run_command("ls -la", false)
USE_TOOL: run_command("npm test", false)
```

**Background Mode:**
When `run_in_background: true`, returns a `shell_id` like "shell_0". Use `bash_output()` to check progress.

---

### 9. bash_output(bash_id)
Get output from a background shell.

**Parameters:**
- `bash_id` (string): Shell ID from `run_command(..., true)`

**Examples:**
```
USE_TOOL: bash_output("shell_0")
USE_TOOL: bash_output("shell_1")
```

**Usage Pattern:**
1. Start background process: `run_command("npm run dev", true)` → returns "shell_0"
2. Check output: `bash_output("shell_0")`
3. Kill when done: `kill_bash("shell_0")`

---

### 10. kill_bash(bash_id)
Terminate a background shell.

**Parameters:**
- `bash_id` (string): Shell ID to terminate

**Examples:**
```
USE_TOOL: kill_bash("shell_0")
USE_TOOL: kill_bash("shell_1")
```

---

## Common Usage Patterns

### Creating a Project File
```
USE_TOOL: create_file("src/index.js", "const express = require('express');\nconst app = express();\n\napp.listen(3000);")
```

### Refactoring Code
```
USE_TOOL: multi_edit("app.js", [
  {"old_string": "var", "new_string": "const", "replace_all": true},
  {"old_string": "require(", "new_string": "import ", "replace_all": true}
])
```

### Finding TODO Comments
```
USE_TOOL: grep("TODO|FIXME", ".", true)
```

### Installing Dependencies
```
USE_TOOL: run_command("npm install express cors dotenv", false)
```

### Running Dev Server in Background
```
USE_TOOL: run_command("npm run dev", true)
// Returns: "shell_0"
USE_TOOL: bash_output("shell_0")
// Check if server started
USE_TOOL: kill_bash("shell_0")
// Stop when done
```

---

## Tips

1. **Always use forward slashes** in paths: `src/file.js` not `src\file.js`
2. **Escape special characters** in strings: `\"` for quotes, `\\n` for newlines
3. **Use absolute paths** when unsure: `/Volumes/DATA/QWEN/file.js`
4. **Background shells** are great for long-running processes (dev servers, watchers)
5. **Multi-edit** is faster than multiple `edit_file` calls
6. **Glob patterns** support `*` (any chars), `**` (recursive), `{a,b}` (alternatives)

---

## Error Handling

If a tool fails, ZIMA will show:
```
Tool error: [error message]
```

Common errors:
- File not found: Check path and spelling
- Permission denied: File may be read-only
- Invalid JSON: Check array/object syntax
- Command failed: Check if command exists and is valid

---

## Tool Categories Summary

| Category | Tools | Count |
|----------|-------|-------|
| File Operations | create_file, read_file, edit_file, multi_edit | 4 |
| Directory Operations | list_files, glob_files, grep | 3 |
| Shell Operations | run_command, bash_output, kill_bash | 3 |
| **TOTAL** | | **10** |

All tools are **production-ready** and follow Claude Code's architecture.
