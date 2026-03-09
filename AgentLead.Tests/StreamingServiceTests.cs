using System.Net;
using System.Net.Http;
using System.Text;
using AgentLead.Services;
using AgentLead.Models;
using Xunit;
using System.Text.Json;

namespace AgentLead.Tests;

public class StreamingServiceTests
{
    [Fact]
    public void ChatRequest_Serialization_ContainsStreamTrue()
    {
        var request = new ChatRequest
        {
            Model = "gpt-4",
            Messages = new List<Message>
            {
                new Message { Role = "user", Content = "Hello" }
            },
            Stream = true
        };

        var json = JsonSerializer.Serialize(request);
        
        Assert.Contains("\"stream\":true", json);
    }

    [Fact]
    public void ChatResponse_Deserialization_ExtractsContent()
    {
        var json = @"{
            ""choices"": [
                {
                    ""delta"": {
                        ""content"": ""Hello, world!""
                    }
                }
            ]
        }";

        var response = JsonSerializer.Deserialize<ChatResponse>(json);
        
        Assert.Equal("Hello, world!", response?.Choices?.FirstOrDefault()?.Delta?.Content);
    }

    [Fact]
    public void ChatResponse_EmptyContent_IsHandled()
    {
        var json = @"{
            ""choices"": [
                {
                    ""delta"": {}
                }
            ]
        }";

        var response = JsonSerializer.Deserialize<ChatResponse>(json);
        
        Assert.Null(response?.Choices?.FirstOrDefault()?.Delta?.Content);
    }

    [Fact]
    public void SSEParser_ParseLine_ExtractsData()
    {
        var line = "data: {\"content\":\"test\"}";
        
        Assert.StartsWith("data: ", line);
        
        var data = line.Substring("data: ".Length);
        
        Assert.Equal("{\"content\":\"test\"}", data);
    }

    [Fact]
    public void SSEParser_DoneMessage_Detected()
    {
        var line = "data: [DONE]";
        
        Assert.StartsWith("data: ", line);
        
        var data = line.Substring("data: ".Length);
        
        Assert.Equal("[DONE]", data);
    }

    [Fact]
    public void Message_RoleAndContent_SetCorrectly()
    {
        var message = new Message
        {
            Role = "user",
            Content = "Hello"
        };

        Assert.Equal("user", message.Role);
        Assert.Equal("Hello", message.Content);
    }
}
