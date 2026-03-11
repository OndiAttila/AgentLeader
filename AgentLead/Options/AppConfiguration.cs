using AgentLead.Models;

namespace AgentLead.Options;

public class AppConfiguration
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ModelName { get; set; } = "gpt-4";
    public string ApiKey { get; set; } = "dummy";
    public string? SystemMessage { get; set; } = null;
    public bool Help { get; set; } = false;
    public List<McpServerConfig> McpServers { get; set; } = new();

    public static AppConfiguration Parse(string[] args)
    {
        var options = new AppConfiguration();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();

            if (arg == "--help" || arg == "-h")
            {
                options.Help = true;
                return options;
            }

            if ((arg == "--base-url" || arg == "-u") && i + 1 < args.Length)
            {
                options.BaseUrl = args[++i];
                continue;
            }

            if (arg == "--api-key" && i + 1 < args.Length)
            {
                options.ApiKey = args[++i];
                continue;
            }

            if ((arg == "--model" || arg == "-m") && i + 1 < args.Length)
            {
                options.ModelName = args[++i];
                continue;
            }

            if (arg.StartsWith("--base-url="))
            {
                options.BaseUrl = arg.Substring("--base-url=".Length);
                continue;
            }

            if (arg.StartsWith("-u="))
            {
                options.BaseUrl = arg.Substring("-u=".Length);
                continue;
            }

            if (arg.StartsWith("--api-key="))
            {
                options.ApiKey = arg.Substring("--api-key=".Length);
                continue;
            }

            if (arg.StartsWith("--model="))
            {
                options.ModelName = arg.Substring("--model=".Length);
                continue;
            }

            if (arg.StartsWith("-m="))
            {
                options.ModelName = arg.Substring("-m=".Length);
                continue;
            }

            if ((arg == "--mcp-servers" || arg == "-mcp") && i + 1 < args.Length)
            {
                var mcpConfigPath = args[++i];
#region debug
Console.WriteLine($"[debug] reading MCP configuration from: {mcpConfigPath}");
#endregion
                if (!options.LoadMcpServersFromFile(mcpConfigPath))
                {
                    var msg = $"Error: Failed to load MCP servers from: {mcpConfigPath}";
                    Console.WriteLine(msg);
                    throw new InvalidOperationException(msg);
                }
                continue;
            }

            if (arg.StartsWith("--mcp-servers=") || arg.StartsWith("-mcp="))
            {
                var mcpConfigPath = arg.Contains("=") ? arg.Substring(arg.IndexOf('=') + 1) : "";
                if (!string.IsNullOrEmpty(mcpConfigPath) && !options.LoadMcpServersFromFile(mcpConfigPath))
                {
                    var msg = $"Error: Failed to load MCP servers from: {mcpConfigPath}";
                    Console.WriteLine(msg);
                    throw new InvalidOperationException(msg);
                }
                continue;
            }

            {
                var msg = $"Error: Unknown option: {arg}";
                Console.WriteLine(msg);
                throw new InvalidOperationException(msg);
            }
        }

        return options;
    }

    public bool LoadMcpServersFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: MCP config file not found: {filePath}");
            return false;
        }

        try
        {
            var json = File.ReadAllText(filePath);
#region debug
Console.WriteLine($"[debug] read MCP configuration json: {json}");
#endregion
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var servers = System.Text.Json.JsonSerializer.Deserialize<List<McpServerConfig>>(json, options);
            if (servers != null)
            {
                McpServers = servers;
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading MCP config: {ex.Message}");
        }
        return false;
    }

    public void SetApiKey(string apiKey) 
    {
        ApiKey = apiKey.Trim();
    }

    public bool SetSystemMessageFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File not found: {filePath}");
            return false;
        }

        try
        {
            SystemMessage = File.ReadAllText(filePath).Trim();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
            return false;
        }
    }

    public void ClearSystemMessage()
    {
        SystemMessage = null;
    }

    public bool PromptForApiKey() 
    {
        Console.Write("Enter your OpenAI API key: ");
        var key = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(key)) {
            ApiKey = key.Trim();
        }
        return true;
    }

    public void DisplayCurrentSettings()
    {
        Console.WriteLine();
        Console.WriteLine("Current Settings:");
        Console.WriteLine($"  Base URL: {BaseUrl}");
        Console.WriteLine($"  Model: {ModelName}");
        Console.WriteLine($"  API Key: {(!string.IsNullOrWhiteSpace(ApiKey) ? "Set" : "Not Set")}");
        Console.WriteLine($"  System Message: {(string.IsNullOrWhiteSpace(SystemMessage) ? "Not Set" : "Set")}");
        Console.WriteLine($"  MCP Servers: {(McpServers.Count > 0 ? $"{McpServers.Count} configured" : "None")}");
        foreach (var server in McpServers)
        {
            var serverDetail = server.ConnectionType == "stdio" ? server.Command : server.Url;
            Console.WriteLine($"    - {server.Name} ({server.ConnectionType}: {serverDetail})");
        }
        Console.WriteLine();
    }

    public static void PrintHelp()
    {
        Console.WriteLine("AgentLead - CLI tool for querying LLMs via OpenAI API");
        Console.WriteLine();
        Console.WriteLine("Usage: AgentLead [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --base-url, -u <url>    OpenAI API base URL (default: https://api.openai.com/v1)");
        Console.WriteLine("  --model, -m <name>      LLM model name (default: gpt-4)");
        Console.WriteLine("  --mcp-servers, -mcp <file>  Path to MCP servers JSON config file");
        Console.WriteLine("  --help, -h             Show this help message");
        Console.WriteLine();
        Console.WriteLine("Interactive Commands:");
        Console.WriteLine("  /base-url <url>, /u <url>   Set base URL");
        Console.WriteLine("  /model <name>, /m <name>    Set model name");
        Console.WriteLine("  /s <file-path>              Set system message from file");
        Console.WriteLine("  /s clear                    Clear system message");
        Console.WriteLine("  /quit, /exit, /q             Exit the program");
    }
}
