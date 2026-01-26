using System.Text.Json;
using System.Text.Json.Serialization;
using ZimaFileService.Api;

namespace ZimaFileService.Core;

/// <summary>
/// Agent system for managing different AI agent modes.
/// Modeled after OpenCode's agent/agent.ts
/// </summary>
public static class AgentManager
{
    private static readonly Dictionary<string, AgentInfo> _agents = new();
    private static readonly object _lock = new();
    private static string _defaultAgent = "build";

    /// <summary>
    /// Initialize built-in agents.
    /// </summary>
    static AgentManager()
    {
        // Build agent - primary agent with full access
        Register(new AgentInfo
        {
            Name = "build",
            Description = "Primary agent for file generation with full tool access",
            Mode = AgentMode.Primary,
            Color = "#22c55e", // Green
            Temperature = 0.7,
            TopP = 0.9,
            Permission = new List<PermissionRule>
            {
                new("*", "*", Permission.Action.Allow)
            },
            Prompt = GetBuildPrompt()
        });

        // Plan agent - read-only exploration mode
        Register(new AgentInfo
        {
            Name = "plan",
            Description = "Read-only agent for exploration and planning",
            Mode = AgentMode.Primary,
            Color = "#3b82f6", // Blue
            Temperature = 0.5,
            TopP = 0.9,
            Permission = new List<PermissionRule>
            {
                new("read_*", "*", Permission.Action.Allow),
                new("list_*", "*", Permission.Action.Allow),
                new("get_*", "*", Permission.Action.Allow),
                new("create_*", "*", Permission.Action.Deny),
                new("delete_*", "*", Permission.Action.Deny),
                new("*", "*", Permission.Action.Deny)
            },
            Prompt = GetPlanPrompt()
        });

        // Explore agent - fast codebase exploration (subagent)
        Register(new AgentInfo
        {
            Name = "explore",
            Description = "Fast agent for codebase exploration",
            Mode = AgentMode.Subagent,
            Color = "#f59e0b", // Amber
            Temperature = 0.3,
            TopP = 0.8,
            Steps = 10,
            Permission = new List<PermissionRule>
            {
                new("read_*", "*", Permission.Action.Allow),
                new("list_*", "*", Permission.Action.Allow),
                new("get_*", "*", Permission.Action.Allow),
                new("*", "*", Permission.Action.Deny)
            },
            Prompt = GetExplorePrompt()
        });

        // General agent - general purpose subagent
        Register(new AgentInfo
        {
            Name = "general",
            Description = "General-purpose agent for complex tasks",
            Mode = AgentMode.Subagent,
            Color = "#8b5cf6", // Purple
            Temperature = 0.7,
            TopP = 0.9,
            Permission = new List<PermissionRule>
            {
                new("*", "*", Permission.Action.Allow)
            },
            Prompt = GetGeneralPrompt()
        });

        // Title agent - generates session titles (hidden)
        Register(new AgentInfo
        {
            Name = "title",
            Description = "Generates session titles",
            Mode = AgentMode.Hidden,
            Temperature = 0.8,
            Steps = 1,
            Prompt = "Generate a short, descriptive title (max 50 chars) for this conversation. Output only the title, nothing else."
        });

        // Summary agent - summarizes sessions (hidden)
        Register(new AgentInfo
        {
            Name = "summary",
            Description = "Summarizes session content",
            Mode = AgentMode.Hidden,
            Temperature = 0.5,
            Prompt = "Summarize the key points of this conversation in 2-3 sentences."
        });

        // Compaction agent - compacts long conversations (hidden)
        Register(new AgentInfo
        {
            Name = "compaction",
            Description = "Compacts long conversations",
            Mode = AgentMode.Hidden,
            Temperature = 0.3,
            Prompt = GetCompactionPrompt()
        });
    }

    /// <summary>
    /// Register an agent.
    /// </summary>
    public static void Register(AgentInfo agent)
    {
        lock (_lock)
        {
            _agents[agent.Name] = agent;
        }

        EventBus.Publish(Events.AgentStarted, new { AgentName = agent.Name, Agent = agent });
    }

    /// <summary>
    /// Get an agent by name.
    /// </summary>
    public static AgentInfo? Get(string name)
    {
        lock (_lock)
        {
            return _agents.TryGetValue(name, out var agent) ? agent : null;
        }
    }

    /// <summary>
    /// Get the default agent.
    /// </summary>
    public static AgentInfo GetDefault()
    {
        return Get(_defaultAgent) ?? Get("build")!;
    }

    /// <summary>
    /// Set the default agent.
    /// </summary>
    public static void SetDefault(string name)
    {
        if (Get(name) != null)
        {
            _defaultAgent = name;
        }
    }

    /// <summary>
    /// List all agents.
    /// </summary>
    public static List<AgentInfo> List(bool includeHidden = false)
    {
        lock (_lock)
        {
            return _agents.Values
                .Where(a => includeHidden || a.Mode != AgentMode.Hidden)
                .OrderBy(a => a.Name)
                .ToList();
        }
    }

    /// <summary>
    /// List primary agents only.
    /// </summary>
    public static List<AgentInfo> ListPrimary()
    {
        return List().Where(a => a.Mode == AgentMode.Primary).ToList();
    }

    /// <summary>
    /// List subagents only.
    /// </summary>
    public static List<AgentInfo> ListSubagents()
    {
        return List().Where(a => a.Mode == AgentMode.Subagent).ToList();
    }

    /// <summary>
    /// Load custom agents from config file.
    /// </summary>
    public static async Task LoadFromFileAsync(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var agents = JsonSerializer.Deserialize<List<AgentInfo>>(json);

            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    Register(agent);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Agent] Error loading agents from {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Get system prompt for an agent.
    /// </summary>
    public static string GetPromptForAgent(AgentInfo agent, string modelId)
    {
        var basePrompt = SystemPrompt.ForModel(modelId);

        if (!string.IsNullOrEmpty(agent.Prompt))
        {
            return $"{basePrompt}\n\n# Agent: {agent.Name}\n\n{agent.Prompt}";
        }

        return basePrompt;
    }

    #region Agent Prompts

    private static string GetBuildPrompt()
    {
        return @"You are the BUILD agent - the primary execution agent.

Your role is to CREATE FILES and EXECUTE TASKS immediately.

Rules:
- Call tools directly without investigation
- Execute requests immediately
- Report results concisely
- Do not explore or analyze code unnecessarily

You have FULL ACCESS to all tools.";
    }

    private static string GetPlanPrompt()
    {
        return @"You are the PLAN agent - a read-only exploration agent.

Your role is to EXPLORE and PLAN without making changes.

Rules:
- You can ONLY use read-only tools (read_*, list_*, get_*)
- You CANNOT create, modify, or delete files
- Analyze requirements and create implementation plans
- Ask clarifying questions before proposing changes

Your output should be a clear plan that the BUILD agent can execute.";
    }

    private static string GetExplorePrompt()
    {
        return @"You are the EXPLORE agent - a fast codebase exploration agent.

Your role is to quickly find information in the codebase.

Rules:
- Focus on speed and efficiency
- Use file listing and reading tools
- Return concise, relevant information
- Maximum 10 steps per exploration

You are read-only and cannot modify files.";
    }

    private static string GetGeneralPrompt()
    {
        return @"You are the GENERAL agent - a versatile subagent.

Your role is to handle complex, multi-step tasks.

Rules:
- Break down complex tasks into steps
- Execute each step methodically
- Report progress and results
- Handle errors gracefully

You have full tool access.";
    }

    private static string GetCompactionPrompt()
    {
        return @"Summarize the conversation above into a compact form that preserves:
1. Key decisions made
2. Files created or modified
3. Important context for continuing the work

Keep the summary under 2000 characters while retaining essential information.";
    }

    #endregion
}

/// <summary>
/// Agent mode types.
/// </summary>
public enum AgentMode
{
    Primary,   // Main agents users can select
    Subagent,  // Agents spawned by other agents
    Hidden     // Internal agents not shown to users
}

/// <summary>
/// Agent configuration.
/// </summary>
public class AgentInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("mode")]
    public AgentMode Mode { get; set; } = AgentMode.Primary;

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("topP")]
    public double TopP { get; set; } = 0.9;

    [JsonPropertyName("steps")]
    public int? Steps { get; set; }

    [JsonPropertyName("model")]
    public ModelReference? Model { get; set; }

    [JsonPropertyName("permission")]
    public List<PermissionRule>? Permission { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("options")]
    public Dictionary<string, object>? Options { get; set; }
}

/// <summary>
/// Model reference for agent configuration.
/// </summary>
public class ModelReference
{
    [JsonPropertyName("providerId")]
    public string ProviderId { get; set; } = "";

    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = "";
}
