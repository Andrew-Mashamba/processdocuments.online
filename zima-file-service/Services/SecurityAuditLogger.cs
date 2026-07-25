using System.Collections.Concurrent;
using System.Text.Json;

namespace ZimaFileService.Services;

/// <summary>
/// Centralized security audit logging service.
/// Logs authentication events, rate limit violations, and security incidents.
/// Thread-safe singleton implementation.
/// </summary>
public class SecurityAuditLogger
{
    private static readonly Lazy<SecurityAuditLogger> _instance =
        new(() => new SecurityAuditLogger());

    public static SecurityAuditLogger Instance => _instance.Value;

    private readonly string _logDirectory;
    private readonly object _fileLock = new();
    private readonly ConcurrentDictionary<string, int> _failedAttempts = new();
    private readonly int _lockoutThreshold = 5;
    private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lockedOutIps = new();

    private SecurityAuditLogger()
    {
        _logDirectory = Path.Combine(
            Environment.GetEnvironmentVariable("ZIMA_WORKING_DIR") ?? Directory.GetCurrentDirectory(),
            "logs", "security"
        );
        Directory.CreateDirectory(_logDirectory);

        // Start cleanup timer
        _ = new Timer(CleanupExpiredLockouts, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Check if an IP is currently locked out
    /// </summary>
    public bool IsLockedOut(string clientIp)
    {
        if (_lockedOutIps.TryGetValue(clientIp, out var lockoutTime))
        {
            if (DateTimeOffset.UtcNow < lockoutTime + _lockoutDuration)
            {
                return true;
            }
            // Lockout expired, remove it
            _lockedOutIps.TryRemove(clientIp, out _);
            _failedAttempts.TryRemove(clientIp, out _);
        }
        return false;
    }

    /// <summary>
    /// Log a successful authentication
    /// </summary>
    public void LogAuthSuccess(string clientIp, string path)
    {
        // Reset failed attempts on success
        _failedAttempts.TryRemove(clientIp, out _);

        var entry = new SecurityLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "AUTH_SUCCESS",
            ClientIp = clientIp,
            Path = path,
            Message = "Authentication successful"
        };

        WriteLogEntry(entry);
    }

    /// <summary>
    /// Log an authentication failure
    /// </summary>
    public void LogAuthFailure(string clientIp, string reason, string path)
    {
        // Increment failed attempts
        var attempts = _failedAttempts.AddOrUpdate(clientIp, 1, (_, count) => count + 1);

        // Check for lockout
        if (attempts >= _lockoutThreshold)
        {
            _lockedOutIps[clientIp] = DateTimeOffset.UtcNow;
            LogSecurityIncident(clientIp, "ACCOUNT_LOCKOUT",
                $"IP locked out after {attempts} failed authentication attempts");
        }

        var entry = new SecurityLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "AUTH_FAILURE",
            ClientIp = clientIp,
            Path = path,
            Message = reason,
            AdditionalData = new Dictionary<string, object>
            {
                ["failedAttempts"] = attempts,
                ["lockoutThreshold"] = _lockoutThreshold
            }
        };

        WriteLogEntry(entry);
        Console.Error.WriteLine($"[SECURITY] Auth failure from {clientIp}: {reason} (attempt {attempts}/{_lockoutThreshold})");
    }

    /// <summary>
    /// Log a rate limit hit
    /// </summary>
    public void LogRateLimitHit(string clientIp, string path, int retryAfterSeconds)
    {
        var entry = new SecurityLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "RATE_LIMIT_HIT",
            ClientIp = clientIp,
            Path = path,
            Message = $"Rate limit exceeded, retry after {retryAfterSeconds}s",
            AdditionalData = new Dictionary<string, object>
            {
                ["retryAfterSeconds"] = retryAfterSeconds
            }
        };

        WriteLogEntry(entry);
        Console.Error.WriteLine($"[SECURITY] Rate limit hit from {clientIp} on {path}");
    }

    /// <summary>
    /// Log a security incident (suspicious activity, attacks, etc.)
    /// </summary>
    public void LogSecurityIncident(string clientIp, string incidentType, string details)
    {
        var entry = new SecurityLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "SECURITY_INCIDENT",
            ClientIp = clientIp,
            Path = "",
            Message = details,
            AdditionalData = new Dictionary<string, object>
            {
                ["incidentType"] = incidentType
            }
        };

        WriteLogEntry(entry);
        Console.Error.WriteLine($"[SECURITY INCIDENT] {incidentType} from {clientIp}: {details}");
    }

    /// <summary>
    /// Log file operation for audit trail
    /// </summary>
    public void LogFileOperation(string clientIp, string operation, string filename, bool success, string? details = null)
    {
        var entry = new SecurityLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "FILE_OPERATION",
            ClientIp = clientIp,
            Path = filename,
            Message = $"{operation}: {(success ? "success" : "failed")}",
            AdditionalData = new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["success"] = success,
                ["details"] = details ?? ""
            }
        };

        WriteLogEntry(entry);
    }

    /// <summary>
    /// Log a blocked file upload attempt
    /// </summary>
    public void LogBlockedUpload(string clientIp, string filename, string reason)
    {
        var entry = new SecurityLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "BLOCKED_UPLOAD",
            ClientIp = clientIp,
            Path = filename,
            Message = reason,
            AdditionalData = new Dictionary<string, object>
            {
                ["filename"] = filename,
                ["blockReason"] = reason
            }
        };

        WriteLogEntry(entry);
        Console.Error.WriteLine($"[SECURITY] Blocked upload from {clientIp}: {filename} - {reason}");
    }

    private void WriteLogEntry(SecurityLogEntry entry)
    {
        var logFile = Path.Combine(_logDirectory, $"security_{DateTime.UtcNow:yyyy-MM-dd}.json");

        lock (_fileLock)
        {
            try
            {
                var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                File.AppendAllText(logFile, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SECURITY] Failed to write audit log: {ex.Message}");
            }
        }
    }

    private void CleanupExpiredLockouts(object? state)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _lockedOutIps
            .Where(kvp => now >= kvp.Value + _lockoutDuration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _lockedOutIps.TryRemove(key, out _);
            _failedAttempts.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// Security log entry structure
/// </summary>
public class SecurityLogEntry
{
    public DateTimeOffset Timestamp { get; set; }
    public string EventType { get; set; } = "";
    public string ClientIp { get; set; } = "";
    public string Path { get; set; } = "";
    public string Message { get; set; } = "";
    public Dictionary<string, object>? AdditionalData { get; set; }
}
