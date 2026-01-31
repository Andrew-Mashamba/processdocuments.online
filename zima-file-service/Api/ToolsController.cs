using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ZimaFileService.Api;

/// <summary>
/// API endpoints for tool discovery and execution.
/// Used by the agent to discover and execute tools programmatically.
/// </summary>
[ApiController]
[Route("api/tools")]
public class ToolsController : ControllerBase
{
    private static readonly McpServer _mcpServer = new();

    /// <summary>
    /// List all available tools.
    /// GET /api/tools
    /// </summary>
    [HttpGet]
    public IActionResult ListTools([FromQuery] string? category = null)
    {
        var tools = ToolsRegistry.Instance.GetAllMcpTools();

        if (!string.IsNullOrEmpty(category))
        {
            tools = tools.Where(t => t.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        var result = tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            category = t.Category,
            usage = t.Usage,
            isCustom = t.IsCustom
        });

        return Ok(new
        {
            total = tools.Count,
            tools = result
        });
    }

    /// <summary>
    /// Get categories of tools.
    /// GET /api/tools/categories
    /// </summary>
    [HttpGet("categories")]
    public IActionResult ListCategories()
    {
        var tools = ToolsRegistry.Instance.GetAllMcpTools();
        var categories = tools
            .Where(t => !string.IsNullOrEmpty(t.Category))
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                name = g.Key,
                count = g.Count()
            })
            .OrderByDescending(c => c.count);

        return Ok(categories);
    }

    /// <summary>
    /// Get schema for a specific tool.
    /// GET /api/tools/{name}/schema
    /// </summary>
    [HttpGet("{name}/schema")]
    public IActionResult GetToolSchema(string name)
    {
        var tool = ToolsRegistry.Instance.GetTool(name);
        if (tool == null)
        {
            return NotFound(new { error = $"Tool '{name}' not found" });
        }

        return Ok(new
        {
            name = tool.Name,
            description = tool.Description,
            usage = tool.Usage,
            category = tool.Category,
            inputSchema = tool.InputSchema,
            isCustom = tool.IsCustom
        });
    }

    /// <summary>
    /// Execute a tool with given arguments.
    /// POST /api/tools/execute
    /// Body: { "tool": "create_excel", "arguments": { "file_path": "...", ... } }
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteTool([FromBody] ToolExecuteRequest request)
    {
        if (string.IsNullOrEmpty(request.Tool))
        {
            return BadRequest(new { error = "Tool name is required" });
        }

        var tool = ToolsRegistry.Instance.GetTool(request.Tool);
        if (tool == null)
        {
            return NotFound(new { error = $"Tool '{request.Tool}' not found" });
        }

        try
        {
            // Convert arguments to Dictionary<string, object>
            var arguments = new Dictionary<string, object>();
            if (request.Arguments.ValueKind != JsonValueKind.Undefined && request.Arguments.ValueKind != JsonValueKind.Null)
            {
                foreach (var prop in request.Arguments.EnumerateObject())
                {
                    arguments[prop.Name] = ConvertJsonElement(prop.Value);
                }
            }

            // Execute the tool
            var result = await _mcpServer.InvokeToolDirectAsync(request.Tool, arguments);

            // Parse result (it's usually JSON)
            try
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(result);
                return Ok(new
                {
                    success = true,
                    tool = request.Tool,
                    result = parsed
                });
            }
            catch
            {
                return Ok(new
                {
                    success = true,
                    tool = request.Tool,
                    result = result
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                tool = request.Tool,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Execute a tool by name (shorthand).
    /// POST /api/tools/{name}/execute
    /// Body: { "file_path": "...", ... }
    /// </summary>
    [HttpPost("{name}/execute")]
    public async Task<IActionResult> ExecuteToolByName(string name, [FromBody] JsonElement arguments)
    {
        var request = new ToolExecuteRequest
        {
            Tool = name,
            Arguments = arguments
        };
        return await ExecuteTool(request);
    }

    /// <summary>
    /// Search tools by name or description.
    /// GET /api/tools/search?q=excel
    /// </summary>
    [HttpGet("search")]
    public IActionResult SearchTools([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q))
        {
            return BadRequest(new { error = "Search query 'q' is required" });
        }

        var tools = ToolsRegistry.Instance.GetAllMcpTools();
        var matches = tools.Where(t =>
            t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            (t.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
        ).Select(t => new
        {
            name = t.Name,
            description = t.Description,
            category = t.Category,
            usage = t.Usage
        });

        return Ok(new
        {
            query = q,
            count = matches.Count(),
            tools = matches
        });
    }

    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.GetRawText()
        };
    }
}

public class ToolExecuteRequest
{
    public string Tool { get; set; } = "";
    public JsonElement Arguments { get; set; }
}
