using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ZimaFileService.Api;

[ApiController]
[Route("api/generate")]
public class GenerateController : ControllerBase
{
    private readonly ZimaService _zimaService;
    private readonly FileManager _fileManager;

    public GenerateController(ZimaService zimaService)
    {
        _zimaService = zimaService;
        _fileManager = FileManager.Instance;
    }

    /// <summary>
    /// Generate files using ZIMA AI
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        // Debug logging
        Console.Error.WriteLine($"========== GENERATE REQUEST ==========");
        Console.Error.WriteLine($"[Generate] Prompt: {request.Prompt?.Substring(0, Math.Min(100, request.Prompt?.Length ?? 0))}...");
        Console.Error.WriteLine($"[Generate] SessionId: '{request.SessionId ?? "NULL"}'");
        Console.Error.WriteLine($"[Generate] Messages count: {request.Messages?.Count ?? 0}");
        Console.Error.WriteLine($"[Generate] Context length: {request.Context?.Length ?? 0}");

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { error = "Prompt is required" });
        }

        try
        {
            var filesBefore = _fileManager.ListFiles(_fileManager.GeneratedFilesPath)
                .Select(f => f.Name).ToHashSet();

            // Convert MessageDto to ConversationMessage for prompt caching
            List<ConversationMessage>? messages = null;
            if (request.Messages != null && request.Messages.Count > 0)
            {
                messages = request.Messages.Select(m => new ConversationMessage
                {
                    Role = m.Role,
                    Content = m.Content,
                    IsSummary = m.IsSummary
                }).ToList();
            }

            // Get uploaded file context for this session (smart loading for large files)
            string? fileContext = null;
            Console.Error.WriteLine($"[Generate] Checking for session files...");
            Console.Error.WriteLine($"[Generate] SessionId is empty: {string.IsNullOrEmpty(request.SessionId)}");

            if (!string.IsNullOrEmpty(request.SessionId))
            {
                Console.Error.WriteLine($"[Generate] Looking for files in session: {request.SessionId}");
                var sessionFiles = _fileManager.GetSessionFileContents(request.SessionId);
                Console.Error.WriteLine($"[Generate] Found {sessionFiles.Count} session files");

                if (sessionFiles.Count > 0)
                {
                    var contextBuilder = new System.Text.StringBuilder();
                    contextBuilder.AppendLine("\n=== UPLOADED FILES FOR THIS SESSION ===");
                    contextBuilder.AppendLine($"The user has uploaded {sessionFiles.Count} file(s) for context.");
                    contextBuilder.AppendLine("Note: Large files are automatically summarized. Use tools (read_excel, read_file_content) for full access.\n");

                    foreach (var (fileName, content) in sessionFiles)
                    {
                        var loadInfo = content.LoadStrategy switch
                        {
                            "full" => "",
                            "preview" => $" [PREVIEW: {content.LoadedLines}/{content.TotalLines} lines loaded]",
                            "summary" => " [SUMMARY - use tools for full data]",
                            "metadata" => " [METADATA ONLY]",
                            _ => ""
                        };

                        if (content.IsText)
                        {
                            contextBuilder.AppendLine($"--- {fileName} ({FormatFileSize(content.Size)}){loadInfo} ---");
                            contextBuilder.AppendLine(content.Content);
                            contextBuilder.AppendLine();
                        }
                        else
                        {
                            contextBuilder.AppendLine($"--- {fileName} ({content.MimeType}, {FormatFileSize(content.Size)}){loadInfo} ---");
                            contextBuilder.AppendLine(content.Content);
                            contextBuilder.AppendLine();
                        }
                    }
                    contextBuilder.AppendLine("=== END UPLOADED FILES ===\n");
                    fileContext = contextBuilder.ToString();
                }
            }

            // Combine file context with conversation context
            var fullContext = request.Context;
            if (!string.IsNullOrEmpty(fileContext))
            {
                fullContext = fileContext + (fullContext ?? "");
                Console.Error.WriteLine($"[Generate] File context injected! Length: {fileContext.Length}");
                Console.Error.WriteLine($"[Generate] File context preview: {fileContext.Substring(0, Math.Min(500, fileContext.Length))}...");
            }
            else
            {
                Console.Error.WriteLine($"[Generate] NO file context to inject");
            }

            Console.Error.WriteLine($"[Generate] Full context length: {fullContext?.Length ?? 0}");
            Console.Error.WriteLine($"[Generate] Calling ZimaService.GenerateAsync...");

            var result = await _zimaService.GenerateAsync(request.Prompt, fullContext, request.SessionId, messages);

            Console.Error.WriteLine($"[Generate] Response received. Success: {result.Success}");
            Console.Error.WriteLine($"[Generate] Output preview: {result.Output?.Substring(0, Math.Min(200, result.Output?.Length ?? 0))}...");

            var filesAfter = _fileManager.ListFiles(_fileManager.GeneratedFilesPath);
            var newFiles = filesAfter
                .Where(f => !filesBefore.Contains(f.Name))
                .Select(f => new FileDto
                {
                    Name = f.Name,
                    Size = f.Length,
                    SizeFormatted = FormatFileSize(f.Length),
                    Extension = f.Extension.TrimStart('.'),
                    Created = f.CreationTime,
                    Modified = f.LastWriteTime,
                    DownloadUrl = $"/api/files/download/{Uri.EscapeDataString(f.Name)}"
                })
                .ToList();

            return Ok(new GenerateResponse
            {
                Success = result.Success,
                Message = result.Message,
                Output = result.Output,
                Errors = result.Errors,
                Model = result.Model,
                Usage = result.Usage != null ? new UsageDto
                {
                    InputTokens = result.Usage.InputTokens,
                    OutputTokens = result.Usage.OutputTokens,
                    CacheCreationTokens = result.Usage.CacheCreationTokens,
                    CacheReadTokens = result.Usage.CacheReadTokens,
                    Cost = result.Usage.Cost
                } : null,
                GeneratedFiles = newFiles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate files with real-time streaming (SSE)
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamGenerate([FromBody] GenerateRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        async Task SendEvent(string eventType, object data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await Response.WriteAsync($"event: {eventType}\n");
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            await SendEvent("error", new { message = "Prompt is required" });
            return;
        }

        try
        {
            var filesBefore = _fileManager.ListFiles(_fileManager.GeneratedFilesPath)
                .Select(f => f.Name).ToHashSet();

            // Convert MessageDto to ConversationMessage for prompt caching
            List<ConversationMessage>? messages = null;
            if (request.Messages != null && request.Messages.Count > 0)
            {
                messages = request.Messages.Select(m => new ConversationMessage
                {
                    Role = m.Role,
                    Content = m.Content,
                    IsSummary = m.IsSummary
                }).ToList();
            }

            // Stream events from the service
            await foreach (var streamEvent in _zimaService.GenerateStreamingAsync(request.Prompt, request.Context, messages))
            {
                await SendEvent(streamEvent.Type, streamEvent.Data ?? new { });
            }

            // Check for new files after streaming completes
            var filesAfter = _fileManager.ListFiles(_fileManager.GeneratedFilesPath);
            var newFiles = filesAfter
                .Where(f => !filesBefore.Contains(f.Name))
                .Select(f => new FileDto
                {
                    Name = f.Name,
                    Size = f.Length,
                    SizeFormatted = FormatFileSize(f.Length),
                    Extension = f.Extension.TrimStart('.'),
                    Created = f.CreationTime,
                    Modified = f.LastWriteTime,
                    DownloadUrl = $"/api/files/download/{Uri.EscapeDataString(f.Name)}"
                })
                .ToList();

            if (newFiles.Any())
            {
                await SendEvent("files", new { files = newFiles });
            }
        }
        catch (Exception ex)
        {
            await SendEvent("error", new { message = ex.Message });
        }
    }

    /// <summary>
    /// Generate a smart title for a session
    /// </summary>
    [HttpPost("title")]
    public async Task<IActionResult> GenerateTitle([FromBody] TitleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required" });
        }

        try
        {
            var title = await _zimaService.GenerateTitleAsync(request.Message);
            return Ok(new { title });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Summarize a conversation for context management
    /// </summary>
    [HttpPost("summarize")]
    public async Task<IActionResult> Summarize([FromBody] SummarizeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConversationHistory))
        {
            return BadRequest(new { error = "Conversation history is required" });
        }

        try
        {
            var summary = await _zimaService.SummarizeSessionAsync(request.ConversationHistory);
            return Ok(new { summary });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get server status and paths
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "running",
            workingDirectory = _fileManager.WorkingDirectory,
            generatedFilesPath = _fileManager.GeneratedFilesPath,
            uploadedFilesPath = _fileManager.UploadedFilesPath,
            generatedFilesCount = Directory.Exists(_fileManager.GeneratedFilesPath)
                ? Directory.GetFiles(_fileManager.GeneratedFilesPath).Length : 0,
            uploadedFilesCount = Directory.Exists(_fileManager.UploadedFilesPath)
                ? Directory.GetFiles(_fileManager.UploadedFilesPath).Length : 0
        });
    }

    /// <summary>
    /// Get performance statistics (cache hits, pool stats, etc.)
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var cacheStats = ResponseCache.Instance.GetStats();
        var poolStats = ClaudeConnectionPool.Instance.GetStats();

        return Ok(new
        {
            cache = new
            {
                totalEntries = cacheStats.TotalEntries,
                hits = cacheStats.CacheHits,
                misses = cacheStats.CacheMisses,
                hitRate = Math.Round(cacheStats.HitRate, 2),
                maxSize = cacheStats.MaxSize
            },
            connectionPool = new
            {
                currentPoolSize = poolStats.CurrentPoolSize,
                warmHits = poolStats.WarmHits,
                coldStarts = poolStats.ColdStarts,
                warmHitRate = Math.Round(poolStats.WarmHitRate, 2),
                warmSessions = poolStats.WarmSessions,
                minPoolSize = poolStats.MinPoolSize,
                maxPoolSize = poolStats.MaxPoolSize
            },
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Clear the response cache
    /// </summary>
    [HttpPost("cache/clear")]
    public IActionResult ClearCache()
    {
        ResponseCache.Instance.Clear();
        return Ok(new { message = "Cache cleared successfully" });
    }

    /// <summary>
    /// Pre-warm the connection pool
    /// </summary>
    [HttpPost("pool/warmup")]
    public async Task<IActionResult> WarmupPool([FromQuery] int count = 2)
    {
        try
        {
            await ClaudeConnectionPool.Instance.WarmupAsync(Math.Min(count, 5));
            var stats = ClaudeConnectionPool.Instance.GetStats();
            return Ok(new
            {
                message = $"Pool warmed up with {count} connections",
                currentPoolSize = stats.CurrentPoolSize,
                warmSessions = stats.WarmSessions
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all available tools (MCP tools, skills, custom tools)
    /// </summary>
    [HttpGet("tools")]
    public IActionResult GetTools()
    {
        var toolNames = ToolsRegistry.Instance.GetAllToolNames();
        var toolsPrompt = ToolsRegistry.Instance.GetToolsPrompt();

        return Ok(new
        {
            tools = toolNames,
            count = toolNames.Count,
            prompt = toolsPrompt
        });
    }

    /// <summary>
    /// Register a new custom tool
    /// </summary>
    [HttpPost("tools/register")]
    public IActionResult RegisterTool([FromBody] RegisterToolRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { error = "Name and description are required" });
        }

        ToolsRegistry.Instance.RegisterTool(request.Name, request.Description, request.FilePath ?? "");
        return Ok(new
        {
            message = $"Tool '{request.Name}' registered successfully",
            tools = ToolsRegistry.Instance.GetAllToolNames()
        });
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

// Request/Response DTOs
public class GenerateRequest
{
    public string Prompt { get; set; } = "";
    public string? Context { get; set; }
    public string? SessionId { get; set; }
    /// <summary>
    /// Structured messages for prompt caching. When provided, enables cache_control: ephemeral
    /// on the last 2-3 messages for 50-90% cost reduction.
    /// </summary>
    public List<MessageDto>? Messages { get; set; }
}

public class MessageDto
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsSummary { get; set; } = false;
}

public class GenerateResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Output { get; set; }
    public string? Errors { get; set; }
    public string? Model { get; set; }
    public UsageDto? Usage { get; set; }
    public List<FileDto> GeneratedFiles { get; set; } = new();
}

public class UsageDto
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CacheCreationTokens { get; set; }
    public int CacheReadTokens { get; set; }
    public decimal Cost { get; set; }
}

public class TitleRequest
{
    public string Message { get; set; } = "";
}

public class SummarizeRequest
{
    public string ConversationHistory { get; set; } = "";
}

public class RegisterToolRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? FilePath { get; set; }
}
