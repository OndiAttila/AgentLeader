using System.Net.Http;
using System.Text;
using System.Text.Json;
using AgentLead.Models;

namespace AgentLead.Services;

public class StreamingService : IDisposable
{
    private readonly HttpClient _httpClient;
    private HttpResponseMessage? _response;
    private Stream? _stream;
    private StreamReader? _reader;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public StreamingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> SendStreamingRequestAsync(
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
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var messages = new List<Message>();

        if (!string.IsNullOrWhiteSpace(systemMessage))
        {
            messages.Add(new Message { Role = "system", Content = systemMessage });
        }

        if (conversationHistory != null)
        {
            messages.AddRange(conversationHistory);
        }

        messages.Add(new Message { Role = "user", Content = userMessage });

        var request = new ChatRequest
        {
            Model = model,
            Messages = messages,
            Stream = true,
            Tools = tools
        };

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
#region debug
Console.WriteLine($"[StreamingService.SendStreamingRequestAsync] sending content: |{json}|");
var fullResponse="";
#endregion

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = content
        };

#region debug
Console.WriteLine($"[StreamingService.SendStreamingRequestAsync] apiKey: |{apiKey}|");
#endregion
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
        httpRequest.Headers.Add("Accept", "text/event-stream");

        try
        {
            _response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                _cts.Token);

            if (!_response.IsSuccessStatusCode)
            {
                var errorContent = await _response.Content.ReadAsStringAsync(_cts.Token);
                Console.WriteLine($"\nError: HTTP {_response.StatusCode} - {errorContent}");
                return false;
            }

            _stream = await _response.Content.ReadAsStreamAsync(_cts.Token);
            _reader = new StreamReader(_stream);

            try
            {
                var accumulatedToolCalls = new Dictionary<string, ToolCall>();
                var hasReceivedToolCalls = false;

                while (true)
                {
                    if (_cts.Token.IsCancellationRequested) {
                        break;
                    }

                    var line = await _reader.ReadLineAsync(_cts.Token);
                    if (line == null) {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line)) {
                        continue;
                    }

#region debug
Console.WriteLine($"\n[StreamingService.SendStreamingRequestAsync]] response line: |{line}|");
#endregion
                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring("data: ".Length);

                        if (data == "[DONE]")
                        {
                            break;
                        }

                        try
                        {
                            var response = JsonSerializer.Deserialize<ChatResponse>(data);
                            var delta = response?.Choices?.FirstOrDefault()?.Delta;
                            var contentDelta = delta?.Content;
                            
                            if (!string.IsNullOrEmpty(contentDelta))
                            {
#region debug
fullResponse += contentDelta;
Console.WriteLine($"\n[StreamingService.SendStreamingRequestAsync]] full response so far: |{fullResponse}|");
#endregion
                                onChunkReceived(contentDelta);
                            }

                            if (delta?.ToolCalls != null && delta.ToolCalls.Count > 0)
                            {
                                hasReceivedToolCalls = true;
                                foreach (var toolCall in delta.ToolCalls)
                                {
                                    if (!accumulatedToolCalls.ContainsKey(toolCall.Id))
                                    {
                                        accumulatedToolCalls[toolCall.Id] = toolCall;
                                    }
                                    else
                                    {
                                        var existing = accumulatedToolCalls[toolCall.Id];
                                        existing.Function.Name += toolCall.Function.Name;
                                        existing.Function.Arguments += toolCall.Function.Arguments;
                                    }
                                }
                            }
                        }
                        catch (JsonException)
                        {
                        }
                    }
                }

                if (hasReceivedToolCalls && accumulatedToolCalls.Count > 0)
                {
                    onToolCallsReceived(accumulatedToolCalls.Values.ToList());
                }
            }
            catch (ObjectDisposedException)
            {
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\nConnection error: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nUnexpected error: {ex.Message}");
            return false;
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _reader?.Dispose();
        _stream?.Dispose();
        _response?.Dispose();

        _disposed = true;
    }
}
