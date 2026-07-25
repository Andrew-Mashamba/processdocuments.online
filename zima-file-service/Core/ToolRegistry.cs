using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZimaFileService.Core;

/// <summary>
/// Dynamic Tool Registry for managing tool definitions.
/// Modeled after OpenCode's tool system.
/// </summary>
public static class ToolRegistry
{
    private static readonly Dictionary<string, ToolDefinition> _tools = new();
    private static readonly Dictionary<string, Func<Dictionary<string, object>, Task<string>>> _handlers = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Register a tool with its definition and handler.
    /// </summary>
    public static void Register(ToolDefinition definition, Func<Dictionary<string, object>, Task<string>> handler)
    {
        lock (_lock)
        {
            _tools[definition.Name] = definition;
            _handlers[definition.Name] = handler;
        }

        EventBus.Publish(Events.ToolRegistered, new ToolRegistrationEvent(definition.Name, definition));
    }

    /// <summary>
    /// Register a tool definition only (handler will be added later).
    /// </summary>
    public static void RegisterDefinition(ToolDefinition definition)
    {
        lock (_lock)
        {
            _tools[definition.Name] = definition;
        }
    }

    /// <summary>
    /// Register a handler for an existing tool.
    /// </summary>
    public static void RegisterHandler(string name, Func<Dictionary<string, object>, Task<string>> handler)
    {
        lock (_lock)
        {
            if (_tools.ContainsKey(name))
            {
                _handlers[name] = handler;
            }
        }
    }

    /// <summary>
    /// Unregister a tool.
    /// </summary>
    public static void Unregister(string name)
    {
        lock (_lock)
        {
            _tools.Remove(name);
            _handlers.Remove(name);
        }

        EventBus.Publish(Events.ToolUnregistered, new { ToolName = name });
    }

    /// <summary>
    /// Get a tool definition by name.
    /// </summary>
    public static ToolDefinition? Get(string name)
    {
        lock (_lock)
        {
            return _tools.TryGetValue(name, out var tool) ? tool : null;
        }
    }

    /// <summary>
    /// Get a tool handler by name.
    /// </summary>
    public static Func<Dictionary<string, object>, Task<string>>? GetHandler(string name)
    {
        lock (_lock)
        {
            return _handlers.TryGetValue(name, out var handler) ? handler : null;
        }
    }

    /// <summary>
    /// Check if a tool exists.
    /// </summary>
    public static bool Exists(string name)
    {
        lock (_lock)
        {
            return _tools.ContainsKey(name);
        }
    }

    /// <summary>
    /// List all registered tools.
    /// </summary>
    public static List<ToolDefinition> List()
    {
        lock (_lock)
        {
            return _tools.Values.ToList();
        }
    }

    /// <summary>
    /// List tools by category.
    /// </summary>
    public static List<ToolDefinition> ListByCategory(string category)
    {
        lock (_lock)
        {
            return _tools.Values
                .Where(t => t.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }
    }

    /// <summary>
    /// Get all categories.
    /// </summary>
    public static List<string> GetCategories()
    {
        lock (_lock)
        {
            return _tools.Values
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }

    /// <summary>
    /// Execute a tool by name.
    /// </summary>
    public static async Task<ToolResult> ExecuteAsync(string name, Dictionary<string, object> args, string? sessionId = null)
    {
        var tool = Get(name);
        if (tool == null)
        {
            return new ToolResult(false, null, $"Tool '{name}' not found");
        }

        var handler = GetHandler(name);
        if (handler == null)
        {
            return new ToolResult(false, null, $"No handler registered for tool '{name}'");
        }

        // Check permissions
        var permitted = await Permission.RequestAsync(new PermissionRequest(name, "*", sessionId));
        if (!permitted)
        {
            return new ToolResult(false, null, $"Permission denied for tool '{name}'");
        }

        // Publish before event
        EventBus.Publish(Events.ToolExecuteBefore, new ToolExecutionEvent(name, sessionId ?? "", null, args));

        try
        {
            var result = await handler(args);

            // Publish after event
            EventBus.Publish(Events.ToolExecuteAfter, new ToolExecutionEvent(name, sessionId ?? "", null, args, result));

            return new ToolResult(true, result, null);
        }
        catch (Exception ex)
        {
            // Publish error event
            EventBus.Publish(Events.ToolExecuteAfter, new ToolExecutionEvent(name, sessionId ?? "", null, args, null, ex.Message));

            return new ToolResult(false, null, ex.Message);
        }
    }

    /// <summary>
    /// Load tool definitions from a JSON file.
    /// </summary>
    public static async Task LoadFromFileAsync(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var tools = JsonSerializer.Deserialize<List<ToolDefinition>>(json);

            if (tools != null)
            {
                foreach (var tool in tools)
                {
                    RegisterDefinition(tool);
                }
                Console.WriteLine($"[ToolRegistry] Loaded {tools.Count} tools from {path}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToolRegistry] Error loading tools from {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Load tool definitions from a directory.
    /// </summary>
    public static async Task LoadFromDirectoryAsync(string path)
    {
        if (!Directory.Exists(path)) return;

        var files = Directory.GetFiles(path, "*.json");
        foreach (var file in files)
        {
            await LoadFromFileAsync(file);
        }
    }

    /// <summary>
    /// Save current tool definitions to a JSON file.
    /// </summary>
    public static async Task SaveToFileAsync(string path)
    {
        try
        {
            List<ToolDefinition> tools;
            lock (_lock)
            {
                tools = _tools.Values.ToList();
            }

            var json = JsonSerializer.Serialize(tools, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToolRegistry] Error saving tools to {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Get tool count.
    /// </summary>
    public static int Count
    {
        get
        {
            lock (_lock)
            {
                return _tools.Count;
            }
        }
    }

    /// <summary>
    /// Clear all tools.
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _tools.Clear();
            _handlers.Clear();
        }
    }

    /// <summary>
    /// Generate MCP tool list for Claude.
    /// </summary>
    public static List<object> ToMcpFormat()
    {
        lock (_lock)
        {
            return _tools.Values.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                input_schema = new
                {
                    type = "object",
                    properties = t.Parameters != null
                        ? t.Parameters.ToDictionary(
                            p => p.Name,
                            p => (object)new { type = p.Type, description = p.Description }
                          )
                        : new Dictionary<string, object>(),
                    required = t.Parameters?
                        .Where(p => p.Required)
                        .Select(p => p.Name)
                        .ToList() ?? new List<string>()
                }
            }).Cast<object>().ToList();
        }
    }
}

/// <summary>
/// Tool definition.
/// </summary>
public class ToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("usage")]
    public string? Usage { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("parameters")]
    public List<ToolParameter>? Parameters { get; set; }

    [JsonPropertyName("examples")]
    public List<string>? Examples { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; } = false;

    [JsonPropertyName("deprecated")]
    public bool Deprecated { get; set; } = false;

    [JsonPropertyName("deprecatedMessage")]
    public string? DeprecatedMessage { get; set; }

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }
}

/// <summary>
/// Tool parameter definition.
/// </summary>
public class ToolParameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; } = false;

    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [JsonPropertyName("enum")]
    public List<string>? Enum { get; set; }
}

/// <summary>
/// Tool execution result.
/// </summary>
public record ToolResult(
    bool Success,
    string? Result,
    string? Error
);

/// <summary>
/// Tool registration event.
/// </summary>
public record ToolRegistrationEvent(
    string ToolName,
    ToolDefinition Definition
);
