using System.Collections.Concurrent;

namespace ZimaFileService.Core.LSP;

/// <summary>
/// LSP Manager - coordinates language server clients.
/// Modeled after OpenCode's LSP module.
/// </summary>
public static class LspManager
{
    private static readonly ConcurrentDictionary<string, LspClient> _clients = new();
    private static readonly ConcurrentDictionary<string, List<LspDiagnostic>> _diagnostics = new();
    private static readonly HashSet<string> _brokenServers = new();
    private static readonly object _lock = new();
    private static bool _enabled = true;

    /// <summary>
    /// Enable or disable LSP.
    /// </summary>
    public static bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    /// <summary>
    /// Initialize LSP manager.
    /// </summary>
    public static void Initialize()
    {
        // Subscribe to diagnostics events
        EventBus.Subscribe<object>("lsp.diagnostics", HandleDiagnostics);

        Console.WriteLine($"[LSP] Manager initialized with {LspServers.List().Count} server definitions");
    }

    /// <summary>
    /// Get or create a client for a file.
    /// </summary>
    public static async Task<LspClient?> GetClientAsync(string file)
    {
        if (!_enabled) return null;

        var extension = Path.GetExtension(file);
        var server = LspServers.GetForExtension(extension);

        if (server == null)
        {
            return null;
        }

        var root = await FindRootAsync(file, server.RootPatterns);
        if (root == null) root = Path.GetDirectoryName(file) ?? ".";

        var clientKey = $"{server.Id}:{root}";

        // Check if already broken
        lock (_lock)
        {
            if (_brokenServers.Contains(clientKey))
            {
                return null;
            }
        }

        // Return existing client
        if (_clients.TryGetValue(clientKey, out var existingClient))
        {
            return existingClient;
        }

        // Create new client
        try
        {
            var client = await LspClient.CreateAsync(server, root);

            if (client == null)
            {
                lock (_lock)
                {
                    _brokenServers.Add(clientKey);
                }
                return null;
            }

            _clients[clientKey] = client;
            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LSP] Failed to create client: {ex.Message}");
            lock (_lock)
            {
                _brokenServers.Add(clientKey);
            }
            return null;
        }
    }

    /// <summary>
    /// Get clients for a file (may return multiple for different servers).
    /// </summary>
    public static async Task<List<LspClient>> GetClientsAsync(string file)
    {
        var client = await GetClientAsync(file);
        return client != null ? new List<LspClient> { client } : new List<LspClient>();
    }

    /// <summary>
    /// Check if LSP is available for a file.
    /// </summary>
    public static bool HasClientFor(string file)
    {
        if (!_enabled) return false;

        var extension = Path.GetExtension(file);
        var server = LspServers.GetForExtension(extension);

        return server != null && LspServers.IsAvailable(server.Id);
    }

    /// <summary>
    /// Open a document in the appropriate LSP server.
    /// </summary>
    public static async Task TouchFileAsync(string path, bool waitForDiagnostics = false)
    {
        var clients = await GetClientsAsync(path);

        foreach (var client in clients)
        {
            await client.OpenDocumentAsync(path);
        }

        if (waitForDiagnostics)
        {
            await Task.Delay(500); // Wait for diagnostics
        }
    }

    /// <summary>
    /// Get diagnostics for all files.
    /// </summary>
    public static Dictionary<string, List<LspDiagnostic>> GetDiagnostics()
    {
        lock (_lock)
        {
            return new Dictionary<string, List<LspDiagnostic>>(_diagnostics);
        }
    }

    /// <summary>
    /// Get diagnostics for a specific file.
    /// </summary>
    public static List<LspDiagnostic> GetDiagnosticsFor(string path)
    {
        var uri = new Uri(path).AbsoluteUri;

        lock (_lock)
        {
            return _diagnostics.TryGetValue(uri, out var diags)
                ? new List<LspDiagnostic>(diags)
                : new List<LspDiagnostic>();
        }
    }

    /// <summary>
    /// Go to definition.
    /// </summary>
    public static async Task<LspLocation[]> DefinitionAsync(string file, int line, int character)
    {
        var clients = await GetClientsAsync(file);
        var results = new List<LspLocation>();

        foreach (var client in clients)
        {
            var locations = await client.DefinitionAsync(file, line, character);
            results.AddRange(locations);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Find references.
    /// </summary>
    public static async Task<LspLocation[]> ReferencesAsync(string file, int line, int character)
    {
        var clients = await GetClientsAsync(file);
        var results = new List<LspLocation>();

        foreach (var client in clients)
        {
            var locations = await client.ReferencesAsync(file, line, character);
            results.AddRange(locations);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Get hover information.
    /// </summary>
    public static async Task<string?> HoverAsync(string file, int line, int character)
    {
        var clients = await GetClientsAsync(file);

        foreach (var client in clients)
        {
            var hover = await client.HoverAsync(file, line, character);
            if (hover != null) return hover;
        }

        return null;
    }

    /// <summary>
    /// Get document symbols.
    /// </summary>
    public static async Task<LspSymbol[]> DocumentSymbolsAsync(string file)
    {
        var clients = await GetClientsAsync(file);
        var results = new List<LspSymbol>();

        foreach (var client in clients)
        {
            var symbols = await client.DocumentSymbolsAsync(file);
            results.AddRange(symbols);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Search workspace symbols.
    /// </summary>
    public static async Task<LspSymbol[]> WorkspaceSymbolsAsync(string query)
    {
        var results = new List<LspSymbol>();

        foreach (var client in _clients.Values)
        {
            var symbols = await client.WorkspaceSymbolsAsync(query);
            results.AddRange(symbols);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Get status of all LSP clients.
    /// </summary>
    public static List<LspStatus> Status()
    {
        return _clients.Values.Select(c => new LspStatus(
            c.ServerId,
            c.Root,
            c.IsInitialized ? "connected" : "error"
        )).ToList();
    }

    /// <summary>
    /// Shutdown all LSP clients.
    /// </summary>
    public static void Shutdown()
    {
        foreach (var client in _clients.Values)
        {
            try
            {
                client.Dispose();
            }
            catch { }
        }

        _clients.Clear();

        lock (_lock)
        {
            _brokenServers.Clear();
            _diagnostics.Clear();
        }

        Console.WriteLine("[LSP] All clients shut down");
    }

    /// <summary>
    /// Handle incoming diagnostics.
    /// </summary>
    private static void HandleDiagnostics(object payload)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Uri", out var uriElement) &&
                root.TryGetProperty("Diagnostics", out var diagsElement))
            {
                var uri = uriElement.GetString();
                if (string.IsNullOrEmpty(uri)) return;

                var diagnostics = new List<LspDiagnostic>();

                foreach (var diag in diagsElement.EnumerateArray())
                {
                    var message = diag.TryGetProperty("message", out var m) ? m.GetString() : "";
                    var severity = diag.TryGetProperty("severity", out var s) ? s.GetInt32() : 1;
                    var range = diag.TryGetProperty("range", out var r) ? ParseRange(r) : null;

                    if (!string.IsNullOrEmpty(message))
                    {
                        diagnostics.Add(new LspDiagnostic(message, severity, range));
                    }
                }

                lock (_lock)
                {
                    _diagnostics[uri] = diagnostics;
                }

                // Publish to event bus
                EventBus.Publish("lsp.diagnostics.updated", new { Uri = uri, Count = diagnostics.Count });
            }
        }
        catch { }
    }

    private static LspRange? ParseRange(System.Text.Json.JsonElement element)
    {
        try
        {
            var start = element.GetProperty("start");
            var end = element.GetProperty("end");

            return new LspRange(
                new LspPosition(start.GetProperty("line").GetInt32(), start.GetProperty("character").GetInt32()),
                new LspPosition(end.GetProperty("line").GetInt32(), end.GetProperty("character").GetInt32())
            );
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Find project root based on patterns.
    /// </summary>
    private static Task<string?> FindRootAsync(string file, string[] patterns)
    {
        var dir = Path.GetDirectoryName(file);

        while (!string.IsNullOrEmpty(dir))
        {
            foreach (var pattern in patterns)
            {
                var matches = Directory.GetFiles(dir, pattern);
                if (matches.Length > 0)
                {
                    return Task.FromResult<string?>(dir);
                }

                var dirs = Directory.GetDirectories(dir, pattern);
                if (dirs.Length > 0)
                {
                    return Task.FromResult<string?>(dir);
                }
            }

            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        return Task.FromResult<string?>(null);
    }
}

/// <summary>
/// LSP Client status.
/// </summary>
public record LspStatus(string Id, string Root, string Status);

/// <summary>
/// LSP Diagnostic.
/// </summary>
public record LspDiagnostic(string Message, int Severity, LspRange? Range)
{
    public string SeverityName => Severity switch
    {
        1 => "ERROR",
        2 => "WARNING",
        3 => "INFO",
        4 => "HINT",
        _ => "UNKNOWN"
    };

    public override string ToString()
    {
        var line = Range?.Start.Line + 1 ?? 0;
        var col = Range?.Start.Character + 1 ?? 0;
        return $"{SeverityName} [{line}:{col}] {Message}";
    }
}
