# ZIMA File Service

A C#/.NET 8 MCP (Model Context Protocol) server that provides Excel, Word, and PDF file creation capabilities for ZIMA.

## Features

- **Excel (.xlsx)**: Create and read spreadsheets with ClosedXML
- **Word (.docx)**: Create documents with DocumentFormat.OpenXml
- **PDF**: Create documents with iText7

## Prerequisites

- .NET 8.0 SDK
- ZIMA configured with MCP support

## Installation

1. Build the project:
```bash
cd /Volumes/DATA/QWEN/zima-file-service
dotnet build
```

2. Configure ZIMA (`.zima.json`):
```json
{
  "mcpServers": {
    "file-service": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/Volumes/DATA/QWEN/zima-file-service"]
    }
  }
}
```

## Available Tools

### create_excel

Create an Excel spreadsheet with data.

**Parameters:**
- `file_path` (required): Output file path
- `sheet_name`: Worksheet name (default: "Sheet1")
- `headers`: Array of column headers
- `rows`: 2D array of data
- `auto_fit_columns`: Auto-fit column widths (default: true)

**Example:**
```json
{
  "file_path": "/tmp/countries.xlsx",
  "headers": ["Country", "Capital", "Population"],
  "rows": [
    ["USA", "Washington DC", 331000000],
    ["UK", "London", 67000000],
    ["France", "Paris", 67000000]
  ]
}
```

### read_excel

Read data from an Excel file.

**Parameters:**
- `file_path` (required): Input file path
- `sheet_name`: Worksheet to read (default: first sheet)
- `has_headers`: First row contains headers (default: true)

### create_word

Create a Word document.

**Parameters:**
- `file_path` (required): Output file path
- `title`: Document title
- `content`: Array of content blocks

**Content block types:**
- `heading` / `h1` / `h2` / `h3`: Heading with `text` and optional `level`
- `paragraph` / `text` / `p`: Paragraph with `text`, optional `bold`, `italic`
- `bullet` / `list`: Bullet list with `items` array
- `table`: Table with `headers` and `rows`
- `pagebreak`: Insert page break

**Example:**
```json
{
  "file_path": "/tmp/report.docx",
  "title": "Annual Report",
  "content": [
    {"type": "heading", "text": "Introduction", "level": 1},
    {"type": "paragraph", "text": "This is the introduction."},
    {"type": "list", "items": ["Item 1", "Item 2", "Item 3"]},
    {"type": "table", "headers": ["Name", "Value"], "rows": [["A", "1"], ["B", "2"]]}
  ]
}
```

### create_pdf

Create a PDF document.

**Parameters:**
- `file_path` (required): Output file path
- `title`: Document title
- `page_size`: A4, Letter, Legal, A3, A5 (default: A4)
- `content`: Array of content blocks (same format as Word)

## Testing

Test the MCP server manually:

```bash
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | dotnet run
```

## Architecture

```
┌─────────────────┐      MCP Protocol      ┌─────────────────────────┐
│     ZIMA (Go)   │ ◀───────────────────▶ │  zima-file-service (C#) │
│                 │   - Tool calls         │                         │
│  Native MCP     │   - Progress events    │  - ClosedXML (Excel)    │
│  integration    │   - Streaming results  │  - OpenXml (Word)       │
│                 │                        │  - iText7 (PDF)         │
└─────────────────┘                        └─────────────────────────┘
```

## License

MIT
