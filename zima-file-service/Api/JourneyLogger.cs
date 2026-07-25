using System.Diagnostics;
using System.Text.Json;

namespace ZimaFileService.Api;

/// <summary>
/// Journey Logger for tracking prompt-to-response flow across all routes.
/// Provides consistent, structured logging for performance analysis.
/// </summary>
public class JourneyLogger
{
    private readonly string _requestId;
    private readonly Stopwatch _stopwatch;
    private readonly List<JourneyStep> _steps = new();
    private readonly string _route;
    private readonly string? _sessionId;

    public string RequestId => _requestId;
    public long ElapsedMs => _stopwatch.ElapsedMilliseconds;

    public JourneyLogger(string route, string? sessionId = null)
    {
        _requestId = Guid.NewGuid().ToString("N")[..12];
        _stopwatch = Stopwatch.StartNew();
        _route = route;
        _sessionId = sessionId;

        LogStep("JOURNEY_START", $"Route: {route}", new Dictionary<string, object?>
        {
            ["route"] = route,
            ["sessionId"] = sessionId,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        });
    }

    /// <summary>
    /// Log a step in the journey with timing information.
    /// </summary>
    public void LogStep(string phase, string message, Dictionary<string, object?>? data = null)
    {
        var step = new JourneyStep
        {
            Phase = phase,
            Message = message,
            ElapsedMs = _stopwatch.ElapsedMilliseconds,
            Data = data ?? new Dictionary<string, object?>()
        };
        _steps.Add(step);

        var dataJson = data != null ? JsonSerializer.Serialize(data) : "";
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{_requestId}] [{phase}] {message} | {_stopwatch.ElapsedMilliseconds}ms | {dataJson}");
    }

    /// <summary>
    /// Log route decision point.
    /// </summary>
    public void LogRouteDecision(string decision, string reason, Dictionary<string, object?>? context = null)
    {
        var data = new Dictionary<string, object?>
        {
            ["decision"] = decision,
            ["reason"] = reason
        };
        if (context != null)
        {
            foreach (var kvp in context)
                data[kvp.Key] = kvp.Value;
        }
        LogStep("ROUTE_DECISION", $"{decision}: {reason}", data);
    }

    /// <summary>
    /// Log model selection.
    /// </summary>
    public void LogModelSelection(string model, string complexity, string costInfo)
    {
        LogStep("MODEL_SELECTION", $"Selected {model} for {complexity} task", new Dictionary<string, object?>
        {
            ["model"] = model,
            ["complexity"] = complexity,
            ["costInfo"] = costInfo
        });
    }

    /// <summary>
    /// Log context tier selection.
    /// </summary>
    public void LogContextTier(string tier, int messageCount, int filteredCount, int contextLength)
    {
        LogStep("CONTEXT_TIER", $"Tier={tier}, Messages={filteredCount}/{messageCount}, Context={contextLength} chars", new Dictionary<string, object?>
        {
            ["tier"] = tier,
            ["totalMessages"] = messageCount,
            ["filteredMessages"] = filteredCount,
            ["contextLength"] = contextLength
        });
    }

    /// <summary>
    /// Log prompt processing.
    /// </summary>
    public void LogPrompt(string prompt, int length)
    {
        var preview = prompt.Length > 100 ? prompt[..100] + "..." : prompt;
        LogStep("PROMPT", $"Length={length}, Preview: {preview}", new Dictionary<string, object?>
        {
            ["length"] = length,
            ["preview"] = preview
        });
    }

    /// <summary>
    /// Log Claude CLI execution start.
    /// </summary>
    public void LogClaudeStart(string model, int promptLength)
    {
        LogStep("CLAUDE_START", $"Starting Claude CLI with {model}, prompt={promptLength} chars", new Dictionary<string, object?>
        {
            ["model"] = model,
            ["promptLength"] = promptLength
        });
    }

    /// <summary>
    /// Log Claude CLI execution complete.
    /// </summary>
    public void LogClaudeComplete(int exitCode, long durationMs)
    {
        LogStep("CLAUDE_COMPLETE", $"Exit={exitCode}, Duration={durationMs}ms", new Dictionary<string, object?>
        {
            ["exitCode"] = exitCode,
            ["durationMs"] = durationMs
        });
    }

    /// <summary>
    /// Log token usage.
    /// </summary>
    public void LogTokenUsage(int inputTokens, int outputTokens, int cacheRead, int cacheCreate, decimal cost)
    {
        var cacheHitRate = inputTokens > 0 ? (cacheRead * 100.0 / inputTokens).ToString("F1") : "0";
        LogStep("TOKEN_USAGE", $"In={inputTokens}, Out={outputTokens}, CacheHit={cacheHitRate}%, Cost=${cost:F6}", new Dictionary<string, object?>
        {
            ["inputTokens"] = inputTokens,
            ["outputTokens"] = outputTokens,
            ["cacheReadTokens"] = cacheRead,
            ["cacheCreateTokens"] = cacheCreate,
            ["cacheHitRate"] = cacheHitRate,
            ["cost"] = cost
        });
    }

    /// <summary>
    /// Log file generation.
    /// </summary>
    public void LogFileGeneration(List<string> files)
    {
        LogStep("FILES_GENERATED", $"Generated {files.Count} files: {string.Join(", ", files)}", new Dictionary<string, object?>
        {
            ["count"] = files.Count,
            ["files"] = files
        });
    }

    /// <summary>
    /// Log parallel execution.
    /// </summary>
    public void LogParallelExecution(int subtaskCount, bool canParallelize)
    {
        LogStep("PARALLEL_CHECK", $"CanParallelize={canParallelize}, Subtasks={subtaskCount}", new Dictionary<string, object?>
        {
            ["canParallelize"] = canParallelize,
            ["subtaskCount"] = subtaskCount
        });
    }

    /// <summary>
    /// Log streaming event.
    /// </summary>
    public void LogStreamEvent(string eventType, int contentLength = 0)
    {
        LogStep("STREAM_EVENT", $"Type={eventType}, ContentLength={contentLength}", new Dictionary<string, object?>
        {
            ["eventType"] = eventType,
            ["contentLength"] = contentLength
        });
    }

    /// <summary>
    /// Log error.
    /// </summary>
    public void LogError(string phase, string error, Exception? ex = null)
    {
        LogStep("ERROR", $"[{phase}] {error}", new Dictionary<string, object?>
        {
            ["phase"] = phase,
            ["error"] = error,
            ["exception"] = ex?.Message,
            ["stackTrace"] = ex?.StackTrace?[..Math.Min(500, ex.StackTrace?.Length ?? 0)]
        });
    }

    /// <summary>
    /// Complete the journey and log summary.
    /// </summary>
    public JourneySummary Complete(bool success, string? outputPreview = null)
    {
        _stopwatch.Stop();

        var summary = new JourneySummary
        {
            RequestId = _requestId,
            Route = _route,
            SessionId = _sessionId,
            Success = success,
            TotalDurationMs = _stopwatch.ElapsedMilliseconds,
            StepCount = _steps.Count,
            Steps = _steps,
            OutputPreview = outputPreview?[..Math.Min(200, outputPreview?.Length ?? 0)]
        };

        // Calculate phase durations
        var phaseDurations = new Dictionary<string, long>();
        for (int i = 0; i < _steps.Count - 1; i++)
        {
            var step = _steps[i];
            var nextStep = _steps[i + 1];
            var duration = nextStep.ElapsedMs - step.ElapsedMs;

            if (!phaseDurations.ContainsKey(step.Phase))
                phaseDurations[step.Phase] = 0;
            phaseDurations[step.Phase] += duration;
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{_requestId}] [JOURNEY_COMPLETE] Success={success}, TotalTime={_stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{_requestId}] [JOURNEY_SUMMARY] Phase durations: {JsonSerializer.Serialize(phaseDurations)}");

        // Identify bottlenecks (phases taking > 1000ms)
        var bottlenecks = phaseDurations.Where(p => p.Value > 1000).OrderByDescending(p => p.Value).ToList();
        if (bottlenecks.Any())
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{_requestId}] [BOTTLENECK] Slow phases: {string.Join(", ", bottlenecks.Select(b => $"{b.Key}={b.Value}ms"))}");
        }

        return summary;
    }
}

public class JourneyStep
{
    public string Phase { get; set; } = "";
    public string Message { get; set; } = "";
    public long ElapsedMs { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();
}

public class JourneySummary
{
    public string RequestId { get; set; } = "";
    public string Route { get; set; } = "";
    public string? SessionId { get; set; }
    public bool Success { get; set; }
    public long TotalDurationMs { get; set; }
    public int StepCount { get; set; }
    public List<JourneyStep> Steps { get; set; } = new();
    public string? OutputPreview { get; set; }
}
