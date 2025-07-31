using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace McpAgent.Client;

/// <summary>
/// MCP client enhanced with Google Gemini AI for intelligent conversation and tool usage
/// This implementation uses direct HTTP calls to Google's Gemini API for simplicity
/// </summary>
public class GeminiMcpClient
{
    private readonly string _serverUrl;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<GeminiMcpClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _geminiApiKey;
    private readonly List<string> _conversationHistory;

    public GeminiMcpClient(string serverUrl, ILoggerFactory loggerFactory, string geminiApiKey)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new ArgumentException("Server URL cannot be null or empty", nameof(serverUrl));
        if (string.IsNullOrWhiteSpace(geminiApiKey))
            throw new ArgumentException("Gemini API key cannot be null or empty", nameof(geminiApiKey));

        _serverUrl = serverUrl;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<GeminiMcpClient>();
        _geminiApiKey = geminiApiKey;
        _httpClient = new HttpClient();
        
        _conversationHistory =
        [
            // Set up system prompt
            "System: You are an AI assistant that helps users interact with MCP (Model Context Protocol) tools. " +
                "You have access to travel booking and research tools through an MCP server. " +
                "When a user asks for help with travel or research, determine if you need to use the appropriate MCP tool. " +
                "Available tools: travel_agent (for booking travel), research_agent (for conducting research). " +
                "Always be helpful and concise.",
        ];
    }

    /// <summary>
    /// Runs the AI-powered interactive client session
    /// </summary>
    public async Task RunAsync()
    {
        try
        {
            await ConnectAndRunInteractively();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI client execution");
            throw;
        }
        finally
        {
            _httpClient.Dispose();
        }
    }

    private async Task ConnectAndRunInteractively()
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_serverUrl.Replace("/mcp", ""));
        
        var mcpClient = new DirectHttpMcpClient(httpClient, _serverUrl, _loggerFactory);
        
        // Initialize MCP connection
        await mcpClient.InitializeAsync();
        _logger.LogInformation("✅ Connected to MCP server");

        // Get available tools
        var toolsResult = await mcpClient.ListToolsAsync();
        var availableTools = ExtractToolNames(toolsResult);
        _logger.LogInformation("🔧 Available tools: {Tools}", string.Join(", ", availableTools));

        try
        {
            await RunAIInteractiveLoop(mcpClient, availableTools);
        }
        finally
        {
            mcpClient.Dispose();
        }
    }

    private async Task RunAIInteractiveLoop(DirectHttpMcpClient mcpClient, List<string> availableTools)
    {
        _logger.LogInformation("\n🤖 AI-powered MCP client started!");
        _logger.LogInformation("💬 I can help you with travel booking and research. Type 'exit' to quit.");

        while (true)
        {
            Console.Write("\n💭 You: ");
            var userInput = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(userInput))
                continue;

            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("👋 Goodbye!");
                break;
            }

            try
            {
                await ProcessUserInput(mcpClient, userInput, availableTools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user input");
                Console.WriteLine("❌ Sorry, I encountered an error. Please try again.");
            }
        }
    }

    private async Task ProcessUserInput(DirectHttpMcpClient mcpClient, string userInput, List<string> availableTools)
    {
        // Add user message to conversation history
        _conversationHistory.Add($"User: {userInput}");

        // Determine if we need to use MCP tools
        var toolAction = DetermineToolAction(userInput, availableTools);

        if (toolAction != null)
        {
            _logger.LogInformation("🔧 Using tool: {Tool}", toolAction.ToolName);
            
            try
            {
                var toolResult = await mcpClient.CallToolAsync(toolAction.ToolName, toolAction.Arguments);
                var toolResultText = ExtractToolResultText(toolResult);
                
                // Add tool result to conversation and get AI response
                var contextMessage = $"Tool result from {toolAction.ToolName}: {toolResultText}";
                _conversationHistory.Add(contextMessage);
                
                var aiResponse = await GetGeminiResponse(_conversationHistory);
                Console.WriteLine($"🤖 Assistant: {aiResponse}");
                
                _conversationHistory.Add($"Assistant: {aiResponse}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {Tool}", toolAction.ToolName);
                Console.WriteLine($"❌ Sorry, I couldn't execute the {toolAction.ToolName} tool. Please try again.");
            }
        }
        else
        {
            // Get direct AI response
            var aiResponse = await GetGeminiResponse(_conversationHistory);
            Console.WriteLine($"🤖 Assistant: {aiResponse}");
            _conversationHistory.Add($"Assistant: {aiResponse}");
        }
    }

    private async Task<string> GetGeminiResponse(List<string> conversationHistory)
    {
        try
        {
            var prompt = string.Join("\n", conversationHistory);
            
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_geminiApiKey}";
            var response = await _httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode}", response.StatusCode);
                return "I'm sorry, I couldn't generate a response at the moment.";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            if (jsonDoc.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentProperty) &&
                    contentProperty.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? "I'm sorry, I couldn't generate a response.";
                    }
                }
            }

            return "I'm sorry, I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Gemini response");
            return "I'm sorry, I encountered an error while processing your request.";
        }
    }

    private ToolAction? DetermineToolAction(string userInput, List<string> availableTools)
    {
        var input = userInput.ToLowerInvariant();
        
        // Simple keyword-based tool detection
        if (input.Contains("travel") || input.Contains("book") || input.Contains("trip") || input.Contains("visit"))
        {
            if (availableTools.Contains("travel_agent"))
            {
                var destination = ExtractDestination(userInput);
                return new ToolAction("travel_agent", new Dictionary<string, object?> { ["destination"] = destination });
            }
        }
        
        if (input.Contains("research") || input.Contains("study") || input.Contains("investigate") || input.Contains("learn about"))
        {
            if (availableTools.Contains("research_agent"))
            {
                var topic = ExtractResearchTopic(userInput);
                return new ToolAction("research_agent", new Dictionary<string, object?> { ["topic"] = topic });
            }
        }
        
        return null;
    }

    private string ExtractDestination(string input)
    {
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].ToLowerInvariant() is "to" or "visit" or "in")
            {
                return string.Join(" ", words.Skip(i + 1)).Trim();
            }
        }
        
        return input;
    }

    private string ExtractResearchTopic(string input)
    {
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].ToLowerInvariant() is "about" or "research" or "study")
            {
                return string.Join(" ", words.Skip(i + 1)).Trim();
            }
        }
        
        return input;
    }

    private List<string> ExtractToolNames(JsonElement toolsResult)
    {
        var toolNames = new List<string>();
        
        if (toolsResult.TryGetProperty("tools", out var toolsArray))
        {
            foreach (var tool in toolsArray.EnumerateArray())
            {
                if (tool.TryGetProperty("name", out var nameElement))
                {
                    var name = nameElement.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        toolNames.Add(name);
                    }
                }
            }
        }
        
        return toolNames;
    }

    private string ExtractToolResultText(JsonElement toolResult)
    {
        if (toolResult.TryGetProperty("content", out var contentArray))
        {
            foreach (var item in contentArray.EnumerateArray())
            {
                if (item.TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString() ?? "No result";
                }
            }
        }
        
        return toolResult.ToString();
    }
}

public record ToolAction(string ToolName, Dictionary<string, object?> Arguments);
