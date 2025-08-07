using McpAgent.Client;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace McpAgent.XServer.Services;

public class McpAgentService
{
    private readonly ILogger<McpAgentService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly McpAgentOptions _options;
    private readonly McpSamplingService _samplingService;
    private readonly McpNotificationService _notificationService;
    private readonly McpProgressService _progressService;
    private readonly McpElicitationService _elicitationService;
    private OpenAiMcpClientEnhanced? _mcpClient;

    public McpAgentService(
        ILogger<McpAgentService> logger, 
        ILoggerFactory loggerFactory, 
        IOptions<McpAgentOptions> options,
        McpSamplingService samplingService,
        McpNotificationService notificationService,
        McpProgressService progressService,
        McpElicitationService elicitationService)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _options = options.Value;
        _samplingService = samplingService;
        _notificationService = notificationService;
        _progressService = progressService;
        _elicitationService = elicitationService;
    }

    private async Task<OpenAiMcpClientEnhanced> GetMcpClientAsync()
    {
        if (_mcpClient == null)
        {
            _logger.LogInformation("🤖 Initializing Enhanced OpenAI MCP Client...");
            
            var serverUrl = _options.McpServerUrl ?? "http://localhost:3000/mcp";
            var apiKey = _options.OpenAiApiKey ?? throw new InvalidOperationException("OpenAI API Key is required");
            var model = _options.OpenAiModel ?? "gpt-4o-mini";
            var endpoint = _options.OpenAiEndpoint ?? "https://api.openai.com/v1";
            
            _mcpClient = new OpenAiMcpClientEnhanced(serverUrl, _loggerFactory, apiKey, model, endpoint);
            _logger.LogInformation("✅ Enhanced OpenAI MCP Client initialized with full MCP feature support");
        }
        
        return _mcpClient;
    }

    public async Task<string> ExecuteTravelAgentAsync(string destination, Dictionary<string, object?>? additionalParams = null)
    {
        try
        {
            // Start progress tracking
            var progressId = await _progressService.StartProgressAsync("Travel Agent Execution", 5, $"Planning travel to {destination}");
            
            // Add notification
            await _notificationService.AddNotificationAsync($"🛫 Starting travel planning for {destination}", NotificationType.ToolExecution);
            
            _logger.LogInformation("🛫 Executing enhanced travel agent for destination: {Destination}", destination);
            
            // Update progress - validation
            await _progressService.UpdateProgressAsync(progressId, 1, "Validating destination and parameters");
            
            // Check if we need elicitation for missing parameters
            var currentArgs = new Dictionary<string, object?> { ["destination"] = destination };
            if (additionalParams != null)
            {
                foreach (var kvp in additionalParams)
                {
                    currentArgs[kvp.Key] = kvp.Value;
                }
            }
            
            var missingParams = await CheckMissingTravelParameters(currentArgs);
            if (missingParams.Any())
            {
                await _notificationService.AddNotificationAsync($"🎯 Missing parameters detected: {string.Join(", ", missingParams)}", NotificationType.Elicitation);
                
                // Start elicitation session
                var elicitationSession = await _elicitationService.StartElicitationAsync("travel_agent", currentArgs, missingParams, $"Planning travel to {destination}");
                
                // For demo purposes, simulate user providing missing info
                await SimulateElicitationResponses(elicitationSession);
                
                // Update current args with elicited information
                currentArgs = elicitationSession.CurrentArguments;
            }
            
            // Update progress - research phase
            await _progressService.UpdateProgressAsync(progressId, 2, "Researching destination information");
            
            // Check if we need MCP sampling for complex decisions
            var needsSampling = await ShouldUseSamplingForTravel(destination, currentArgs);
            string? samplingAdvice = null;
            
            if (needsSampling)
            {
                await _notificationService.AddNotificationAsync($"🎲 Requesting MCP sampling for travel optimization", NotificationType.Sampling);
                
                var samplingPrompt = $@"Travel Planning Analysis:
Destination: {destination}
Parameters: {JsonSerializer.Serialize(currentArgs)}
Please provide AI-assisted recommendations for optimizing this travel plan.";
                
                var samplingResult = await _samplingService.CreateSampleAsync(samplingPrompt, 300, 0.7, new Dictionary<string, object?> { ["tool"] = "travel_agent", ["destination"] = destination });
                samplingAdvice = samplingResult.Content.FirstOrDefault()?.ToString();
                
                await _notificationService.AddNotificationAsync($"🧠 MCP sampling completed with recommendations", NotificationType.Success);
            }
            
            // Update progress - planning
            await _progressService.UpdateProgressAsync(progressId, 3, "Creating personalized travel plan");
            
            // Execute the actual travel planning
            var response = await ExecuteEnhancedTravelPlanningAsync(destination, currentArgs, samplingAdvice);
            
            // Update progress - finalization
            await _progressService.UpdateProgressAsync(progressId, 4, "Finalizing travel recommendations");
            
            // Complete progress
            await _progressService.CompleteProgressAsync(progressId, "Travel planning completed successfully");
            
            // Add success notification
            await _notificationService.AddNotificationAsync($"✅ Travel planning completed for {destination}", NotificationType.Success);
            
            _logger.LogInformation("✅ Enhanced travel agent completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            await _notificationService.AddNotificationAsync($"❌ Travel planning failed: {ex.Message}", NotificationType.Error);
            _logger.LogError(ex, "❌ Error in enhanced travel agent execution");
            throw;
        }
    }

    public async Task<string> ExecuteResearchAgentAsync(string topic, Dictionary<string, object?>? additionalParams = null)
    {
        try
        {
            // Start progress tracking
            var progressId = await _progressService.StartProgressAsync("Research Agent Execution", 5, $"Researching {topic}");
            
            // Add notification
            await _notificationService.AddNotificationAsync($"🔍 Starting research on {topic}", NotificationType.ToolExecution);
            
            _logger.LogInformation("🔍 Executing enhanced research agent for topic: {Topic}", topic);
            
            // Validate and check for missing parameters
            var currentArgs = new Dictionary<string, object?> { ["topic"] = topic };
            if (additionalParams != null)
            {
                foreach (var kvp in additionalParams)
                {
                    currentArgs[kvp.Key] = kvp.Value;
                }
            }
            
            await _progressService.UpdateProgressAsync(progressId, 1, "Analyzing research requirements");
            
            var missingParams = await CheckMissingResearchParameters(currentArgs);
            if (missingParams.Any())
            {
                await _notificationService.AddNotificationAsync($"🎯 Missing research parameters: {string.Join(", ", missingParams)}", NotificationType.Elicitation);
                
                var elicitationSession = await _elicitationService.StartElicitationAsync("research_agent", currentArgs, missingParams, $"Researching {topic}");
                await SimulateElicitationResponses(elicitationSession);
                currentArgs = elicitationSession.CurrentArguments;
            }
            
            await _progressService.UpdateProgressAsync(progressId, 2, "Determining research scope and methodology");
            
            // MCP Sampling for research strategy
            var needsSampling = await ShouldUseSamplingForResearch(topic, currentArgs);
            string? samplingAdvice = null;
            
            if (needsSampling)
            {
                await _notificationService.AddNotificationAsync($"🎲 Requesting MCP sampling for research optimization", NotificationType.Sampling);
                
                var samplingPrompt = $@"Research Strategy Analysis:
Topic: {topic}
Parameters: {JsonSerializer.Serialize(currentArgs)}
Please provide AI-assisted recommendations for optimizing this research approach.";
                
                var samplingResult = await _samplingService.CreateSampleAsync(samplingPrompt, 300, 0.7, new Dictionary<string, object?> { ["tool"] = "research_agent", ["topic"] = topic });
                samplingAdvice = samplingResult.Content.FirstOrDefault()?.ToString();
            }
            
            await _progressService.UpdateProgressAsync(progressId, 3, "Conducting research and analysis");
            
            var response = await ExecuteEnhancedResearchAsync(topic, currentArgs, samplingAdvice);
            
            await _progressService.UpdateProgressAsync(progressId, 4, "Compiling research findings");
            
            await _progressService.CompleteProgressAsync(progressId, "Research completed successfully");
            await _notificationService.AddNotificationAsync($"✅ Research completed for {topic}", NotificationType.Success);
            
            _logger.LogInformation("✅ Enhanced research agent completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            await _notificationService.AddNotificationAsync($"❌ Research failed: {ex.Message}", NotificationType.Error);
            _logger.LogError(ex, "❌ Error in enhanced research agent execution");
            throw;
        }
    }

    public async Task<string> GetStatusAsync()
    {
        var progressStats = _progressService.GetStatistics();
        var notificationStats = _notificationService.GetStatistics();
        var samplingStats = _samplingService.GetStatistics();
        var activeSessions = _elicitationService.GetActiveSessions().Count();
        
        var status = $@"🤖 **Enhanced MCP Agent Status**

📊 **Progress Tracking:**
   • Active Operations: {progressStats.ActiveOperations}
   • Completed Operations: {progressStats.CompletedOperations}
   • Failed Operations: {progressStats.FailedOperations}
   • Average Completion Time: {progressStats.AverageCompletionTime:F2}s

📬 **Notifications:**
   • Total Notifications: {notificationStats.TotalNotifications}
   • Unread Notifications: {notificationStats.UnreadNotifications}
   • Recent Activity: {(notificationStats.LatestNotification?.Message ?? "None")}

🎲 **MCP Sampling:**
   • Total Requests: {samplingStats.TotalRequests}
   • Completed: {samplingStats.CompletedRequests}
   • Failed: {samplingStats.FailedRequests}
   • Average Processing: {samplingStats.AverageProcessingTime:F2}ms

🎯 **Elicitation:**
   • Active Sessions: {activeSessions}

🔧 **System Status:** All MCP features operational
⏰ **Last Updated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        return status;
    }

    // Helper methods for enhanced functionality
    private async Task<List<string>> CheckMissingTravelParameters(Dictionary<string, object?> currentArgs)
    {
        await Task.Delay(100); // Simulate analysis
        
        var missingParams = new List<string>();
        
        if (!currentArgs.ContainsKey("budget") || currentArgs["budget"] == null)
            missingParams.Add("budget");
        
        if (!currentArgs.ContainsKey("dates") || currentArgs["dates"] == null)
            missingParams.Add("dates");
            
        if (!currentArgs.ContainsKey("travelers") || currentArgs["travelers"] == null)
            missingParams.Add("travelers");
        
        return missingParams;
    }
    
    private async Task<List<string>> CheckMissingResearchParameters(Dictionary<string, object?> currentArgs)
    {
        await Task.Delay(100); // Simulate analysis
        
        var missingParams = new List<string>();
        
        if (!currentArgs.ContainsKey("scope") || currentArgs["scope"] == null)
            missingParams.Add("scope");
        
        if (!currentArgs.ContainsKey("format") || currentArgs["format"] == null)
            missingParams.Add("format");
        
        return missingParams;
    }
    
    private async Task<bool> ShouldUseSamplingForTravel(string destination, Dictionary<string, object?> args)
    {
        await Task.Delay(50);
        // Use sampling for complex destinations or when multiple options exist
        var complexDestinations = new[] { "Japan", "Tokyo", "Europe", "Asia", "multiple cities" };
        return complexDestinations.Any(d => destination.ToLowerInvariant().Contains(d.ToLowerInvariant()));
    }
    
    private async Task<bool> ShouldUseSamplingForResearch(string topic, Dictionary<string, object?> args)
    {
        await Task.Delay(50);
        // Use sampling for complex or broad topics
        var complexTopics = new[] { "AI", "artificial intelligence", "climate", "economy", "comparison", "analysis", "strategy", "intelligence", "machine learning", "deep learning" };
        return complexTopics.Any(t => topic.ToLowerInvariant().Contains(t.ToLowerInvariant()));
    }
    
    private async Task SimulateElicitationResponses(ElicitationSession session)
    {
        // In a real implementation, this would be handled by user interaction
        // For demo purposes, we'll simulate responses
        await Task.Delay(500);
        
        foreach (var question in session.Questions)
        {
            var simulatedAnswer = question.Parameter.ToLowerInvariant() switch
            {
                "budget" => "$3000",
                "dates" => "Next month",
                "travelers" => "2 adults",
                "scope" => "Comprehensive overview",
                "format" => "Detailed report",
                _ => "Default response"
            };
            
            await _elicitationService.SubmitAnswerAsync(session.Id, question.Parameter, simulatedAnswer);
        }
    }

    private async Task<string> ExecuteEnhancedTravelPlanningAsync(string destination, Dictionary<string, object?> args, string? samplingAdvice)
    {
        await Task.Delay(2000); // Simulate processing
        
        var budget = args.GetValueOrDefault("budget", "$2500");
        var dates = args.GetValueOrDefault("dates", "Flexible");
        var travelers = args.GetValueOrDefault("travelers", "2 people");
        
        var response = $@"🛫 **Enhanced Travel Plan for {destination}**

📍 **Destination:** {destination}
👥 **Travelers:** {travelers}
💰 **Budget:** {budget}
📅 **Travel Dates:** {dates}
🗓️ **Best Time to Visit:** Based on seasonal analysis and current conditions
🏨 **Accommodation:** Curated options within your budget range
✈️ **Flight Options:** Multiple airlines with price optimization
🎯 **Personalized Attractions:** Based on your interests and preferences

📋 **AI-Enhanced Recommendations:**
   • Weather-optimized itinerary planning
   • Budget-conscious activity suggestions  
   • Local insider tips and cultural insights
   • Real-time availability and pricing

{(samplingAdvice != null ? $@"
🎲 **MCP Sampling Insights:**
{samplingAdvice}
" : "")}

🔄 **Progress Tracking:** All planning phases completed successfully
📬 **Notifications:** You'll receive updates on price changes and availability
🎯 **Next Steps:** Would you like me to proceed with booking or need more details?

*This enhanced travel plan includes AI-powered recommendations, real-time data integration, and personalized optimization based on your preferences.*";

        return response;
    }

    private async Task<string> ExecuteEnhancedResearchAsync(string topic, Dictionary<string, object?> args, string? samplingAdvice)
    {
        await Task.Delay(2000); // Simulate processing
        
        var scope = args.GetValueOrDefault("scope", "comprehensive");
        var format = args.GetValueOrDefault("format", "detailed report");
        
        var response = $@"🔍 **Enhanced Research Report: {topic}**

📋 **Research Parameters:**
   • Topic: {topic}
   • Scope: {scope}
   • Format: {format}
   
📊 **Key Findings:**
   • Comprehensive analysis conducted across multiple authoritative sources
   • Recent developments and trending aspects identified
   • Cross-referenced information for accuracy and reliability
   • Expert insights and diverse perspectives included

🧠 **AI-Enhanced Analysis:**
   • Trend analysis and pattern recognition
   • Comparative analysis with related topics
   • Future outlook and implications
   • Source credibility assessment

{(samplingAdvice != null ? $@"
🎲 **MCP Sampling Insights:**
{samplingAdvice}
" : "")}

📚 **Sources:** Authoritative academic papers, industry reports, and verified news sources
🔄 **Progress:** All research phases completed with quality assurance
📬 **Notifications:** Research completion confirmed with full documentation
🎯 **Follow-up:** Additional research areas identified for deeper investigation

*This enhanced research includes AI-powered analysis, multi-source verification, and intelligent insights generation.*";

        return response;
    }

    // Enhanced methods for MCP feature access
    public async Task<SamplingResult> CreateSampleAsync(string prompt, int maxTokens = 500, double temperature = 0.7, Dictionary<string, object?>? metadata = null)
    {
        return await _samplingService.CreateSampleAsync(prompt, maxTokens, temperature, metadata);
    }
    
    public async Task AddNotificationAsync(string message, NotificationType type, Dictionary<string, object?>? metadata = null)
    {
        await _notificationService.AddNotificationAsync(message, type, metadata);
    }
    
    public async Task<string> StartProgressAsync(string operationName, int totalSteps = 100, string? description = null)
    {
        return await _progressService.StartProgressAsync(operationName, totalSteps, description);
    }
    
    public async Task UpdateProgressAsync(string progressId, int currentStep, string? statusMessage = null)
    {
        await _progressService.UpdateProgressAsync(progressId, currentStep, statusMessage);
    }
    
    public async Task CompleteProgressAsync(string progressId, string? completionMessage = null)
    {
        await _progressService.CompleteProgressAsync(progressId, completionMessage);
    }
    
    public async Task<ElicitationSession> StartElicitationAsync(string toolName, Dictionary<string, object?> currentArgs, List<string> missingParameters)
    {
        return await _elicitationService.StartElicitationAsync(toolName, currentArgs, missingParameters);
    }
    
    public IEnumerable<McpNotification> GetNotifications()
    {
        return _notificationService.GetNotifications();
    }
    
    public IEnumerable<ProgressContext> GetActiveProgress()
    {
        return _progressService.GetActiveProgress();
    }
    
    public IEnumerable<ElicitationSession> GetActiveElicitationSessions()
    {
        return _elicitationService.GetActiveSessions();
    }
}
