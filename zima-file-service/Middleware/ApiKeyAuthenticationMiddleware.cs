using System.Security.Cryptography;
using System.Text;
using ZimaFileService.Services;

namespace ZimaFileService.Middleware;

/// <summary>
/// Military-grade API authentication middleware using API Key + HMAC-SHA256 request signing.
/// Validates X-API-Key, X-API-Signature, and X-API-Timestamp headers.
/// Prevents tampering and replay attacks.
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly TimeSpan _timestampTolerance = TimeSpan.FromMinutes(5);
    private readonly HashSet<string> _excludedPaths;
    private readonly bool _enabled;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
        _apiKey = Environment.GetEnvironmentVariable("ZIMA_API_KEY") ?? "";
        _apiSecret = Environment.GetEnvironmentVariable("ZIMA_API_SECRET") ?? "";
        _enabled = !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_apiSecret);

        // Paths that don't require authentication (health checks, static files)
        _excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/health",
            "/up",
            "/downloads"
        };

        if (!_enabled)
        {
            Console.Error.WriteLine("[SECURITY WARNING] API authentication disabled - ZIMA_API_KEY or ZIMA_API_SECRET not set");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip authentication for excluded paths
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        // Skip authentication if not enabled (development mode)
        if (!_enabled)
        {
            await _next(context);
            return;
        }

        // Extract authentication headers
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var signature = context.Request.Headers["X-API-Signature"].FirstOrDefault();
        var timestamp = context.Request.Headers["X-API-Timestamp"].FirstOrDefault();

        // Validate all required headers are present
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
        {
            SecurityAuditLogger.Instance.LogAuthFailure(
                GetClientIp(context),
                "Missing authentication headers",
                path
            );

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Authentication required\",\"code\":\"MISSING_AUTH_HEADERS\"}");
            return;
        }

        // Validate API key using constant-time comparison
        if (!SecureCompare(apiKey, _apiKey))
        {
            SecurityAuditLogger.Instance.LogAuthFailure(
                GetClientIp(context),
                "Invalid API key",
                path
            );

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Invalid API key\",\"code\":\"INVALID_API_KEY\"}");
            return;
        }

        // Validate timestamp to prevent replay attacks
        if (!ValidateTimestamp(timestamp, out var requestTime))
        {
            SecurityAuditLogger.Instance.LogAuthFailure(
                GetClientIp(context),
                "Invalid or expired timestamp",
                path
            );

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Invalid or expired timestamp\",\"code\":\"TIMESTAMP_INVALID\"}");
            return;
        }

        // Build signature payload and validate HMAC signature
        var signaturePayload = await BuildSignaturePayload(context, timestamp);
        var expectedSignature = ComputeHmacSignature(signaturePayload);

        if (!SecureCompare(signature, expectedSignature))
        {
            SecurityAuditLogger.Instance.LogAuthFailure(
                GetClientIp(context),
                "Invalid signature",
                path
            );

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Invalid signature\",\"code\":\"SIGNATURE_INVALID\"}");
            return;
        }

        // Authentication successful
        SecurityAuditLogger.Instance.LogAuthSuccess(GetClientIp(context), path);

        await _next(context);
    }

    private bool IsExcludedPath(string path)
    {
        foreach (var excluded in _excludedPaths)
        {
            if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private bool ValidateTimestamp(string timestampStr, out DateTimeOffset requestTime)
    {
        requestTime = DateTimeOffset.MinValue;

        // Try parsing as Unix timestamp (seconds)
        if (long.TryParse(timestampStr, out var unixSeconds))
        {
            requestTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }
        // Try parsing as ISO 8601
        else if (DateTimeOffset.TryParse(timestampStr, out requestTime))
        {
            // Successfully parsed
        }
        else
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var timeDiff = now - requestTime;

        // Reject if timestamp is too old or too far in the future
        return timeDiff.Duration() <= _timestampTolerance;
    }

    private async Task<string> BuildSignaturePayload(HttpContext context, string timestamp)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";
        var query = context.Request.QueryString.Value ?? "";

        // For requests with body, include body hash
        var bodyHash = "";
        if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            using var sha256 = SHA256.Create();
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var hashBytes = sha256.ComputeHash(bodyBytes);
            bodyHash = Convert.ToHexString(hashBytes).ToLower();
        }

        // Payload format: METHOD\nPATH\nQUERY\nTIMESTAMP\nBODY_HASH
        return $"{method}\n{path}\n{query}\n{timestamp}\n{bodyHash}";
    }

    private string ComputeHmacSignature(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks
    /// </summary>
    private static bool SecureCompare(string a, string b)
    {
        if (a == null || b == null)
            return false;

        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static string GetClientIp(HttpContext context)
    {
        // Check for forwarded IP (behind proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take first IP in the chain
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Extension methods for registering the authentication middleware
/// </summary>
public static class ApiKeyAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
