namespace ZimaFileService.Middleware;

/// <summary>
/// Security headers middleware adding comprehensive protection headers.
/// Implements CSP, HSTS, X-Frame-Options, and other security headers.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _allowedOrigins;
    private readonly bool _isProduction;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
        _allowedOrigins = Environment.GetEnvironmentVariable("ZIMA_ALLOWED_ORIGINS") ?? "*";
        _isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() == "production";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before passing to next middleware
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent MIME type sniffing
            headers.Append("X-Content-Type-Options", "nosniff");

            // Prevent clickjacking
            headers.Append("X-Frame-Options", "DENY");

            // Enable XSS filter (legacy browsers)
            headers.Append("X-XSS-Protection", "1; mode=block");

            // Control referrer information
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Disable permissions/features not needed
            headers.Append("Permissions-Policy",
                "accelerometer=(), " +
                "camera=(), " +
                "geolocation=(), " +
                "gyroscope=(), " +
                "magnetometer=(), " +
                "microphone=(), " +
                "payment=(), " +
                "usb=()");

            // Content Security Policy - restrictive for API
            var csp = BuildContentSecurityPolicy();
            headers.Append("Content-Security-Policy", csp);

            // HSTS - only in production with HTTPS
            if (_isProduction)
            {
                // max-age = 1 year, include subdomains, allow preload list submission
                headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            }

            // Prevent caching of sensitive responses
            if (IsSensitiveEndpoint(context.Request.Path.Value ?? ""))
            {
                headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
                headers.Append("Pragma", "no-cache");
                headers.Append("Expires", "0");
            }

            // Add unique request ID for tracking
            if (!headers.ContainsKey("X-Request-ID"))
            {
                headers.Append("X-Request-ID", context.TraceIdentifier);
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }

    private string BuildContentSecurityPolicy()
    {
        // Restrictive CSP for API server
        // - No inline scripts or styles (XSS prevention)
        // - No object/embed tags
        // - Self-hosted resources only (with exception for necessary origins)
        var cspParts = new[]
        {
            "default-src 'self'",
            "script-src 'self'",
            "style-src 'self'",
            "img-src 'self' data:",
            "font-src 'self'",
            "connect-src 'self'",
            "media-src 'self'",
            "object-src 'none'",
            "frame-ancestors 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "upgrade-insecure-requests"
        };

        return string.Join("; ", cspParts);
    }

    private static bool IsSensitiveEndpoint(string path)
    {
        var sensitiveEndpoints = new[]
        {
            "/api/generate",
            "/api/files/upload",
            "/api/files/session",
            "/api/tools"
        };

        return sensitiveEndpoints.Any(e => path.StartsWith(e, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for registering the security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
