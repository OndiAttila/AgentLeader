using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

var commandArgs = args.Length > 0 ? args : Environment.GetCommandLineArgs().Skip(1).ToArray();

int port = 5000;
string host = "localhost";
bool runServer = false;
for (int i = 0; i < commandArgs.Length; i++)
{
    if (commandArgs[i] == "--server")
    {
        runServer = true;
    }
    if (commandArgs[i] == "--port" && i + 1 < commandArgs.Length)
    {
        if (int.TryParse(commandArgs[i + 1], out var parsedPort))
        {
            port = parsedPort;
            i++;
        }
    }
    if (commandArgs[i] == "--host" && i + 1 < commandArgs.Length)
    {
        host = commandArgs[i + 1];
        i++;
    }
}

if (runServer)
{
    RunHttpServer(host, port);
}
else
{
    RunStdioServer();
}

static void RunStdioServer()
{
    while (true)
    {
        var line = Console.ReadLine();
        if (string.IsNullOrEmpty(line)) break;

        using var doc = JsonDocument.Parse(line);
        var id = doc.RootElement.TryGetProperty("id", out var idProp) ? (JsonElement?)idProp : null;
        var result = HandleMcpRequest(doc.RootElement);

        if (result != null)
        {
            var response = new { jsonrpc = "2.0", id, result };
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            Console.WriteLine(json);
        }
    }
}

static void RunHttpServer(string host, int port)
{
    var builder = WebApplication.CreateBuilder();
    var app = builder.Build();

    app.MapPost("/mcp", async (HttpContext context) =>
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        var result = HandleMcpRequest(doc.RootElement);

        if (result != null)
        {
            var id = doc.RootElement.TryGetProperty("id", out var idProp) ? (JsonElement?)idProp : null;
            var response = new { jsonrpc = "2.0", id, result };
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    });

    app.MapGet("/", () => "MCPServer is running. POST to /mcp for MCP protocol.");

    Console.WriteLine($"Starting MCPServer HTTP on http://{host}:{port}/mcp");
    app.Run($"http://{host}:{port}");
}

static object? HandleMcpRequest(JsonElement root)
{
    if (!root.TryGetProperty("method", out var methodProp))
        return null;

    var method = methodProp.GetString();

    switch (method)
    {
        case "initialize":
            return new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new { name = "MCPServer", version = "1.0.0" }
            };

        case "tools/list":
            var tools = GetTools();
            return new { tools = tools.Values };

        case "tools/call":
            if (root.TryGetProperty("params", out var parameters))
            {
                return ExecuteTool(parameters);
            }
            break;
    }

    return null;
}

static Dictionary<string, object> GetTools()
{
    return new Dictionary<string, object>
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
}

static object ExecuteTool(JsonElement parameters)
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
    return result;
}
