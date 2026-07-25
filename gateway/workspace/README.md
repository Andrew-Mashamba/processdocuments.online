# Workspace - OpenClaw System Prompt Files

This directory contains configuration files that define ZIMA's personality, operational guidelines, and environment context. These files are loaded into the system prompt at runtime.

## Core Files (Required)

### SOUL.md
**Purpose:** Defines agent identity, tone, boundaries, and vibe

**What to customize:**
- Core truths and values
- Boundaries and safety guidelines
- Communication style and personality
- Trust and competence guidelines

**When to edit:** When you want to change how ZIMA behaves or communicates

### AGENTS.md
**Purpose:** Operational manual for the agent

**What to customize:**
- Session startup procedures
- Memory system preferences
- Safety guidelines
- Tool usage preferences
- Communication patterns
- Platform-specific behaviors

**When to edit:** When you want to change how ZIMA operates or makes decisions

### TOOLS.md
**Purpose:** Environment-specific notes and infrastructure details

**What to customize:**
- Server URLs and endpoints
- Storage paths
- Device names and integration details
- API key references (NOT actual keys)
- Tool preferences
- Workflow notes

**When to edit:** When infrastructure changes or you want to document environment details

## Optional Files (Not Tracked by Git)

### USER.md
User profile information (created automatically or manually)

### IDENTITY.md
Extended agent identity information

### MEMORY.md
Long-term memory and learned preferences (curated from daily logs)

### HEARTBEAT.md
Heartbeat system configuration (for proactive monitoring)

## Memory Directory

### memory/
Contains daily logs (YYYY-MM-DD.md) of work, decisions, and context

**Daily logs are:**
- Auto-created as the agent works
- Not tracked by git (personal/session-specific)
- Used to provide context about recent work
- Curated into MEMORY.md for long-term retention

## How It Works

1. **On each request**, the system prompt builder loads these files
2. **Workspace files** are composed into the system prompt
3. **Runtime context** (tools, session info) is injected
4. **Complete prompt** is sent to Claude API
5. **Memory logs** are updated as work progresses

## Git Tracking

### Tracked (committed to repository):
- ✅ SOUL.md (default template)
- ✅ AGENTS.md (default template)
- ✅ TOOLS.md (default template)
- ✅ README.md (this file)
- ✅ .gitignore
- ✅ memory/.gitkeep (directory structure)

### Not Tracked (user-specific):
- ❌ USER.md
- ❌ IDENTITY.md
- ❌ MEMORY.md
- ❌ HEARTBEAT.md
- ❌ memory/*.md (daily logs)

## Deployment

### Development
- Files loaded from: `<project>/workspace/`
- Edit files directly in the project directory
- Changes take effect immediately (next request)

### Production
- Files loaded from: `$WORKSPACE_PATH` or `<project>/workspace/`
- Set `WORKSPACE_PATH` environment variable to override
- Template files should be committed to git
- User-specific files (MEMORY.md, etc.) should be created per environment

## Configuration

Workspace path can be configured in:

1. **Environment variable**: `WORKSPACE_PATH=/path/to/workspace`
2. **Config file**: `~/.zima/config.json` → `storage.workspace`
3. **Default**: `<project>/workspace/` (current working directory)

## Example Customization

```bash
# Edit personality
nano workspace/SOUL.md

# Edit operational guidelines
nano workspace/AGENTS.md

# Update environment notes
nano workspace/TOOLS.md

# Restart gateway to reload (changes auto-load on next request)
npm restart
```

## Architecture

This implements the **OpenClaw system prompt architecture**:
- User-editable personality files
- Memory-first approach
- Multi-mode prompting (full/minimal/none)
- Special token support (HEARTBEAT_OK, reply tags)
- Runtime context injection

For more details, see: `OPENCLAW_PROMPT_IMPLEMENTATION.md`

---

**Last Updated:** 2026-01-31
