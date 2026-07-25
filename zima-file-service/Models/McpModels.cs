using System.Text.Json.Serialization;

namespace ZimaFileService.Models;

// JSON-RPC Request/Response models for MCP protocol
public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    [JsonPropertyName("params")]
    public JsonRpcParams? Params { get; set; }
}

public class JsonRpcParams
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; set; }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

// MCP-specific models
public class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "zima-file-service";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

public class McpCapabilities
{
    [JsonPropertyName("tools")]
    public McpToolsCapability? Tools { get; set; }
}

public class McpToolsCapability
{
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; } = false;
}

public class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();

    [JsonPropertyName("capabilities")]
    public McpCapabilities Capabilities { get; set; } = new() { Tools = new() };
}

public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("inputSchema")]
    public McpInputSchema InputSchema { get; set; } = new();
}

public class McpInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, McpPropertySchema> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

public class McpPropertySchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("items")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpPropertySchema? Items { get; set; }
}

public class McpToolsListResult
{
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = new();
}

public class McpToolResult
{
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; set; } = new();

    [JsonPropertyName("isError")]
    public bool IsError { get; set; } = false;
}

public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
