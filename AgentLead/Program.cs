using System.Security;
using AgentLead;
using AgentLead.Options;
using AgentLead.Services;

var config = AppConfiguration.Parse(args);

if (config.Help)
{
    AppConfiguration.PrintHelp();
    return;
}

if (!config.PromptForApiKey())
{
    return;
}

config.DisplayCurrentSettings();

var client = new OpenAIClient();
var shouldExit = false;

while (!shouldExit)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    input = input.Trim();

    if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("/exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("/q", StringComparison.OrdinalIgnoreCase))
    {
        shouldExit = true;
        continue;
    }

    if (input.StartsWith("/base-url ", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("/u ", StringComparison.OrdinalIgnoreCase))
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            config.BaseUrl = parts[1];
            Console.WriteLine($"Base URL set to: {config.BaseUrl}");
        }
        else
        {
            Console.WriteLine("Usage: /base-url <url> or /bu <url>");
        }
        continue;
    }

    if (input.StartsWith("/model ", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("/m ", StringComparison.OrdinalIgnoreCase))
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            config.ModelName = parts[1];
            Console.WriteLine($"Model set to: {config.ModelName}");
        }
        else
        {
            Console.WriteLine("Usage: /model <name> or /m <name>");
        }
        continue;
    }

    if (input == "/s")
    {
        config.ClearSystemMessage();
        Console.WriteLine("System message cleared.");
        continue;
    }

    if (input.StartsWith("/s ", StringComparison.OrdinalIgnoreCase))
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            if (config.SetSystemMessageFromFile(parts[1]))
            {
                Console.WriteLine("System message set successfully.");
            }
        }
        else
        {
            Console.WriteLine("Usage: /s <file-path> (to set a System message) or /s (to clear the current System message)");
        }
        continue;
    }

    if (input.StartsWith("/"))
    {
        Console.WriteLine("Unknown command. Available commands:");
        Console.WriteLine("  /base-url <url>, /u <url>   Set base URL");
        Console.WriteLine("  /model <name>, /m <name>    Set model name");
        Console.WriteLine("  /s <file-path>              Set system message from file");
        Console.WriteLine("  /s                          Clear system message");
        Console.WriteLine("  /quit, /exit, /q            Exit the program");
        continue;
    }

    Console.WriteLine();
    var fullResponse = new System.Text.StringBuilder();
    var isFirstChunk = true;

    try
    {
        var success = await client.SendChatMessageAsync(
            config.BaseUrl,
            config.ModelName,
            config.ApiKey,
            input,
            config.SystemMessage,
            chunk =>
            {
                if (isFirstChunk)
                {
                    isFirstChunk = false;
                }
                fullResponse.Append(chunk);
                Console.Write(chunk);
            });

        if (!success)
        {
            Console.WriteLine("\n\nFailed to get response. You can try again or use /quit to exit.");
        }
        else
        {
            Console.WriteLine();
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n\nOperation cancelled.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n\nError: {ex.Message}");
        Console.WriteLine("You can try again or use /quit to exit.");
    }
}

Console.WriteLine("Goodbye!");
