Performance Comparison for Long Operations

  | Approach     | Startup Time         | Progress Updates | Concurrent Ops   | Memory |
  |--------------|----------------------|------------------|------------------|--------|
  | CLI Tool     | ~200ms each call     | Limited (stdout) | New process each | Higher |
  | REST Service | Once (stays running) | WebSocket/SSE    | Excellent        | Shared |
  | MCP Server   | Once (stays running) | Native streaming | Excellent        | Shared |

  Best for Long-Running Operations: MCP Server

  ZIMA already has native MCP (Model Context Protocol) support - see agent/mcp.go. This is the cleanest integration:

  ┌─────────────────┐     MCP Protocol      ┌─────────────────────────┐
  │     ZIMA (Go)   │ ◀───────────────────▶ │  FileService (C#/.NET)  │
  │                 │   - Tool calls        │                         │
  │  Native MCP     │   - Progress events   │  - Excel (ClosedXML)    │
  │  integration    │   - Streaming results │  - Word (DocX)          │
  │                 │                       │  - PDF (iTextSharp)     │
  └─────────────────┘                       └─────────────────────────┘

  Benefits:
  - ✅ Service starts once, stays running
  - ✅ Native progress streaming to ZIMA
  - ✅ No timeout issues for long operations
  - ✅ ZIMA auto-discovers tools via MCP
  - ✅ Can handle concurrent file operations

  Implementation Plan

  I can help you build this. Here's what we'd create:

  zima-file-service/           # C# .NET 8 project
  ├── Program.cs               # MCP Server entry point
  ├── Tools/
  │   ├── ExcelTool.cs        # Create/read Excel files
  │   ├── WordTool.cs         # Create/read Word documents
  │   └── PdfTool.cs          # Create PDF files
  ├── McpServer.cs            # MCP protocol handler
  └── zima-file-service.csproj

  ZIMA Configuration (.zima.json)

  {
    "mcpServers": {
      "file-service": {
        "type": "stdio",
        "command": "dotnet",
        "args": ["run", "--project", "/path/to/zima-file-service"]
      }
    }
  }

