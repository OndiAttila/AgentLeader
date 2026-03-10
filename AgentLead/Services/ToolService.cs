using System.Text.Json;
using AgentLead.Models;

namespace AgentLead.Services;

public class ToolService
{
    private readonly Dictionary<string, Func<string, Task<string>>> _tools = new();

    public ToolService()
    {
        RegisterTool("echo", "Prints the given text to the console", Echo);
        RegisterTool("ls", "Lists files in the current directory", Ls);
    }

    private void RegisterTool(string name, string description, Func<string, Task<string>> handler)
    {
        _tools[name] = handler;
    }

    public List<Tool> GetToolDefinitions()
    {
        return new List<Tool>
        {
            new Tool
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
            },
            new Tool
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
        };
    }

    public async Task<string> ExecuteToolAsync(string toolName, string arguments)
    {
        if (!_tools.TryGetValue(toolName, out var handler))
        {
            return $"Error: Unknown tool '{toolName}'";
        }

        try
        {
            return await handler(arguments);
        }
        catch (Exception ex)
        {
            return $"Error executing tool: {ex.Message}";
        }
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
