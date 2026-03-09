using AgentLead.Options;
using Xunit;

namespace AgentLead.Tests;

public class AppConfigurationTests
{
    [Fact]
    public void Parse_DefaultValues_AreAppliedWhenNoArgsProvided()
    {
        var config = AppConfiguration.Parse(Array.Empty<string>());

        Assert.Equal("https://api.openai.com/v1", config.BaseUrl);
        Assert.Equal("gpt-4", config.ModelName);
        Assert.False(config.Help);
    }

    [Theory]
    [InlineData(new[] { "--base-url", "https://custom.api.com/v1" }, "https://custom.api.com/v1")]
    [InlineData(new[] { "-u", "https://custom.api.com/v1" }, "https://custom.api.com/v1")]
    [InlineData(new[] { "--base-url=https://custom.api.com/v1" }, "https://custom.api.com/v1")]
    [InlineData(new[] { "-u=https://custom.api.com/v1" }, "https://custom.api.com/v1")]
    public void Parse_BaseUrlArgument_ParsedCorrectly(string[] args, string expectedBaseUrl)
    {
        var config = AppConfiguration.Parse(args);

        Assert.Equal(expectedBaseUrl, config.BaseUrl);
    }

    [Theory]
    [InlineData(new[] { "--model", "gpt-3.5-turbo" }, "gpt-3.5-turbo")]
    [InlineData(new[] { "-m", "gpt-3.5-turbo" }, "gpt-3.5-turbo")]
    [InlineData(new[] { "--model=gpt-3.5-turbo" }, "gpt-3.5-turbo")]
    [InlineData(new[] { "-m=gpt-3.5-turbo" }, "gpt-3.5-turbo")]
    public void Parse_ModelArgument_ParsedCorrectly(string[] args, string expectedModel)
    {
        var config = AppConfiguration.Parse(args);

        Assert.Equal(expectedModel, config.ModelName);
    }

    [Fact]
    public void Parse_HelpFlag_SetsHelpToTrue()
    {
        var config = AppConfiguration.Parse(new[] { "--help" });

        Assert.True(config.Help);
    }

    [Fact]
    public void Parse_HelpFlagShort_SetsHelpToTrue()
    {
        var config = AppConfiguration.Parse(new[] { "-h" });

        Assert.True(config.Help);
    }

    [Fact]
    public void Parse_MultipleArgs_AllParsedCorrectly()
    {
        var config = AppConfiguration.Parse(new[] { 
            "--base-url", "https://custom.api.com/v1", 
            "--model", "gpt-4-turbo",
            "--api-key", "test-key"
        });

        Assert.Equal("https://custom.api.com/v1", config.BaseUrl);
        Assert.Equal("gpt-4-turbo", config.ModelName);
        Assert.Equal("test-key", config.ApiKey);
    }

    [Fact]
    public void Parse_UnknownArgs_Ignored()
    {
        var config = AppConfiguration.Parse(new[] { "--unknown", "value" });

        Assert.Equal("https://api.openai.com/v1", config.BaseUrl);
        Assert.Equal("gpt-4", config.ModelName);
    }

    [Fact]
    public void SetApiKey_TrimsWhitespace()
    {
        var config = new AppConfiguration();
        config.SetApiKey("  test-key  ");

        Assert.Equal("test-key", config.ApiKey);
    }
}
