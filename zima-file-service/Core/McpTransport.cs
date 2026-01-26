using System.Net;
using System.Text;
using System.Text.Json;

namespace ZimaFileService.Core;

/// <summary>
/// MCP Transport abstraction for multi-transport support.
/// Supports Stdio, HTTP, and SSE transports.
/// </summary>
public interface IMcpTransport : IDisposable
{
    string Name { get; }
    bool IsRunning { get; }
    Task StartAsync(Func<string, Task<string>> requestHandler);
    Task StopAsync();
}

/// <summary>
/// MCP Transport Manager - manages multiple transports simultaneously.
/// </summary>
public static class McpTransportManager
{
    private static readonly List<IMcpTransport> _transports = new();
    private static Func<string, Task<string>>? _requestHandler;
    private static bool _running = false;

    /// <summary>
    /// Configure the request handler for all transports.
    /// </summary>
    public static void SetRequestHandler(Func<string, Task<string>> handler)
    {
        _requestHandler = handler;
    }

    /// <summary>
    /// Add a transport.
    /// </summary>
    public static void AddTransport(IMcpTransport transport)
    {
        _transports.Add(transport);
        Console.WriteLine($"[MCP] Added transport: {transport.Name}");
    }

    /// <summary>
    /// Start all transports.
    /// </summary>
    public static async Task StartAllAsync()
    {
        if (_requestHandler == null)
        {
            throw new InvalidOperationException("Request handler not set");
        }

        _running = true;

        foreach (var transport in _transports)
        {
            try
            {
                await transport.StartAsync(_requestHandler);
                Console.WriteLine($"[MCP] Started transport: {transport.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP] Failed to start {transport.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"[MCP] {_transports.Count(t => t.IsRunning)} transports running");
    }

    /// <summary>
    /// Stop all transports.
    /// </summary>
    public static async Task StopAllAsync()
    {
        _running = false;

        foreach (var transport in _transports)
        {
            try
            {
                await transport.StopAsync();
                Console.WriteLine($"[MCP] Stopped transport: {transport.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP] Error stopping {transport.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get status of all transports.
    /// </summary>
    public static List<TransportStatus> GetStatus()
    {
        return _transports.Select(t => new TransportStatus(t.Name, t.IsRunning)).ToList();
    }

    /// <summary>
    /// Check if any transport is running.
    /// </summary>
    public static bool IsRunning => _running && _transports.Any(t => t.IsRunning);
}

/// <summary>
/// Transport status.
/// </summary>
public record TransportStatus(string Name, bool Running);

/// <summary>
/// Stdio MCP Transport - the default transport for CLI usage.
/// </summary>
public class StdioMcpTransport : IMcpTransport
{
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private Func<string, Task<string>>? _handler;

    public string Name => "stdio";
    public bool IsRunning => _readTask != null && !_readTask.IsCompleted;

    public async Task StartAsync(Func<string, Task<string>> requestHandler)
    {
        _handler = requestHandler;
        _cts = new CancellationTokenSource();

        _readTask = Task.Run(async () =>
        {
            using var stdin = Console.OpenStandardInput();
            using var stdout = Console.OpenStandardOutput();
            using var reader = new StreamReader(stdin);
            using var writer = new StreamWriter(stdout) { AutoFlush = true };

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    var response = await _handler(line);
                    if (!string.IsNullOrEmpty(response))
                    {
                        await writer.WriteLineAsync(response);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Stdio] Error: {ex.Message}");
                }
            }
        }, _cts.Token);

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_readTask != null)
        {
            await Task.WhenAny(_readTask, Task.Delay(1000));
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}

/// <summary>
/// HTTP MCP Transport - serves MCP over HTTP POST requests.
/// </summary>
public class HttpMcpTransport : IMcpTransport
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private Func<string, Task<string>>? _handler;
    private readonly int _port;
    private readonly string _path;

    public string Name => $"http://localhost:{_port}{_path}";
    public bool IsRunning => _listener?.IsListening == true;

    public HttpMcpTransport(int port = 5100, string path = "/mcp")
    {
        _port = port;
        _path = path;
    }

    public async Task StartAsync(Func<string, Task<string>> requestHandler)
    {
        _handler = requestHandler;
        _cts = new CancellationTokenSource();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}{_path}/");

        try
        {
            _listener.Start();

            _listenTask = Task.Run(async () =>
            {
                while (_listener.IsListening && !_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = HandleRequestAsync(context);
                    }
                    catch (HttpListenerException) when (_cts.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[HTTP] Error: {ex.Message}");
                    }
                }
            }, _cts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[HTTP] Failed to start: {ex.Message}");
            throw;
        }

        await Task.CompletedTask;
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            // CORS headers
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            if (request.HttpMethod != "POST")
            {
                response.StatusCode = 405;
                response.Close();
                return;
            }

            // Read request body
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();

            // Process through MCP handler
            var result = await _handler!(body);

            // Send response
            var buffer = Encoding.UTF8.GetBytes(result);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
            response.Close();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[HTTP] Request error: {ex.Message}");
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();

        if (_listenTask != null)
        {
            await Task.WhenAny(_listenTask, Task.Delay(1000));
        }
    }

    public void Dispose()
    {
        _listener?.Close();
        _cts?.Dispose();
    }
}

/// <summary>
/// SSE MCP Transport - serves MCP over Server-Sent Events.
/// </summary>
public class SseMcpTransport : IMcpTransport
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private Func<string, Task<string>>? _handler;
    private readonly int _port;
    private readonly string _path;
    private readonly List<StreamWriter> _clients = new();
    private readonly object _clientLock = new();

    public string Name => $"sse://localhost:{_port}{_path}";
    public bool IsRunning => _listener?.IsListening == true;

    public SseMcpTransport(int port = 5101, string path = "/mcp/sse")
    {
        _port = port;
        _path = path;
    }

    public async Task StartAsync(Func<string, Task<string>> requestHandler)
    {
        _handler = requestHandler;
        _cts = new CancellationTokenSource();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}{_path}/");
        _listener.Prefixes.Add($"http://localhost:{_port}{_path}/message/");

        try
        {
            _listener.Start();

            _listenTask = Task.Run(async () =>
            {
                while (_listener.IsListening && !_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = HandleRequestAsync(context);
                    }
                    catch (HttpListenerException) when (_cts.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[SSE] Error: {ex.Message}");
                    }
                }
            }, _cts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SSE] Failed to start: {ex.Message}");
            throw;
        }

        await Task.CompletedTask;
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // CORS headers
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 200;
            response.Close();
            return;
        }

        // SSE endpoint for event stream
        if (request.RawUrl?.EndsWith("/sse") == true || request.RawUrl?.EndsWith("/sse/") == true)
        {
            await HandleSseConnectionAsync(context);
            return;
        }

        // Message endpoint for receiving requests
        if (request.RawUrl?.Contains("/message") == true)
        {
            await HandleMessageAsync(context);
            return;
        }

        response.StatusCode = 404;
        response.Close();
    }

    private async Task HandleSseConnectionAsync(HttpListenerContext context)
    {
        var response = context.Response;

        response.ContentType = "text/event-stream";
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");

        var writer = new StreamWriter(response.OutputStream) { AutoFlush = true };

        lock (_clientLock)
        {
            _clients.Add(writer);
        }

        Console.Error.WriteLine($"[SSE] Client connected. Total: {_clients.Count}");

        // Send initial connection event
        await writer.WriteLineAsync("event: connected");
        await writer.WriteLineAsync($"data: {{\"message\": \"Connected to ZIMA MCP SSE\"}}");
        await writer.WriteLineAsync();

        // Keep connection alive
        try
        {
            while (!_cts!.Token.IsCancellationRequested)
            {
                await Task.Delay(30000, _cts.Token); // Heartbeat every 30s
                await writer.WriteLineAsync(": heartbeat");
                await writer.WriteLineAsync();
            }
        }
        catch
        {
            // Client disconnected
        }
        finally
        {
            lock (_clientLock)
            {
                _clients.Remove(writer);
            }
            Console.Error.WriteLine($"[SSE] Client disconnected. Total: {_clients.Count}");
        }
    }

    private async Task HandleMessageAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod != "POST")
            {
                response.StatusCode = 405;
                response.Close();
                return;
            }

            // Read request body
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();

            // Process through MCP handler
            var result = await _handler!(body);

            // Broadcast result to all SSE clients
            await BroadcastEventAsync("message", result);

            // Also send direct response
            var buffer = Encoding.UTF8.GetBytes(result);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
            response.Close();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SSE] Message error: {ex.Message}");
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    /// <summary>
    /// Broadcast an event to all connected SSE clients.
    /// </summary>
    public async Task BroadcastEventAsync(string eventType, string data)
    {
        List<StreamWriter> clients;
        lock (_clientLock)
        {
            clients = _clients.ToList();
        }

        foreach (var client in clients)
        {
            try
            {
                await client.WriteLineAsync($"event: {eventType}");
                await client.WriteLineAsync($"data: {data}");
                await client.WriteLineAsync();
            }
            catch
            {
                // Client disconnected, will be cleaned up later
            }
        }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();

        lock (_clientLock)
        {
            _clients.Clear();
        }

        if (_listenTask != null)
        {
            await Task.WhenAny(_listenTask, Task.Delay(1000));
        }
    }

    public void Dispose()
    {
        _listener?.Close();
        _cts?.Dispose();
    }
}

/// <summary>
/// Factory for creating transports.
/// </summary>
public static class McpTransportFactory
{
    public static IMcpTransport CreateStdio()
    {
        return new StdioMcpTransport();
    }

    public static IMcpTransport CreateHttp(int port = 5100, string path = "/mcp")
    {
        return new HttpMcpTransport(port, path);
    }

    public static IMcpTransport CreateSse(int port = 5101, string path = "/mcp/sse")
    {
        return new SseMcpTransport(port, path);
    }
}
