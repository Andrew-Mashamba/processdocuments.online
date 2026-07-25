# ZIMA Agent Permissions

## Allowed Operations

### File System
- READ: `./uploaded_files/**/*`
- READ: `./generated_files/**/*`
- READ: `./tools/**/*`
- READ: `./memory/**/*`
- WRITE: `./generated_files/{{SESSION_ID}}/**/*`
- WRITE: `./memory/sessions/{{SESSION_ID}}/**/*`
- WRITE: `./tools/**/*` (for creating new tools)

### Tool Execution
- EXECUTE: Any tool in `./tools/`
- CREATE: New tools with manifest
- REGISTER: Tools in registry

### External
- DENIED: Network access
- DENIED: System commands outside project
- DENIED: Reading files outside allowed paths

## Session Isolation

Each session:
- Has isolated output folder: `./generated_files/{{SESSION_ID}}/`
- Has isolated memory: `./memory/sessions/{{SESSION_ID}}/`
- Can read shared tools: `./tools/`
- Cannot access other sessions' data
