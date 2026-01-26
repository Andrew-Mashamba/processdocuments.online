using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ZimaFileService.Api;

[ApiController]
[Route("api/tools")]
public class ToolsController : ControllerBase
{
    private readonly McpServer _mcpServer;
    private readonly JsonSerializerOptions _jsonOptions;

    public ToolsController()
    {
        _mcpServer = new McpServer();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// List all available tools
    /// </summary>
    [HttpGet]
    public IActionResult ListTools()
    {
        var tools = ToolsRegistry.Instance.GetAllMcpTools();
        return Ok(new
        {
            count = tools.Count,
            tools = tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                category = t.Category
            })
        });
    }

    /// <summary>
    /// Invoke a tool directly
    /// POST /api/tools/{toolName}
    /// Session ID can be provided via:
    /// - Header: X-Session-Id
    /// - Body: session_id field (preferred for AI-generated requests)
    /// - Query: ?sessionId=xxx
    /// </summary>
    [HttpPost("{toolName}")]
    public async Task<IActionResult> InvokeTool(
        string toolName,
        [FromBody] JsonElement body,
        [FromHeader(Name = "X-Session-Id")] string? headerSessionId = null,
        [FromQuery] string? sessionId = null)
    {
        try
        {
            // Convert JsonElement to Dictionary
            var arguments = new Dictionary<string, object>();
            foreach (var prop in body.EnumerateObject())
            {
                arguments[prop.Name] = prop.Value;
            }

            // Get session ID - QUERY PARAMETER takes priority (can't be overridden by AI)
            string? effectiveSessionId = null;

            // Always remove session_id from body if present (don't let AI override)
            if (arguments.ContainsKey("session_id"))
            {
                arguments.Remove("session_id");
            }

            // 1. Query parameter takes HIGHEST priority (set by server, not AI)
            if (!string.IsNullOrEmpty(sessionId))
            {
                effectiveSessionId = sessionId;
            }
            // 2. Fall back to header
            else if (!string.IsNullOrEmpty(headerSessionId))
            {
                effectiveSessionId = headerSessionId;
            }

            Console.WriteLine($"[ToolsController] Query sessionId: {sessionId ?? "NONE"}");
            Console.WriteLine($"[ToolsController] Header sessionId: {headerSessionId ?? "NONE"}");
            Console.WriteLine($"[ToolsController] Effective Session ID: {effectiveSessionId ?? "NONE"}");

            // If sessionId provided, update file_path to use session folder
            if (!string.IsNullOrEmpty(effectiveSessionId) && arguments.ContainsKey("file_path"))
            {
                var originalPath = arguments["file_path"]?.ToString() ?? "";
                var fileName = Path.GetFileName(originalPath);
                var sessionPath = FileManager.Instance.GetSessionGeneratedPath(effectiveSessionId);
                arguments["file_path"] = Path.Combine(sessionPath, fileName);
                Console.WriteLine($"[ToolsController] Session path: {arguments["file_path"]}");
            }

            Console.WriteLine($"[ToolsController] Invoking: {toolName}");
            Console.WriteLine($"[ToolsController] Arguments: {JsonSerializer.Serialize(arguments)}");

            var result = await _mcpServer.InvokeToolDirectAsync(toolName, arguments);

            // Try to parse result as JSON, otherwise return as text
            try
            {
                var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
                return Ok(jsonResult);
            }
            catch
            {
                return Ok(new { success = true, result });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToolsController] Error: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }
}
