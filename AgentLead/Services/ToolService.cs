using System.Text.Json;
using AgentLead.Models;

namespace AgentLead.Services;

public class ToolService
{
    private readonly Dictionary<string, Func<string, Task<string>>> _builtInTools = new();
    private readonly List<ToolDefinition> _toolDefinitions = new();
    private McpClientService? _mcpClientService;

    public ToolService()
    {
        RegisterBuiltInTools();
    }

    private void RegisterBuiltInTools()
    {
        RegisterBuiltInTool("echo", "Prints the given text to the console", Echo);
        RegisterBuiltInTool("ls", "Lists files in the current directory", Ls);

        _toolDefinitions.Add(new ToolDefinition
        {
            McpName = null,
            ToolName = "echo",
            Tool = new Tool
            {
                Type = "function",
                Function = new FunctionDefinition
                {
                    Name = "echo",
                    Description = "Prints the given text to the console",
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            text = new { type = "string", description = "The text to print" }
                        },
                        required = new[] { "text" }
                    }
                }
            }
        });

        _toolDefinitions.Add(new ToolDefinition
        {
            McpName = null,
            ToolName = "ls",
            Tool = new Tool
            {
                Type = "function",
                Function = new FunctionDefinition
                {
                    Name = "ls",
                    Description = "Lists files in the current directory",
                    Parameters = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            }
        });
    }

    public void SetMcpClientService(McpClientService mcpClientService)
    {
        _mcpClientService = mcpClientService;
        RefreshMcpToolDefinitions();
    }

    public void RefreshMcpToolDefinitions()
    {
        _toolDefinitions.RemoveAll(t => t.McpName != null);

        if (_mcpClientService == null) return;

        foreach (var kvp in _mcpClientService.ToolDefinitions)
        {
            _toolDefinitions.Add(new ToolDefinition
            {
                McpName = kvp.McpName,
                ToolName = kvp.ToolName,
                Tool = kvp.Tool
            });
        }
    }

    private void RegisterBuiltInTool(string name, string description, Func<string, Task<string>> handler)
    {
        _builtInTools[name] = handler;
    }

    public List<Tool> GetToolDefinitions()
    {
        return _toolDefinitions.Select(td => td.Tool).ToList();
    }

    public async Task<string> ExecuteToolAsync(string toolName, string arguments)
    {
        if (_mcpClientService != null && _mcpClientService.HasMcpTools())
        {
            var isMcpTool = _toolDefinitions.Any(t => t.Tool.Function.Name == toolName && t.McpName != null);
            if (isMcpTool)
            {
                try
                {
                    return await _mcpClientService.ExecuteToolAsync(toolName, arguments);
                }
                catch (Exception ex)
                {
                    return $"Error executing MCP tool: {ex.Message}";
                }
            }
        }

        if (_builtInTools.TryGetValue(toolName, out var handler))
        {
            try
            {
                return await handler(arguments);
            }
            catch (Exception ex)
            {
                return $"Error executing tool: {ex.Message}";
            }
        }

        return $"Error: Unknown tool '{toolName}'";
    }

    private Task<string> Echo(string arguments)
    {
        string text;
        try
        {
            using var doc = JsonDocument.Parse(arguments);
            text = doc.RootElement.GetProperty("text").GetString() ?? "";
        }
        catch
        {
            text = arguments;
        }

        Console.WriteLine(text);
        return Task.FromResult(text);
    }

    private Task<string> Ls(string arguments)
    {
        var files = Directory.GetFiles(Directory.GetCurrentDirectory())
            .Select(Path.GetFileName)
            .ToList();
        
        var result = string.Join(Environment.NewLine, files);
        return Task.FromResult(result);
    }
}
