namespace AgentLead.Options;

public class AppConfiguration
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ModelName { get; set; } = "gpt-4";
    public string ApiKey { get; set; } = "dummy";
    public bool Help { get; set; } = false;

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
        }

        return options;
    }

    public void SetApiKey(string apiKey) 
    {
        ApiKey = apiKey.Trim();
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
        Console.WriteLine("  --model, -m <name>     LLM model name (default: gpt-4)");
        Console.WriteLine("  --help, -h             Show this help message");
        Console.WriteLine();
        Console.WriteLine("Interactive Commands:");
        Console.WriteLine("  /base-url <url>, /u <url>   Set base URL");
        Console.WriteLine("  /model <name>, /m <name>    Set model name");
        Console.WriteLine("  /quit, /exit, /q             Exit the program");
    }
}
