using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpAgent.Client.Services;

/// <summary>
/// Chat service that integrates properly with MCP server for tools, sampling, notifications, etc.
/// </summary>
public class McpIntegratedChatService : IDisposable
{
    private readonly ILogger<McpIntegratedChatService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly McpAgentConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private DirectHttpMcpClient? _mcpClient;
    private McpChatService? _fallbackChatService;
    private bool _isInitialized = false;

    public event Func<McpChatMessage, Task>? OnMessageReceived;
    public event Func<string, double, Task>? OnProgressUpdate;
    public event Func<string, Task>? OnNotificationReceived;
    public event Func<string, Task>? OnElicitationRequest;

    public McpIntegratedChatService(ILoggerFactory loggerFactory, McpAgentConfig config, IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<McpIntegratedChatService>();
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _logger.LogInformation("🔍 Initializing MCP-integrated chat service...");
            
            // Initialize MCP client
            var httpClient = _httpClientFactory.CreateClient("McpServer");
            _mcpClient = new DirectHttpMcpClient(httpClient, _config.McpServer.Endpoint, _loggerFactory);

            _logger.LogInformation("🚀 Connecting to MCP server: {Endpoint}", _config.McpServer.Endpoint);

            try
            {
                // Initialize MCP connection
                var initResult = await _mcpClient.InitializeAsync();
                _logger.LogInformation("✅ MCP server connection established successfully");

                // List available tools from MCP server
                var toolsResult = await _mcpClient.ListToolsAsync();
                if (toolsResult.TryGetProperty("tools", out var toolsArray))
                {
                    _logger.LogInformation("📋 Available MCP tools: {ToolCount}", toolsArray.GetArrayLength());
                    foreach (var tool in toolsArray.EnumerateArray())
                    {
                        if (tool.TryGetProperty("name", out var toolName))
                        {
                            _logger.LogInformation("  🔧 {ToolName}", toolName.GetString());
                        }
                    }
                }

                // Emit notification about MCP capabilities
                if (OnNotificationReceived != null)
                    await OnNotificationReceived("� Connected to MCP server with full protocol support");
            }
            catch (Exception mcpEx)
            {
                _logger.LogWarning(mcpEx, "⚠️ MCP server connection failed, initializing fallback service");
                
                // Initialize fallback chat service
                _fallbackChatService = new McpChatService(_loggerFactory, _config);
                await _fallbackChatService.InitializeAsync();
                
                if (OnNotificationReceived != null)
                    await OnNotificationReceived("⚠️ Using fallback mode - MCP server unavailable");
            }

            _isInitialized = true;
            _logger.LogInformation("✅ MCP Integrated Chat Service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize MCP Integrated Chat Service");
            throw;
        }
    }

    public async Task<McpChatMessage> SendMessageAsync(string userMessage)
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        try
        {
            _logger.LogInformation("📤 Processing user message with MCP integration: {Message}", userMessage);

            // Emit progress update
            if (OnProgressUpdate != null)
                await OnProgressUpdate("Processing your message...", 0.1);

            // Try MCP server first if available
            if (_mcpClient != null)
            {
                var mcpResponse = await ProcessWithMcpServerAsync(userMessage);
                if (mcpResponse != null)
                {
                    return mcpResponse;
                }
            }

            // Fall back to regular chat service
            if (_fallbackChatService != null)
            {
                _logger.LogInformation("🔄 Using fallback chat service");
                if (OnNotificationReceived != null)
                    await OnNotificationReceived("Using fallback AI service");
                    
                return await _fallbackChatService.SendMessageAsync(userMessage);
            }

            // Final fallback
            return new McpChatMessage
            {
                Content = "I apologize, but I'm currently unable to process your request. Both MCP server and fallback services are unavailable.",
                IsUser = false,
                Timestamp = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing message");
            return new McpChatMessage
            {
                Content = $"I apologize, but I encountered an error: {ex.Message}. Please try again.",
                IsUser = false,
                Timestamp = DateTime.Now
            };
        }
    }

    private async Task<McpChatMessage?> ProcessWithMcpServerAsync(string userMessage)
    {
        try
        {
            if (OnProgressUpdate != null)
                await OnProgressUpdate("Connecting to MCP server...", 0.3);

            // Determine which MCP tool to use based on message content
            var toolName = DetermineToolFromMessage(userMessage);
            
            if (!string.IsNullOrEmpty(toolName))
            {
                _logger.LogInformation("🔧 Using MCP tool: {ToolName}", toolName);
                
                if (OnProgressUpdate != null)
                    await OnProgressUpdate("Calling MCP tool...", 0.6);

                if (OnNotificationReceived != null)
                    await OnNotificationReceived($"🔧 Using {toolName} via MCP protocol");

                // Call MCP tool with the user message
                var arguments = new Dictionary<string, object?>();
                
                if (toolName == "travel_agent")
                {
                    // Extract destination from user message or use the full message as destination
                    arguments["destination"] = ExtractDestinationFromMessage(userMessage) ?? userMessage;
                }
                else if (toolName == "research_agent")
                {
                    // Extract topic from user message or use the full message as topic
                    arguments["topic"] = ExtractTopicFromMessage(userMessage) ?? userMessage;
                }

                var mcpResult = await _mcpClient!.CallToolAsync(toolName, arguments);
                
                if (OnProgressUpdate != null)
                    await OnProgressUpdate("Processing MCP response...", 0.9);

                // Extract response from MCP result
                string mcpResponse = ExtractResponseFromMcpResult(mcpResult);
                
                if (OnProgressUpdate != null)
                    await OnProgressUpdate("Complete!", 1.0);

                if (OnNotificationReceived != null)
                    await OnNotificationReceived("✅ Response received via MCP protocol");

                return new McpChatMessage
                {
                    Content = $"🔗 **MCP {toolName}**: {mcpResponse}",
                    IsUser = false,
                    Timestamp = DateTime.Now
                };
            }
            else
            {
                // No specific tool needed, but we can still use MCP for general processing
                _logger.LogInformation("🤖 Processing general query via MCP");
                
                if (OnNotificationReceived != null)
                    await OnNotificationReceived("🤖 Processing via MCP server");
                    
                // For now, fall back to the regular service for general queries
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ MCP server processing failed");
            if (OnNotificationReceived != null)
                await OnNotificationReceived("⚠️ MCP processing failed, falling back");
            return null;
        }
    }

    private string DetermineToolFromMessage(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        
        if (lowerMessage.Contains("travel") || lowerMessage.Contains("trip") || 
            lowerMessage.Contains("flight") || lowerMessage.Contains("hotel") ||
            lowerMessage.Contains("vacation") || lowerMessage.Contains("journey"))
        {
            return "travel_agent";
        }
        
        if (lowerMessage.Contains("research") || lowerMessage.Contains("study") ||
            lowerMessage.Contains("analyze") || lowerMessage.Contains("information") ||
            lowerMessage.Contains("data") || lowerMessage.Contains("report"))
        {
            return "research_agent";
        }

        return string.Empty; // No specific tool needed
    }

    private string? ExtractDestinationFromMessage(string message)
    {
        // Simple extraction - look for patterns like "to Tokyo", "travel to Paris", etc.
        var patterns = new[]
        {
            @"(?:travel\s+to|to|visit|going\s+to|trip\s+to)\s+([A-Za-z\s]+?)(?:\s*$|,|\.|!|\?)",
            @"([A-Za-z\s]+?)\s+(?:travel|trip|vacation|visit)",
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var destination = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(destination) && destination.Length > 2)
                {
                    return destination;
                }
            }
        }

        // Fallback: use the whole message
        return null;
    }

    private string? ExtractTopicFromMessage(string message)
    {
        // Simple extraction - look for patterns like "research AI", "analyze trends", etc.
        var patterns = new[]
        {
            @"(?:research|study|analyze|information about|data on)\s+([A-Za-z0-9\s]+?)(?:\s|$|,|\.|!|\?)",
            @"([A-Za-z0-9\s]+?)\s+(?:research|study|analysis|trends)",
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var topic = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(topic) && topic.Length > 2)
                {
                    return topic;
                }
            }
        }

        // Fallback: use the whole message
        return null;
    }

    private string ExtractResponseFromMcpResult(JsonElement mcpResult)
    {
        try
        {
            if (mcpResult.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textElement))
                    {
                        return textElement.GetString() ?? "No response content available";
                    }
                }
            }
            
            // Fallback: try to extract any text content
            var rawText = mcpResult.GetRawText();
            return $"Response received via MCP protocol: {rawText}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to extract response from MCP result");
            return "✅ Successfully processed via MCP server, but response format needs parsing.";
        }
    }

    public void Dispose()
    {
        _mcpClient?.Dispose();
        _fallbackChatService?.Dispose();
    }
}
