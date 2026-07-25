using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ZimaFileService.Api;

/// <summary>
/// Response cache for storing and retrieving cached prompt responses.
/// Reduces response time for repeated or similar prompts.
/// </summary>
public class ResponseCache
{
    private static readonly Lazy<ResponseCache> _instance = new(() => new ResponseCache());
    public static ResponseCache Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(30);
    private readonly int _maxCacheSize = 1000;
    private readonly Timer _cleanupTimer;

    // Cache statistics
    public int CacheHits { get; private set; }
    public int CacheMisses { get; private set; }
    public double HitRate => CacheHits + CacheMisses > 0
        ? (double)CacheHits / (CacheHits + CacheMisses) * 100
        : 0;

    private ResponseCache()
    {
        // Cleanup expired entries every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        Log("INFO", "ResponseCache initialized");
    }

    /// <summary>
    /// Generate a cache key from prompt and context.
    /// Uses SHA256 hash for consistent key generation.
    /// </summary>
    public static string GenerateCacheKey(string prompt, string? sessionId = null, int messageCount = 0)
    {
        // Normalize prompt (trim, lowercase for comparison)
        var normalizedPrompt = prompt.Trim().ToLowerInvariant();

        // Include context that affects the response
        var keySource = $"{normalizedPrompt}|{messageCount}";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
        return Convert.ToHexString(hashBytes)[..16]; // Use first 16 chars
    }

    /// <summary>
    /// Try to get a cached response.
    /// </summary>
    public bool TryGet(string cacheKey, out ZimaResponse? response)
    {
        if (_cache.TryGetValue(cacheKey, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                CacheHits++;
                response = entry.Response;
                Log("DEBUG", $"Cache HIT: {cacheKey} (hits: {CacheHits}, rate: {HitRate:F1}%)");
                return true;
            }

            // Entry expired, remove it
            _cache.TryRemove(cacheKey, out _);
        }

        CacheMisses++;
        response = null;
        Log("DEBUG", $"Cache MISS: {cacheKey} (misses: {CacheMisses}, rate: {HitRate:F1}%)");
        return false;
    }

    /// <summary>
    /// Store a response in the cache.
    /// </summary>
    public void Set(string cacheKey, ZimaResponse response, TimeSpan? ttl = null)
    {
        // Don't cache failed responses or responses with files (files may change)
        if (!response.Success || response.GeneratedFiles.Count > 0)
        {
            Log("DEBUG", $"Cache SKIP: {cacheKey} (success={response.Success}, files={response.GeneratedFiles.Count})");
            return;
        }

        // Enforce max cache size
        if (_cache.Count >= _maxCacheSize)
        {
            EvictOldestEntries(_maxCacheSize / 10); // Evict 10%
        }

        var entry = new CacheEntry
        {
            Response = response,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl ?? _defaultTtl),
            CacheKey = cacheKey
        };

        _cache[cacheKey] = entry;
        Log("DEBUG", $"Cache SET: {cacheKey} (size: {_cache.Count}, ttl: {(ttl ?? _defaultTtl).TotalMinutes}min)");
    }

    /// <summary>
    /// Check if a prompt is cacheable (simple queries are more cacheable).
    /// </summary>
    public static bool IsCacheable(string prompt)
    {
        var lowerPrompt = prompt.ToLower();

        // Don't cache file generation requests
        if (lowerPrompt.Contains("create") || lowerPrompt.Contains("generate") ||
            lowerPrompt.Contains("make") || lowerPrompt.Contains("build"))
            return false;

        // Don't cache modification requests
        if (lowerPrompt.Contains("update") || lowerPrompt.Contains("modify") ||
            lowerPrompt.Contains("change") || lowerPrompt.Contains("edit"))
            return false;

        // Cache simple questions and queries
        if (lowerPrompt.StartsWith("what") || lowerPrompt.StartsWith("how") ||
            lowerPrompt.StartsWith("why") || lowerPrompt.StartsWith("explain") ||
            lowerPrompt.StartsWith("list") || lowerPrompt.StartsWith("describe"))
            return true;

        // Cache short prompts (likely simple queries)
        if (prompt.Length < 100)
            return true;

        return false;
    }

    /// <summary>
    /// Get cache statistics.
    /// </summary>
    public CacheStats GetStats()
    {
        return new CacheStats
        {
            TotalEntries = _cache.Count,
            CacheHits = CacheHits,
            CacheMisses = CacheMisses,
            HitRate = HitRate,
            MaxSize = _maxCacheSize
        };
    }

    /// <summary>
    /// Clear the entire cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        CacheHits = 0;
        CacheMisses = 0;
        Log("INFO", "Cache cleared");
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            Log("DEBUG", $"Cache cleanup: removed {expiredKeys.Count} expired entries");
        }
    }

    private void EvictOldestEntries(int count)
    {
        var oldestKeys = _cache
            .OrderBy(kvp => kvp.Value.CreatedAt)
            .Take(count)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in oldestKeys)
        {
            _cache.TryRemove(key, out _);
        }

        Log("DEBUG", $"Cache eviction: removed {oldestKeys.Count} oldest entries");
    }

    private static void Log(string level, string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [CACHE] [{level}] {message}");
    }
}

public class CacheEntry
{
    public ZimaResponse Response { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string CacheKey { get; set; } = "";
}

public class CacheStats
{
    public int TotalEntries { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public double HitRate { get; set; }
    public int MaxSize { get; set; }
}
