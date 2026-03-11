using System.Text.Json.Serialization;

namespace AgentLead.Models;

public class McpServerConfig
{
    public string Name { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = "stdio";
    public string? Command { get; set; }
    public string? Args { get; set; }
    public string? Url { get; set; }
}

public class McpJsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class McpJsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

public class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

public class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string? ProtocolVersion { get; set; }

    [JsonPropertyName("capabilities")]
    public McpCapabilities? Capabilities { get; set; }

    [JsonPropertyName("serverInfo")]
    public McpServerInfo? ServerInfo { get; set; }
}

public class McpCapabilities
{
    [JsonPropertyName("tools")]
    public object? Tools { get; set; }
}

public class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

public class McpToolsListResult
{
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = new();
}

public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]
    public object? InputSchema { get; set; }
}

public class McpToolCallParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public object? Arguments { get; set; }
}

public class McpToolCallResult
{
    [JsonPropertyName("content")]
    public List<McpContentItem> Content { get; set; } = new();
}

public class McpContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
