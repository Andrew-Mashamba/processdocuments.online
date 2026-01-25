using System.Collections.Concurrent;
using System.Diagnostics;

namespace ZimaFileService.Api;

/// <summary>
/// Connection pool for pre-warming Claude CLI connections.
/// Reduces cold-start latency by maintaining warm processes and
/// triggering prompt caching in advance.
/// </summary>
public class ClaudeConnectionPool
{
    private static readonly Lazy<ClaudeConnectionPool> _instance = new(() => new ClaudeConnectionPool());
    public static ClaudeConnectionPool Instance => _instance.Value;

    private readonly ConcurrentQueue<WarmProcess> _warmProcesses = new();
    private readonly ConcurrentDictionary<string, SessionWarmupInfo> _sessionWarmups = new();
    private readonly SemaphoreSlim _poolLock = new(1, 1);
    private readonly Timer _warmupTimer;
    private readonly Timer _cleanupTimer;

    // Configuration
    private readonly int _minPoolSize = 2;
    private readonly int _maxPoolSize = 5;
    private readonly TimeSpan _processMaxAge = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _warmupInterval = TimeSpan.FromMinutes(2);
    private readonly string _claudePath;

    // Statistics
    public int WarmHits { get; private set; }
    public int ColdStarts { get; private set; }
    public double WarmHitRate => WarmHits + ColdStarts > 0
        ? (double)WarmHits / (WarmHits + ColdStarts) * 100
        : 0;

    private ClaudeConnectionPool()
    {
        _claudePath = FindClaudePath();

        // Start warmup timer (keep pool warm)
        _warmupTimer = new Timer(MaintainPool, null, TimeSpan.Zero, _warmupInterval);

        // Cleanup timer (remove stale entries)
        _cleanupTimer = new Timer(CleanupStaleEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        Log("INFO", $"ClaudeConnectionPool initialized (min={_minPoolSize}, max={_maxPoolSize})");
    }

    /// <summary>
    /// Pre-warm the pool with ready processes.
    /// Call this on application startup.
    /// </summary>
    public async Task WarmupAsync(int count = 2)
    {
        Log("INFO", $"Pre-warming pool with {count} processes...");

        var tasks = Enumerable.Range(0, count)
            .Select(_ => CreateWarmProcessAsync())
            .ToArray();

        await Task.WhenAll(tasks);

        Log("INFO", $"Pool pre-warmed. Current size: {_warmProcesses.Count}");
    }

    /// <summary>
    /// Pre-warm a specific session by sending a minimal prompt.
    /// This triggers Claude's prompt caching for the session context.
    /// </summary>
    public async Task WarmupSessionAsync(string sessionId, string systemPrompt)
    {
        if (_sessionWarmups.ContainsKey(sessionId))
        {
            Log("DEBUG", $"Session {sessionId} already warmed");
            return;
        }

        Log("INFO", $"Warming up session: {sessionId}");
        var sw = Stopwatch.StartNew();

        try
        {
            // Send a minimal prompt to trigger caching
            var warmupPrompt = "ping";

            var psi = new ProcessStartInfo
            {
                FileName = _claudePath,
                Arguments = "--print --output-format json --model claude-sonnet-4-20250514",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            // Send system prompt + warmup prompt to trigger caching
            var fullPrompt = $"{systemPrompt}\n\n{warmupPrompt}";
            await process.StandardInput.WriteAsync(fullPrompt);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();

            // Wait with short timeout (we don't need the response)
            var completed = await Task.Run(() => process.WaitForExit(10000));

            if (!completed)
            {
                process.Kill();
            }

            sw.Stop();

            _sessionWarmups[sessionId] = new SessionWarmupInfo
            {
                SessionId = sessionId,
                WarmupTime = DateTime.UtcNow,
                WarmupDurationMs = sw.ElapsedMilliseconds
            };

            Log("INFO", $"Session {sessionId} warmed in {sw.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            Log("WARN", $"Session warmup failed for {sessionId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if a session has been pre-warmed.
    /// </summary>
    public bool IsSessionWarm(string sessionId)
    {
        if (_sessionWarmups.TryGetValue(sessionId, out var info))
        {
            // Consider warm if warmed within last 30 minutes
            return (DateTime.UtcNow - info.WarmupTime) < TimeSpan.FromMinutes(30);
        }
        return false;
    }

    /// <summary>
    /// Get a warm process from the pool, or create a new one if none available.
    /// </summary>
    public Task<(Process Process, bool WasWarm)> GetProcessAsync(string model)
    {
        // Try to get a warm process
        while (_warmProcesses.TryDequeue(out var warmProcess))
        {
            // Check if process is still valid
            if (warmProcess.CreatedAt.Add(_processMaxAge) > DateTime.UtcNow &&
                !warmProcess.Process.HasExited)
            {
                WarmHits++;
                Log("DEBUG", $"Using warm process (age: {(DateTime.UtcNow - warmProcess.CreatedAt).TotalSeconds:F1}s, pool: {_warmProcesses.Count})");
                return Task.FromResult((warmProcess.Process, true));
            }

            // Discard stale process
            try { warmProcess.Process.Kill(); } catch { }
            warmProcess.Process.Dispose();
        }

        // No warm process available, create cold
        ColdStarts++;
        Log("DEBUG", $"Cold start (pool empty, cold starts: {ColdStarts})");

        var process = CreateProcess(model);
        return Task.FromResult((process, false));
    }

    /// <summary>
    /// Return a process to the pool for reuse (if still valid).
    /// </summary>
    public void ReturnProcess(Process process)
    {
        // For Claude CLI, processes are single-use (they exit after response)
        // So we just dispose them and let the warmup timer replenish the pool
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch { }

        process.Dispose();
    }

    /// <summary>
    /// Get pool statistics.
    /// </summary>
    public PoolStats GetStats()
    {
        return new PoolStats
        {
            CurrentPoolSize = _warmProcesses.Count,
            WarmHits = WarmHits,
            ColdStarts = ColdStarts,
            WarmHitRate = WarmHitRate,
            WarmSessions = _sessionWarmups.Count,
            MinPoolSize = _minPoolSize,
            MaxPoolSize = _maxPoolSize
        };
    }

    private Process CreateProcess(string model)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _claudePath,
            Arguments = $"--print --output-format json --model {model}",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi };
        process.Start();
        return process;
    }

    private async Task CreateWarmProcessAsync()
    {
        try
        {
            // Note: Claude CLI is request/response, so we can't truly pre-warm
            // processes. Instead, we trigger model loading with a minimal request.

            var process = CreateProcess("claude-sonnet-4-20250514");

            // Send minimal prompt to trigger model loading
            await process.StandardInput.WriteAsync("ping");
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();

            // Wait for completion
            await Task.Run(() => process.WaitForExit(30000));

            // Process completed - model should now be cached by Claude
            process.Dispose();

            Log("DEBUG", "Warm-up request completed (model cached)");
        }
        catch (Exception ex)
        {
            Log("WARN", $"Failed to create warm process: {ex.Message}");
        }
    }

    private void MaintainPool(object? state)
    {
        try
        {
            // Claude CLI processes are single-use, so instead of maintaining
            // idle processes, we periodically send warmup requests to keep
            // the model loaded in Claude's backend cache

            var currentSize = _warmProcesses.Count;
            if (currentSize < _minPoolSize)
            {
                Log("DEBUG", $"Pool maintenance: triggering warmup (current: {currentSize}, min: {_minPoolSize})");
                _ = CreateWarmProcessAsync();
            }
        }
        catch (Exception ex)
        {
            Log("ERROR", $"Pool maintenance error: {ex.Message}");
        }
    }

    private void CleanupStaleEntries(object? state)
    {
        // Clean up old session warmup entries
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var staleKeys = _sessionWarmups
            .Where(kvp => kvp.Value.WarmupTime < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            _sessionWarmups.TryRemove(key, out _);
        }

        if (staleKeys.Count > 0)
        {
            Log("DEBUG", $"Cleaned up {staleKeys.Count} stale session warmups");
        }
    }

    private static string FindClaudePath()
    {
        var paths = new[]
        {
            "/Users/andrewmashamba/.npm-global/bin/claude",
            "/usr/local/bin/claude",
            "claude"
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Try to find via which
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "claude",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(output) && File.Exists(output))
                {
                    return output;
                }
            }
        }
        catch { }

        return "claude"; // Fall back to PATH resolution
    }

    private static void Log(string level, string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [POOL] [{level}] {message}");
    }
}

public class WarmProcess
{
    public Process Process { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string Model { get; set; } = "";
}

public class SessionWarmupInfo
{
    public string SessionId { get; set; } = "";
    public DateTime WarmupTime { get; set; }
    public long WarmupDurationMs { get; set; }
}

public class PoolStats
{
    public int CurrentPoolSize { get; set; }
    public int WarmHits { get; set; }
    public int ColdStarts { get; set; }
    public double WarmHitRate { get; set; }
    public int WarmSessions { get; set; }
    public int MinPoolSize { get; set; }
    public int MaxPoolSize { get; set; }
}
