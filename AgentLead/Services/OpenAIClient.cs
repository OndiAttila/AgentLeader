using System.Net.Http;
using AgentLead.Models;

namespace AgentLead.Services;

public class OpenAIClient : IDisposable
{
    private readonly ConnectionManager _connectionManager;
    private readonly ToolService _toolService;
    private bool _disposed;

    public OpenAIClient()
    {
        _connectionManager = new ConnectionManager();
        _toolService = new ToolService();
    }

    public ToolService ToolService => _toolService;

    public async Task<bool> SendChatMessageAsync(
        string baseUrl,
        string model,
        string apiKey,
        string message,
        string? systemMessage,
        List<Tool>? tools,
        List<Message>? conversationHistory,
        Action<string> onChunkReceived,
        Action<List<ToolCall>> onToolCallsReceived,
        CancellationToken cancellationToken = default)
    {
        var result = await _connectionManager.ExecuteWithRetry(
            baseUrl,
            model,
            apiKey,
            message,
            systemMessage,
            tools,
            conversationHistory,
            onChunkReceived,
            onToolCallsReceived,
            cancellationToken);

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _connectionManager.Dispose();
        _disposed = true;
    }
}
