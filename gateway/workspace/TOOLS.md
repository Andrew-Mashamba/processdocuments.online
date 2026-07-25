# TOOLS.md — Environment-Specific Notes

This file contains environment-specific information to help you use tools effectively.

## Infrastructure

### Servers
- ZIMA Core: http://localhost:5000
- Gateway: http://localhost:18790
- Laravel Frontend: http://localhost:8000

### Storage Paths
Storage paths are configured via environment or defaults:
- Workspace: `./workspace` (project directory, tracked in git)
- Generated Files: `$STORAGE_ROOT/generated_files` or `./generated_files`
- Uploaded Files: `$STORAGE_ROOT/uploaded_files` or `./uploaded_files`
- Session Transcripts: `$STORAGE_ROOT/sessions` or `~/.zima/agents/main/sessions`

Production: Set `STORAGE_ROOT` environment variable to configure all paths

## ZIMA Backend Structure

### Project Layout
```
zima-file-service/
├── Tools/                        # Tool implementations (.NET)
│   ├── ExcelProcessingTool.cs
│   ├── PdfProcessingTool.cs
│   ├── WordProcessingTool.cs
│   ├── PowerPointProcessingTool.cs
│   ├── JsonProcessingTool.cs
│   ├── ImageProcessingTool.cs
│   └── ConversionTools.cs
├── Api/
│   └── ToolsRegistry.cs          # Tool definitions (219+ tools)
├── McpServer.cs                  # Tool routing logic
├── FileManager.cs                # File path management
└── generated_files/              # Output directory
```

### API Endpoints (ZIMA Core)
- `POST /api/generate` - Generate files from prompts
- `POST /api/generate/stream` - SSE streaming generation
- `POST /api/mcp/call` - Direct MCP tool calls
- `GET /api/files/*` - File management
- `POST /api/files/*` - Upload files
- `DELETE /api/files/*` - Delete files

### Skills Available
- `/generate-excel` - Excel generation from natural language
- `/generate-word` - Word document generation
- `/generate-pdf` - PDF creation
- `/analyze-file` - File analysis and insights
- `/document-agent` - Full autonomous document agent

## Devices & Integration

### Cameras
(Add camera names and RTSP URLs here when configured)

### SSH Hosts
(Add frequently accessed hosts here)

### API Keys
(Reference only - never store actual keys here)
- ANTHROPIC_API_KEY: Set in environment
- OPENAI_API_KEY: For embeddings (Week 7-8)

## Tool Preferences

### TTS (Text-to-Speech)
- Preferred voice: (configure when TTS is added)

### Browser
- Default profile: OpenClaw managed Chrome

### Document Formats
- Default Excel format: .xlsx
- Default PDF compression: medium
- Default image format: PNG

## Workflow Notes

(Add your workflow preferences and shortcuts here)

---

*Keep this updated with environment details that help you work efficiently.*
