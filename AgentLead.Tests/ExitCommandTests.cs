using AgentLead.Options;
using Xunit;

namespace AgentLead.Tests;

public class ExitCommandTests
{
    [Theory]
    [InlineData("/quit")]
    [InlineData("/quit ")]
    [InlineData("/QUIT")]
    [InlineData("/Quit")]
    public void IsExitCommand_Quit_ReturnsTrue(string input)
    {
        var result = IsExitCommand(input);
        
        Assert.True(result);
    }

    [Theory]
    [InlineData("/exit")]
    [InlineData("/exit ")]
    [InlineData("/EXIT")]
    [InlineData("/Exit")]
    public void IsExitCommand_Exit_ReturnsTrue(string input)
    {
        var result = IsExitCommand(input);
        
        Assert.True(result);
    }

    [Theory]
    [InlineData("/q")]
    [InlineData("/q ")]
    [InlineData("/Q")]
    public void IsExitCommand_Q_ReturnsTrue(string input)
    {
        var result = IsExitCommand(input);
        
        Assert.True(result);
    }

    [Theory]
    [InlineData("quit")]
    [InlineData("exit")]
    [InlineData("q")]
    [InlineData("/quitx")]
    [InlineData("/exitz")]
    [InlineData("/qq")]
    public void IsExitCommand_NonExitCommands_ReturnsFalse(string input)
    {
        var result = IsExitCommand(input);
        
        Assert.False(result);
    }

    [Theory]
    [InlineData("/base-url https://custom.api.com", true)]
    [InlineData("/u https://custom.api.com", true)]
    [InlineData("/model gpt-3.5-turbo", true)]
    [InlineData("/m gpt-3.5-turbo", true)]
    [InlineData("/quit", false)]
    [InlineData("/exit", false)]
    [InlineData("/q", false)]
    [InlineData("hello", false)]
    public void IsConfigCommand_DetectsConfigCommands(string input, bool expected)
    {
        var result = IsConfigCommand(input);
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/base-url https://custom.api.com", "https://custom.api.com")]
    [InlineData("/u https://custom.api.com", "https://custom.api.com")]
    [InlineData("/model gpt-3.5-turbo", "gpt-3.5-turbo")]
    [InlineData("/m gpt-3.5-turbo", "gpt-3.5-turbo")]
    public void ParseConfigCommand_ParsesCorrectly(string input, string expectedValue)
    {
        var (command, value) = ParseConfigCommand(input);
        
        Assert.NotNull(value);
        Assert.Contains(expectedValue, value);
    }

    private static bool IsExitCommand(string input)
    {
        return input.Trim().Equals("/quit", StringComparison.OrdinalIgnoreCase) ||
               input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase) ||
               input.Trim().Equals("/q", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConfigCommand(string input)
    {
        var trimmed = input.Trim();
        return trimmed.StartsWith("/base-url ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("/u ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("/model ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("/m ", StringComparison.OrdinalIgnoreCase);
    }

    private static (string command, string? value) ParseConfigCommand(string input)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }
        return (input, null);
    }
}
