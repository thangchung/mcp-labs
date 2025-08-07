using Microsoft.AspNetCore.SignalR;
using McpAgent.XServer.Services;
using McpAgent.XServer.Models;

namespace McpAgent.XServer.Hubs;

public class ChatHub : Hub
{
    private readonly McpAgentService _mcpAgentService;
    private readonly McpNotificationService _notificationService;
    private readonly McpProgressService _progressService;
    private readonly McpElicitationService _elicitationService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        McpAgentService mcpAgentService, 
        McpNotificationService notificationService,
        McpProgressService progressService,
        McpElicitationService elicitationService,
        ILogger<ChatHub> logger)
    {
        _mcpAgentService = mcpAgentService;
        _notificationService = notificationService;
        _progressService = progressService;
        _elicitationService = elicitationService;
        _logger = logger;
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
        await _notificationService.AddNotificationAsync($"Message from {user}: {message}", NotificationType.UserAction);
    }

    public async Task ExecuteTravelAgent(string user, string destination, Dictionary<string, object?>? additionalParams = null)
    {
        try
        {
            _logger.LogInformation("🛫 Enhanced travel request from {User} to {Destination}", user, destination);
            
            // Check for missing parameters and start elicitation if needed
            var requiredParams = new Dictionary<string, object?>
            {
                ["destination"] = destination,
                ["budget"] = additionalParams?.GetValueOrDefault("budget"),
                ["duration"] = additionalParams?.GetValueOrDefault("duration"),
                ["preferences"] = additionalParams?.GetValueOrDefault("preferences"),
                ["travelDate"] = additionalParams?.GetValueOrDefault("travelDate")
            };

            var missingParams = requiredParams.Where(p => p.Value == null || string.IsNullOrEmpty(p.Value?.ToString())).Select(p => p.Key).ToList();
            
            if (missingParams.Any() && missingParams.Count > 1) // destination is always provided
            {
                // Start elicitation process
                var session = await _elicitationService.StartElicitationAsync("travel_agent", requiredParams, missingParams, $"Planning travel to {destination}");
                var elicitationRequest = CreateElicitationRequest(session);
                
                await Clients.Caller.SendAsync("StartElicitation", elicitationRequest);
                return;
            }
            
            // Send immediate acknowledgment with progress tracking
            await Clients.All.SendAsync("ReceiveMessage", "System", $"🛫 Starting enhanced travel planning to {destination} with full MCP features...");
            await Clients.All.SendAsync("ReceiveProgress", new { operation = "travel_planning", status = "started", destination });
            
            // Execute enhanced travel agent with all MCP features
            var result = await _mcpAgentService.ExecuteTravelAgentAsync(destination, additionalParams);
            
            // Send the result back to all clients
            await Clients.All.SendAsync("ReceiveMessage", "Enhanced Travel Agent", result);
            await Clients.All.SendAsync("ReceiveProgress", new { operation = "travel_planning", status = "completed", destination });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing enhanced travel agent");
            await Clients.All.SendAsync("ReceiveMessage", "System", $"❌ Error in enhanced travel planning: {ex.Message}");
            await Clients.All.SendAsync("ReceiveProgress", new { operation = "travel_planning", status = "failed", error = ex.Message });
        }
    }

    public async Task ExecuteResearchAgent(string user, string topic, Dictionary<string, object?>? additionalParams = null)
    {
        try
        {
            _logger.LogInformation("🔍 Enhanced research request from {User} for {Topic}", user, topic);
            
            // Send immediate acknowledgment with progress tracking
            await Clients.All.SendAsync("ReceiveMessage", "System", $"🔍 Starting enhanced research on {topic} with full MCP features...");
            await Clients.All.SendAsync("ReceiveProgress", new { operation = "research", status = "started", topic });
            
            // Execute enhanced research agent with all MCP features
            var result = await _mcpAgentService.ExecuteResearchAgentAsync(topic, additionalParams);
            
            // Send the result back to all clients
            await Clients.All.SendAsync("ReceiveMessage", "Enhanced Research Agent", result);
            await Clients.All.SendAsync("ReceiveProgress", new { operation = "research", status = "completed", topic });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing enhanced research agent");
            await Clients.All.SendAsync("ReceiveMessage", "System", $"❌ Error in enhanced research: {ex.Message}");
            await Clients.All.SendAsync("ReceiveProgress", new { operation = "research", status = "failed", error = ex.Message });
        }
    }

    public async Task SendStatus(string user)
    {
        try
        {
            var status = await _mcpAgentService.GetStatusAsync();
            await Clients.All.SendAsync("ReceiveMessage", "Enhanced MCP System", status);
            
            // Send additional MCP feature status
            var notifications = _mcpAgentService.GetNotifications().Take(5);
            var activeProgress = _mcpAgentService.GetActiveProgress();
            var activeSessions = _mcpAgentService.GetActiveElicitationSessions();
            
            await Clients.All.SendAsync("ReceiveNotifications", notifications);
            await Clients.All.SendAsync("ReceiveActiveProgress", activeProgress);
            await Clients.All.SendAsync("ReceiveActiveSessions", activeSessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced status");
            await Clients.All.SendAsync("ReceiveMessage", "System", $"❌ Error getting enhanced status: {ex.Message}");
        }
    }

    public async Task GetNotifications(string user)
    {
        try
        {
            var notifications = _mcpAgentService.GetNotifications().Take(20);
            await Clients.Caller.SendAsync("ReceiveNotifications", notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            await Clients.Caller.SendAsync("ReceiveMessage", "System", $"❌ Error getting notifications: {ex.Message}");
        }
    }

    public async Task CreateSample(string user, string prompt, int maxTokens = 500, double temperature = 0.7)
    {
        try
        {
            _logger.LogInformation("🎲 MCP sampling request from {User}", user);
            
            await Clients.All.SendAsync("ReceiveMessage", "System", "🎲 Creating MCP sample for AI-assisted decision making...");
            
            var result = await _mcpAgentService.CreateSampleAsync(prompt, maxTokens, temperature);
            var content = result.Content.FirstOrDefault()?.ToString() ?? "No content generated";
            
            await Clients.All.SendAsync("ReceiveMessage", "MCP Sampling AI", $"🎲 **Sampling Result:**\n{content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating MCP sample");
            await Clients.All.SendAsync("ReceiveMessage", "System", $"❌ Error in MCP sampling: {ex.Message}");
        }
    }

    public async Task StartElicitation(string user, string toolName, Dictionary<string, object?> currentArgs, List<string> missingParams)
    {
        try
        {
            _logger.LogInformation("🎯 Elicitation request from {User} for {ToolName}", user, toolName);
            
            var session = await _mcpAgentService.StartElicitationAsync(toolName, currentArgs, missingParams);
            
            await Clients.All.SendAsync("ReceiveMessage", "System", $"🎯 Started elicitation session #{session.Id} for {toolName}");
            await Clients.All.SendAsync("ReceiveElicitationSession", session);
            
            // Send questions to the user
            foreach (var question in session.Questions)
            {
                await Clients.All.SendAsync("ReceiveMessage", "Elicitation Agent", $"❓ {question.QuestionText}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting elicitation");
            await Clients.All.SendAsync("ReceiveMessage", "System", $"❌ Error starting elicitation: {ex.Message}");
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("ReceiveMessage", "Enhanced MCP System", "🔗 Connected to Enhanced MCP Agent SignalR Hub with full feature support");
        await Clients.Caller.SendAsync("ReceiveMessage", "System", "🚀 Features: Sampling, Notifications, Progress Tracking, Elicitation");
        
        // Send initial status
        var status = await _mcpAgentService.GetStatusAsync();
        await Clients.Caller.SendAsync("ReceiveMessage", "System Status", status);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await _notificationService.AddNotificationAsync($"Client disconnected: {Context.ConnectionId}", NotificationType.SystemEvent);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Handle elicitation response from client
    /// </summary>
    public async Task SubmitElicitationResponse(McpElicitationResponse response)
    {
        try
        {
            _logger.LogInformation("📝 Received elicitation response for session {SessionId} with action {Action}", response.SessionId, response.Action);
            
            var result = await _elicitationService.SubmitAnswerAsync(response.SessionId, response.Content);
            
            if (response.Action == "accept" && result.IsComplete)
            {
                // Execute the tool with complete parameters
                var session = await _elicitationService.GetSessionAsync(response.SessionId);
                if (session != null)
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", "System", $"✅ Parameters confirmed! Executing {session.ToolName}...");
                    
                    if (session.ToolName == "travel_agent")
                    {
                        var destination = session.CurrentArguments.GetValueOrDefault("destination")?.ToString() ?? "Unknown";
                        var result2 = await _mcpAgentService.ExecuteTravelAgentAsync(destination, session.CurrentArguments);
                        await Clients.All.SendAsync("ReceiveMessage", "Enhanced Travel Agent", result2);
                    }
                    else if (session.ToolName == "research_agent")
                    {
                        var topic = session.CurrentArguments.GetValueOrDefault("topic")?.ToString() ?? "Unknown";
                        var result2 = await _mcpAgentService.ExecuteResearchAgentAsync(topic, session.CurrentArguments);
                        await Clients.All.SendAsync("ReceiveMessage", "Enhanced Research Agent", result2);
                    }
                }
            }
            else if (response.Action == "reject")
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "❌ Parameter collection rejected. Operation cancelled.");
            }
            else if (response.Action == "cancel")
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "⚠️ Parameter collection cancelled by user.");
            }
            
            await Clients.Caller.SendAsync("ElicitationComplete", response.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing elicitation response");
            await Clients.Caller.SendAsync("ReceiveMessage", "System", $"❌ Error processing parameters: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancel an active elicitation session
    /// </summary>
    public async Task CancelElicitation(int sessionId)
    {
        try
        {
            await _elicitationService.CancelSessionAsync(sessionId);
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "⚠️ Parameter collection cancelled.");
            await Clients.Caller.SendAsync("ElicitationComplete", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling elicitation session");
        }
    }

    /// <summary>
    /// Create MCP-compliant elicitation request from session
    /// </summary>
    private McpElicitationRequest CreateElicitationRequest(ElicitationSession session)
    {
        var schema = new McpJsonSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpSchemaProperty>(),
            Required = new List<string>()
        };

        // Build schema based on tool type and missing parameters
        foreach (var param in session.MissingParameters)
        {
            var property = param.ToLowerInvariant() switch
            {
                "budget" => new McpSchemaProperty
                {
                    Type = "number",
                    Title = "Budget",
                    Description = "Your travel budget in USD",
                    Minimum = 100,
                    Maximum = 50000,
                    Default = 2000
                },
                "duration" => new McpSchemaProperty
                {
                    Type = "string", 
                    Title = "Duration",
                    Description = "Trip duration (e.g., '7 days', '2 weeks')",
                    Pattern = @"^\d+\s+(day|days|week|weeks|month|months)$"
                },
                "preferences" => new McpSchemaProperty
                {
                    Type = "string",
                    Title = "Travel Preferences", 
                    Description = "Your travel preferences and interests"
                },
                "traveldate" => new McpSchemaProperty
                {
                    Type = "string",
                    Title = "Travel Date",
                    Description = "Preferred travel date",
                    Format = "date"
                },
                "keywords" => new McpSchemaProperty
                {
                    Type = "string",
                    Title = "Research Keywords",
                    Description = "Keywords to focus the research on"
                },
                "depth" => new McpSchemaProperty
                {
                    Type = "string",
                    Title = "Research Depth",
                    Description = "Level of detail for the research",
                    Enum = new List<string> { "basic", "detailed", "comprehensive" },
                    EnumNames = new List<string> { "Basic Overview", "Detailed Analysis", "Comprehensive Report" },
                    Default = "detailed"
                },
                _ => new McpSchemaProperty
                {
                    Type = "string",
                    Title = param.Replace("_", " ").Replace("-", " "),
                    Description = $"Please provide {param.Replace("_", " ")}"
                }
            };

            schema.Properties[param] = property;
            if (param != "preferences" && param != "keywords") // Make most fields required except optional ones
            {
                schema.Required.Add(param);
            }
        }

        return new McpElicitationRequest
        {
            SessionId = session.Id,
            ToolName = session.ToolName,
            Message = $"To complete your {session.ToolName.Replace("_", " ")} request, I need some additional information:",
            RequestedSchema = schema
        };
    }

    // Quick action methods for ChatFixed UI
    public async Task InitiateTravelBooking()
    {
        try
        {
            await _notificationService.AddNotificationAsync("🛫 Starting travel booking process...", NotificationType.Info);
            await _progressService.UpdateProgressAsync("travel_booking", 0, "Initializing travel agent");
            
            // Start the travel agent elicitation process
            await ExecuteTravelAgent(Context.UserIdentifier ?? "User", "Tokyo", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating travel booking");
            await Clients.Caller.SendAsync("Error", $"Failed to start travel booking: {ex.Message}");
        }
    }

    public async Task InitiateResearch()
    {
        try
        {
            await _notificationService.AddNotificationAsync("🔬 Starting research process...", NotificationType.Info);
            await _progressService.UpdateProgressAsync("research", 0, "Initializing research agent");
            
            // Start the research agent elicitation process
            await ExecuteResearchAgent(Context.UserIdentifier ?? "User", "AI Technologies", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating research");
            await Clients.Caller.SendAsync("Error", $"Failed to start research: {ex.Message}");
        }
    }
}
