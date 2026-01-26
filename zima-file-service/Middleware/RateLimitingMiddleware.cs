using System.Collections.Concurrent;
using ZimaFileService.Services;

namespace ZimaFileService.Middleware;

/// <summary>
/// Rate limiting middleware with per-IP tracking and endpoint-specific limits.
/// Uses sliding window algorithm for accurate rate limiting.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets = new();
    private readonly Timer _cleanupTimer;

    // Rate limit configurations per endpoint category
    private readonly Dictionary<string, RateLimitConfig> _configs = new()
    {
        // General API endpoints: 60 requests/minute
        { "default", new RateLimitConfig(60, TimeSpan.FromMinutes(1)) },

        // File upload endpoints: 20 requests/minute (more restrictive)
        { "upload", new RateLimitConfig(20, TimeSpan.FromMinutes(1)) },

        // Generate endpoints: 10 requests/minute (most restrictive due to resource usage)
        { "generate", new RateLimitConfig(10, TimeSpan.FromMinutes(1)) },

        // Stream endpoints: 5 concurrent per IP
        { "stream", new RateLimitConfig(5, TimeSpan.FromMinutes(1)) },

        // Tools endpoints: 30 requests/minute
        { "tools", new RateLimitConfig(30, TimeSpan.FromMinutes(1)) }
    };

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;

        // Clean up old entries every 5 minutes
        _cleanupTimer = new Timer(CleanupOldEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIp(context);
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var config = GetConfigForPath(path);
        var bucketKey = $"{clientIp}:{GetCategoryForPath(path)}";

        var bucket = _buckets.GetOrAdd(bucketKey, _ => new RateLimitBucket(config));

        // Check rate limit
        if (!bucket.TryAcquire())
        {
            SecurityAuditLogger.Instance.LogRateLimitHit(clientIp, path, bucket.RetryAfterSeconds);

            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";
            context.Response.Headers.Append("Retry-After", bucket.RetryAfterSeconds.ToString());
            context.Response.Headers.Append("X-RateLimit-Limit", config.MaxRequests.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", "0");
            context.Response.Headers.Append("X-RateLimit-Reset", bucket.ResetTime.ToUnixTimeSeconds().ToString());

            await context.Response.WriteAsync($"{{\"error\":\"Rate limit exceeded\",\"retryAfter\":{bucket.RetryAfterSeconds},\"code\":\"RATE_LIMIT_EXCEEDED\"}}");
            return;
        }

        // Add rate limit headers to response
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append("X-RateLimit-Limit", config.MaxRequests.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", bucket.RemainingRequests.ToString());
            context.Response.Headers.Append("X-RateLimit-Reset", bucket.ResetTime.ToUnixTimeSeconds().ToString());
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private string GetCategoryForPath(string path)
    {
        if (path.Contains("/upload")) return "upload";
        if (path.Contains("/generate/stream")) return "stream";
        if (path.Contains("/generate")) return "generate";
        if (path.Contains("/tools")) return "tools";
        return "default";
    }

    private RateLimitConfig GetConfigForPath(string path)
    {
        var category = GetCategoryForPath(path);
        return _configs.GetValueOrDefault(category, _configs["default"]);
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private void CleanupOldEntries(object? state)
    {
        var expiredKeys = _buckets
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _buckets.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// Rate limit configuration
/// </summary>
public class RateLimitConfig
{
    public int MaxRequests { get; }
    public TimeSpan Window { get; }

    public RateLimitConfig(int maxRequests, TimeSpan window)
    {
        MaxRequests = maxRequests;
        Window = window;
    }
}

/// <summary>
/// Sliding window rate limit bucket
/// </summary>
public class RateLimitBucket
{
    private readonly RateLimitConfig _config;
    private readonly object _lock = new();
    private readonly Queue<DateTimeOffset> _requests = new();
    private DateTimeOffset _lastAccess = DateTimeOffset.UtcNow;

    public RateLimitBucket(RateLimitConfig config)
    {
        _config = config;
    }

    public bool TryAcquire()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            _lastAccess = now;

            // Remove requests outside the window
            while (_requests.Count > 0 && _requests.Peek() < now - _config.Window)
            {
                _requests.Dequeue();
            }

            // Check if we can make a new request
            if (_requests.Count >= _config.MaxRequests)
            {
                return false;
            }

            _requests.Enqueue(now);
            return true;
        }
    }

    public int RemainingRequests
    {
        get
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;
                var count = _requests.Count(r => r >= now - _config.Window);
                return Math.Max(0, _config.MaxRequests - count);
            }
        }
    }

    public int RetryAfterSeconds
    {
        get
        {
            lock (_lock)
            {
                if (_requests.Count == 0)
                    return 0;

                var oldestInWindow = _requests.Peek();
                var resetTime = oldestInWindow + _config.Window;
                var remaining = resetTime - DateTimeOffset.UtcNow;
                return Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
            }
        }
    }

    public DateTimeOffset ResetTime
    {
        get
        {
            lock (_lock)
            {
                if (_requests.Count == 0)
                    return DateTimeOffset.UtcNow;

                return _requests.Peek() + _config.Window;
            }
        }
    }

    public bool IsExpired => DateTimeOffset.UtcNow - _lastAccess > TimeSpan.FromHours(1);
}

/// <summary>
/// Extension methods for registering the rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
