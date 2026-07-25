using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZimaFileService.Core;

/// <summary>
/// Session management system for handling conversation state.
/// Modeled after OpenCode's session system with fork and compaction.
/// </summary>
public static class SessionManager
{
    private static readonly Dictionary<string, Session> _sessions = new();
    private static readonly object _lock = new();
    private static string? _activeSessionId;

    /// <summary>
    /// Create a new session.
    /// </summary>
    public static Session Create(string? title = null, string? agentName = null)
    {
        var session = new Session
        {
            Id = GenerateId(),
            Title = title ?? "New Session",
            AgentName = agentName ?? AgentManager.GetDefault().Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _sessions[session.Id] = session;
        }

        EventBus.Publish(Events.SessionCreated, new SessionEvent(session.Id, Title: session.Title));
        return session;
    }

    /// <summary>
    /// Get a session by ID.
    /// </summary>
    public static Session? Get(string id)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(id, out var session) ? session : null;
        }
    }

    /// <summary>
    /// Get or create a session.
    /// </summary>
    public static Session GetOrCreate(string? id = null)
    {
        if (id != null)
        {
            var existing = Get(id);
            if (existing != null) return existing;
        }

        return Create();
    }

    /// <summary>
    /// Get the active session.
    /// </summary>
    public static Session? GetActive()
    {
        if (_activeSessionId == null) return null;
        return Get(_activeSessionId);
    }

    /// <summary>
    /// Set the active session.
    /// </summary>
    public static void SetActive(string id)
    {
        _activeSessionId = id;
    }

    /// <summary>
    /// List all sessions.
    /// </summary>
    public static List<Session> List()
    {
        lock (_lock)
        {
            return _sessions.Values
                .OrderByDescending(s => s.UpdatedAt)
                .ToList();
        }
    }

    /// <summary>
    /// Delete a session.
    /// </summary>
    public static bool Delete(string id)
    {
        lock (_lock)
        {
            if (_sessions.Remove(id))
            {
                if (_activeSessionId == id)
                {
                    _activeSessionId = null;
                }
                EventBus.Publish(Events.SessionDeleted, new SessionEvent(id));
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Fork a session (create a branch from a specific point).
    /// </summary>
    public static Session? Fork(string id, int? atMessageIndex = null)
    {
        var original = Get(id);
        if (original == null) return null;

        var forked = new Session
        {
            Id = GenerateId(),
            Title = $"{original.Title} (Fork)",
            AgentName = original.AgentName,
            ParentId = id,
            ForkPoint = atMessageIndex ?? original.Messages.Count,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Copy messages up to fork point
        var messageCount = atMessageIndex ?? original.Messages.Count;
        for (var i = 0; i < messageCount && i < original.Messages.Count; i++)
        {
            forked.Messages.Add(original.Messages[i].Clone());
        }

        lock (_lock)
        {
            _sessions[forked.Id] = forked;
        }

        EventBus.Publish(Events.SessionForked, new SessionEvent(forked.Id, original.Id, forked.Title));
        return forked;
    }

    /// <summary>
    /// Compact a session (summarize old messages to reduce context).
    /// </summary>
    public static async Task<bool> CompactAsync(string id, int keepRecentCount = 10)
    {
        var session = Get(id);
        if (session == null) return false;

        if (session.Messages.Count <= keepRecentCount)
        {
            return true; // Nothing to compact
        }

        // Get the compaction agent
        var compactionAgent = AgentManager.Get("compaction");
        if (compactionAgent == null) return false;

        // Messages to summarize
        var toCompact = session.Messages.Take(session.Messages.Count - keepRecentCount).ToList();

        // Create summary using compaction agent
        var summaryText = await GenerateSummaryAsync(toCompact, compactionAgent);

        // Create compaction record
        var compaction = new CompactionRecord
        {
            Id = GenerateId(),
            OriginalMessageCount = toCompact.Count,
            Summary = summaryText,
            CompactedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            // Add compaction to history
            session.Compactions.Add(compaction);

            // Replace old messages with summary message
            var recentMessages = session.Messages.Skip(session.Messages.Count - keepRecentCount).ToList();
            session.Messages.Clear();

            // Add summary as system context
            session.Messages.Add(new Message
            {
                Id = GenerateId(),
                Role = "system",
                Content = $"[Previous conversation summary]\n{summaryText}",
                CreatedAt = DateTime.UtcNow,
                IsCompactionSummary = true
            });

            // Add recent messages
            session.Messages.AddRange(recentMessages);
            session.UpdatedAt = DateTime.UtcNow;
        }

        EventBus.Publish(Events.SessionCompacted, new SessionEvent(id, Action: "compacted"));
        return true;
    }

    /// <summary>
    /// Add a message to a session.
    /// </summary>
    public static Message AddMessage(string sessionId, string role, string content)
    {
        var session = Get(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"Session '{sessionId}' not found");
        }

        var message = new Message
        {
            Id = GenerateId(),
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            session.Messages.Add(message);
            session.UpdatedAt = DateTime.UtcNow;
        }

        EventBus.Publish(Events.MessageCreated, new { SessionId = sessionId, Message = message });
        return message;
    }

    /// <summary>
    /// Update session title.
    /// </summary>
    public static void UpdateTitle(string id, string title)
    {
        var session = Get(id);
        if (session != null)
        {
            lock (_lock)
            {
                session.Title = title;
                session.UpdatedAt = DateTime.UtcNow;
            }
            EventBus.Publish(Events.SessionUpdated, new SessionEvent(id, Title: title));
        }
    }

    /// <summary>
    /// Switch agent for a session.
    /// </summary>
    public static bool SwitchAgent(string sessionId, string agentName)
    {
        var session = Get(sessionId);
        if (session == null) return false;

        var agent = AgentManager.Get(agentName);
        if (agent == null) return false;

        var oldAgent = session.AgentName;
        lock (_lock)
        {
            session.AgentName = agentName;
            session.UpdatedAt = DateTime.UtcNow;
        }

        EventBus.Publish(Events.AgentSwitched, new { SessionId = sessionId, OldAgent = oldAgent, NewAgent = agentName });
        return true;
    }

    /// <summary>
    /// Save sessions to a file.
    /// </summary>
    public static async Task SaveToFileAsync(string path)
    {
        try
        {
            List<Session> sessions;
            lock (_lock)
            {
                sessions = _sessions.Values.ToList();
            }

            var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Session] Error saving sessions: {ex.Message}");
        }
    }

    /// <summary>
    /// Load sessions from a file.
    /// </summary>
    public static async Task LoadFromFileAsync(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var sessions = JsonSerializer.Deserialize<List<Session>>(json);

            if (sessions != null)
            {
                lock (_lock)
                {
                    foreach (var session in sessions)
                    {
                        _sessions[session.Id] = session;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Session] Error loading sessions: {ex.Message}");
        }
    }

    /// <summary>
    /// Get session count.
    /// </summary>
    public static int Count
    {
        get
        {
            lock (_lock)
            {
                return _sessions.Count;
            }
        }
    }

    /// <summary>
    /// Clear all sessions.
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _sessions.Clear();
            _activeSessionId = null;
        }
    }

    /// <summary>
    /// Generate a unique ID.
    /// </summary>
    private static string GenerateId()
    {
        return Guid.NewGuid().ToString("N")[..12];
    }

    /// <summary>
    /// Generate a summary for compaction.
    /// </summary>
    private static Task<string> GenerateSummaryAsync(List<Message> messages, AgentInfo agent)
    {
        // Build context from messages
        var context = string.Join("\n", messages.Select(m => $"{m.Role}: {m.Content}"));

        // For now, create a simple summary
        // In production, this would call the AI with the compaction agent
        var summary = $"[Compacted {messages.Count} messages]\n" +
                     $"Roles involved: {string.Join(", ", messages.Select(m => m.Role).Distinct())}\n" +
                     $"First message: {messages.FirstOrDefault()?.Content?[..Math.Min(100, messages.FirstOrDefault()?.Content?.Length ?? 0)]}...";

        return Task.FromResult(summary);
    }
}

/// <summary>
/// Session data.
/// </summary>
public class Session
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = "build";

    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("forkPoint")]
    public int? ForkPoint { get; set; }

    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();

    [JsonPropertyName("compactions")]
    public List<CompactionRecord> Compactions { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Get total token estimate for session.
    /// </summary>
    [JsonIgnore]
    public int EstimatedTokens => Messages.Sum(m => EstimateTokens(m.Content));

    private static int EstimateTokens(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Length / 4; // Rough estimate
    }
}

/// <summary>
/// Message in a session.
/// </summary>
public class Message
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("toolCalls")]
    public List<ToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("toolResults")]
    public List<ToolCallResult>? ToolResults { get; set; }

    [JsonPropertyName("isCompactionSummary")]
    public bool IsCompactionSummary { get; set; } = false;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Clone this message.
    /// </summary>
    public Message Clone()
    {
        return new Message
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Role = Role,
            Content = Content,
            ToolCalls = ToolCalls?.ToList(),
            ToolResults = ToolResults?.ToList(),
            IsCompactionSummary = IsCompactionSummary,
            CreatedAt = CreatedAt
        };
    }
}

/// <summary>
/// Tool call within a message.
/// </summary>
public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; set; }
}

/// <summary>
/// Tool call result.
/// </summary>
public class ToolCallResult
{
    [JsonPropertyName("callId")]
    public string CallId { get; set; } = "";

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

/// <summary>
/// Record of a compaction event.
/// </summary>
public class CompactionRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("originalMessageCount")]
    public int OriginalMessageCount { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";

    [JsonPropertyName("compactedAt")]
    public DateTime CompactedAt { get; set; }
}
