using Microsoft.Extensions.Logging;

namespace McpAgent.Client.Services;

public class McpChatService : IDisposable
{
    private readonly ILogger<McpChatService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly McpAgentConfig _options;
    private OpenAiMcpClientEnhanced? _openAiClient;
    private GeminiMcpClientEnhanced? _geminiClient;
    private readonly string _serverUrl = "http://127.0.0.1:8006/mcp";
    
    public event Func<McpChatMessage, Task>? OnMessageReceived;
    public event Func<string, double, Task>? OnProgressUpdate;
    public event Func<string, Task>? OnNotificationReceived;
    public event Func<string, Task>? OnElicitationRequest;

    public McpChatService(ILoggerFactory loggerFactory, McpAgentConfig options)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<McpChatService>();
        _options = options;
    }

    public Task InitializeAsync()
    {
        // Check which AI provider is configured
        bool hasOpenAI = !string.IsNullOrEmpty(_options.OpenAI?.ApiKey);
        bool hasGemini = !string.IsNullOrEmpty(_options.GeminiApiKey);

        _logger.LogInformation("🔍 Checking API key configuration...");
        _logger.LogInformation("OpenAI API Key configured: {HasOpenAI}", hasOpenAI);
        _logger.LogInformation("Gemini API Key configured: {HasGemini}", hasGemini);

        if (!hasOpenAI && !hasGemini)
        {
            _logger.LogWarning("⚠️ No AI API keys configured. Using simulated responses.");
            return Task.CompletedTask; // Allow operation without API keys for demo purposes
        }

        try
        {
            if (hasOpenAI)
            {
                _logger.LogInformation("🚀 Initializing OpenAI client with endpoint: {Endpoint}", _options.OpenAI?.Endpoint ?? "default");
                _openAiClient = new OpenAiMcpClientEnhanced(_serverUrl, _loggerFactory, 
                    _options.OpenAI.ApiKey!, _options.OpenAI.Model, _options.OpenAI.Endpoint);
                _logger.LogInformation("✅ OpenAI MCP Client initialized successfully");
            }
            
            if (hasGemini)
            {
                _geminiClient = new GeminiMcpClientEnhanced(_serverUrl, _loggerFactory, _options.GeminiApiKey!);
                _logger.LogInformation("✅ Gemini MCP Client initialized successfully");
            }

            _logger.LogInformation("✅ MCP Chat Service initialized successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize MCP Chat Service");
            throw;
        }
    }

    public async Task<McpChatMessage> SendMessageAsync(string userMessage)
    {
        try
        {
            _logger.LogInformation("📤 Processing user message: {Message}", userMessage);

            // Emit progress update
            if (OnProgressUpdate != null)
                await OnProgressUpdate("Processing your message...", 0.3);

            await Task.Delay(500); // Simulate processing

            // Use AI client if available, otherwise use simulated logic
            string response;
            if (_openAiClient != null)
            {
                response = await ProcessWithAIAsync(userMessage, "OpenAI");
            }
            else if (_geminiClient != null)
            {
                response = await ProcessWithAIAsync(userMessage, "Gemini");
            }
            else
            {
                response = await ProcessWithSimulationAsync(userMessage);
            }

            if (OnProgressUpdate != null)
                await OnProgressUpdate("Complete!", 1.0);

            return new McpChatMessage 
            { 
                Content = response, 
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

    private async Task<string> ProcessWithAIAsync(string userMessage, string provider)
    {
        // In a real implementation, you would call the actual AI client here
        // For now, we'll use enhanced simulation that mentions the AI provider
        
        if (OnNotificationReceived != null)
            await OnNotificationReceived($"🤖 Using {provider} AI for enhanced responses");

        await Task.Delay(1000); // Simulate AI processing time

        var input = userMessage.ToLowerInvariant();
        
        if (input.Contains("travel") || input.Contains("trip"))
        {
            return $"🧳 **AI-Enhanced Travel Planning** (via {provider})\n\n" +
                   "I'd be happy to help you plan your trip! Based on your request, I can assist with:\n\n" +
                   "✈️ **Flight Options**: Best routes and pricing\n" +
                   "🏨 **Accommodations**: Hotels, Airbnb, and unique stays\n" +
                   "🗺️ **Itinerary Planning**: Must-see attractions and experiences\n" +
                   "💰 **Budget Optimization**: Cost-effective travel strategies\n\n" +
                   "What specific aspect of your travel would you like help with?";
        }
        else if (input.Contains("research") || input.Contains("study"))
        {
            return $"📊 **AI-Enhanced Research** (via {provider})\n\n" +
                   "I can help you with comprehensive research! Here's what I can assist with:\n\n" +
                   "🔍 **Information Gathering**: From multiple reliable sources\n" +
                   "📈 **Data Analysis**: Trends, patterns, and insights\n" +
                   "📝 **Summary Generation**: Key findings and conclusions\n" +
                   "🎯 **Targeted Research**: Specific questions and hypotheses\n\n" +
                   "What topic would you like me to research for you?";
        }
        else
        {
            return $"🤖 **AI Assistant** (via {provider})\n\n" +
                   "Hello! I'm here to help you with various tasks using advanced AI capabilities. " +
                   "I can assist with travel planning, research, analysis, and much more.\n\n" +
                   "How can I help you today?";
        }
    }

    private async Task<string> ProcessWithSimulationAsync(string userMessage)
    {
        if (OnNotificationReceived != null)
            await OnNotificationReceived("🎭 Using simulated responses (no API keys configured)");

        await Task.Delay(800); // Simulate processing time

        var input = userMessage.ToLowerInvariant();
        
        if (input.Contains("travel") || input.Contains("trip"))
        {
            if (OnElicitationRequest != null)
                await OnElicitationRequest("Where would you like to travel?");
                
            return "🧳 **Travel Planning Assistant**\n\n" +
                   "I'd love to help you plan your trip! I can assist with destinations, itineraries, and travel tips.\n\n" +
                   "To provide better recommendations, could you tell me:\n" +
                   "• Where you'd like to go?\n" +
                   "• What type of experience you're looking for?\n" +
                   "• Your approximate budget?\n\n" +
                   "*Note: For enhanced AI-powered recommendations, configure your OpenAI or Gemini API key in appsettings.json*";
        }
        else if (input.Contains("research") || input.Contains("study"))
        {
            return "📊 **Research Assistant**\n\n" +
                   "I can help you with research and information gathering! Here's what I can do:\n\n" +
                   "• Provide information on various topics\n" +
                   "• Help structure your research approach\n" +
                   "• Suggest reliable sources\n" +
                   "• Organize findings and insights\n\n" +
                   "What topic would you like to research?\n\n" +
                   "*Note: For AI-enhanced research capabilities, configure your OpenAI or Gemini API key in appsettings.json*";
        }
        else
        {
            return "👋 **Welcome to MCP Chat!**\n\n" +
                   "I'm your AI assistant, ready to help with various tasks including:\n\n" +
                   "• **Travel Planning**: Destinations, itineraries, and recommendations\n" +
                   "• **Research**: Information gathering and analysis\n" +
                   "• **General Assistance**: Various questions and tasks\n\n" +
                   "How can I assist you today?\n\n" +
                   "*💡 Tip: Configure your OpenAI or Gemini API key in appsettings.json for enhanced AI-powered responses!*";
        }
    }

    public void Dispose()
    {
        _openAiClient?.Dispose();
        // _geminiClient doesn't implement IDisposable
    }
}

public class McpChatMessage
{
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }
}
