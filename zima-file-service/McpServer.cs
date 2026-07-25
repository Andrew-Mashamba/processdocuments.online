using System.Text.Json;
using ZimaFileService.Models;
using ZimaFileService.Tools;
using ZimaFileService.Api;

namespace ZimaFileService;

/// <summary>
/// MCP Server that reads tool definitions from ToolsRegistry.
/// This ensures McpServer and the HTTP API always have the same tools available.
/// </summary>
public class McpServer
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly FileManagementTool _fileManagementTool;

    public McpServer()
    {
        _fileManagementTool = new FileManagementTool();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Subscribe to tool changes for potential future use
        ToolsRegistry.Instance.OnToolsChanged += () =>
        {
            Console.Error.WriteLine("[MCP] Tools registry updated");
        };
    }

    public async Task RunAsync()
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin);
        using var writer = new StreamWriter(stdout) { AutoFlush = true };

        var fm = FileManager.Instance;
        Console.Error.WriteLine("zima-file-service MCP server starting...");
        Console.Error.WriteLine($"Working directory: {fm.WorkingDirectory}");
        Console.Error.WriteLine($"Generated files: {fm.GeneratedFilesPath}");
        Console.Error.WriteLine($"Uploaded files: {fm.UploadedFilesPath}");

        // Log available tools from registry
        var tools = ToolsRegistry.Instance.GetAllMcpTools();
        Console.Error.WriteLine($"Available tools: {tools.Count} ({tools.Count(t => !t.IsCustom)} built-in, {tools.Count(t => t.IsCustom)} custom)");

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;

            try
            {
                var response = await HandleRequestAsync(line);
                if (!string.IsNullOrEmpty(response))
                {
                    await writer.WriteLineAsync(response);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing request: {ex.Message}");
                var errorResponse = CreateErrorResponse(null, -32603, ex.Message);
                await writer.WriteLineAsync(errorResponse);
            }
        }
    }

    /// <summary>
    /// Invoke a tool directly from the API controller (bypasses MCP protocol)
    /// </summary>
    public async Task<string> InvokeToolDirectAsync(string toolName, Dictionary<string, object> arguments)
    {
        Console.Error.WriteLine($"[InvokeToolDirect] Executing tool: {toolName}");

        try
        {
            // Check if this is a custom tool
            var toolDef = ToolsRegistry.Instance.GetTool(toolName);
            if (toolDef?.IsCustom == true && !string.IsNullOrEmpty(toolDef.FilePath))
            {
                return await ExecuteCustomToolAsync(toolName, toolDef.FilePath, arguments);
            }

            // Resolve file paths for document creation tools
            if (toolName is "create_excel" or "create_word" or "create_pdf")
            {
                ResolveFilePath(arguments, "file_path");
            }
            else if (toolName == "create_powerpoint")
            {
                ResolveFilePath(arguments, "filename");
            }
            else if (toolName == "read_excel")
            {
                ResolveFilePathForReading(arguments, "file_path");
            }

            var pdfTool = new PdfProcessingTool();
            var excelProcTool = new ExcelProcessingTool();
            var textTool = new TextProcessingTool();
            var jsonTool = new JsonProcessingTool();
            var wordTool = new WordProcessingTool();
            var pptTool = new PowerPointProcessingTool();
            var ocrTool = new OcrProcessingTool();
            var conversionTool = new ConversionTools();
            var imageTool = new ImageProcessingTool();

            string result = toolName switch
            {
                // Document creation
                "create_excel" => await new ExcelTool().CreateExcelAsync(arguments),
                "read_excel" => await new ExcelTool().ReadExcelAsync(arguments),
                "create_word" => await new WordTool().CreateWordAsync(arguments),
                "create_pdf" => await new PdfTool().CreatePdfAsync(arguments),
                "create_powerpoint" => await CreatePowerPointAsync(arguments),

                // File management
                "list_files" => await _fileManagementTool.ListFilesAsync(arguments),
                "get_file_info" => await _fileManagementTool.GetFileInfoAsync(arguments),
                "delete_file" => await _fileManagementTool.DeleteFileAsync(arguments),
                "copy_file" => await _fileManagementTool.CopyFileAsync(arguments),
                "move_file" => await _fileManagementTool.MoveFileAsync(arguments),
                "read_file_content" => await _fileManagementTool.ReadFileContentAsync(arguments),
                "get_directory_info" => await _fileManagementTool.GetDirectoryInfoAsync(arguments),

                // PDF Processing
                "merge_pdf" => await pdfTool.MergePdfAsync(arguments),
                "split_pdf" => await pdfTool.SplitPdfAsync(arguments),
                "extract_pages" => await pdfTool.ExtractPagesAsync(arguments),
                "ocr_pdf" => await ocrTool.OcrPdfAsync(arguments),

                // Excel Processing
                "excel_to_csv" => await excelProcTool.ExcelToCsvAsync(arguments),

                // Image Processing
                "ocr_image" => await ocrTool.OcrImageAsync(arguments),
                "resize_image" => await imageTool.ResizeImageAsync(arguments),

                _ => throw new NotSupportedException($"Tool '{toolName}' is not supported for direct invocation")
            };

            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InvokeToolDirect] Error: {ex.Message}");
            return System.Text.Json.JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    private async Task<string> HandleRequestAsync(string requestJson)
    {
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(requestJson, _jsonOptions);
        if (request == null)
        {
            return CreateErrorResponse(null, -32700, "Parse error");
        }

        Console.Error.WriteLine($"Received method: {request.Method}");

        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "notifications/initialized" => "",
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolCallAsync(request),
            "ping" => HandlePing(request),
            _ => CreateErrorResponse(request.Id, -32601, $"Method not found: {request.Method}")
        };
    }

    private string HandleInitialize(JsonRpcRequest request)
    {
        var result = new McpInitializeResult
        {
            ProtocolVersion = "2024-11-05",
            ServerInfo = new McpServerInfo
            {
                Name = "zima-file-service",
                Version = "2.0.0"
            },
            Capabilities = new McpCapabilities
            {
                Tools = new McpToolsCapability { ListChanged = true } // Now supports dynamic tools
            }
        };

        return CreateSuccessResponse(request.Id, result);
    }

    private string HandlePing(JsonRpcRequest request)
    {
        return CreateSuccessResponse(request.Id, new { });
    }

    /// <summary>
    /// Returns tool list from ToolsRegistry - single source of truth.
    /// </summary>
    private string HandleToolsList(JsonRpcRequest request)
    {
        var registryTools = ToolsRegistry.Instance.GetAllMcpTools();
        var mcpTools = new List<McpTool>();

        foreach (var tool in registryTools)
        {
            mcpTools.Add(new McpTool
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = ConvertToMcpSchema(tool.InputSchema)
            });
        }

        var result = new McpToolsListResult { Tools = mcpTools };
        return CreateSuccessResponse(request.Id, result);
    }

    private Models.McpInputSchema ConvertToMcpSchema(Api.McpInputSchema? schema)
    {
        if (schema == null)
        {
            return new Models.McpInputSchema
            {
                Type = "object",
                Properties = new Dictionary<string, Models.McpPropertySchema>
                {
                    ["request"] = new() { Type = "string", Description = "JSON request data" }
                },
                Required = new List<string> { "request" }
            };
        }

        var props = new Dictionary<string, Models.McpPropertySchema>();
        foreach (var (key, value) in schema.Properties)
        {
            props[key] = new Models.McpPropertySchema
            {
                Type = value.Type,
                Description = value.Description,
                Items = value.Items != null ? new Models.McpPropertySchema { Type = value.Items.Type } : null
            };
        }

        return new Models.McpInputSchema
        {
            Type = schema.Type,
            Properties = props,
            Required = schema.Required
        };
    }

    private async Task<string> HandleToolCallAsync(JsonRpcRequest request)
    {
        var toolName = request.Params?.Name;
        var arguments = request.Params?.Arguments ?? new Dictionary<string, object>();

        if (string.IsNullOrEmpty(toolName))
        {
            return CreateToolErrorResponse(request.Id, "Tool name is required");
        }

        Console.Error.WriteLine($"Executing tool: {toolName}");

        try
        {
            // Check if this is a custom tool
            var toolDef = ToolsRegistry.Instance.GetTool(toolName);
            if (toolDef?.IsCustom == true && !string.IsNullOrEmpty(toolDef.FilePath))
            {
                // Execute custom tool
                var customResult = await ExecuteCustomToolAsync(toolName, toolDef.FilePath, arguments);
                return CreateToolSuccessResponse(request.Id, customResult);
            }

            // Resolve file paths for document creation tools
            if (toolName is "create_excel" or "create_word" or "create_pdf")
            {
                ResolveFilePath(arguments, "file_path");
            }
            else if (toolName == "create_powerpoint")
            {
                ResolveFilePath(arguments, "filename");
            }
            else if (toolName == "read_excel")
            {
                ResolveFilePathForReading(arguments, "file_path");
            }

            var pdfTool = new PdfProcessingTool();
            var excelProcTool = new ExcelProcessingTool();
            var textTool = new TextProcessingTool();
            var jsonTool = new JsonProcessingTool();
            var wordTool = new WordProcessingTool();
            var pptTool = new PowerPointProcessingTool();
            var ocrTool = new OcrProcessingTool();
            var conversionTool = new ConversionTools();
            var imageTool = new ImageProcessingTool();

            string result = toolName switch
            {
                // Document creation
                "create_excel" => await new ExcelTool().CreateExcelAsync(arguments),
                "read_excel" => await new ExcelTool().ReadExcelAsync(arguments),
                "create_word" => await new WordTool().CreateWordAsync(arguments),
                "create_pdf" => await new PdfTool().CreatePdfAsync(arguments),
                "create_powerpoint" => await CreatePowerPointAsync(arguments),

                // File management
                "list_files" => await _fileManagementTool.ListFilesAsync(arguments),
                "get_file_info" => await _fileManagementTool.GetFileInfoAsync(arguments),
                "delete_file" => await _fileManagementTool.DeleteFileAsync(arguments),
                "copy_file" => await _fileManagementTool.CopyFileAsync(arguments),
                "move_file" => await _fileManagementTool.MoveFileAsync(arguments),
                "read_file_content" => await _fileManagementTool.ReadFileContentAsync(arguments),
                "get_directory_info" => await _fileManagementTool.GetDirectoryInfoAsync(arguments),

                // PDF Processing
                "merge_pdf" => await pdfTool.MergePdfAsync(arguments),
                "split_pdf" => await pdfTool.SplitPdfAsync(arguments),
                "extract_pages" => await pdfTool.ExtractPagesAsync(arguments),
                "remove_pages" => await pdfTool.RemovePagesAsync(arguments),
                "rotate_pdf" => await pdfTool.RotatePdfAsync(arguments),
                "add_watermark" => await pdfTool.AddWatermarkAsync(arguments),
                "add_page_numbers" => await pdfTool.AddPageNumbersAsync(arguments),
                "compress_pdf" => await pdfTool.CompressPdfAsync(arguments),
                "get_pdf_info" => await pdfTool.GetPdfInfoAsync(arguments),
                "protect_pdf" => await pdfTool.ProtectPdfAsync(arguments),
                "unlock_pdf" => await pdfTool.UnlockPdfAsync(arguments),
                "pdf_to_text" => await pdfTool.PdfToTextAsync(arguments),
                "text_to_pdf" => await pdfTool.TextToPdfAsync(arguments),
                "compare_pdf" => await pdfTool.ComparePdfAsync(arguments),
                "crop_pdf" => await pdfTool.CropPdfAsync(arguments),
                "set_pdf_metadata" => await pdfTool.SetPdfMetadataAsync(arguments),
                "image_to_pdf" => await pdfTool.ImageToPdfAsync(arguments),
                "html_to_pdf" => await pdfTool.HtmlToPdfAsync(arguments),
                "redact_pdf" => await pdfTool.RedactPdfAsync(arguments),
                "repair_pdf" => await pdfTool.RepairPdfAsync(arguments),

                // PDF Signing
                "sign_pdf" => await pdfTool.SignPdfAsync(arguments),
                "verify_pdf_signature" => await pdfTool.VerifyPdfSignatureAsync(arguments),
                "create_test_certificate" => await pdfTool.CreateTestCertificateAsync(arguments),

                // PDF Annotations
                "add_sticky_note" => await pdfTool.AddStickyNoteAsync(arguments),
                "add_highlight" => await pdfTool.AddHighlightAsync(arguments),
                "add_underline" => await pdfTool.AddUnderlineAsync(arguments),
                "add_strikethrough" => await pdfTool.AddStrikethroughAsync(arguments),
                "add_free_text" => await pdfTool.AddFreeTextAsync(arguments),
                "add_stamp" => await pdfTool.AddStampAsync(arguments),
                "add_link" => await pdfTool.AddLinkAsync(arguments),
                "list_annotations" => await pdfTool.ListAnnotationsAsync(arguments),
                "remove_annotations" => await pdfTool.RemoveAnnotationsAsync(arguments),
                "flatten_annotations" => await pdfTool.FlattenAnnotationsAsync(arguments),

                // Excel Processing
                "merge_workbooks" => await excelProcTool.MergeWorkbooksAsync(arguments),
                "split_workbook" => await excelProcTool.SplitWorkbookAsync(arguments),
                "excel_to_csv" => await excelProcTool.ExcelToCsvAsync(arguments),
                "excel_to_json" => await excelProcTool.ExcelToJsonAsync(arguments),
                "csv_to_excel" => await excelProcTool.CsvToExcelAsync(arguments),
                "json_to_excel" => await excelProcTool.JsonToExcelAsync(arguments),
                "clean_excel" => await excelProcTool.CleanExcelAsync(arguments),
                "get_excel_info" => await excelProcTool.GetExcelInfoAsync(arguments),
                "extract_sheets" => await excelProcTool.ExtractSheetsAsync(arguments),
                "reorder_sheets" => await excelProcTool.ReorderSheetsAsync(arguments),
                "rename_sheets" => await excelProcTool.RenameSheetsAsync(arguments),
                "delete_sheets" => await excelProcTool.DeleteSheetsAsync(arguments),
                "copy_sheet" => await excelProcTool.CopySheetAsync(arguments),
                "find_replace_excel" => await excelProcTool.FindReplaceExcelAsync(arguments),
                "excel_to_html" => await excelProcTool.ExcelToHtmlAsync(arguments),
                "add_formulas" => await excelProcTool.AddFormulasAsync(arguments),
                "text_to_excel" => await excelProcTool.TextToExcelAsync(arguments),
                "add_chart" => await excelProcTool.AddChartAsync(arguments),
                "create_pivot_summary" => await excelProcTool.CreatePivotSummaryAsync(arguments),
                "validate_excel_data" => await excelProcTool.ValidateExcelDataAsync(arguments),
                "compress_excel" => await excelProcTool.CompressWorkbookAsync(arguments),
                "repair_excel" => await excelProcTool.RepairWorkbookAsync(arguments),
                "protect_workbook" => await excelProcTool.ProtectWorkbookAsync(arguments),
                "unprotect_workbook" => await excelProcTool.UnprotectWorkbookAsync(arguments),
                "add_conditional_formatting" => await excelProcTool.AddConditionalFormattingAsync(arguments),
                "clear_conditional_formatting" => await excelProcTool.ClearConditionalFormattingAsync(arguments),

                // Text Processing
                "merge_text" => await textTool.MergeTextFilesAsync(arguments),
                "split_text" => await textTool.SplitTextFileAsync(arguments),
                "find_replace" => await textTool.FindReplaceAsync(arguments),
                "remove_duplicates" => await textTool.RemoveDuplicateLinesAsync(arguments),
                "sort_lines" => await textTool.SortLinesAsync(arguments),
                "convert_case" => await textTool.ConvertCaseAsync(arguments),
                "add_line_numbers" => await textTool.AddLineNumbersAsync(arguments),
                "compare_files" => await textTool.CompareFilesAsync(arguments),
                "clean_whitespace" => await textTool.CleanWhitespaceAsync(arguments),
                "reverse_text" => await textTool.ReverseTextAsync(arguments),
                "convert_encoding" => await textTool.ConvertEncodingAsync(arguments),
                "standardize_line_endings" => await textTool.StandardizeLineEndingsAsync(arguments),
                "wrap_text" => await textTool.WrapTextAsync(arguments),
                "text_to_json" => await textTool.TextToJsonAsync(arguments),
                "extract_columns" => await textTool.ExtractColumnsAsync(arguments),
                "filter_lines" => await textTool.FilterLinesAsync(arguments),
                "get_text_stats" => await textTool.GetTextStatsAsync(arguments),
                "text_to_html" => await textTool.TextToHtmlAsync(arguments),
                "text_to_xml" => await textTool.TextToXmlAsync(arguments),
                "html_to_text" => await textTool.HtmlToTextAsync(arguments),
                "xml_to_text" => await textTool.XmlToTextAsync(arguments),
                "compress_text" => await textTool.CompressTextAsync(arguments),
                "decompress_text" => await textTool.DecompressTextAsync(arguments),
                "encrypt_text" => await textTool.EncryptTextAsync(arguments),
                "decrypt_text" => await textTool.DecryptTextAsync(arguments),
                "calculate_checksum" => await textTool.CalculateChecksumAsync(arguments),
                "validate_checksum" => await textTool.ValidateChecksumAsync(arguments),

                // JSON Processing
                "format_json" => await jsonTool.FormatJsonAsync(arguments),
                "minify_json" => await jsonTool.MinifyJsonAsync(arguments),
                "validate_json" => await jsonTool.ValidateJsonAsync(arguments),
                "merge_json" => await jsonTool.MergeJsonAsync(arguments),
                "split_json" => await jsonTool.SplitJsonAsync(arguments),
                "query_json" => await jsonTool.QueryJsonAsync(arguments),
                "sort_json_keys" => await jsonTool.SortKeysAsync(arguments),
                "flatten_json" => await jsonTool.FlattenJsonAsync(arguments),
                "json_to_csv" => await jsonTool.JsonToCsvAsync(arguments),
                "remove_json_keys" => await jsonTool.RemoveKeysAsync(arguments),
                "csv_to_json" => await jsonTool.CsvToJsonAsync(arguments),
                "xml_to_json" => await jsonTool.XmlToJsonAsync(arguments),
                "json_to_xml" => await jsonTool.JsonToXmlAsync(arguments),
                "validate_schema" => await jsonTool.ValidateSchemaAsync(arguments),
                "get_json_stats" => await jsonTool.GetJsonStatsAsync(arguments),
                "remove_duplicates_json" => await jsonTool.RemoveDuplicatesJsonAsync(arguments),
                "transform_json" => await jsonTool.TransformJsonAsync(arguments),
                "yaml_to_json" => await jsonTool.YamlToJsonAsync(arguments),
                "json_to_yaml" => await jsonTool.JsonToYamlAsync(arguments),
                "json_to_html_table" => await jsonTool.JsonToHtmlTableAsync(arguments),
                "encrypt_json" => await jsonTool.EncryptJsonAsync(arguments),
                "decrypt_json" => await jsonTool.DecryptJsonAsync(arguments),
                "array_operations" => await jsonTool.ArrayOperationsAsync(arguments),
                "repair_json" => await jsonTool.RepairJsonAsync(arguments),
                "sql_to_json" => await jsonTool.SqlToJsonAsync(arguments),
                "json_to_pdf" => await jsonTool.JsonToPdfAsync(arguments),
                "sign_json" => await jsonTool.SignJsonAsync(arguments),
                "verify_json_signature" => await jsonTool.VerifyJsonSignatureAsync(arguments),
                "generate_json_signing_keys" => await jsonTool.GenerateJsonSigningKeysAsync(arguments),

                // Word Processing
                "merge_word" => await wordTool.MergeDocumentsAsync(arguments),
                "split_word" => await wordTool.SplitDocumentAsync(arguments),
                "extract_word_sections" => await wordTool.ExtractSectionsAsync(arguments),
                "remove_word_sections" => await wordTool.RemoveSectionsAsync(arguments),
                "word_to_text" => await wordTool.WordToTextAsync(arguments),
                "word_to_html" => await wordTool.WordToHtmlAsync(arguments),
                "word_to_json" => await wordTool.WordToJsonAsync(arguments),
                "text_to_word" => await wordTool.TextToWordAsync(arguments),
                "find_replace_word" => await wordTool.FindReplaceAsync(arguments),
                "add_header_footer" => await wordTool.AddHeaderFooterAsync(arguments),
                "get_word_info" => await wordTool.GetWordInfoAsync(arguments),
                "compare_word" => await wordTool.CompareDocumentsAsync(arguments),
                "clean_word_formatting" => await wordTool.CleanFormattingAsync(arguments),
                "word_to_pdf" => await wordTool.WordToPdfAsync(arguments),
                "mail_merge" => await wordTool.MailMergeAsync(arguments),
                "accept_track_changes" => await wordTool.AcceptTrackChangesAsync(arguments),
                "add_watermark_word" => await wordTool.AddWatermarkAsync(arguments),
                "manage_word_tables" => await wordTool.ManageTablesAsync(arguments),
                "manage_word_comments" => await wordTool.ManageCommentsAsync(arguments),
                "add_page_numbers_word" => await wordTool.AddPageNumbersAsync(arguments),
                "compress_word" => await wordTool.CompressDocumentAsync(arguments),
                "repair_word" => await wordTool.RepairDocumentAsync(arguments),
                "html_to_word" => await wordTool.HtmlToWordAsync(arguments),
                "protect_word" => await wordTool.ProtectDocumentAsync(arguments),
                "unprotect_word" => await wordTool.UnprotectDocumentAsync(arguments),
                "sign_word" => await wordTool.SignDocumentAsync(arguments),
                "verify_word_signature" => await wordTool.VerifyDocumentSignatureAsync(arguments),

                // PowerPoint Processing
                "merge_ppt" => await pptTool.MergePresentationsAsync(arguments),
                "split_ppt" => await pptTool.SplitPresentationAsync(arguments),
                "extract_slides" => await pptTool.ExtractSlidesAsync(arguments),
                "remove_slides" => await pptTool.RemoveSlidesAsync(arguments),
                "reorder_slides" => await pptTool.ReorderSlidesAsync(arguments),
                "ppt_to_text" => await pptTool.PresentationToTextAsync(arguments),
                "ppt_to_json" => await pptTool.PresentationToJsonAsync(arguments),
                "get_ppt_info" => await pptTool.GetPresentationInfoAsync(arguments),
                "duplicate_slides" => await pptTool.DuplicateSlidesAsync(arguments),
                "ppt_to_pdf" => await pptTool.PresentationToPdfAsync(arguments),
                "add_slide" => await pptTool.AddSlideAsync(arguments),
                "add_watermark_ppt" => await pptTool.AddWatermarkAsync(arguments),
                "extract_ppt_notes" => await pptTool.ExtractNotesAsync(arguments),
                "set_transitions" => await pptTool.SetTransitionsAsync(arguments),
                "find_replace_ppt" => await pptTool.FindReplaceAsync(arguments),
                "extract_ppt_images" => await pptTool.ExtractImagesAsync(arguments),
                "compress_ppt" => await pptTool.CompressPresentationAsync(arguments),
                "repair_ppt" => await pptTool.RepairPresentationAsync(arguments),
                "ppt_to_images" => await pptTool.PresentationToImagesAsync(arguments),
                "ppt_to_video" => await pptTool.PresentationToVideoAsync(arguments),
                "add_animations" => await pptTool.AddAnimationsAsync(arguments),
                "protect_ppt" => await pptTool.ProtectPresentationAsync(arguments),
                "unprotect_ppt" => await pptTool.UnprotectPresentationAsync(arguments),

                // OCR Processing
                "ocr_pdf" => await ocrTool.OcrPdfAsync(arguments),
                "ocr_image" => await ocrTool.OcrImageAsync(arguments),
                "get_ocr_languages" => await ocrTool.GetOcrLanguagesAsync(arguments),
                "batch_ocr" => await ocrTool.BatchOcrAsync(arguments),

                // Image Processing
                "add_text_watermark" => await imageTool.AddTextWatermarkAsync(arguments),
                "add_image_watermark" => await imageTool.AddImageWatermarkAsync(arguments),
                "redact_strings" => await imageTool.RedactStringsAsync(arguments),
                "redact_regions" => await imageTool.RedactRegionsAsync(arguments),
                "redact_sensitive_info" => await imageTool.RedactSensitiveInfoAsync(arguments),
                "resize_image" => await imageTool.ResizeImageAsync(arguments),
                "convert_image_format" => await imageTool.ConvertImageFormatAsync(arguments),
                "crop_image" => await imageTool.CropImageAsync(arguments),
                "rotate_image" => await imageTool.RotateImageAsync(arguments),

                // File Conversion
                "pdf_to_word" => await conversionTool.PdfToWordAsync(arguments),
                "pdf_to_excel" => await conversionTool.PdfToExcelAsync(arguments),
                "pdf_to_jpg" => await conversionTool.PdfToJpgAsync(arguments),
                "pdf_to_png" => await conversionTool.PdfToPngAsync(arguments),
                "pdf_to_pdfa" => await conversionTool.PdfToPdfAAsync(arguments),
                "excel_to_pdf" => await conversionTool.ExcelToPdfAsync(arguments),

                _ => throw new ArgumentException($"Unknown tool: {toolName}")
            };

            return CreateToolSuccessResponse(request.Id, result);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Tool error: {ex.Message}");
            return CreateToolErrorResponse(request.Id, ex.Message);
        }
    }

    /// <summary>
    /// Execute a custom tool created by AI.
    /// Custom tools are C# classes with a static method that takes JSON input.
    /// </summary>
    private async Task<string> ExecuteCustomToolAsync(string toolName, string filePath, Dictionary<string, object> arguments)
    {
        Console.Error.WriteLine($"Executing custom tool: {toolName} from {filePath}");

        // For now, handle known custom tools
        // In the future, this could use Roslyn to compile and execute dynamically
        if (toolName == "create_powerpoint" || filePath.Contains("PowerPoint"))
        {
            return await CreatePowerPointAsync(arguments);
        }

        // Generic execution for tools with standard interface
        var requestJson = JsonSerializer.Serialize(arguments, _jsonOptions);

        // Try to find and invoke the tool's static method
        var className = Path.GetFileNameWithoutExtension(filePath);
        var type = Type.GetType($"ZimaFileService.Api.{className}");

        if (type != null)
        {
            var method = type.GetMethod("Execute") ?? type.GetMethod("CreatePresentation") ?? type.GetMethod("Create");
            if (method != null)
            {
                var result = method.Invoke(null, new object[] { requestJson });
                return result?.ToString() ?? "Tool executed successfully";
            }
        }

        return $"Custom tool '{toolName}' executed with arguments: {requestJson}";
    }

    private void ResolveFilePath(Dictionary<string, object> arguments, string key)
    {
        if (arguments.TryGetValue(key, out var value))
        {
            string? path = null;
            if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
            {
                path = je.GetString();
            }
            else
            {
                path = value?.ToString();
            }

            if (!string.IsNullOrEmpty(path))
            {
                var resolved = FileManager.Instance.ResolveGeneratedFilePath(path);
                arguments[key] = resolved;
                Console.Error.WriteLine($"Resolved path: {path} -> {resolved}");
            }
        }
    }

    private void ResolveFilePathForReading(Dictionary<string, object> arguments, string key)
    {
        if (arguments.TryGetValue(key, out var value))
        {
            string? path = null;
            if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
            {
                path = je.GetString();
            }
            else
            {
                path = value?.ToString();
            }

            if (!string.IsNullOrEmpty(path) && !Path.IsPathRooted(path))
            {
                var genPath = Path.Combine(FileManager.Instance.GeneratedFilesPath, path);
                var upPath = Path.Combine(FileManager.Instance.UploadedFilesPath, path);

                if (File.Exists(genPath))
                {
                    arguments[key] = genPath;
                    Console.Error.WriteLine($"Found in generated_files: {path} -> {genPath}");
                }
                else if (File.Exists(upPath))
                {
                    arguments[key] = upPath;
                    Console.Error.WriteLine($"Found in uploaded_files: {path} -> {upPath}");
                }
                else
                {
                    arguments[key] = genPath;
                }
            }
        }
    }

    private string CreateSuccessResponse(object? id, object result)
    {
        var response = new JsonRpcResponse
        {
            Id = id,
            Result = result
        };
        return JsonSerializer.Serialize(response, _jsonOptions);
    }

    private string CreateErrorResponse(object? id, int code, string message)
    {
        var response = new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError { Code = code, Message = message }
        };
        return JsonSerializer.Serialize(response, _jsonOptions);
    }

    private string CreateToolSuccessResponse(object? id, string resultText)
    {
        var result = new McpToolResult
        {
            Content = new List<McpContent>
            {
                new McpContent { Type = "text", Text = resultText }
            },
            IsError = false
        };
        return CreateSuccessResponse(id, result);
    }

    private string CreateToolErrorResponse(object? id, string errorMessage)
    {
        var result = new McpToolResult
        {
            Content = new List<McpContent>
            {
                new McpContent { Type = "text", Text = $"Error: {errorMessage}" }
            },
            IsError = true
        };
        return CreateSuccessResponse(id, result);
    }

    private async Task<string> CreatePowerPointAsync(Dictionary<string, object> arguments)
    {
        try
        {
            var requestJson = JsonSerializer.Serialize(arguments, _jsonOptions);
            var result = SimplePowerPointTool.CreatePresentation(requestJson);
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"PowerPoint creation failed: {ex.Message}");
        }
    }
}
