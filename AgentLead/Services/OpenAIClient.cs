using System.Net.Http;
using AgentLead.Services;

namespace AgentLead.Services;

public class OpenAIClient : IDisposable
{
    private readonly ConnectionManager _connectionManager;
    private bool _disposed;

    public OpenAIClient()
    {
        _connectionManager = new ConnectionManager();
    }

    public async Task<bool> SendChatMessageAsync(
        string baseUrl,
        string model,
        string apiKey,
        string message,
        Action<string> onChunkReceived,
        CancellationToken cancellationToken = default)
    {
        var result = await _connectionManager.ExecuteWithRetry(
            baseUrl,
            model,
            apiKey,
            message,
            onChunkReceived,
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
