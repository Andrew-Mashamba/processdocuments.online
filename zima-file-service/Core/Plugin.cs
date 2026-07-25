using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZimaFileService.Core;

/// <summary>
/// Plugin system for extending ZIMA functionality.
/// Modeled after OpenCode's hook/plugin system.
/// </summary>
public static class PluginManager
{
    private static readonly Dictionary<string, PluginInfo> _plugins = new();
    private static readonly List<HookRegistration> _hooks = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Hook types that can be registered.
    /// </summary>
    public static class HookTypes
    {
        // Tool hooks
        public const string BeforeToolExecute = "before:tool:execute";
        public const string AfterToolExecute = "after:tool:execute";
        public const string OnToolError = "on:tool:error";

        // Session hooks
        public const string BeforeSessionCreate = "before:session:create";
        public const string AfterSessionCreate = "after:session:create";
        public const string BeforeSessionFork = "before:session:fork";
        public const string AfterSessionFork = "after:session:fork";

        // Message hooks
        public const string BeforeMessageSend = "before:message:send";
        public const string AfterMessageReceive = "after:message:receive";

        // File hooks
        public const string BeforeFileCreate = "before:file:create";
        public const string AfterFileCreate = "after:file:create";
        public const string BeforeFileDelete = "before:file:delete";
        public const string AfterFileDelete = "after:file:delete";

        // Agent hooks
        public const string BeforeAgentSwitch = "before:agent:switch";
        public const string AfterAgentSwitch = "after:agent:switch";

        // Permission hooks
        public const string OnPermissionRequest = "on:permission:request";
        public const string OnPermissionDecision = "on:permission:decision";
    }

    /// <summary>
    /// Register a plugin.
    /// </summary>
    public static void Register(PluginInfo plugin)
    {
        lock (_lock)
        {
            _plugins[plugin.Name] = plugin;

            // Register plugin hooks
            if (plugin.Hooks != null)
            {
                foreach (var hook in plugin.Hooks)
                {
                    _hooks.Add(new HookRegistration(
                        plugin.Name,
                        hook.Type,
                        hook.Handler,
                        hook.Priority
                    ));
                }

                // Sort hooks by priority
                _hooks.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }

        EventBus.Publish(Events.PluginLoaded, new PluginEvent(plugin.Name, "loaded"));
    }

    /// <summary>
    /// Unregister a plugin.
    /// </summary>
    public static void Unregister(string name)
    {
        lock (_lock)
        {
            _plugins.Remove(name);
            _hooks.RemoveAll(h => h.PluginName == name);
        }

        EventBus.Publish(Events.PluginUnloaded, new PluginEvent(name, "unloaded"));
    }

    /// <summary>
    /// Get a plugin by name.
    /// </summary>
    public static PluginInfo? Get(string name)
    {
        lock (_lock)
        {
            return _plugins.TryGetValue(name, out var plugin) ? plugin : null;
        }
    }

    /// <summary>
    /// List all plugins.
    /// </summary>
    public static List<PluginInfo> List()
    {
        lock (_lock)
        {
            return _plugins.Values.ToList();
        }
    }

    /// <summary>
    /// Register a standalone hook (without a full plugin).
    /// </summary>
    public static void RegisterHook(string hookType, Func<HookContext, Task<HookResult>> handler, int priority = 0)
    {
        lock (_lock)
        {
            _hooks.Add(new HookRegistration("_standalone", hookType, handler, priority));
            _hooks.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
    }

    /// <summary>
    /// Execute hooks of a specific type.
    /// </summary>
    public static async Task<HookChainResult> ExecuteHooksAsync(string hookType, HookContext context)
    {
        List<HookRegistration> matchingHooks;

        lock (_lock)
        {
            matchingHooks = _hooks.Where(h => h.HookType == hookType).ToList();
        }

        var results = new List<HookResult>();
        var continueChain = true;
        object? modifiedData = context.Data;

        foreach (var hook in matchingHooks)
        {
            if (!continueChain) break;

            try
            {
                var hookContext = new HookContext(
                    hookType,
                    modifiedData,
                    context.SessionId,
                    context.Metadata
                );

                var result = await hook.Handler(hookContext);
                results.Add(result);

                if (result.ModifiedData != null)
                {
                    modifiedData = result.ModifiedData;
                }

                if (result.Action == HookAction.Stop)
                {
                    continueChain = false;
                }
                else if (result.Action == HookAction.Skip)
                {
                    // Skip remaining hooks but continue main operation
                    break;
                }
            }
            catch (Exception ex)
            {
                EventBus.Publish(Events.PluginError, new PluginErrorEvent(
                    hook.PluginName,
                    hookType,
                    ex.Message
                ));

                results.Add(new HookResult(HookAction.Continue, null, ex.Message));
            }
        }

        return new HookChainResult(
            continueChain,
            modifiedData,
            results
        );
    }

    /// <summary>
    /// Load plugins from a directory.
    /// </summary>
    public static async Task LoadFromDirectoryAsync(string path)
    {
        if (!Directory.Exists(path)) return;

        var files = Directory.GetFiles(path, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var plugin = JsonSerializer.Deserialize<PluginInfo>(json);

                if (plugin != null && plugin.Enabled)
                {
                    // Note: For JSON-defined plugins, handlers would need to be
                    // mapped from shell commands or script paths
                    RegisterDefinition(plugin);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Plugin] Error loading {file}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Register a plugin definition without handlers (for config-based plugins).
    /// </summary>
    public static void RegisterDefinition(PluginInfo plugin)
    {
        lock (_lock)
        {
            _plugins[plugin.Name] = plugin;
        }
    }

    /// <summary>
    /// Check if a plugin is enabled.
    /// </summary>
    public static bool IsEnabled(string name)
    {
        lock (_lock)
        {
            return _plugins.TryGetValue(name, out var plugin) && plugin.Enabled;
        }
    }

    /// <summary>
    /// Enable a plugin.
    /// </summary>
    public static void Enable(string name)
    {
        lock (_lock)
        {
            if (_plugins.TryGetValue(name, out var plugin))
            {
                plugin.Enabled = true;
            }
        }
    }

    /// <summary>
    /// Disable a plugin.
    /// </summary>
    public static void Disable(string name)
    {
        lock (_lock)
        {
            if (_plugins.TryGetValue(name, out var plugin))
            {
                plugin.Enabled = false;
            }
        }
    }

    /// <summary>
    /// Get plugin count.
    /// </summary>
    public static int Count
    {
        get
        {
            lock (_lock)
            {
                return _plugins.Count;
            }
        }
    }

    /// <summary>
    /// Clear all plugins and hooks.
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _plugins.Clear();
            _hooks.Clear();
        }
    }
}

/// <summary>
/// Plugin definition.
/// </summary>
public class PluginInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("hooks")]
    public List<HookDefinition>? Hooks { get; set; }

    [JsonPropertyName("settings")]
    public Dictionary<string, object>? Settings { get; set; }
}

/// <summary>
/// Hook definition within a plugin.
/// </summary>
public class HookDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonIgnore]
    public Func<HookContext, Task<HookResult>> Handler { get; set; } = _ => Task.FromResult(new HookResult(HookAction.Continue));
}

/// <summary>
/// Internal hook registration.
/// </summary>
public record HookRegistration(
    string PluginName,
    string HookType,
    Func<HookContext, Task<HookResult>> Handler,
    int Priority
);

/// <summary>
/// Context passed to hook handlers.
/// </summary>
public record HookContext(
    string HookType,
    object? Data,
    string? SessionId = null,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Result from a hook handler.
/// </summary>
public record HookResult(
    HookAction Action,
    object? ModifiedData = null,
    string? Error = null
);

/// <summary>
/// Result from executing a hook chain.
/// </summary>
public record HookChainResult(
    bool Continue,
    object? FinalData,
    List<HookResult> Results
);

/// <summary>
/// Actions a hook can take.
/// </summary>
public enum HookAction
{
    Continue,  // Continue to next hook and main operation
    Skip,      // Skip remaining hooks but continue main operation
    Stop       // Stop everything (abort operation)
}

/// <summary>
/// Plugin event payload.
/// </summary>
public record PluginEvent(
    string PluginName,
    string Action
);

/// <summary>
/// Plugin error event payload.
/// </summary>
public record PluginErrorEvent(
    string PluginName,
    string HookType,
    string Error
);
