using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZimaFileService.Api;

/// <summary>
/// Dynamic tools registry - single source of truth for all MCP tools.
/// Tools can be added at runtime and persist across sessions.
/// McpServer reads from this registry for tool definitions.
/// </summary>
public class ToolsRegistry
{
    private static readonly Lazy<ToolsRegistry> _instance = new(() => new ToolsRegistry());
    public static ToolsRegistry Instance => _instance.Value;

    private readonly string _toolsFilePath;
    private readonly string _apiDirectory;
    private readonly object _lock = new();
    private ToolsManifest _manifest;

    // Event fired when tools change (for McpServer to refresh)
    public event Action? OnToolsChanged;

    private ToolsRegistry()
    {
        _toolsFilePath = Path.Combine(FileManager.Instance.WorkingDirectory, ".zima-tools.json");
        _apiDirectory = Path.Combine(FileManager.Instance.WorkingDirectory, "Api");
        _manifest = LoadOrCreateManifest();

        // Scan for any new tool files on startup
        ScanForNewTools();
    }

    /// <summary>
    /// Get the full tools prompt to include in system messages.
    /// Guaranteed to include ALL tools - dynamically grouped by category.
    /// </summary>
    public string GetToolsPrompt()
    {
        lock (_lock)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== AVAILABLE MCP TOOLS ===");
            sb.AppendLine($"Total Tools: {_manifest.McpTools.Count}");
            sb.AppendLine();

            sb.AppendLine("## File Storage Paths");
            sb.AppendLine($"- Generated files: {FileManager.Instance.GeneratedFilesPath}");
            sb.AppendLine($"- Uploaded files: {FileManager.Instance.UploadedFilesPath}");
            sb.AppendLine();

            // Category display names
            var categoryNames = new Dictionary<string, string>
            {
                ["document"] = "Document Creation",
                ["file"] = "File Management",
                ["pdf"] = "PDF Processing",
                ["excel"] = "Excel Processing",
                ["text"] = "Text Processing",
                ["json"] = "JSON Processing",
                ["word"] = "Word Processing",
                ["powerpoint"] = "PowerPoint Processing",
                ["ocr"] = "OCR Processing",
                ["image"] = "Image Processing",
                ["conversion"] = "File Conversion",
                ["custom"] = "Custom Tools"
            };

            // Preferred category order
            var categoryOrder = new[] { "document", "file", "pdf", "excel", "word", "powerpoint", "text", "json", "ocr", "image", "conversion", "custom" };

            // Group ALL tools by category
            var toolsByCategory = _manifest.McpTools
                .GroupBy(t => t.Category ?? "other")
                .ToDictionary(g => g.Key, g => g.ToList());

            // Output in preferred order first
            foreach (var category in categoryOrder)
            {
                if (toolsByCategory.TryGetValue(category, out var tools) && tools.Count > 0)
                {
                    var displayName = categoryNames.GetValueOrDefault(category, category.ToUpper());
                    sb.AppendLine($"## {displayName} Tools ({tools.Count})");
                    foreach (var tool in tools)
                    {
                        sb.AppendLine($"- **{tool.Name}**: {tool.Description}");
                        if (!string.IsNullOrEmpty(tool.Usage))
                            sb.AppendLine($"  Usage: `{tool.Usage}`");
                    }
                    sb.AppendLine();
                    toolsByCategory.Remove(category);
                }
            }

            // Output any remaining categories not in preferred order (ensures nothing is missed)
            foreach (var (category, tools) in toolsByCategory)
            {
                if (tools.Count > 0)
                {
                    var displayName = categoryNames.GetValueOrDefault(category, $"{category.ToUpper()} Tools");
                    sb.AppendLine($"## {displayName} ({tools.Count})");
                    foreach (var tool in tools)
                    {
                        sb.AppendLine($"- **{tool.Name}**: {tool.Description}");
                        if (!string.IsNullOrEmpty(tool.Usage))
                            sb.AppendLine($"  Usage: `{tool.Usage}`");
                    }
                    sb.AppendLine();
                }
            }

            // Custom tools (AI-created at runtime)
            if (_manifest.CustomTools.Count > 0)
            {
                sb.AppendLine($"## Custom Tools - AI Created ({_manifest.CustomTools.Count})");
                foreach (var tool in _manifest.CustomTools)
                {
                    sb.AppendLine($"- **{tool.Name}**: {tool.Description}");
                    if (!string.IsNullOrEmpty(tool.Usage))
                        sb.AppendLine($"  Usage: `{tool.Usage}`");
                }
                sb.AppendLine();
            }

            // Skills
            sb.AppendLine("## Skills (Natural Language)");
            foreach (var skill in _manifest.Skills)
            {
                sb.AppendLine($"- **{skill.Name}**: {skill.Description}");
            }
            sb.AppendLine();

            sb.AppendLine("## Tool Creation Instructions");
            sb.AppendLine("If no existing tool fits the task:");
            sb.AppendLine("1. Create a new C# tool class in the Api/ directory");
            sb.AppendLine("2. Use libraries: ClosedXML (Excel), DocumentFormat.OpenXml (Word/PPT), iText7 (PDF)");
            sb.AppendLine("3. Register it: [REGISTER_TOOL: name|description|usage|filepath]");
            sb.AppendLine("4. The tool will be automatically available for future requests");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Get all MCP tools (built-in + custom) for McpServer.
    /// </summary>
    public List<McpToolDefinition> GetAllMcpTools()
    {
        lock (_lock)
        {
            var tools = new List<McpToolDefinition>();

            // Add built-in MCP tools
            tools.AddRange(_manifest.McpTools);

            // Add custom tools as MCP tools
            foreach (var custom in _manifest.CustomTools)
            {
                tools.Add(new McpToolDefinition
                {
                    Name = custom.Name,
                    Description = custom.Description,
                    Usage = custom.Usage,
                    Category = "custom",
                    InputSchema = custom.InputSchema ?? GetDefaultInputSchema(custom.Name),
                    IsCustom = true,
                    FilePath = custom.FilePath
                });
            }

            return tools;
        }
    }

    /// <summary>
    /// Get tool by name.
    /// </summary>
    public McpToolDefinition? GetTool(string name)
    {
        lock (_lock)
        {
            var tool = _manifest.McpTools.FirstOrDefault(t => t.Name == name);
            if (tool != null) return tool;

            var custom = _manifest.CustomTools.FirstOrDefault(t => t.Name == name);
            if (custom != null)
            {
                return new McpToolDefinition
                {
                    Name = custom.Name,
                    Description = custom.Description,
                    Category = "custom",
                    IsCustom = true,
                    FilePath = custom.FilePath
                };
            }

            return null;
        }
    }

    /// <summary>
    /// Register a new custom tool created by the AI.
    /// </summary>
    public void RegisterTool(string name, string description, string? usage, string filePath)
    {
        lock (_lock)
        {
            var existing = _manifest.CustomTools.FirstOrDefault(t => t.Name == name);
            if (existing != null)
            {
                existing.Description = description;
                existing.Usage = usage;
                existing.FilePath = filePath;
                existing.UpdatedAt = DateTime.UtcNow;
                Log("INFO", $"Updated tool: {name}");
            }
            else
            {
                _manifest.CustomTools.Add(new CustomTool
                {
                    Name = name,
                    Description = description,
                    Usage = usage,
                    FilePath = filePath,
                    InputSchema = GetDefaultInputSchema(name),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                Log("INFO", $"Registered new tool: {name} at {filePath}");
            }

            SaveManifest();
            OnToolsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Backward compatible register method.
    /// </summary>
    public void RegisterTool(string name, string description, string filePath)
    {
        RegisterTool(name, description, null, filePath);
    }

    /// <summary>
    /// Get list of all tool names.
    /// </summary>
    public List<string> GetAllToolNames()
    {
        lock (_lock)
        {
            var names = new List<string>();
            names.AddRange(_manifest.McpTools.Select(t => t.Name));
            names.AddRange(_manifest.Skills.Select(s => s.Name));
            names.AddRange(_manifest.CustomTools.Select(t => t.Name));
            return names;
        }
    }

    /// <summary>
    /// Parse AI output for tool registration commands.
    /// Supports: [REGISTER_TOOL: name|description|filepath] or [REGISTER_TOOL: name|description|usage|filepath]
    /// </summary>
    public void ParseAndRegisterTools(string output)
    {
        // Pattern with usage: [REGISTER_TOOL: name|description|usage|filepath]
        var regexWithUsage = new System.Text.RegularExpressions.Regex(
            @"\[REGISTER_TOOL:\s*([^|]+)\|([^|]+)\|([^|]+)\|([^\]]+)\]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in regexWithUsage.Matches(output))
        {
            var name = match.Groups[1].Value.Trim();
            var description = match.Groups[2].Value.Trim();
            var usage = match.Groups[3].Value.Trim();
            var filePath = match.Groups[4].Value.Trim();
            RegisterTool(name, description, usage, filePath);
        }

        // Pattern without usage: [REGISTER_TOOL: name|description|filepath]
        var regexSimple = new System.Text.RegularExpressions.Regex(
            @"\[REGISTER_TOOL:\s*([^|]+)\|([^|]+)\|([^\]|]+)\]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in regexSimple.Matches(output))
        {
            var name = match.Groups[1].Value.Trim();
            // Skip if already registered by the with-usage pattern
            if (_manifest.CustomTools.Any(t => t.Name == name)) continue;

            var description = match.Groups[2].Value.Trim();
            var filePath = match.Groups[3].Value.Trim();
            RegisterTool(name, description, null, filePath);
        }
    }

    /// <summary>
    /// Scan Api/ directory for new tool files and auto-register them.
    /// </summary>
    public void ScanForNewTools()
    {
        if (!Directory.Exists(_apiDirectory)) return;

        var toolFiles = Directory.GetFiles(_apiDirectory, "*Tool.cs")
            .Concat(Directory.GetFiles(_apiDirectory, "*Tool*.cs"))
            .Distinct();

        foreach (var file in toolFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            // Skip known built-in tools
            if (fileName is "ExcelTool" or "WordTool" or "PdfTool" or "FileManagementTool" or "ToolsRegistry")
                continue;

            // Check if already registered
            var toolName = ConvertToToolName(fileName);
            if (_manifest.CustomTools.Any(t => t.Name == toolName))
                continue;
            if (_manifest.McpTools.Any(t => t.Name == toolName))
                continue;

            // Try to extract description from file
            var description = ExtractToolDescription(file);
            if (string.IsNullOrEmpty(description))
                description = $"Custom tool: {fileName}";

            Log("INFO", $"Auto-discovered tool: {toolName} from {fileName}");
            RegisterTool(toolName, description, null, file);
        }
    }

    /// <summary>
    /// Scan for new files created by AI and register them.
    /// Call this after AI generates output.
    /// </summary>
    public void ScanForNewToolFiles(string[] newFiles)
    {
        foreach (var file in newFiles)
        {
            if (!file.EndsWith("Tool.cs") && !file.Contains("Tool"))
                continue;

            var fileName = Path.GetFileNameWithoutExtension(file);
            var toolName = ConvertToToolName(fileName);

            // Skip if already registered
            if (_manifest.CustomTools.Any(t => t.Name == toolName))
                continue;

            var description = ExtractToolDescription(file);
            if (string.IsNullOrEmpty(description))
                description = $"Custom tool: {fileName}";

            Log("INFO", $"Auto-registered new tool file: {toolName}");
            RegisterTool(toolName, description, null, file);
        }
    }

    private string ConvertToToolName(string fileName)
    {
        // SimplePowerPointTool -> create_powerpoint
        // ExcelChartTool -> create_excel_chart
        var name = fileName.Replace("Tool", "").Replace("Simple", "");

        // Convert PascalCase to snake_case
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                result.Append('_');
            result.Append(char.ToLower(name[i]));
        }

        var toolName = result.ToString();

        // Add create_ prefix if not already present
        if (!toolName.StartsWith("create_") && !toolName.StartsWith("read_") &&
            !toolName.StartsWith("list_") && !toolName.StartsWith("get_"))
        {
            toolName = "create_" + toolName;
        }

        return toolName;
    }

    private string? ExtractToolDescription(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);

            // Look for /// <summary> ... </summary>
            var summaryMatch = System.Text.RegularExpressions.Regex.Match(
                content,
                @"///\s*<summary>\s*(.+?)\s*</summary>",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (summaryMatch.Success)
            {
                var desc = summaryMatch.Groups[1].Value
                    .Replace("///", "")
                    .Replace("\n", " ")
                    .Replace("\r", "")
                    .Trim();
                return desc;
            }

            // Look for // Description: ... comment
            var descMatch = System.Text.RegularExpressions.Regex.Match(
                content,
                @"//\s*Description:\s*(.+)$",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            if (descMatch.Success)
                return descMatch.Groups[1].Value.Trim();

            return null;
        }
        catch
        {
            return null;
        }
    }

    private McpInputSchema GetDefaultInputSchema(string toolName)
    {
        // Default schema for custom tools
        return new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpPropertySchema>
            {
                ["request"] = new() { Type = "string", Description = "JSON request data for the tool" }
            },
            Required = new List<string> { "request" }
        };
    }

    private ToolsManifest LoadOrCreateManifest()
    {
        if (File.Exists(_toolsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_toolsFilePath);
                var manifest = JsonSerializer.Deserialize<ToolsManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (manifest != null)
                {
                    // Merge with defaults to ensure all built-in tools exist
                    MergeWithDefaults(manifest);
                    Log("INFO", $"Loaded tools manifest: {manifest.McpTools.Count} MCP tools, {manifest.CustomTools.Count} custom tools");
                    return manifest;
                }
            }
            catch (Exception ex)
            {
                Log("WARN", $"Failed to load tools manifest: {ex.Message}");
            }
        }

        var defaultManifest = CreateDefaultManifest();
        SaveManifest(defaultManifest);
        return defaultManifest;
    }

    private void MergeWithDefaults(ToolsManifest manifest)
    {
        var defaults = CreateDefaultManifest();

        // Add any missing built-in tools
        foreach (var tool in defaults.McpTools)
        {
            if (!manifest.McpTools.Any(t => t.Name == tool.Name))
            {
                manifest.McpTools.Add(tool);
                Log("INFO", $"Added missing built-in tool: {tool.Name}");
            }
        }

        // Add any missing skills
        foreach (var skill in defaults.Skills)
        {
            if (!manifest.Skills.Any(s => s.Name == skill.Name))
            {
                manifest.Skills.Add(skill);
            }
        }
    }

    private ToolsManifest CreateDefaultManifest()
    {
        return new ToolsManifest
        {
            Version = "2.0",
            UpdatedAt = DateTime.UtcNow,
            McpTools = new List<McpToolDefinition>
            {
                // === Document Creation Tools ===
                new()
                {
                    Name = "create_excel",
                    Description = "Create Excel spreadsheets (.xlsx) with data, formatting, formulas, and multiple sheets",
                    Usage = "create_excel(file_path, headers?, rows, sheet_name?, auto_fit_columns?)",
                    Category = "document",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["file_path"] = new() { Type = "string", Description = "Filename or full path" },
                            ["sheet_name"] = new() { Type = "string", Description = "Worksheet name (default: Sheet1)" },
                            ["headers"] = new() { Type = "array", Description = "Column headers", Items = new() { Type = "string" } },
                            ["rows"] = new() { Type = "array", Description = "Data rows", Items = new() { Type = "array" } },
                            ["auto_fit_columns"] = new() { Type = "boolean", Description = "Auto-fit column widths" }
                        },
                        Required = new List<string> { "file_path", "rows" }
                    }
                },
                new()
                {
                    Name = "read_excel",
                    Description = "Read data from Excel spreadsheets",
                    Usage = "read_excel(file_path, sheet_name?, has_headers?) -> {headers, rows}",
                    Category = "document",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["file_path"] = new() { Type = "string", Description = "Path to Excel file" },
                            ["sheet_name"] = new() { Type = "string", Description = "Sheet to read" },
                            ["has_headers"] = new() { Type = "boolean", Description = "First row is headers" }
                        },
                        Required = new List<string> { "file_path" }
                    }
                },
                new()
                {
                    Name = "create_word",
                    Description = "Create Word documents (.docx) with text, headings, tables, and formatting",
                    Usage = "create_word(file_path, title?, content[])",
                    Category = "document",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["file_path"] = new() { Type = "string", Description = "Filename or full path" },
                            ["title"] = new() { Type = "string", Description = "Document title" },
                            ["content"] = new() { Type = "array", Description = "Content blocks", Items = new() { Type = "object" } }
                        },
                        Required = new List<string> { "file_path", "content" }
                    }
                },
                new()
                {
                    Name = "create_pdf",
                    Description = "Create PDF documents with text, tables, and professional formatting",
                    Usage = "create_pdf(file_path, title?, content[], page_size?)",
                    Category = "document",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["file_path"] = new() { Type = "string", Description = "Filename or full path" },
                            ["title"] = new() { Type = "string", Description = "Document title" },
                            ["content"] = new() { Type = "array", Description = "Content blocks", Items = new() { Type = "object" } },
                            ["page_size"] = new() { Type = "string", Description = "A4, Letter, Legal" }
                        },
                        Required = new List<string> { "file_path", "content" }
                    }
                },
                new()
                {
                    Name = "create_powerpoint",
                    Description = "Create PowerPoint presentations (.pptx) with slides, titles, and content",
                    Usage = "create_powerpoint(filename, slides[])",
                    Category = "document",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["filename"] = new() { Type = "string", Description = "Filename or full path" },
                            ["slides"] = new() { Type = "array", Description = "Slide objects", Items = new() { Type = "object" } }
                        },
                        Required = new List<string> { "filename", "slides" }
                    }
                },

                // === File Management Tools ===
                new()
                {
                    Name = "list_files",
                    Description = "List files in generated_files or uploaded_files directory",
                    Usage = "list_files(folder?, pattern?) -> [{name, size, modified}]",
                    Category = "file",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["folder"] = new() { Type = "string", Description = "'generated' or 'uploaded'" },
                            ["pattern"] = new() { Type = "string", Description = "Filter pattern (*.xlsx)" }
                        },
                        Required = new List<string>()
                    }
                },
                new()
                {
                    Name = "get_file_info",
                    Description = "Get detailed information about a file",
                    Usage = "get_file_info(file_path) -> {name, size, created, modified}",
                    Category = "file",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["file_path"] = new() { Type = "string", Description = "Path to file" }
                        },
                        Required = new List<string> { "file_path" }
                    }
                },
                new()
                {
                    Name = "read_file_content",
                    Description = "Read content of text files or get base64 of binary files",
                    Usage = "read_file_content(file_path, as_base64?) -> string",
                    Category = "file",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["file_path"] = new() { Type = "string", Description = "Path to file" },
                            ["as_base64"] = new() { Type = "boolean", Description = "Return as base64" }
                        },
                        Required = new List<string> { "file_path" }
                    }
                },
                new()
                {
                    Name = "delete_file",
                    Description = "Delete a file from generated_files directory",
                    Usage = "delete_file(file_path) -> {success, message}",
                    Category = "file",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["file_path"] = new() { Type = "string", Description = "File to delete" }
                        },
                        Required = new List<string> { "file_path" }
                    }
                },
                new()
                {
                    Name = "copy_file",
                    Description = "Copy a file to a new location",
                    Usage = "copy_file(source_path, dest_path, overwrite?)",
                    Category = "file",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["source_path"] = new() { Type = "string", Description = "Source file" },
                            ["dest_path"] = new() { Type = "string", Description = "Destination" },
                            ["overwrite"] = new() { Type = "boolean", Description = "Overwrite if exists" }
                        },
                        Required = new List<string> { "source_path", "dest_path" }
                    }
                },
                new()
                {
                    Name = "move_file",
                    Description = "Move or rename a file",
                    Usage = "move_file(source_path, dest_path, overwrite?)",
                    Category = "file",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>
                        {
                            ["source_path"] = new() { Type = "string", Description = "Source file" },
                            ["dest_path"] = new() { Type = "string", Description = "Destination" },
                            ["overwrite"] = new() { Type = "boolean", Description = "Overwrite if exists" }
                        },
                        Required = new List<string> { "source_path", "dest_path" }
                    }
                },
                new()
                {
                    Name = "get_directory_info",
                    Description = "Get file storage directory paths and stats",
                    Usage = "get_directory_info() -> {generated_path, uploaded_path, counts}",
                    Category = "file",
                    InputSchema = new McpInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpPropertySchema>(),
                        Required = new List<string>()
                    }
                },

                // === PDF Processing Tools ===
                new() { Name = "merge_pdf", Description = "Merge multiple PDF files into one", Usage = "merge_pdf(files[], output_file)", Category = "pdf" },
                new() { Name = "split_pdf", Description = "Split PDF by pages, ranges, or every N pages", Usage = "split_pdf(file, mode, output_prefix)", Category = "pdf" },
                new() { Name = "extract_pages", Description = "Extract specific pages from PDF", Usage = "extract_pages(file, pages[], output_file)", Category = "pdf" },
                new() { Name = "remove_pages", Description = "Remove specific pages from PDF", Usage = "remove_pages(file, pages[], output_file)", Category = "pdf" },
                new() { Name = "rotate_pdf", Description = "Rotate PDF pages by 90, 180, or 270 degrees", Usage = "rotate_pdf(file, rotation, pages?)", Category = "pdf" },
                new() { Name = "add_watermark", Description = "Add text watermark to PDF pages", Usage = "add_watermark(file, text, opacity?, font_size?)", Category = "pdf" },
                new() { Name = "add_page_numbers", Description = "Add page numbers to PDF", Usage = "add_page_numbers(file, position, format?)", Category = "pdf" },
                new() { Name = "compress_pdf", Description = "Compress/optimize PDF file size", Usage = "compress_pdf(file, output_file)", Category = "pdf" },
                new() { Name = "get_pdf_info", Description = "Get PDF metadata and page info", Usage = "get_pdf_info(file)", Category = "pdf" },
                new() { Name = "protect_pdf", Description = "Protect PDF with password encryption", Usage = "protect_pdf(file, user_password?, owner_password)", Category = "pdf" },
                new() { Name = "unlock_pdf", Description = "Unlock password-protected PDF", Usage = "unlock_pdf(file, password)", Category = "pdf" },
                new() { Name = "pdf_to_text", Description = "Extract text from PDF", Usage = "pdf_to_text(file, pages?)", Category = "pdf" },
                new() { Name = "text_to_pdf", Description = "Convert text file to PDF", Usage = "text_to_pdf(file, title?, font_size?)", Category = "pdf" },
                new() { Name = "compare_pdf", Description = "Compare two PDF files", Usage = "compare_pdf(file1, file2)", Category = "pdf" },
                new() { Name = "crop_pdf", Description = "Crop PDF page margins", Usage = "crop_pdf(file, left, bottom, right, top)", Category = "pdf" },
                new() { Name = "set_pdf_metadata", Description = "Set PDF metadata (title, author, etc.)", Usage = "set_pdf_metadata(file, title?, author?)", Category = "pdf" },
                new() { Name = "image_to_pdf", Description = "Convert images to PDF document", Usage = "image_to_pdf(images[], output_file)", Category = "pdf" },
                new() { Name = "html_to_pdf", Description = "Convert HTML to PDF", Usage = "html_to_pdf(file, output_file)", Category = "pdf" },
                new() { Name = "redact_pdf", Description = "Redact/blackout areas in PDF", Usage = "redact_pdf(file, areas[])", Category = "pdf" },
                new() { Name = "repair_pdf", Description = "Attempt to repair corrupted PDF", Usage = "repair_pdf(file, output_file)", Category = "pdf" },

                // PDF Signing
                new() { Name = "sign_pdf", Description = "Sign PDF with digital certificate (PFX/P12)", Usage = "sign_pdf(file, certificate, password, reason?, location?, visible?, page?, x?, y?)", Category = "pdf" },
                new() { Name = "verify_pdf_signature", Description = "Verify PDF digital signatures", Usage = "verify_pdf_signature(file)", Category = "pdf" },
                new() { Name = "create_test_certificate", Description = "Create a self-signed test certificate for PDF signing", Usage = "create_test_certificate(name?, organization?, password?, valid_years?)", Category = "pdf" },

                // PDF Annotations
                new() { Name = "add_sticky_note", Description = "Add sticky note (text annotation) to PDF", Usage = "add_sticky_note(file, page, x, y, title?, content?, color?)", Category = "pdf" },
                new() { Name = "add_highlight", Description = "Add highlight annotation to PDF", Usage = "add_highlight(file, page, x, y, width, height, color?)", Category = "pdf" },
                new() { Name = "add_underline", Description = "Add underline annotation to PDF", Usage = "add_underline(file, page, x, y, width, height, color?)", Category = "pdf" },
                new() { Name = "add_strikethrough", Description = "Add strikethrough annotation to PDF", Usage = "add_strikethrough(file, page, x, y, width, height, color?)", Category = "pdf" },
                new() { Name = "add_free_text", Description = "Add free text box annotation to PDF", Usage = "add_free_text(file, page, x, y, width, height, content, font_size?, color?)", Category = "pdf" },
                new() { Name = "add_stamp", Description = "Add stamp annotation to PDF (Approved, Draft, Final, etc.)", Usage = "add_stamp(file, page, x, y, stamp_type)", Category = "pdf" },
                new() { Name = "add_link", Description = "Add link annotation to PDF (URL or internal page)", Usage = "add_link(file, page, x, y, width, height, url?, target_page?)", Category = "pdf" },
                new() { Name = "list_annotations", Description = "List all annotations in PDF", Usage = "list_annotations(file)", Category = "pdf" },
                new() { Name = "remove_annotations", Description = "Remove annotations from PDF", Usage = "remove_annotations(file, type?, pages?)", Category = "pdf" },
                new() { Name = "flatten_annotations", Description = "Flatten annotations into PDF content", Usage = "flatten_annotations(file, output_file?)", Category = "pdf" },

                // === Excel Processing Tools ===
                new() { Name = "merge_workbooks", Description = "Merge multiple Excel workbooks", Usage = "merge_workbooks(files[], output_file, mode?)", Category = "excel" },
                new() { Name = "split_workbook", Description = "Split Excel workbook by sheets", Usage = "split_workbook(file, output_prefix)", Category = "excel" },
                new() { Name = "excel_to_csv", Description = "Convert Excel sheet to CSV", Usage = "excel_to_csv(file, sheet_name?, delimiter?)", Category = "excel" },
                new() { Name = "excel_to_json", Description = "Convert Excel sheet to JSON", Usage = "excel_to_json(file, sheet_name?, has_headers?)", Category = "excel" },
                new() { Name = "csv_to_excel", Description = "Convert CSV to Excel", Usage = "csv_to_excel(file, delimiter?, sheet_name?)", Category = "excel" },
                new() { Name = "json_to_excel", Description = "Convert JSON array to Excel", Usage = "json_to_excel(file, sheet_name?)", Category = "excel" },
                new() { Name = "clean_excel", Description = "Remove blank rows/columns, trim whitespace", Usage = "clean_excel(file, options)", Category = "excel" },
                new() { Name = "get_excel_info", Description = "Get Excel workbook metadata", Usage = "get_excel_info(file)", Category = "excel" },
                new() { Name = "extract_sheets", Description = "Extract specific sheets from workbook", Usage = "extract_sheets(file, sheets[])", Category = "excel" },
                new() { Name = "reorder_sheets", Description = "Reorder sheets in workbook", Usage = "reorder_sheets(file, new_order[])", Category = "excel" },
                new() { Name = "rename_sheets", Description = "Rename sheets in workbook", Usage = "rename_sheets(file, rename_map)", Category = "excel" },
                new() { Name = "delete_sheets", Description = "Delete sheets from workbook", Usage = "delete_sheets(file, sheets[])", Category = "excel" },
                new() { Name = "copy_sheet", Description = "Copy sheet within workbook", Usage = "copy_sheet(file, sheet, new_name)", Category = "excel" },
                new() { Name = "find_replace_excel", Description = "Find and replace in Excel", Usage = "find_replace_excel(file, find, replace)", Category = "excel" },
                new() { Name = "excel_to_html", Description = "Convert Excel to HTML table", Usage = "excel_to_html(file, sheet?)", Category = "excel" },
                new() { Name = "add_formulas", Description = "Add formulas to Excel cells", Usage = "add_formulas(file, formulas)", Category = "excel" },
                new() { Name = "text_to_excel", Description = "Convert text file to Excel", Usage = "text_to_excel(file, delimiter?)", Category = "excel" },
                new() { Name = "add_chart", Description = "Add chart to Excel workbook", Usage = "add_chart(file, data_range, chart_type)", Category = "excel" },
                new() { Name = "create_pivot_summary", Description = "Create pivot-table style summary", Usage = "create_pivot_summary(file, row_field, value_field, aggregation?)", Category = "excel" },
                new() { Name = "validate_excel_data", Description = "Validate Excel data against rules", Usage = "validate_excel_data(file, rules[])", Category = "excel" },
                new() { Name = "compress_excel", Description = "Compress Excel workbook to reduce file size", Usage = "compress_excel(file, remove_hidden_sheets?, remove_comments?, optimize_images?, output_file?)", Category = "excel" },
                new() { Name = "repair_excel", Description = "Repair corrupted Excel workbook", Usage = "repair_excel(file, output_file?)", Category = "excel" },
                new() { Name = "protect_workbook", Description = "Protect Excel workbook with password", Usage = "protect_workbook(file, password, protect_structure?, protect_sheets?, allowed_operations[]?, output_file?)", Category = "excel" },
                new() { Name = "unprotect_workbook", Description = "Remove protection from Excel workbook", Usage = "unprotect_workbook(file, password?, unprotect_sheets?, output_file?)", Category = "excel" },
                new() { Name = "add_conditional_formatting", Description = "Add conditional formatting to Excel", Usage = "add_conditional_formatting(file, range, format_type, condition?, value1?, value2?, background_color?, output_file?)", Category = "excel" },
                new() { Name = "clear_conditional_formatting", Description = "Clear conditional formatting from Excel", Usage = "clear_conditional_formatting(file, range?, clear_all?, output_file?)", Category = "excel" },

                // === Text Processing Tools ===
                new() { Name = "merge_text", Description = "Merge multiple text files", Usage = "merge_text(files[], output_file, separator?)", Category = "text" },
                new() { Name = "split_text", Description = "Split text file by lines, pattern, or size", Usage = "split_text(file, mode, options)", Category = "text" },
                new() { Name = "find_replace", Description = "Find and replace text (supports regex)", Usage = "find_replace(file, find, replace, use_regex?)", Category = "text" },
                new() { Name = "remove_duplicates", Description = "Remove duplicate lines from text", Usage = "remove_duplicates(file, case_sensitive?)", Category = "text" },
                new() { Name = "sort_lines", Description = "Sort lines alphabetically or numerically", Usage = "sort_lines(file, order?, numeric?)", Category = "text" },
                new() { Name = "convert_case", Description = "Convert text case (upper/lower/title)", Usage = "convert_case(file, case_type)", Category = "text" },
                new() { Name = "add_line_numbers", Description = "Add line numbers to text file", Usage = "add_line_numbers(file, format?, start_at?)", Category = "text" },
                new() { Name = "compare_files", Description = "Compare two text files (diff)", Usage = "compare_files(file1, file2, output_file?)", Category = "text" },
                new() { Name = "clean_whitespace", Description = "Remove extra whitespace from text", Usage = "clean_whitespace(file, options)", Category = "text" },
                new() { Name = "reverse_text", Description = "Reverse text (lines or characters)", Usage = "reverse_text(file, mode)", Category = "text" },
                new() { Name = "convert_encoding", Description = "Convert text encoding (UTF-8, ASCII, etc.)", Usage = "convert_encoding(file, from, to)", Category = "text" },
                new() { Name = "standardize_line_endings", Description = "Standardize line endings (LF, CRLF, CR)", Usage = "standardize_line_endings(file, line_ending)", Category = "text" },
                new() { Name = "wrap_text", Description = "Wrap text at specified width", Usage = "wrap_text(file, width)", Category = "text" },
                new() { Name = "text_to_json", Description = "Convert text file to JSON", Usage = "text_to_json(file, format)", Category = "text" },
                new() { Name = "extract_columns", Description = "Extract columns from delimited text", Usage = "extract_columns(file, columns[], delimiter?)", Category = "text" },
                new() { Name = "filter_lines", Description = "Filter lines by pattern", Usage = "filter_lines(file, pattern, use_regex?, invert?)", Category = "text" },
                new() { Name = "get_text_stats", Description = "Get text file statistics", Usage = "get_text_stats(file)", Category = "text" },
                new() { Name = "text_to_html", Description = "Convert text to HTML document", Usage = "text_to_html(file, title?)", Category = "text" },
                new() { Name = "text_to_xml", Description = "Convert text to XML document", Usage = "text_to_xml(file, root_element?, line_element?)", Category = "text" },
                new() { Name = "html_to_text", Description = "Strip HTML to plain text", Usage = "html_to_text(file, preserve_links?)", Category = "text" },
                new() { Name = "xml_to_text", Description = "Convert XML to plain text", Usage = "xml_to_text(file, include_attributes?)", Category = "text" },
                new() { Name = "compress_text", Description = "Compress text file using GZIP", Usage = "compress_text(file, output_file?)", Category = "text" },
                new() { Name = "decompress_text", Description = "Decompress GZIP file", Usage = "decompress_text(file, output_file?)", Category = "text" },
                new() { Name = "encrypt_text", Description = "Encrypt text file with AES", Usage = "encrypt_text(file, password)", Category = "text" },
                new() { Name = "decrypt_text", Description = "Decrypt AES-encrypted file", Usage = "decrypt_text(file, password)", Category = "text" },
                new() { Name = "calculate_checksum", Description = "Calculate file checksum (MD5, SHA1, SHA256)", Usage = "calculate_checksum(file, algorithm?)", Category = "text" },
                new() { Name = "validate_checksum", Description = "Validate file checksum", Usage = "validate_checksum(file, expected, algorithm?)", Category = "text" },

                // === JSON Processing Tools ===
                new() { Name = "format_json", Description = "Format/beautify JSON with indentation", Usage = "format_json(file, indent?)", Category = "json" },
                new() { Name = "minify_json", Description = "Minify JSON (remove whitespace)", Usage = "minify_json(file, output_file)", Category = "json" },
                new() { Name = "validate_json", Description = "Validate JSON syntax", Usage = "validate_json(file)", Category = "json" },
                new() { Name = "merge_json", Description = "Merge multiple JSON files", Usage = "merge_json(files[], mode, output_file)", Category = "json" },
                new() { Name = "split_json", Description = "Split large JSON array into files", Usage = "split_json(file, items_per_file)", Category = "json" },
                new() { Name = "query_json", Description = "Query JSON with path expressions", Usage = "query_json(file, path)", Category = "json" },
                new() { Name = "sort_json_keys", Description = "Sort JSON object keys alphabetically", Usage = "sort_json_keys(file, recursive?)", Category = "json" },
                new() { Name = "flatten_json", Description = "Flatten nested JSON structure", Usage = "flatten_json(file, separator?)", Category = "json" },
                new() { Name = "json_to_csv", Description = "Convert JSON array to CSV", Usage = "json_to_csv(file, delimiter?)", Category = "json" },
                new() { Name = "remove_json_keys", Description = "Remove specific keys from JSON", Usage = "remove_json_keys(file, keys[], recursive?)", Category = "json" },
                new() { Name = "csv_to_json", Description = "Convert CSV to JSON array", Usage = "csv_to_json(file, delimiter?, has_header?)", Category = "json" },
                new() { Name = "xml_to_json", Description = "Convert XML to JSON", Usage = "xml_to_json(file)", Category = "json" },
                new() { Name = "json_to_xml", Description = "Convert JSON to XML", Usage = "json_to_xml(file, root_element?)", Category = "json" },
                new() { Name = "validate_schema", Description = "Validate JSON against schema", Usage = "validate_schema(file, schema_file)", Category = "json" },
                new() { Name = "get_json_stats", Description = "Get JSON file statistics", Usage = "get_json_stats(file)", Category = "json" },
                new() { Name = "remove_duplicates_json", Description = "Remove duplicate objects from JSON array", Usage = "remove_duplicates_json(file, key_field?)", Category = "json" },
                new() { Name = "transform_json", Description = "Transform JSON using mappings", Usage = "transform_json(file, mappings)", Category = "json" },
                new() { Name = "yaml_to_json", Description = "Convert YAML to JSON", Usage = "yaml_to_json(file, output_file?)", Category = "json" },
                new() { Name = "json_to_yaml", Description = "Convert JSON to YAML", Usage = "json_to_yaml(file, output_file?)", Category = "json" },
                new() { Name = "json_to_html_table", Description = "Convert JSON to HTML table", Usage = "json_to_html_table(file, title?, include_styles?)", Category = "json" },
                new() { Name = "encrypt_json", Description = "Encrypt JSON file with AES", Usage = "encrypt_json(file, password)", Category = "json" },
                new() { Name = "decrypt_json", Description = "Decrypt AES-encrypted JSON", Usage = "decrypt_json(file, password)", Category = "json" },
                new() { Name = "array_operations", Description = "JSON array operations (concat, slice, reverse, shuffle, unique)", Usage = "array_operations(file, operation, options?)", Category = "json" },
                new() { Name = "repair_json", Description = "Repair invalid/malformed JSON", Usage = "repair_json(file, output_file?)", Category = "json" },
                new() { Name = "sql_to_json", Description = "Convert SQL query/schema to JSON", Usage = "sql_to_json(file?, sql?, mode?, row_count?, output_file?)", Category = "json" },
                new() { Name = "json_to_pdf", Description = "Convert JSON data to PDF document", Usage = "json_to_pdf(file, title?, style?, page_size?, landscape?, output_file?)", Category = "json" },
                new() { Name = "sign_json", Description = "Sign JSON with digital signature (HMAC/RSA)", Usage = "sign_json(file, algorithm?, secret?, private_key?, output_file?)", Category = "json" },
                new() { Name = "verify_json_signature", Description = "Verify JSON digital signature", Usage = "verify_json_signature(file, secret?, public_key?)", Category = "json" },
                new() { Name = "generate_json_signing_keys", Description = "Generate RSA key pair for JSON signing", Usage = "generate_json_signing_keys(key_size?, private_key_file?, public_key_file?)", Category = "json" },

                // === Word Processing Tools ===
                new() { Name = "merge_word", Description = "Merge multiple Word documents", Usage = "merge_word(files[], output_file)", Category = "word" },
                new() { Name = "split_word", Description = "Split Word document by sections or headings", Usage = "split_word(file, mode)", Category = "word" },
                new() { Name = "extract_word_sections", Description = "Extract specific sections from Word document", Usage = "extract_word_sections(file, sections[])", Category = "word" },
                new() { Name = "remove_word_sections", Description = "Remove sections from Word document", Usage = "remove_word_sections(file, sections[])", Category = "word" },
                new() { Name = "word_to_text", Description = "Convert Word document to plain text", Usage = "word_to_text(file)", Category = "word" },
                new() { Name = "word_to_html", Description = "Convert Word document to HTML", Usage = "word_to_html(file)", Category = "word" },
                new() { Name = "word_to_json", Description = "Convert Word document to JSON structure", Usage = "word_to_json(file)", Category = "word" },
                new() { Name = "text_to_word", Description = "Convert text file to Word document", Usage = "text_to_word(file, detect_headings?)", Category = "word" },
                new() { Name = "find_replace_word", Description = "Find and replace in Word document", Usage = "find_replace_word(file, find, replace, use_regex?)", Category = "word" },
                new() { Name = "add_header_footer", Description = "Add header/footer to Word document", Usage = "add_header_footer(file, header?, footer?)", Category = "word" },
                new() { Name = "get_word_info", Description = "Get Word document info", Usage = "get_word_info(file)", Category = "word" },
                new() { Name = "compare_word", Description = "Compare two Word documents", Usage = "compare_word(file1, file2)", Category = "word" },
                new() { Name = "clean_word_formatting", Description = "Clean formatting from Word document", Usage = "clean_word_formatting(file, keep_structure?)", Category = "word" },
                new() { Name = "word_to_pdf", Description = "Convert Word document to PDF", Usage = "word_to_pdf(file, output_file?)", Category = "word" },
                new() { Name = "mail_merge", Description = "Mail merge with data placeholders", Usage = "mail_merge(file, data[], output_prefix?, single_output?)", Category = "word" },
                new() { Name = "accept_track_changes", Description = "Accept or reject track changes", Usage = "accept_track_changes(file, action)", Category = "word" },
                new() { Name = "add_watermark_word", Description = "Add watermark to Word document", Usage = "add_watermark_word(file, text)", Category = "word" },
                new() { Name = "manage_word_tables", Description = "Extract, remove, or count tables", Usage = "manage_word_tables(file, action, table_index?)", Category = "word" },
                new() { Name = "manage_word_comments", Description = "Extract or remove comments", Usage = "manage_word_comments(file, action)", Category = "word" },
                new() { Name = "add_page_numbers_word", Description = "Add page numbers to Word document", Usage = "add_page_numbers_word(file, position?, alignment?)", Category = "word" },

                // Word Compression, Repair, Convert, Protect, Sign
                new() { Name = "compress_word", Description = "Compress Word document to reduce file size", Usage = "compress_word(file, remove_comments?, remove_revisions?, compress_images?)", Category = "word" },
                new() { Name = "repair_word", Description = "Repair corrupted Word document", Usage = "repair_word(file, output_file?)", Category = "word" },
                new() { Name = "html_to_word", Description = "Convert HTML file or content to Word document", Usage = "html_to_word(file?, html?, output_file?, preserve_styles?)", Category = "word" },
                new() { Name = "protect_word", Description = "Protect Word document with password", Usage = "protect_word(file, password, protection_type?)", Category = "word" },
                new() { Name = "unprotect_word", Description = "Remove protection from Word document", Usage = "unprotect_word(file, password?)", Category = "word" },
                new() { Name = "sign_word", Description = "Sign Word document with digital certificate", Usage = "sign_word(file, certificate, password, signer_name?, signer_title?)", Category = "word" },
                new() { Name = "verify_word_signature", Description = "Verify Word document signature", Usage = "verify_word_signature(file)", Category = "word" },

                // === PowerPoint Processing Tools ===
                new() { Name = "merge_ppt", Description = "Merge multiple PowerPoint presentations", Usage = "merge_ppt(files[], output_file)", Category = "powerpoint" },
                new() { Name = "split_ppt", Description = "Split presentation by slides", Usage = "split_ppt(file, slides_per_file?)", Category = "powerpoint" },
                new() { Name = "extract_slides", Description = "Extract specific slides from presentation", Usage = "extract_slides(file, slides[])", Category = "powerpoint" },
                new() { Name = "remove_slides", Description = "Remove slides from presentation", Usage = "remove_slides(file, slides[])", Category = "powerpoint" },
                new() { Name = "reorder_slides", Description = "Reorder slides in presentation", Usage = "reorder_slides(file, new_order[])", Category = "powerpoint" },
                new() { Name = "ppt_to_text", Description = "Extract text from presentation", Usage = "ppt_to_text(file, include_notes?)", Category = "powerpoint" },
                new() { Name = "ppt_to_json", Description = "Convert presentation to JSON structure", Usage = "ppt_to_json(file)", Category = "powerpoint" },
                new() { Name = "get_ppt_info", Description = "Get presentation info", Usage = "get_ppt_info(file)", Category = "powerpoint" },
                new() { Name = "duplicate_slides", Description = "Duplicate slides in presentation", Usage = "duplicate_slides(file, slides[], copies?)", Category = "powerpoint" },
                new() { Name = "ppt_to_pdf", Description = "Convert PowerPoint to PDF", Usage = "ppt_to_pdf(file, output_file?)", Category = "powerpoint" },
                new() { Name = "add_slide", Description = "Add new slide to presentation", Usage = "add_slide(file, position?, title?, content?)", Category = "powerpoint" },
                new() { Name = "add_watermark_ppt", Description = "Add watermark to all slides", Usage = "add_watermark_ppt(file, text)", Category = "powerpoint" },
                new() { Name = "extract_ppt_notes", Description = "Extract speaker notes from slides", Usage = "extract_ppt_notes(file, output_file?)", Category = "powerpoint" },
                new() { Name = "set_transitions", Description = "Set slide transitions", Usage = "set_transitions(file, type, duration_ms?, slides?)", Category = "powerpoint" },
                new() { Name = "find_replace_ppt", Description = "Find and replace text in presentation", Usage = "find_replace_ppt(file, find, replace)", Category = "powerpoint" },
                new() { Name = "extract_ppt_images", Description = "Extract images from presentation", Usage = "extract_ppt_images(file, output_folder?)", Category = "powerpoint" },
                new() { Name = "compress_ppt", Description = "Compress PowerPoint presentation to reduce file size", Usage = "compress_ppt(file, remove_notes?, remove_comments?, compress_images?, output_file?)", Category = "powerpoint" },
                new() { Name = "repair_ppt", Description = "Repair corrupted PowerPoint presentation", Usage = "repair_ppt(file, output_file?)", Category = "powerpoint" },
                new() { Name = "ppt_to_images", Description = "Convert PowerPoint slides to images", Usage = "ppt_to_images(file, format?, width?, height?, slides?, output_folder?)", Category = "powerpoint" },
                new() { Name = "ppt_to_video", Description = "Convert PowerPoint to video (creates image sequence with metadata)", Usage = "ppt_to_video(file, seconds_per_slide?, width?, height?, fps?, output_file?)", Category = "powerpoint" },
                new() { Name = "add_animations", Description = "Add animations to presentation elements", Usage = "add_animations(file, animation_type, trigger?, duration_ms?, slides?, target?, output_file?)", Category = "powerpoint" },
                new() { Name = "protect_ppt", Description = "Protect PowerPoint presentation with password", Usage = "protect_ppt(file, password, protect_structure?, read_only?, output_file?)", Category = "powerpoint" },
                new() { Name = "unprotect_ppt", Description = "Remove protection from PowerPoint presentation", Usage = "unprotect_ppt(file, output_file?)", Category = "powerpoint" },

                // === OCR Processing Tools ===
                new() { Name = "ocr_pdf", Description = "Extract text from scanned/image-based PDF using OCR", Usage = "ocr_pdf(file, language?, output_format?, pages?)", Category = "ocr" },
                new() { Name = "ocr_image", Description = "Extract text from image file (JPG, PNG, TIFF)", Usage = "ocr_image(file, language?)", Category = "ocr" },
                new() { Name = "get_ocr_languages", Description = "Get available OCR languages", Usage = "get_ocr_languages()", Category = "ocr" },
                new() { Name = "batch_ocr", Description = "OCR multiple files at once", Usage = "batch_ocr(files[], language?, output_folder?)", Category = "ocr" },

                // === Image Processing Tools ===
                new() { Name = "add_text_watermark", Description = "Add text watermark to image", Usage = "add_text_watermark(file, text, position?, font_size?, color?, opacity?, rotation?, output_file?)", Category = "image" },
                new() { Name = "add_image_watermark", Description = "Add image watermark to another image", Usage = "add_image_watermark(file, watermark_image, position?, opacity?, scale?, output_file?)", Category = "image" },
                new() { Name = "redact_strings", Description = "Redact text patterns from image using OCR", Usage = "redact_strings(file, patterns[], use_regex?, redact_color?, language?, output_file?)", Category = "image" },
                new() { Name = "redact_regions", Description = "Redact specific regions from image", Usage = "redact_regions(file, regions[], redact_color?, blur?, blur_radius?, output_file?)", Category = "image" },
                new() { Name = "redact_sensitive_info", Description = "Auto-detect and redact sensitive info (email, phone, SSN)", Usage = "redact_sensitive_info(file, types[]?, redact_color?, language?, output_file?)", Category = "image" },
                new() { Name = "resize_image", Description = "Resize an image", Usage = "resize_image(file, width?, height?, maintain_aspect?, output_file?)", Category = "image" },
                new() { Name = "convert_image_format", Description = "Convert image format (PNG, JPG, WebP, etc.)", Usage = "convert_image_format(file, format, quality?, output_file?)", Category = "image" },
                new() { Name = "crop_image", Description = "Crop an image", Usage = "crop_image(file, x, y, width, height, output_file?)", Category = "image" },
                new() { Name = "rotate_image", Description = "Rotate an image", Usage = "rotate_image(file, degrees, output_file?)", Category = "image" },

                // === File Conversion Tools ===
                new() { Name = "pdf_to_word", Description = "Convert PDF to Word document (.docx)", Usage = "pdf_to_word(file, output_file?, preserve_layout?)", Category = "conversion" },
                new() { Name = "pdf_to_excel", Description = "Convert PDF to Excel spreadsheet (.xlsx) with table detection", Usage = "pdf_to_excel(file, output_file?, detect_tables?)", Category = "conversion" },
                new() { Name = "pdf_to_jpg", Description = "Convert PDF pages to JPG images", Usage = "pdf_to_jpg(file, output_folder?, dpi?, quality?, pages?)", Category = "conversion" },
                new() { Name = "pdf_to_png", Description = "Convert PDF pages to PNG images (lossless)", Usage = "pdf_to_png(file, output_folder?, dpi?, pages?)", Category = "conversion" },
                new() { Name = "pdf_to_pdfa", Description = "Convert PDF to PDF/A archival format", Usage = "pdf_to_pdfa(file, output_file?, conformance?)", Category = "conversion" },
                new() { Name = "excel_to_pdf", Description = "Convert Excel spreadsheet to PDF", Usage = "excel_to_pdf(file, output_file?, sheets?, landscape?, fit_to_page?)", Category = "conversion" },
                new() { Name = "word_to_pdf", Description = "Convert Word document to PDF", Usage = "word_to_pdf(file, output_file?)", Category = "conversion" }
            },
            Skills = new List<SkillInfo>
            {
                new() { Name = "generate-excel", Description = "Generate Excel spreadsheet from natural language" },
                new() { Name = "generate-word", Description = "Generate Word document from natural language" },
                new() { Name = "generate-pdf", Description = "Generate PDF document from natural language" },
                new() { Name = "analyze-file", Description = "Analyze uploaded file and extract insights" }
            },
            CustomTools = new List<CustomTool>()
        };
    }

    private void SaveManifest(ToolsManifest? manifest = null)
    {
        manifest ??= _manifest;
        manifest.UpdatedAt = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        File.WriteAllText(_toolsFilePath, json);
    }

    /// <summary>
    /// Force reload of manifest from disk.
    /// </summary>
    public void Reload()
    {
        lock (_lock)
        {
            _manifest = LoadOrCreateManifest();
            OnToolsChanged?.Invoke();
        }
    }

    private static void Log(string level, string message)
    {
        Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [TOOLS] [{level}] {message}");
    }
}

// === Data Models ===

public class ToolsManifest
{
    public string Version { get; set; } = "2.0";
    public DateTime UpdatedAt { get; set; }
    public List<McpToolDefinition> McpTools { get; set; } = new();
    public List<SkillInfo> Skills { get; set; } = new();
    public List<CustomTool> CustomTools { get; set; } = new();
}

public class McpToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Usage { get; set; }
    public string Category { get; set; } = "custom";
    public McpInputSchema? InputSchema { get; set; }
    public bool IsCustom { get; set; } = false;
    public string? FilePath { get; set; }
}

public class McpInputSchema
{
    public string Type { get; set; } = "object";
    public Dictionary<string, McpPropertySchema> Properties { get; set; } = new();
    public List<string> Required { get; set; } = new();
}

public class McpPropertySchema
{
    public string Type { get; set; } = "string";
    public string? Description { get; set; }
    public McpPropertySchema? Items { get; set; }
}

public class SkillInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class CustomTool
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Usage { get; set; }
    public string FilePath { get; set; } = "";
    public McpInputSchema? InputSchema { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
