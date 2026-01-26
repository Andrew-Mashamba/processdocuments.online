using System.Collections.Concurrent;

namespace ZimaFileService.Core;

/// <summary>
/// Event Bus for pub/sub communication between components.
/// Modeled after OpenCode's bus system.
/// </summary>
public static class EventBus
{
    private static readonly ConcurrentDictionary<string, List<Delegate>> _subscriptions = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Subscribe to an event type.
    /// </summary>
    public static Action Subscribe<T>(string eventType, Action<T> handler)
    {
        lock (_lock)
        {
            if (!_subscriptions.ContainsKey(eventType))
            {
                _subscriptions[eventType] = new List<Delegate>();
            }
            _subscriptions[eventType].Add(handler);
        }

        // Return unsubscribe function
        return () => Unsubscribe(eventType, handler);
    }

    /// <summary>
    /// Subscribe to an event with no payload.
    /// </summary>
    public static Action Subscribe(string eventType, Action handler)
    {
        return Subscribe<object?>(eventType, _ => handler());
    }

    /// <summary>
    /// Unsubscribe a handler from an event type.
    /// </summary>
    public static void Unsubscribe<T>(string eventType, Action<T> handler)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
    }

    /// <summary>
    /// Publish an event to all subscribers.
    /// </summary>
    public static void Publish<T>(string eventType, T payload)
    {
        List<Delegate>? handlers = null;

        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var subs))
            {
                handlers = new List<Delegate>(subs);
            }
        }

        if (handlers != null)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is Action<T> typedHandler)
                    {
                        typedHandler(payload);
                    }
                    else if (handler is Action<object?> objHandler)
                    {
                        objHandler(payload);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EventBus] Error in handler for {eventType}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Publish an event with no payload.
    /// </summary>
    public static void Publish(string eventType)
    {
        Publish<object?>(eventType, null);
    }

    /// <summary>
    /// Clear all subscriptions (useful for testing).
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _subscriptions.Clear();
        }
    }

    /// <summary>
    /// Get count of subscribers for an event type.
    /// </summary>
    public static int SubscriberCount(string eventType)
    {
        lock (_lock)
        {
            return _subscriptions.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }
    }
}

/// <summary>
/// Standard event types used throughout the system.
/// </summary>
public static class Events
{
    // Session events
    public const string SessionCreated = "session.created";
    public const string SessionUpdated = "session.updated";
    public const string SessionDeleted = "session.deleted";
    public const string SessionForked = "session.forked";
    public const string SessionCompacted = "session.compacted";

    // Message events
    public const string MessageCreated = "message.created";
    public const string MessageUpdated = "message.updated";
    public const string MessageDeleted = "message.deleted";

    // Tool events
    public const string ToolExecuteBefore = "tool.execute.before";
    public const string ToolExecuteAfter = "tool.execute.after";
    public const string ToolRegistered = "tool.registered";
    public const string ToolUnregistered = "tool.unregistered";

    // Agent events
    public const string AgentStarted = "agent.started";
    public const string AgentCompleted = "agent.completed";
    public const string AgentSwitched = "agent.switched";

    // Permission events
    public const string PermissionRequested = "permission.requested";
    public const string PermissionGranted = "permission.granted";
    public const string PermissionDenied = "permission.denied";

    // Plugin events
    public const string PluginLoaded = "plugin.loaded";
    public const string PluginUnloaded = "plugin.unloaded";
    public const string PluginError = "plugin.error";

    // File events
    public const string FileCreated = "file.created";
    public const string FileModified = "file.modified";
    public const string FileDeleted = "file.deleted";

    // Error events
    public const string Error = "error";
    public const string Warning = "warning";
}

/// <summary>
/// Event payload for tool execution.
/// </summary>
public record ToolExecutionEvent(
    string ToolId,
    string SessionId,
    string? CallId,
    Dictionary<string, object>? Args = null,
    object? Result = null,
    string? Error = null
);

/// <summary>
/// Event payload for permission requests.
/// </summary>
public record PermissionEvent(
    string Permission,
    string Pattern,
    string SessionId,
    string? ToolId = null,
    bool Granted = false
);

/// <summary>
/// Event payload for session events.
/// </summary>
public record SessionEvent(
    string SessionId,
    string? ParentId = null,
    string? Title = null,
    string? Action = null
);

/// <summary>
/// Event payload for file events.
/// </summary>
public record FileEvent(
    string FilePath,
    string Action,
    long? Size = null
);
