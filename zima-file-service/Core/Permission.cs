using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ZimaFileService.Core;

/// <summary>
/// Permission system for controlling tool access.
/// Modeled after OpenCode's permission/next.ts
/// </summary>
public static class Permission
{
    private static readonly List<PermissionRule> _globalRules = new();
    private static readonly Dictionary<string, List<PermissionRule>> _sessionRules = new();
    private static readonly Dictionary<string, PermissionApproval> _approvals = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Permission actions.
    /// </summary>
    public enum Action
    {
        Allow,
        Deny,
        Ask
    }

    /// <summary>
    /// Initialize default permission rules.
    /// </summary>
    static Permission()
    {
        // Default rules - allow most file operations
        AddGlobalRule("create_word", "*", Action.Allow);
        AddGlobalRule("create_excel", "*", Action.Allow);
        AddGlobalRule("create_pdf", "*", Action.Allow);
        AddGlobalRule("create_powerpoint", "*", Action.Allow);
        AddGlobalRule("read_excel", "*", Action.Allow);
        AddGlobalRule("list_files", "*", Action.Allow);
        AddGlobalRule("get_file_info", "*", Action.Allow);

        // Potentially destructive operations - ask first
        AddGlobalRule("delete_file", "*", Action.Ask);
        AddGlobalRule("move_file", "*", Action.Ask);

        // Default fallback - allow
        AddGlobalRule("*", "*", Action.Allow);
    }

    /// <summary>
    /// Add a global permission rule.
    /// </summary>
    public static void AddGlobalRule(string permission, string pattern, Action action)
    {
        lock (_lock)
        {
            _globalRules.Add(new PermissionRule(permission, pattern, action));
        }
    }

    /// <summary>
    /// Add a session-specific permission rule.
    /// </summary>
    public static void AddSessionRule(string sessionId, string permission, string pattern, Action action)
    {
        lock (_lock)
        {
            if (!_sessionRules.ContainsKey(sessionId))
            {
                _sessionRules[sessionId] = new List<PermissionRule>();
            }
            _sessionRules[sessionId].Add(new PermissionRule(permission, pattern, action));
        }
    }

    /// <summary>
    /// Clear session-specific rules.
    /// </summary>
    public static void ClearSessionRules(string sessionId)
    {
        lock (_lock)
        {
            _sessionRules.Remove(sessionId);
        }
    }

    /// <summary>
    /// Evaluate if an action is permitted.
    /// </summary>
    public static PermissionResult Evaluate(string permission, string value, string? sessionId = null)
    {
        lock (_lock)
        {
            // Check session-specific rules first
            if (sessionId != null && _sessionRules.TryGetValue(sessionId, out var sessionRules))
            {
                foreach (var rule in sessionRules)
                {
                    if (MatchesRule(rule, permission, value))
                    {
                        return new PermissionResult(rule.Action, rule);
                    }
                }
            }

            // Check global rules
            foreach (var rule in _globalRules)
            {
                if (MatchesRule(rule, permission, value))
                {
                    return new PermissionResult(rule.Action, rule);
                }
            }

            // Default: allow
            return new PermissionResult(Action.Allow, null);
        }
    }

    /// <summary>
    /// Check if a rule matches the permission and value.
    /// </summary>
    private static bool MatchesRule(PermissionRule rule, string permission, string value)
    {
        // Check permission match
        if (rule.Permission != "*" && !WildcardMatch(rule.Permission, permission))
        {
            return false;
        }

        // Check pattern match
        if (rule.Pattern != "*" && !WildcardMatch(rule.Pattern, value))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Simple wildcard matching (* and ?).
    /// </summary>
    private static bool WildcardMatch(string pattern, string value)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Request permission (for Ask actions).
    /// Returns true if granted, false if denied.
    /// </summary>
    public static async Task<bool> RequestAsync(PermissionRequest request)
    {
        var result = Evaluate(request.Permission, request.Value, request.SessionId);

        if (result.Action == Action.Allow)
        {
            EventBus.Publish(Events.PermissionGranted, new PermissionEvent(
                request.Permission, request.Value, request.SessionId ?? "", Granted: true));
            return true;
        }

        if (result.Action == Action.Deny)
        {
            EventBus.Publish(Events.PermissionDenied, new PermissionEvent(
                request.Permission, request.Value, request.SessionId ?? "", Granted: false));
            return false;
        }

        // Action == Ask - check for prior approval
        var approvalKey = $"{request.SessionId}:{request.Permission}:{request.Value}";
        lock (_lock)
        {
            if (_approvals.TryGetValue(approvalKey, out var approval))
            {
                if (approval.ExpiresAt > DateTime.UtcNow)
                {
                    EventBus.Publish(approval.Granted ? Events.PermissionGranted : Events.PermissionDenied,
                        new PermissionEvent(request.Permission, request.Value, request.SessionId ?? "", Granted: approval.Granted));
                    return approval.Granted;
                }
                _approvals.Remove(approvalKey);
            }
        }

        // For now, auto-approve in non-interactive mode
        // In a full implementation, this would prompt the user
        EventBus.Publish(Events.PermissionRequested, new PermissionEvent(
            request.Permission, request.Value, request.SessionId ?? ""));

        // Auto-approve for ZIMA (file generation should proceed)
        var granted = true;

        // Store approval for this session
        lock (_lock)
        {
            _approvals[approvalKey] = new PermissionApproval(
                granted,
                DateTime.UtcNow.AddHours(1) // 1 hour expiry
            );
        }

        EventBus.Publish(granted ? Events.PermissionGranted : Events.PermissionDenied,
            new PermissionEvent(request.Permission, request.Value, request.SessionId ?? "", Granted: granted));

        return granted;
    }

    /// <summary>
    /// Load permission rules from a JSON file.
    /// </summary>
    public static async Task LoadFromFileAsync(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var rules = JsonSerializer.Deserialize<List<PermissionRule>>(json);

            if (rules != null)
            {
                lock (_lock)
                {
                    foreach (var rule in rules)
                    {
                        _globalRules.Insert(0, rule); // Insert at beginning for priority
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Permission] Error loading rules from {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Save current rules to a JSON file.
    /// </summary>
    public static async Task SaveToFileAsync(string path)
    {
        try
        {
            List<PermissionRule> rules;
            lock (_lock)
            {
                rules = new List<PermissionRule>(_globalRules);
            }

            var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Permission] Error saving rules to {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all current rules.
    /// </summary>
    public static List<PermissionRule> GetRules()
    {
        lock (_lock)
        {
            return new List<PermissionRule>(_globalRules);
        }
    }
}

/// <summary>
/// A permission rule definition.
/// </summary>
public record PermissionRule(
    [property: JsonPropertyName("permission")] string Permission,
    [property: JsonPropertyName("pattern")] string Pattern,
    [property: JsonPropertyName("action")] Permission.Action Action
);

/// <summary>
/// Result of a permission evaluation.
/// </summary>
public record PermissionResult(
    Permission.Action Action,
    PermissionRule? MatchedRule
);

/// <summary>
/// A permission request.
/// </summary>
public record PermissionRequest(
    string Permission,
    string Value,
    string? SessionId = null,
    string? ToolId = null,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Stored permission approval.
/// </summary>
public record PermissionApproval(
    bool Granted,
    DateTime ExpiresAt
);
