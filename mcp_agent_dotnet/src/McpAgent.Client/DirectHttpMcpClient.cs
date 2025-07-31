using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace McpAgent.Client;

/// <summary>
/// Direct HTTP client for communicating with MCP server using JSON-RPC protocol
/// This bypasses the MCP SDK transport layer to work with our custom endpoint
/// </summary>
public class DirectHttpMcpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _mcpEndpoint;
    private readonly ILogger<DirectHttpMcpClient> _logger;
    private int _requestId = 1;

    public DirectHttpMcpClient(HttpClient httpClient, string mcpEndpoint, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _mcpEndpoint = mcpEndpoint ?? throw new ArgumentNullException(nameof(mcpEndpoint));
        _logger = loggerFactory.CreateLogger<DirectHttpMcpClient>();
    }

    public async Task<JsonElement> SendRequestAsync(string method, object? parameters = null)
    {
        var requestId = _requestId++;
        
        var request = new
        {
            jsonrpc = "2.0",
            id = requestId,
            method = method,
            @params = parameters
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending request: {Method} with ID: {RequestId}", method, requestId);

        var response = await _httpClient.PostAsync(_mcpEndpoint, content);
        
        var responseJson = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("Received response: {Response} (Status: {StatusCode})", responseJson, response.StatusCode);

        // Check for HTTP errors first, but read the response body to get detailed error info
        if (!response.IsSuccessStatusCode)
        {
            try 
            {
                var errorDocument = JsonSerializer.Deserialize<JsonElement>(responseJson);
                if (errorDocument.TryGetProperty("error", out var httpError))
                {
                    var errorMessage = httpError.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown server error";
                    var errorCode = httpError.TryGetProperty("code", out var code) ? code.GetInt32().ToString() : "Unknown";
                    throw new InvalidOperationException($"Server error ({response.StatusCode}): {errorMessage} (Code: {errorCode})");
                }
            }
            catch (JsonException)
            {
                // If we can't parse the JSON, fall back to the raw response
            }
            
            throw new HttpRequestException($"Server returned {response.StatusCode}: {responseJson}");
        }

        var jsonDocument = JsonSerializer.Deserialize<JsonElement>(responseJson);
        
        if (jsonDocument.TryGetProperty("error", out var error))
        {
            var errorMessage = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
            var errorCode = error.TryGetProperty("code", out var code) ? code.GetInt32().ToString() : "Unknown";
            throw new InvalidOperationException($"Server error: {errorMessage} (Code: {errorCode})");
        }

        if (jsonDocument.TryGetProperty("result", out var result))
        {
            return result;
        }

        throw new InvalidOperationException("Invalid response format");
    }

    public async Task<JsonElement> CallToolAsync(string toolName, Dictionary<string, object?> arguments)
    {
        var parameters = new
        {
            name = toolName,
            arguments = arguments
        };

        return await SendRequestAsync("tools/call", parameters);
    }

    public async Task<JsonElement> ListToolsAsync()
    {
        return await SendRequestAsync("tools/list");
    }

    public async Task<JsonElement> CreateSampleAsync(string prompt, int? maxTokens = null, double? temperature = null, 
        string[]? stopSequences = null, Dictionary<string, object?>? metadata = null)
    {
        var parameters = new
        {
            prompt = prompt,
            maxTokens = maxTokens,
            temperature = temperature,
            stopSequences = stopSequences,
            metadata = metadata
        };

        return await SendRequestAsync("sampling/createSample", parameters);
    }

    public async Task<JsonElement> InitializeAsync()
    {
        return await SendRequestAsync("initialize");
    }

    public void Dispose()
    {
        // HttpClient is owned by the caller, so we don't dispose it here
    }
}
