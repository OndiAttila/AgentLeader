using System.Net.Http;
using System.Timers;
using AgentLead.Models;
using AgentLead.Options;
using Timer = System.Timers.Timer;

namespace AgentLead.Services;

public class ConnectionManager : IDisposable
{
    private const int MaxRetryAttempts = 5;
    private const int PingIntervalSeconds = 60;
    private const int ConnectionTimeoutSeconds = 120;

    private readonly HttpClient _httpClient;
    private Timer? _pingTimer;
    private bool _isConnected;
    private bool _disposed;

    public event Action? OnPingRequired;
    public event Action<int, int>? OnRetryAttempt;

    public ConnectionManager()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(ConnectionTimeoutSeconds)
        };
    }

    public async Task<bool> ExecuteWithConnectionManagement(
        string baseUrl,
        string model,
        string apiKey,
        string userMessage,
        string? systemMessage,
        List<Tool>? tools,
        List<Message>? conversationHistory,
        Action<string> onChunkReceived,
        Action<List<ToolCall>> onToolCallsReceived,
        CancellationToken cancellationToken = default)
    {
        var streamingService = new StreamingService(_httpClient);

        try
        {
            StartPingTimer();

            var result = await streamingService.SendStreamingRequestAsync(
                baseUrl,
                model,
                apiKey,
                userMessage,
                systemMessage,
                tools,
                conversationHistory,
                onChunkReceived,
                onToolCallsReceived,
                cancellationToken);

            StopPingTimer();
            return result;
        }
        catch (Exception)
        {
            StopPingTimer();
            throw;
        }
        finally
        {
            streamingService.Dispose();
        }
    }

    public async Task<bool> ExecuteWithRetry(
        string baseUrl,
        string model,
        string apiKey,
        string userMessage,
        string? systemMessage,
        List<Tool>? tools,
        List<Message>? conversationHistory,
        Action<string> onChunkReceived,
        Action<List<ToolCall>> onToolCallsReceived,
        CancellationToken cancellationToken = default)
    {
        int attempt = 0;

        while (attempt < MaxRetryAttempts)
        {
            attempt++;

            if (attempt > 1)
            {
                var delaySeconds = (int)Math.Pow(2, attempt - 1);
                Console.WriteLine($"\nRetrying in {delaySeconds} seconds... (Attempt {attempt}/{MaxRetryAttempts})");
                await Task.Delay(delaySeconds * 1000, cancellationToken);
            }

            try
            {
                OnRetryAttempt?.Invoke(attempt, MaxRetryAttempts);

                var result = await ExecuteWithConnectionManagement(
                    baseUrl,
                    model,
                    apiKey,
                    userMessage,
                    systemMessage,
                    tools,
                    conversationHistory,
                    onChunkReceived,
                    onToolCallsReceived,
                    cancellationToken);

                if (result)
                {
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (attempt >= MaxRetryAttempts)
                {
                    Console.WriteLine($"\nConnection failed after {attempt} attempts: {ex.Message}");
                    return false;
                }
            }
        }

        Console.WriteLine($"\nConnection failed after {MaxRetryAttempts} attempts.");
        return false;
    }

    private void StartPingTimer()
    {
        _pingTimer = new Timer(PingIntervalSeconds * 1000);
        _pingTimer.Elapsed += OnPingTimerElapsed;
        _pingTimer.AutoReset = true;
        _pingTimer.Start();
        _isConnected = true;
    }

    private void StopPingTimer()
    {
        _isConnected = false;

        if (_pingTimer != null)
        {
            _pingTimer.Stop();
            _pingTimer.Elapsed -= OnPingTimerElapsed;
            _pingTimer.Dispose();
            _pingTimer = null;
        }
    }

    private void OnPingTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_isConnected)
        {
            OnPingRequired?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopPingTimer();
        _httpClient.Dispose();

        _disposed = true;
    }
}
