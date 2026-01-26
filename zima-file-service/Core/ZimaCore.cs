using System.Text.Json;
using ZimaFileService.Api;

namespace ZimaFileService.Core;

/// <summary>
/// ZIMA Core - Initializes and manages all core subsystems.
/// Modeled after OpenCode's initialization pattern.
/// </summary>
public static class ZimaCore
{
    private static bool _initialized = false;
    private static readonly object _lock = new();

    /// <summary>
    /// Initialize all core systems.
    /// </summary>
    public static async Task InitializeAsync(string workingDirectory)
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;
            _initialized = true;
        }

        Console.WriteLine("[ZimaCore] Initializing core systems...");

        // 1. Event Bus is already static and ready

        // 2. Set up event logging
        SetupEventLogging();

        // 3. Load custom agents from config
        var agentsPath = Path.Combine(workingDirectory, ".zima-agents.json");
        await AgentManager.LoadFromFileAsync(agentsPath);
        Console.WriteLine($"[ZimaCore] Agents: {AgentManager.List(true).Count} registered");

        // 4. Load custom permissions from config
        var permissionsPath = Path.Combine(workingDirectory, ".zima-permissions.json");
        await Permission.LoadFromFileAsync(permissionsPath);
        Console.WriteLine($"[ZimaCore] Permissions: {Permission.GetRules().Count} rules loaded");

        // 5. Load plugins from directory
        var pluginsPath = Path.Combine(workingDirectory, ".zima-plugins");
        await PluginManager.LoadFromDirectoryAsync(pluginsPath);
        Console.WriteLine($"[ZimaCore] Plugins: {PluginManager.Count} loaded");

        // 6. Load tool definitions from config
        var toolsPath = Path.Combine(workingDirectory, ".zima-tools.json");
        await ToolRegistry.LoadFromFileAsync(toolsPath);
        Console.WriteLine($"[ZimaCore] Tools: {ToolRegistry.Count} registered");

        // 7. Load sessions from storage
        var sessionsPath = Path.Combine(workingDirectory, ".zima-sessions.json");
        await SessionManager.LoadFromFileAsync(sessionsPath);
        Console.WriteLine($"[ZimaCore] Sessions: {SessionManager.Count} loaded");

        // 8. Register default plugins/hooks
        RegisterDefaultHooks();

        Console.WriteLine("[ZimaCore] Core systems initialized successfully");
    }

    /// <summary>
    /// Shutdown all core systems gracefully.
    /// </summary>
    public static async Task ShutdownAsync(string workingDirectory)
    {
        Console.WriteLine("[ZimaCore] Shutting down core systems...");

        // Save sessions
        var sessionsPath = Path.Combine(workingDirectory, ".zima-sessions.json");
        await SessionManager.SaveToFileAsync(sessionsPath);

        // Save tool definitions
        var toolsPath = Path.Combine(workingDirectory, ".zima-tools.json");
        await ToolRegistry.SaveToFileAsync(toolsPath);

        Console.WriteLine("[ZimaCore] Core systems shutdown complete");
    }

    /// <summary>
    /// Set up event logging for debugging.
    /// </summary>
    private static void SetupEventLogging()
    {
        // Log tool executions
        EventBus.Subscribe<ToolExecutionEvent>(Events.ToolExecuteBefore, e =>
        {
            Console.WriteLine($"[Tool] Executing: {e.ToolId}");
        });

        EventBus.Subscribe<ToolExecutionEvent>(Events.ToolExecuteAfter, e =>
        {
            if (e.Error != null)
            {
                Console.WriteLine($"[Tool] {e.ToolId} failed: {e.Error}");
            }
            else
            {
                Console.WriteLine($"[Tool] {e.ToolId} completed");
            }
        });

        // Log permission events
        EventBus.Subscribe<PermissionEvent>(Events.PermissionGranted, e =>
        {
            Console.WriteLine($"[Permission] Granted: {e.Permission}");
        });

        EventBus.Subscribe<PermissionEvent>(Events.PermissionDenied, e =>
        {
            Console.WriteLine($"[Permission] Denied: {e.Permission}");
        });

        // Log session events
        EventBus.Subscribe<SessionEvent>(Events.SessionCreated, e =>
        {
            Console.WriteLine($"[Session] Created: {e.SessionId}");
        });

        EventBus.Subscribe<SessionEvent>(Events.SessionForked, e =>
        {
            Console.WriteLine($"[Session] Forked: {e.SessionId} from {e.ParentId}");
        });

        // Log file events
        EventBus.Subscribe<FileEvent>(Events.FileCreated, e =>
        {
            Console.WriteLine($"[File] Created: {e.FilePath} ({e.Size} bytes)");
        });
    }

    /// <summary>
    /// Register default system hooks.
    /// </summary>
    private static void RegisterDefaultHooks()
    {
        // Before tool execution - check permissions
        PluginManager.RegisterHook(
            PluginManager.HookTypes.BeforeToolExecute,
            async context =>
            {
                if (context.Data is ToolExecutionContext toolContext)
                {
                    var permitted = await Permission.RequestAsync(new PermissionRequest(
                        toolContext.ToolId,
                        "*",
                        toolContext.SessionId
                    ));

                    if (!permitted)
                    {
                        return new HookResult(HookAction.Stop, null, $"Permission denied for {toolContext.ToolId}");
                    }
                }
                return new HookResult(HookAction.Continue);
            },
            priority: 100  // High priority - runs first
        );

        // After file creation - publish event
        PluginManager.RegisterHook(
            PluginManager.HookTypes.AfterFileCreate,
            context =>
            {
                if (context.Data is FileCreationContext fileContext)
                {
                    var fileInfo = new FileInfo(fileContext.FilePath);
                    EventBus.Publish(Events.FileCreated, new FileEvent(
                        fileContext.FilePath,
                        "created",
                        fileInfo.Exists ? fileInfo.Length : null
                    ));
                }
                return Task.FromResult(new HookResult(HookAction.Continue));
            },
            priority: 0
        );

        // Session compaction hook - auto-compact long sessions
        PluginManager.RegisterHook(
            PluginManager.HookTypes.AfterMessageReceive,
            async context =>
            {
                if (context.Data is MessageContext msgContext)
                {
                    var session = SessionManager.Get(msgContext.SessionId);
                    if (session != null && session.Messages.Count > 50)
                    {
                        // Auto-compact when session gets long
                        await SessionManager.CompactAsync(msgContext.SessionId, 20);
                    }
                }
                return new HookResult(HookAction.Continue);
            },
            priority: -100  // Low priority - runs last
        );
    }

    /// <summary>
    /// Get the current agent for a session.
    /// </summary>
    public static AgentInfo GetSessionAgent(string? sessionId)
    {
        if (sessionId != null)
        {
            var session = SessionManager.Get(sessionId);
            if (session != null)
            {
                var agent = AgentManager.Get(session.AgentName);
                if (agent != null) return agent;
            }
        }
        return AgentManager.GetDefault();
    }

    /// <summary>
    /// Execute a tool with full hook integration.
    /// </summary>
    public static async Task<ToolResult> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object> args,
        string? sessionId = null)
    {
        // Before hooks
        var beforeResult = await PluginManager.ExecuteHooksAsync(
            PluginManager.HookTypes.BeforeToolExecute,
            new HookContext(
                PluginManager.HookTypes.BeforeToolExecute,
                new ToolExecutionContext(toolName, sessionId, args),
                sessionId
            )
        );

        if (!beforeResult.Continue)
        {
            return new ToolResult(false, null, "Execution blocked by hook");
        }

        // Execute tool
        var result = await ToolRegistry.ExecuteAsync(toolName, args, sessionId);

        // After hooks
        await PluginManager.ExecuteHooksAsync(
            PluginManager.HookTypes.AfterToolExecute,
            new HookContext(
                PluginManager.HookTypes.AfterToolExecute,
                new ToolExecutionContext(toolName, sessionId, args, result.Result),
                sessionId
            )
        );

        return result;
    }

    /// <summary>
    /// Create or get a session.
    /// </summary>
    public static Session GetOrCreateSession(string? sessionId, string? title = null)
    {
        if (sessionId != null)
        {
            var existing = SessionManager.Get(sessionId);
            if (existing != null) return existing;
        }

        return SessionManager.Create(title);
    }

    /// <summary>
    /// Add a message to a session.
    /// </summary>
    public static async Task<Message> AddMessageAsync(
        string sessionId,
        string role,
        string content)
    {
        var message = SessionManager.AddMessage(sessionId, role, content);

        // Trigger message hook
        await PluginManager.ExecuteHooksAsync(
            PluginManager.HookTypes.AfterMessageReceive,
            new HookContext(
                PluginManager.HookTypes.AfterMessageReceive,
                new MessageContext(sessionId, message.Id, role, content),
                sessionId
            )
        );

        return message;
    }

    /// <summary>
    /// Switch agent for a session.
    /// </summary>
    public static async Task<bool> SwitchAgentAsync(string sessionId, string agentName)
    {
        // Before hook
        var beforeResult = await PluginManager.ExecuteHooksAsync(
            PluginManager.HookTypes.BeforeAgentSwitch,
            new HookContext(
                PluginManager.HookTypes.BeforeAgentSwitch,
                new AgentSwitchContext(sessionId, agentName),
                sessionId
            )
        );

        if (!beforeResult.Continue)
        {
            return false;
        }

        var result = SessionManager.SwitchAgent(sessionId, agentName);

        // After hook
        if (result)
        {
            await PluginManager.ExecuteHooksAsync(
                PluginManager.HookTypes.AfterAgentSwitch,
                new HookContext(
                    PluginManager.HookTypes.AfterAgentSwitch,
                    new AgentSwitchContext(sessionId, agentName),
                    sessionId
                )
            );
        }

        return result;
    }

    /// <summary>
    /// Fork a session.
    /// </summary>
    public static async Task<Session?> ForkSessionAsync(string sessionId, int? atMessageIndex = null)
    {
        // Before hook
        var beforeResult = await PluginManager.ExecuteHooksAsync(
            PluginManager.HookTypes.BeforeSessionFork,
            new HookContext(
                PluginManager.HookTypes.BeforeSessionFork,
                new SessionForkContext(sessionId, atMessageIndex),
                sessionId
            )
        );

        if (!beforeResult.Continue)
        {
            return null;
        }

        var forked = SessionManager.Fork(sessionId, atMessageIndex);

        // After hook
        if (forked != null)
        {
            await PluginManager.ExecuteHooksAsync(
                PluginManager.HookTypes.AfterSessionFork,
                new HookContext(
                    PluginManager.HookTypes.AfterSessionFork,
                    new SessionForkContext(forked.Id, atMessageIndex),
                    forked.Id
                )
            );
        }

        return forked;
    }
}

// Hook context types
public record ToolExecutionContext(
    string ToolId,
    string? SessionId,
    Dictionary<string, object>? Args = null,
    string? Result = null
);

public record FileCreationContext(
    string FilePath,
    string? SessionId = null
);

public record MessageContext(
    string SessionId,
    string MessageId,
    string Role,
    string Content
);

public record AgentSwitchContext(
    string SessionId,
    string AgentName
);

public record SessionForkContext(
    string SessionId,
    int? AtMessageIndex
);
