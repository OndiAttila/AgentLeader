using System.Diagnostics;
using System.Text.Json;
using AgentLead.Models;

namespace AgentLead.Services;

public class McpClient : IDisposable
{
    private readonly McpServerConfig _config;
    private readonly HttpClient? _httpClient;
    private Process? _process;
    private StreamWriter? _stdinWriter;
    private StreamReader? _stdoutReader;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int _requestId = 1;
    private readonly Dictionary<string, string> _serverInfo = new();
    private bool _initialized = false;

    public string ServerName => _serverInfo.GetValueOrDefault("name", _config.Name);
    public string ServerVersion => _serverInfo.GetValueOrDefault("version", "unknown");

    public McpClient(McpServerConfig config)
    {
        _config = config;

        if (_config.ConnectionType.Equals("stdio", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(_config.Command))
            {
                throw new InvalidOperationException("Stdio MCP server requires a command");
            }
        }
        else if (config.ConnectionType.Equals("http", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(config.Url))
        {
            if (string.IsNullOrEmpty(_config.Url))
            {
                throw new InvalidOperationException("HTTP MCP server requires a URL");
            }
            _httpClient = new HttpClient();
            _initialized = true;
        }
    }

    public async Task ConnectAsync()
    {
        if (_config.ConnectionType.Equals("stdio", StringComparison.OrdinalIgnoreCase))
        {
            await ConnectStdioAsync();
        }
        else if (_config.ConnectionType.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            // do nothing
        }
        else
        {
            throw new InvalidOperationException($"Unknown MCP connection type: {_config.ConnectionType}");
        }
    }

    private async Task ConnectStdioAsync()
    {
        var args = string.IsNullOrEmpty(_config.Args) ? "" : _config.Args;
        var argumentList = string.IsNullOrEmpty(args) 
            ? new List<string>() 
            : args.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        var startInfo = new ProcessStartInfo
        {
            FileName = _config.Command,
            Arguments = string.Join(" ", argumentList),
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _process = new Process { StartInfo = startInfo };
        _process.Start();

        _stdinWriter = _process.StandardInput;
        _stdoutReader = _process.StandardOutput;

        await Task.Yield();
        _initialized = true;
    }

    public async Task InitializeAsync()
    {
        if (!_initialized)
        {
            await ConnectAsync();
        }

        var request = new McpJsonRpcRequest
        {
            Id = NextId(),
            Method = "initialize",
            Params = new { }
        };

        var response = await SendRequestAsync<McpInitializeResult>(request);

        if (response?.ServerInfo != null)
        {
            _serverInfo["name"] = response.ServerInfo.Name;
            _serverInfo["version"] = response.ServerInfo.Version;
        }
    }

    public async Task<List<McpTool>> ListToolsAsync()
    {
        var request = new McpJsonRpcRequest
        {
            Id = NextId(),
            Method = "tools/list",
            Params = new { }
        };

        var response = await SendRequestAsync<McpToolsListResult>(request);
        return response?.Tools ?? new List<McpTool>();
    }

    public async Task<string> CallToolAsync(string toolName, string arguments)
    {
        object? parsedArgs = null;
        if (!string.IsNullOrEmpty(arguments))
        {
            try
            {
                parsedArgs = JsonSerializer.Deserialize<object>(arguments);
            }
            catch
            {
                parsedArgs = arguments;
            }
        }

        var request = new McpJsonRpcRequest
        {
            Id = NextId(),
            Method = "tools/call",
            Params = new McpToolCallParams
            {
                Name = toolName,
                Arguments = parsedArgs
            }
        };

        var response = await SendRequestAsync<McpToolCallResult>(request);

        if (response?.Content == null || response.Content.Count == 0)
        {
            return "No content returned from tool";
        }

        return string.Join("\n", response.Content.Select(c => c.Text));
    }

    private async Task<T?> SendRequestAsync<T>(McpJsonRpcRequest request) where T : class
    {
        await _semaphore.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(request);
            string? responseJson = null;

            if (_config.ConnectionType.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient!.PostAsync(_config.Url, httpContent);
                httpResponse.EnsureSuccessStatusCode();
                responseJson = await httpResponse.Content.ReadAsStringAsync();
            }
            else
            {
                if (_stdinWriter == null || _stdoutReader == null)
                {
                    throw new InvalidOperationException("Stdio connection not initialized");
                }

                await _stdinWriter.WriteLineAsync(json);
                await _stdinWriter.FlushAsync();

                var line = await _stdoutReader.ReadLineAsync();
                responseJson = line;
            }

            if (string.IsNullOrEmpty(responseJson))
            {
                return null;
            }

            var doc = JsonDocument.Parse(responseJson);
            
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                var error = JsonSerializer.Deserialize<McpError>(errorElement);
                throw new InvalidOperationException($"MCP error: {error?.Message ?? "Unknown error"}");
            }

            if (doc.RootElement.TryGetProperty("result", out var resultElement))
            {
                return JsonSerializer.Deserialize<T>(resultElement);
            }

            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private object NextId()
    {
        return _requestId++;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        
        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill(true);
                _process.Dispose();
            }
            catch { }
        }
        
        _httpClient?.Dispose();
    }
}

public class McpClientService : IDisposable
{
    private readonly List<McpClient> _clients = new();
    private readonly List<ToolDefinition> _toolDefinitions = new();
    private readonly Dictionary<string, (string mcpName, string originalToolName)> _llmNameToMcp = new();
    private readonly Dictionary<(string mcpName, string toolName), McpClient> _toolToClient = new();
    private readonly Dictionary<string, int> _toolNameCounts = new();
    private bool _initialized = false;

    public IReadOnlyList<ToolDefinition> ToolDefinitions => _toolDefinitions;

    public async Task InitializeAsync(List<McpServerConfig> servers)
    {
        if (_initialized || servers.Count == 0)
        {
            return;
        }

        foreach (var serverConfig in servers)
        {
            try
            {
                var client = new McpClient(serverConfig);
                await client.InitializeAsync();

                var mcpName = client.ServerName;
                var tools = await client.ListToolsAsync();
                foreach (var tool in tools)
                {
                    var llmToolName = GetUniqueToolName(tool.Name);
                    
                    var toolDef = new ToolDefinition
                    {
                        McpName = mcpName,
                        ToolName = tool.Name,
                        Tool = new Tool
                        {
                            Type = "function",
                            Function = new FunctionDefinition
                            {
                                Name = llmToolName,
                                Description = $"[{mcpName}] {tool.Description}",
                                Parameters = tool.InputSchema ?? new { type = "object", properties = new { } }
                            }
                        }
                    };
                    _toolDefinitions.Add(toolDef);
                    _toolToClient[(mcpName, tool.Name)] = client;
                    _llmNameToMcp[llmToolName] = (mcpName, tool.Name);
                    Console.WriteLine($"[MCP] Registered tool '{llmToolName}' from server '{mcpName}' (v{client.ServerVersion})");
                }

                _clients.Add(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP] Failed to connect to server '{serverConfig.Name}': {ex.Message}");
            }
        }

        _initialized = true;
    }

    private string GetUniqueToolName(string originalName)
    {
        if (!_toolNameCounts.ContainsKey(originalName))
        {
            _toolNameCounts[originalName] = 0;
            return originalName;
        }

        var count = ++_toolNameCounts[originalName];
        return $"{originalName}_{count}";
    }

    public async Task<string> ExecuteToolAsync(string llmToolName, string arguments)
    {
        if (_llmNameToMcp.TryGetValue(llmToolName, out var mcpToolInfo))
        {
            if (_toolToClient.TryGetValue((mcpToolInfo.mcpName, mcpToolInfo.originalToolName), out var client))
            {
                return await client.CallToolAsync(mcpToolInfo.originalToolName, arguments);
            }
        }

        throw new InvalidOperationException($"Tool '{llmToolName}' not found in any MCP server");
    }

    public bool HasMcpTools() => _toolDefinitions.Count > 0;

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }
        _clients.Clear();
        _toolDefinitions.Clear();
        _toolToClient.Clear();
        _llmNameToMcp.Clear();
        _toolNameCounts.Clear();
    }
}
