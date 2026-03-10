using System.Text.Json;

var tools = new Dictionary<string, object>
{
    ["capitalize"] = new
    {
        name = "capitalize",
        description = "Returns the input text in all-caps format",
        inputSchema = new
        {
            type = "object",
            properties = new
            {
                text = new { type = "string", description = "The text to capitalize" }
            },
            required = new[] { "text" }
        }
    },
    ["datetime"] = new
    {
        name = "datetime",
        description = "Returns the current date and time in ISO 8601 format",
        inputSchema = new
        {
            type = "object",
            properties = new { }
        }
    }
};

while (true)
{
    var line = Console.ReadLine();
    if (string.IsNullOrEmpty(line)) break;

    using var doc = JsonDocument.Parse(line);
    var root = doc.RootElement;

    if (root.TryGetProperty("method", out var methodProp))
    {
        var method = methodProp.GetString();

        switch (method)
        {
            case "initialize":
                var initResponse = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { tools = tools.Values },
                    serverInfo = new { name = "MCPServer", version = "1.0.0" }
                };
                SendResponse(initResponse);
                break;

            case "tools/list":
                var listResponse = new { tools = tools.Values };
                SendResponse(listResponse);
                break;

            case "tools/call":
                if (root.TryGetProperty("params", out var parameters))
                {
                    var toolName = parameters.GetProperty("name").GetString();
                    JsonElement? arguments = parameters.TryGetProperty("arguments", out var toolArgs) ? toolArgs : null;

                    object result;
                    if (toolName == "capitalize" && arguments is { } capitalizeArgs && capitalizeArgs.TryGetProperty("text", out var textProp))
                    {
                        result = new { content = new[] { new { type = "text", text = textProp.GetString()?.ToUpper() } } };
                    }
                    else if (toolName == "datetime")
                    {
                        result = new { content = new[] { new { type = "text", text = DateTime.UtcNow.ToString("o") } } };
                    }
                    else
                    {
                        result = new { content = new[] { new { type = "text", text = "Unknown tool" } } };
                    }
                    SendResponse(result);
                }
                break;
        }
    }
}

void SendResponse(object response)
{
    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    Console.WriteLine(json);
}
