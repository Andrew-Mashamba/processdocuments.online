using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ZimaFileService.Api;

/// <summary>
/// Simplified agent service that leverages Claude CLI's built-in capabilities.
/// Instead of managing context ourselves, we:
/// 1. Pass goal + file paths + permissions
/// 2. Let Claude CLI read files, use tools, and execute autonomously
/// 3. Scan for generated files after completion
/// </summary>
public class AgentService
{
    private static readonly Lazy<AgentService> _instance = new(() => new AgentService());
    public static AgentService Instance => _instance.Value;

    private readonly string _agentPromptPath;
    private readonly string _toolsPath;
    private readonly string _memoryPath;

    private AgentService()
    {
        var basePath = FileManager.Instance.WorkingDirectory;
        _agentPromptPath = Path.Combine(basePath, "agent", "prompt.md");
        _toolsPath = Path.Combine(basePath, "tools");
        _memoryPath = Path.Combine(basePath, "memory", "sessions");

        // Ensure directories exist
        Directory.CreateDirectory(_toolsPath);
        Directory.CreateDirectory(_memoryPath);

        // Generate tool manifests on startup
        GenerateToolManifests();
    }

    /// <summary>
    /// Execute a goal using the agent architecture.
    /// </summary>
    public async Task<AgentResponse> ExecuteAsync(string goal, string? sessionId = null)
    {
        sessionId ??= Guid.NewGuid().ToString();
        var journey = new AgentJourney(sessionId);

        try
        {
            journey.Log("AGENT_START", $"Goal: {goal}");

            // Setup session paths
            var paths = SetupSession(sessionId);
            journey.Log("SESSION_SETUP", $"Paths configured");

            // Build the agent prompt
            var prompt = BuildPrompt(goal, sessionId, paths);
            journey.Log("PROMPT_BUILT", $"Length: {prompt.Length} chars");

            // Get files before execution
            var filesBefore = GetExistingFiles(paths.OutputPath);

            // Execute Claude CLI with the prompt
            journey.Log("CLAUDE_START", "Executing Claude CLI");
            var (output, exitCode) = await ExecuteClaudeAsync(prompt, paths);
            journey.Log("CLAUDE_COMPLETE", $"Exit code: {exitCode}");

            // Get files after execution
            var filesAfter = GetExistingFiles(paths.OutputPath);
            var newFiles = filesAfter.Except(filesBefore).ToList();
            journey.Log("FILES_DETECTED", $"New files: {newFiles.Count}");

            // Update session memory
            await UpdateSessionMemory(paths.MemoryPath, goal, output, newFiles);

            // Parse output for completion status
            var (status, message) = ParseAgentOutput(output);

            return new AgentResponse
            {
                Success = status == "COMPLETED",
                SessionId = sessionId,
                Output = output,
                Message = message,
                Files = newFiles.Select(f => new GeneratedFile
                {
                    FileName = Path.GetFileName(f),
                    FilePath = f,
                    Size = new FileInfo(f).Length,
                    CreatedAt = File.GetCreationTime(f)
                }).ToList(),
                Journey = journey.GetSummary()
            };
        }
        catch (Exception ex)
        {
            journey.Log("ERROR", ex.Message);
            return new AgentResponse
            {
                Success = false,
                SessionId = sessionId,
                Output = "",
                Message = $"Agent error: {ex.Message}",
                Files = new List<GeneratedFile>(),
                Journey = journey.GetSummary()
            };
        }
    }

    /// <summary>
    /// Execute with streaming output.
    /// Event format matches /api/generate/stream for frontend compatibility.
    /// </summary>
    public async IAsyncEnumerable<AgentStreamEvent> ExecuteStreamAsync(string goal, string? sessionId = null)
    {
        sessionId ??= Guid.NewGuid().ToString();

        // Match generate stream format: start event with requestId and model
        yield return new AgentStreamEvent { Type = "start", Data = new { requestId = sessionId, model = "agent", complexity = "auto", sessionId, goal } };

        // Setup session paths
        var paths = SetupSession(sessionId);

        // Build the agent prompt
        var prompt = BuildPrompt(goal, sessionId, paths);

        // Get files before
        var filesBefore = GetExistingFiles(paths.OutputPath);

        // Execute Claude CLI with streaming
        var outputBuilder = new StringBuilder();
        await foreach (var chunk in ExecuteClaudeStreamAsync(prompt, paths))
        {
            outputBuilder.Append(chunk);
            // Match generate stream format: content event with { content: "text" }
            yield return new AgentStreamEvent { Type = "content", Data = new { content = chunk } };
        }

        // Get new files
        var filesAfter = GetExistingFiles(paths.OutputPath);
        var newFiles = filesAfter.Except(filesBefore).ToList();

        // Match generate stream format: files event with { files: [...] }
        var filesData = newFiles.Select(f => new
        {
            Name = Path.GetFileName(f),
            Size = new FileInfo(f).Length,
            SizeFormatted = FormatFileSize(new FileInfo(f).Length),
            Extension = Path.GetExtension(f).TrimStart('.'),
            Created = File.GetCreationTime(f),
            Modified = File.GetLastWriteTime(f),
            DownloadUrl = $"/api/files/download/{sessionId}/{Uri.EscapeDataString(Path.GetFileName(f))}"
        }).ToList();

        if (filesData.Count > 0)
        {
            yield return new AgentStreamEvent { Type = "files", Data = new { files = filesData } };
        }

        // Update memory
        await UpdateSessionMemory(paths.MemoryPath, goal, outputBuilder.ToString(), newFiles);

        // Match generate stream format: complete event with output, usage, files, model
        yield return new AgentStreamEvent
        {
            Type = "complete",
            Data = new
            {
                output = outputBuilder.ToString(),
                usage = new { inputTokens = 0, outputTokens = 0, cost = 0 },
                files = filesData,
                model = "agent",
                sessionId
            }
        };
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

    /// <summary>
    /// Continue a previous session with a new goal.
    /// </summary>
    public async Task<AgentResponse> ContinueSessionAsync(string sessionId, string goal)
    {
        // Session memory persists, so Claude can re-read it
        return await ExecuteAsync(goal, sessionId);
    }

    private SessionPaths SetupSession(string sessionId)
    {
        var paths = new SessionPaths
        {
            SessionId = sessionId,
            InputPath = Path.Combine(FileManager.Instance.UploadedFilesPath, sessionId),
            OutputPath = Path.Combine(FileManager.Instance.GeneratedFilesPath, sessionId),
            MemoryPath = Path.Combine(_memoryPath, sessionId)
        };

        // Create directories
        Directory.CreateDirectory(paths.InputPath);
        Directory.CreateDirectory(paths.OutputPath);
        Directory.CreateDirectory(paths.MemoryPath);

        return paths;
    }

    private string BuildPrompt(string goal, string sessionId, SessionPaths paths)
    {
        // Load the minimal agent prompt template
        var template = File.Exists(_agentPromptPath)
            ? File.ReadAllText(_agentPromptPath)
            : GetDefaultPromptTemplate();

        // Inject session-specific values
        var prompt = template
            .Replace("{{USER_GOAL}}", goal)
            .Replace("{{SESSION_ID}}", sessionId)
            .Replace("{{UPLOADED_FILES_PATH}}", paths.InputPath)
            .Replace("{{GENERATED_FILES_PATH}}", paths.OutputPath)
            .Replace("{{MEMORY_PATH}}", paths.MemoryPath);

        // Add context about uploaded files if any exist
        var uploadedFiles = Directory.Exists(paths.InputPath)
            ? Directory.GetFiles(paths.InputPath, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();

        if (uploadedFiles.Length > 0)
        {
            prompt += "\n\n## Uploaded Files\n";
            prompt += "The user has uploaded these files (read them as needed):\n";
            foreach (var file in uploadedFiles)
            {
                var info = new FileInfo(file);
                prompt += $"- `{file}` ({info.Length} bytes)\n";
            }
        }

        // Add hint about session memory if it exists
        var logPath = Path.Combine(paths.MemoryPath, "log.md");
        if (File.Exists(logPath))
        {
            prompt += "\n\n## Previous Session Context\n";
            prompt += $"This is a continuation. Read `{logPath}` for previous actions.\n";
        }

        return prompt;
    }

    private string GetDefaultPromptTemplate()
    {
        return @"# ZIMA Agent

You are ZIMA, an autonomous file generation agent.

## Goal
{{USER_GOAL}}

## Session
- ID: {{SESSION_ID}}
- Input files: {{UPLOADED_FILES_PATH}}
- Output folder: {{GENERATED_FILES_PATH}}
- Memory: {{MEMORY_PATH}}

## Permissions
- Read files from input folder
- Write files to output folder
- Use available tools
- Create new tools if needed

## Rules
1. Complete the goal autonomously
2. Save outputs to the output folder
3. Log actions to memory

## Output
After completing: COMPLETED: [description]
If blocked: BLOCKED: [reason]
";
    }

    private async Task<(string output, int exitCode)> ExecuteClaudeAsync(string prompt, SessionPaths paths)
    {
        // Write prompt to temp file for reliability
        var promptFile = Path.Combine(Path.GetTempPath(), $"zima-prompt-{paths.SessionId}.txt");
        await File.WriteAllTextAsync(promptFile, prompt);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = "--print --dangerously-skip-permissions",
                WorkingDirectory = FileManager.Instance.WorkingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();
            var errors = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) output.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) errors.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Send prompt via stdin
            await process.StandardInput.WriteAsync(prompt);
            process.StandardInput.Close();

            // Wait with timeout (10 minutes)
            var completed = await Task.Run(() => process.WaitForExit(600_000));
            if (!completed)
            {
                process.Kill();
                throw new TimeoutException("Claude CLI timed out after 10 minutes");
            }

            return (output.ToString(), process.ExitCode);
        }
        finally
        {
            if (File.Exists(promptFile))
                File.Delete(promptFile);
        }
    }

    private async IAsyncEnumerable<string> ExecuteClaudeStreamAsync(string prompt, SessionPaths paths)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "claude",
            // Use --include-partial-messages for true token-by-token streaming (requires --verbose)
            Arguments = "--print --dangerously-skip-permissions --output-format stream-json --verbose --include-partial-messages",
            WorkingDirectory = FileManager.Instance.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Send prompt
        await process.StandardInput.WriteAsync(prompt);
        process.StandardInput.Close();

        // Read streaming output
        using var reader = process.StandardOutput;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Parse stream-json format - extract text to yield outside try-catch
            string? textToYield = null;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeEl))
                {
                    var type = typeEl.GetString();

                    switch (type)
                    {
                        case "stream_event":
                            // Handle streaming partial messages (--include-partial-messages)
                            // Format: {"type":"stream_event","event":{"type":"content_block_delta","delta":{"type":"text_delta","text":"..."}}}
                            if (root.TryGetProperty("event", out var eventObj))
                            {
                                var eventType = eventObj.TryGetProperty("type", out var et) ? et.GetString() : null;
                                if (eventType == "content_block_delta" &&
                                    eventObj.TryGetProperty("delta", out var delta) &&
                                    delta.TryGetProperty("text", out var textDelta))
                                {
                                    textToYield = textDelta.GetString();
                                }
                            }
                            break;

                        case "assistant":
                            // Extract content from assistant message (final message)
                            if (root.TryGetProperty("message", out var message) &&
                                message.TryGetProperty("content", out var contentArray) &&
                                contentArray.ValueKind == JsonValueKind.Array)
                            {
                                var textContent = new StringBuilder();
                                foreach (var item in contentArray.EnumerateArray())
                                {
                                    if (item.TryGetProperty("type", out var itemType) &&
                                        itemType.GetString() == "text" &&
                                        item.TryGetProperty("text", out var text))
                                    {
                                        textContent.Append(text.GetString());
                                    }
                                }
                                // Only yield if we haven't been streaming (fallback)
                                if (textContent.Length > 0)
                                {
                                    // Skip if we've been streaming - this would be duplicate
                                }
                            }
                            break;

                        case "content_block_delta":
                            // Legacy format without stream_event wrapper
                            if (root.TryGetProperty("delta", out var legacyDelta) &&
                                legacyDelta.TryGetProperty("text", out var legacyText))
                            {
                                textToYield = legacyText.GetString();
                            }
                            break;
                    }
                }
            }
            catch
            {
                // Not JSON, skip
            }

            // Yield outside try-catch block
            if (!string.IsNullOrEmpty(textToYield))
            {
                yield return textToYield;
            }
        }

        await process.WaitForExitAsync();
    }

    private List<string> GetExistingFiles(string path)
    {
        if (!Directory.Exists(path)) return new List<string>();
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
    }

    private async Task UpdateSessionMemory(string memoryPath, string goal, string output, List<string> newFiles)
    {
        var logPath = Path.Combine(memoryPath, "log.md");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var logEntry = new StringBuilder();
        logEntry.AppendLine($"\n## {timestamp}");
        logEntry.AppendLine($"**Goal:** {goal}");
        logEntry.AppendLine($"**Files Generated:** {newFiles.Count}");
        foreach (var file in newFiles)
        {
            logEntry.AppendLine($"- {Path.GetFileName(file)}");
        }
        logEntry.AppendLine();

        // Append to log
        await File.AppendAllTextAsync(logPath, logEntry.ToString());
    }

    private (string status, string message) ParseAgentOutput(string output)
    {
        // Look for COMPLETED: or BLOCKED: markers
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("COMPLETED:"))
                return ("COMPLETED", line.Substring("COMPLETED:".Length).Trim());
            if (line.StartsWith("BLOCKED:"))
                return ("BLOCKED", line.Substring("BLOCKED:".Length).Trim());
        }
        return ("UNKNOWN", output.Length > 200 ? output.Substring(0, 200) + "..." : output);
    }

    /// <summary>
    /// Generate tool manifests from ToolsRegistry for filesystem-based discovery.
    /// </summary>
    public void GenerateToolManifests()
    {
        var tools = ToolsRegistry.Instance.GetAllMcpTools();

        foreach (var tool in tools)
        {
            var toolDir = Path.Combine(_toolsPath, tool.Name);
            Directory.CreateDirectory(toolDir);

            var inputs = new Dictionary<string, object>();
            if (tool.InputSchema?.Properties != null)
            {
                foreach (var p in tool.InputSchema.Properties)
                {
                    inputs[p.Key] = new { type = p.Value.Type, description = p.Value.Description };
                }
            }

            var manifest = new
            {
                name = tool.Name,
                description = tool.Description,
                usage = tool.Usage,
                category = tool.Category,
                inputs,
                required = tool.InputSchema?.Required ?? new List<string>(),
                command = $"dotnet run --project {FileManager.Instance.WorkingDirectory} -- mcp-tool {tool.Name}"
            };

            var manifestPath = Path.Combine(toolDir, "manifest.json");
            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, json);
        }

        Log("INFO", $"Generated {tools.Count} tool manifests in {_toolsPath}");
    }

    /// <summary>
    /// Get list of available tools from filesystem.
    /// </summary>
    public List<ToolManifest> GetAvailableTools()
    {
        var tools = new List<ToolManifest>();

        if (!Directory.Exists(_toolsPath)) return tools;

        foreach (var dir in Directory.GetDirectories(_toolsPath))
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (File.Exists(manifestPath))
            {
                try
                {
                    var json = File.ReadAllText(manifestPath);
                    var manifest = JsonSerializer.Deserialize<ToolManifest>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (manifest != null)
                        tools.Add(manifest);
                }
                catch { /* skip invalid manifests */ }
            }
        }

        return tools;
    }

    private static void Log(string level, string message)
    {
        Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [AGENT] [{level}] {message}");
    }
}

// === Models ===

public class SessionPaths
{
    public string SessionId { get; set; } = "";
    public string InputPath { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public string MemoryPath { get; set; } = "";
}

public class AgentResponse
{
    public bool Success { get; set; }
    public string SessionId { get; set; } = "";
    public string Output { get; set; } = "";
    public string Message { get; set; } = "";
    public List<GeneratedFile> Files { get; set; } = new();
    public string Journey { get; set; } = "";
}

public class GeneratedFile
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AgentStreamEvent
{
    public string Type { get; set; } = "";
    public object? Data { get; set; }
}

public class ToolManifest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Usage { get; set; }
    public string Category { get; set; } = "";
    public Dictionary<string, ToolInput> Inputs { get; set; } = new();
    public List<string> Required { get; set; } = new();
    public string Command { get; set; } = "";
}

public class ToolInput
{
    public string Type { get; set; } = "";
    public string? Description { get; set; }
}

public class AgentJourney
{
    private readonly string _sessionId;
    private readonly List<(DateTime Time, string Phase, string Data)> _entries = new();
    private readonly DateTime _startTime;

    public AgentJourney(string sessionId)
    {
        _sessionId = sessionId;
        _startTime = DateTime.Now;
    }

    public void Log(string phase, string data)
    {
        _entries.Add((DateTime.Now, phase, data));
    }

    public string GetSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Session: {_sessionId}");
        sb.AppendLine($"Duration: {(DateTime.Now - _startTime).TotalMilliseconds:F0}ms");
        foreach (var (time, phase, data) in _entries)
        {
            sb.AppendLine($"  [{(time - _startTime).TotalMilliseconds:F0}ms] {phase}: {data}");
        }
        return sb.ToString();
    }
}
