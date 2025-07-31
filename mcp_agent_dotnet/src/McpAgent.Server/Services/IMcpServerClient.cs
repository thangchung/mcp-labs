using System.Text.Json;
using McpAgent.Core.Agents;
using static McpAgent.Server.Services.McpJsonRpcTypes;

namespace McpAgent.Server.Services;

/// <summary>
/// Interface for MCP client operations from the server side
/// </summary>
public interface IMcpServerClient
{
    /// <summary>
    /// Sends a progress notification to the MCP client
    /// </summary>
    Task SendProgressNotificationAsync(string progressToken, int progress, int total, string message, string? relatedRequestId = null);

    /// <summary>
    /// Sends a log message to the MCP client
    /// </summary>
    Task SendLogMessageAsync(string level, string data, string logger, string? relatedRequestId = null);

    /// <summary>
    /// Requests user input via elicitation
    /// </summary>
    Task<ElicitationResult?> ElicitAsync(string message, object? requestedSchema = null, string? relatedRequestId = null);

    /// <summary>
    /// Requests AI assistance via sampling
    /// </summary>
    Task<SamplingResult?> CreateMessageAsync(IEnumerable<SamplingMessage> messages, int? maxTokens = null, string? relatedRequestId = null);
}

/// <summary>
/// HTTP-based MCP client for server-side operations
/// This sends JSON-RPC requests to connected MCP clients
/// </summary>
public class HttpMcpServerClient : IMcpServerClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _clientEndpoint;
    private readonly ILogger<HttpMcpServerClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private int _requestIdCounter = 1;

    public HttpMcpServerClient(HttpClient httpClient, string clientEndpoint, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _clientEndpoint = clientEndpoint ?? throw new ArgumentNullException(nameof(clientEndpoint));
        _logger = loggerFactory.CreateLogger<HttpMcpServerClient>();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task SendProgressNotificationAsync(string progressToken, int progress, int total, string message, string? relatedRequestId = null)
    {
        var notification = new JsonRpcNotification
        {
            Method = Methods.Progress,
            Params = new ProgressParams
            {
                ProgressToken = progressToken,
                Progress = progress,
                Total = total,
                Message = message,
                RelatedRequestId = relatedRequestId
            }
        };

        await SendNotificationAsync(notification);
    }

    public async Task SendLogMessageAsync(string level, string data, string logger, string? relatedRequestId = null)
    {
        var notification = new JsonRpcNotification
        {
            Method = Methods.Log,
            Params = new LogParams
            {
                Level = level,
                Data = data,
                Logger = logger,
                RelatedRequestId = relatedRequestId
            }
        };

        await SendNotificationAsync(notification);
    }

    public async Task<ElicitationResult?> ElicitAsync(string message, object? requestedSchema = null, string? relatedRequestId = null)
    {
        var requestId = Interlocked.Increment(ref _requestIdCounter);
        
        var request = new JsonRpcRequest
        {
            Id = requestId,
            Method = Methods.Elicit,
            Params = new ElicitationParams
            {
                Message = message,
                RequestedSchema = requestedSchema,
                RelatedRequestId = relatedRequestId
            }
        };

        try
        {
            var response = await SendRequestAsync(request);
            
            if (response?.Result is JsonElement resultElement)
            {
                var elicitationResponse = JsonSerializer.Deserialize<ElicitationResponse>(resultElement.GetRawText(), _jsonOptions);
                if (elicitationResponse != null)
                {
                    return new ElicitationResult
                    {
                        Action = elicitationResponse.Action,
                        Content = elicitationResponse.Content
                    };
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send elicitation request: {Message}", ex.Message);
            
            // Return a fallback result for graceful degradation
            return new ElicitationResult
            {
                Action = "decline",
                Content = new Dictionary<string, object?> { ["error"] = "Elicitation unavailable" }
            };
        }
    }

    public async Task<SamplingResult?> CreateMessageAsync(IEnumerable<SamplingMessage> messages, int? maxTokens = null, string? relatedRequestId = null)
    {
        var requestId = Interlocked.Increment(ref _requestIdCounter);
        
        var jsonMessages = messages.Select(m => new SamplingMessageJson
        {
            Role = m.Role,
            Content = new SamplingContentJson
            {
                Type = m.Content.Type,
                Text = m.Content.Text
            }
        });

        var request = new JsonRpcRequest
        {
            Id = requestId,
            Method = Methods.Sampling,
            Params = new SamplingParams
            {
                Messages = jsonMessages,
                MaxTokens = maxTokens,
                RelatedRequestId = relatedRequestId
            }
        };

        try
        {
            var response = await SendRequestAsync(request);
            
            if (response?.Result is JsonElement resultElement)
            {
                var samplingResponse = JsonSerializer.Deserialize<SamplingResponse>(resultElement.GetRawText(), _jsonOptions);
                if (samplingResponse != null)
                {
                    return new SamplingResult
                    {
                        Role = samplingResponse.Role,
                        Content = new SamplingContent
                        {
                            Type = samplingResponse.Content.Type,
                            Text = samplingResponse.Content.Text
                        },
                        Model = samplingResponse.Model,
                        StopReason = samplingResponse.StopReason
                    };
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send sampling request: {Message}", ex.Message);
            
            // Return a fallback result for graceful degradation
            return new SamplingResult
            {
                Role = "assistant",
                Content = new SamplingContent
                {
                    Type = "text",
                    Text = "I apologize, but I'm unable to process your request at the moment due to a connection issue."
                },
                Model = "fallback-model",
                StopReason = "error"
            };
        }
    }

    private async Task SendNotificationAsync(JsonRpcNotification notification)
    {
        try
        {
            var json = JsonSerializer.Serialize(notification, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Sending MCP notification: {Method} to {Endpoint}", notification.Method, _clientEndpoint);
            
            var response = await _httpClient.PostAsync(_clientEndpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send notification {Method}: {StatusCode}", notification.Method, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MCP notification {Method}: {Message}", notification.Method, ex.Message);
        }
    }

    private async Task<JsonRpcResponse?> SendRequestAsync(JsonRpcRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        _logger.LogDebug("Sending MCP request: {Method} (ID: {Id}) to {Endpoint}", request.Method, request.Id, _clientEndpoint);
        
        var response = await _httpClient.PostAsync(_clientEndpoint, content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<JsonRpcResponse>(responseJson, _jsonOptions);
        
        if (jsonResponse?.Error != null)
        {
            throw new InvalidOperationException($"MCP error: {jsonResponse.Error.Message} (Code: {jsonResponse.Error.Code})");
        }
        
        return jsonResponse;
    }

    public void Dispose()
    {
        // HttpClient is managed externally, don't dispose it here
    }
}
