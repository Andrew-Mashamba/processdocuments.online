using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ZimaFileService.Core.LSP;

/// <summary>
/// LSP Client for communicating with language servers.
/// Uses JSON-RPC over stdio.
/// </summary>
public class LspClient : IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _writer;
    private readonly StreamReader _reader;
    private readonly string _serverId;
    private readonly string _root;
    private int _requestId = 0;
    private readonly Dictionary<int, TaskCompletionSource<JsonElement?>> _pendingRequests = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _initialized = false;
    private bool _disposed = false;

    public string ServerId => _serverId;
    public string Root => _root;
    public bool IsInitialized => _initialized;

    private LspClient(Process process, string serverId, string root)
    {
        _process = process;
        _writer = process.StandardInput;
        _reader = process.StandardOutput;
        _serverId = serverId;
        _root = root;
    }

    /// <summary>
    /// Create and initialize an LSP client.
    /// </summary>
    public static async Task<LspClient?> CreateAsync(LspServerInfo server, string root)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = server.SpawnCommand,
                Arguments = string.Join(" ", server.SpawnArgs),
                WorkingDirectory = root,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine($"[LSP] Failed to start {server.Id}");
                return null;
            }

            var client = new LspClient(process, server.Id, root);

            // Start reading responses
            _ = client.ReadResponsesAsync();

            // Initialize the server
            var initResult = await client.InitializeAsync(root, server.Initialization);
            if (initResult == null)
            {
                Console.WriteLine($"[LSP] Failed to initialize {server.Id}");
                process.Kill();
                return null;
            }

            client._initialized = true;
            Console.WriteLine($"[LSP] Initialized {server.Id} at {root}");

            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LSP] Error creating client for {server.Id}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Initialize the language server.
    /// </summary>
    private async Task<JsonElement?> InitializeAsync(string root, Dictionary<string, object>? initialization)
    {
        var rootUri = new Uri(root).AbsoluteUri;

        var initParams = new
        {
            processId = Environment.ProcessId,
            rootUri = rootUri,
            workspaceFolders = new[]
            {
                new { name = "workspace", uri = rootUri }
            },
            initializationOptions = initialization ?? new Dictionary<string, object>(),
            capabilities = new
            {
                textDocument = new
                {
                    synchronization = new { didOpen = true, didChange = true },
                    hover = new { contentFormat = new[] { "plaintext", "markdown" } },
                    definition = new { linkSupport = true },
                    references = new { },
                    documentSymbol = new { },
                    publishDiagnostics = new { versionSupport = true }
                },
                workspace = new
                {
                    workspaceFolders = true,
                    configuration = true
                }
            }
        };

        var result = await SendRequestAsync("initialize", initParams);

        // Send initialized notification
        await SendNotificationAsync("initialized", new { });

        return result;
    }

    /// <summary>
    /// Send a JSON-RPC request and wait for response.
    /// </summary>
    public async Task<JsonElement?> SendRequestAsync(string method, object? parameters = null, int timeoutMs = 30000)
    {
        var id = Interlocked.Increment(ref _requestId);
        var tcs = new TaskCompletionSource<JsonElement?>();

        lock (_pendingRequests)
        {
            _pendingRequests[id] = tcs;
        }

        var request = new
        {
            jsonrpc = "2.0",
            id = id,
            method = method,
            @params = parameters
        };

        await SendMessageAsync(request);

        using var cts = new CancellationTokenSource(timeoutMs);
        cts.Token.Register(() => tcs.TrySetResult(null));

        return await tcs.Task;
    }

    /// <summary>
    /// Send a JSON-RPC notification (no response expected).
    /// </summary>
    public async Task SendNotificationAsync(string method, object? parameters = null)
    {
        var notification = new
        {
            jsonrpc = "2.0",
            method = method,
            @params = parameters
        };

        await SendMessageAsync(notification);
    }

    /// <summary>
    /// Send a message to the server.
    /// </summary>
    private async Task SendMessageAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var header = $"Content-Length: {Encoding.UTF8.GetByteCount(json)}\r\n\r\n";

        await _writer.WriteAsync(header);
        await _writer.WriteAsync(json);
        await _writer.FlushAsync();
    }

    /// <summary>
    /// Read responses from the server.
    /// </summary>
    private async Task ReadResponsesAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested && !_process.HasExited)
            {
                // Read headers
                var contentLength = 0;
                string? line;

                while ((line = await _reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrEmpty(line)) break;

                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        contentLength = int.Parse(line.Substring(15).Trim());
                    }
                }

                if (contentLength == 0) continue;

                // Read content
                var buffer = new char[contentLength];
                var read = await _reader.ReadBlockAsync(buffer, 0, contentLength);

                if (read != contentLength) continue;

                var json = new string(buffer);
                HandleMessage(json);
            }
        }
        catch (Exception ex)
        {
            if (!_disposed)
            {
                Console.WriteLine($"[LSP] Read error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handle an incoming message.
    /// </summary>
    private void HandleMessage(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check if this is a response
            if (root.TryGetProperty("id", out var idElement))
            {
                var id = idElement.GetInt32();
                TaskCompletionSource<JsonElement?>? tcs;

                lock (_pendingRequests)
                {
                    _pendingRequests.TryGetValue(id, out tcs);
                    if (tcs != null) _pendingRequests.Remove(id);
                }

                if (tcs != null)
                {
                    if (root.TryGetProperty("result", out var result))
                    {
                        tcs.TrySetResult(result.Clone());
                    }
                    else if (root.TryGetProperty("error", out var error))
                    {
                        Console.WriteLine($"[LSP] Error: {error}");
                        tcs.TrySetResult(null);
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }
                }
            }
            // Check if this is a notification
            else if (root.TryGetProperty("method", out var methodElement))
            {
                var method = methodElement.GetString();
                if (method == "textDocument/publishDiagnostics")
                {
                    // Handle diagnostics
                    if (root.TryGetProperty("params", out var paramsElement))
                    {
                        HandleDiagnostics(paramsElement);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LSP] Parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle diagnostics from the server.
    /// </summary>
    private void HandleDiagnostics(JsonElement paramsElement)
    {
        if (paramsElement.TryGetProperty("uri", out var uriElement) &&
            paramsElement.TryGetProperty("diagnostics", out var diagnosticsElement))
        {
            var uri = uriElement.GetString();
            var count = diagnosticsElement.GetArrayLength();

            if (!string.IsNullOrEmpty(uri))
            {
                EventBus.Publish("lsp.diagnostics", new
                {
                    ServerId = _serverId,
                    Uri = uri,
                    Count = count,
                    Diagnostics = diagnosticsElement.Clone()
                });
            }
        }
    }

    #region LSP Methods

    /// <summary>
    /// Open a document.
    /// </summary>
    public async Task OpenDocumentAsync(string path, string? languageId = null)
    {
        var uri = new Uri(path).AbsoluteUri;
        var text = await File.ReadAllTextAsync(path);
        var ext = Path.GetExtension(path);

        languageId ??= GetLanguageId(ext);

        await SendNotificationAsync("textDocument/didOpen", new
        {
            textDocument = new
            {
                uri = uri,
                languageId = languageId,
                version = 1,
                text = text
            }
        });
    }

    /// <summary>
    /// Go to definition.
    /// </summary>
    public async Task<LspLocation[]> DefinitionAsync(string path, int line, int character)
    {
        var uri = new Uri(path).AbsoluteUri;

        var result = await SendRequestAsync("textDocument/definition", new
        {
            textDocument = new { uri = uri },
            position = new { line = line, character = character }
        });

        return ParseLocations(result);
    }

    /// <summary>
    /// Find references.
    /// </summary>
    public async Task<LspLocation[]> ReferencesAsync(string path, int line, int character)
    {
        var uri = new Uri(path).AbsoluteUri;

        var result = await SendRequestAsync("textDocument/references", new
        {
            textDocument = new { uri = uri },
            position = new { line = line, character = character },
            context = new { includeDeclaration = true }
        });

        return ParseLocations(result);
    }

    /// <summary>
    /// Get hover information.
    /// </summary>
    public async Task<string?> HoverAsync(string path, int line, int character)
    {
        var uri = new Uri(path).AbsoluteUri;

        var result = await SendRequestAsync("textDocument/hover", new
        {
            textDocument = new { uri = uri },
            position = new { line = line, character = character }
        });

        if (result?.TryGetProperty("contents", out var contents) == true)
        {
            if (contents.ValueKind == JsonValueKind.String)
            {
                return contents.GetString();
            }
            if (contents.ValueKind == JsonValueKind.Object && contents.TryGetProperty("value", out var value))
            {
                return value.GetString();
            }
        }

        return null;
    }

    /// <summary>
    /// Get document symbols.
    /// </summary>
    public async Task<LspSymbol[]> DocumentSymbolsAsync(string path)
    {
        var uri = new Uri(path).AbsoluteUri;

        var result = await SendRequestAsync("textDocument/documentSymbol", new
        {
            textDocument = new { uri = uri }
        });

        return ParseSymbols(result);
    }

    /// <summary>
    /// Search workspace symbols.
    /// </summary>
    public async Task<LspSymbol[]> WorkspaceSymbolsAsync(string query)
    {
        var result = await SendRequestAsync("workspace/symbol", new
        {
            query = query
        });

        return ParseSymbols(result);
    }

    #endregion

    #region Helpers

    private static LspLocation[] ParseLocations(JsonElement? result)
    {
        if (result == null) return Array.Empty<LspLocation>();

        var locations = new List<LspLocation>();

        try
        {
            if (result.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in result.Value.EnumerateArray())
                {
                    var loc = ParseLocation(item);
                    if (loc != null) locations.Add(loc);
                }
            }
            else if (result.Value.ValueKind == JsonValueKind.Object)
            {
                var loc = ParseLocation(result.Value);
                if (loc != null) locations.Add(loc);
            }
        }
        catch { }

        return locations.ToArray();
    }

    private static LspLocation? ParseLocation(JsonElement element)
    {
        try
        {
            string? uri = null;
            LspRange? range = null;

            if (element.TryGetProperty("uri", out var uriElement))
            {
                uri = uriElement.GetString();
            }
            else if (element.TryGetProperty("targetUri", out var targetUri))
            {
                uri = targetUri.GetString();
            }

            if (element.TryGetProperty("range", out var rangeElement))
            {
                range = ParseRange(rangeElement);
            }
            else if (element.TryGetProperty("targetRange", out var targetRange))
            {
                range = ParseRange(targetRange);
            }

            if (uri != null && range != null)
            {
                return new LspLocation(uri, range);
            }
        }
        catch { }

        return null;
    }

    private static LspRange ParseRange(JsonElement element)
    {
        var start = element.GetProperty("start");
        var end = element.GetProperty("end");

        return new LspRange(
            new LspPosition(start.GetProperty("line").GetInt32(), start.GetProperty("character").GetInt32()),
            new LspPosition(end.GetProperty("line").GetInt32(), end.GetProperty("character").GetInt32())
        );
    }

    private static LspSymbol[] ParseSymbols(JsonElement? result)
    {
        if (result == null) return Array.Empty<LspSymbol>();

        var symbols = new List<LspSymbol>();

        try
        {
            if (result.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in result.Value.EnumerateArray())
                {
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var kind = item.TryGetProperty("kind", out var k) ? k.GetInt32() : 0;

                    if (name != null)
                    {
                        LspLocation? location = null;
                        if (item.TryGetProperty("location", out var loc))
                        {
                            location = ParseLocation(loc);
                        }
                        else if (item.TryGetProperty("range", out var range))
                        {
                            // DocumentSymbol format
                            var fileUri = result.Value.GetProperty("uri").GetString();
                            location = new LspLocation(fileUri ?? "", ParseRange(range));
                        }

                        symbols.Add(new LspSymbol(name, kind, location));
                    }
                }
            }
        }
        catch { }

        return symbols.ToArray();
    }

    private static string GetLanguageId(string extension)
    {
        return extension.ToLower() switch
        {
            ".cs" => "csharp",
            ".ts" => "typescript",
            ".tsx" => "typescriptreact",
            ".js" => "javascript",
            ".jsx" => "javascriptreact",
            ".py" => "python",
            ".go" => "go",
            ".rs" => "rust",
            ".json" => "json",
            ".html" => "html",
            ".css" => "css",
            ".md" => "markdown",
            _ => "plaintext"
        };
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();

        try
        {
            if (!_process.HasExited)
            {
                // Send shutdown request
                SendRequestAsync("shutdown").Wait(1000);
                SendNotificationAsync("exit").Wait(1000);
                _process.Kill();
            }
        }
        catch { }

        _process.Dispose();
        _cts.Dispose();
    }
}

// LSP Types
public record LspPosition(int Line, int Character);
public record LspRange(LspPosition Start, LspPosition End);
public record LspLocation(string Uri, LspRange Range);
public record LspSymbol(string Name, int Kind, LspLocation? Location);
