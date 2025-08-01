using Microsoft.AspNetCore.SignalR;
using McpAgent.Client.Services;

namespace McpAgent.Client.Hubs;

/// <summary>
/// SignalR Hub for real-time MCP notifications and chat messaging
/// </summary>
public class McpNotificationHub : Hub
{
    private readonly ILogger<McpNotificationHub> _logger;
    private readonly McpIntegratedChatService _mcpService;

    public McpNotificationHub(ILogger<McpNotificationHub> logger, McpIntegratedChatService mcpService)
    {
        _logger = logger;
        _mcpService = mcpService;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("🔌 Client connected to MCP notification hub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("🔌 Client disconnected from MCP notification hub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Handle basic chat messages between clients
    /// </summary>
    public async Task SendMessage(string user, string message)
    {
        _logger.LogInformation("💬 Message from {User}: {Message}", user, message);
        
        // Broadcast the message to all connected clients
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    /// <summary>
    /// Trigger an MCP action and broadcast the result
    /// </summary>
    public async Task TriggerMcpAction(string actionType, string request)
    {
        _logger.LogInformation("🚀 Triggering MCP action: {ActionType} - {Request}", actionType, request);
        
        try
        {
            // Send immediate acknowledgment
            await Clients.Caller.SendAsync("McpNotification", $"Processing {actionType}: {request}");
            
            // Process the action based on type
            string result = actionType switch
            {
                "travel_agent" => await ProcessTravelRequest(request),
                "research_agent" => await ProcessResearchRequest(request),
                "test_travel_request" => await ProcessTravelRequest(request),
                "test_research_request" => await ProcessResearchRequest(request),
                _ => $"Unknown action type: {actionType}"
            };
            
            // Broadcast the result
            await Clients.All.SendAsync("McpNotification", $"✅ {actionType} completed: {result}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to process MCP action: {ActionType}", actionType);
            await Clients.Caller.SendAsync("McpNotification", $"❌ Error processing {actionType}: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle sampling response from client
    /// </summary>
    public async Task SendSamplingResponse(string requestId, object response)
    {
        _logger.LogInformation("📤 Received sampling response for request {RequestId}: {Response}", requestId, response);
        
        // Broadcast the response to other components that might be waiting
        await Clients.All.SendAsync("SamplingResponseReceived", requestId, response);
    }

    private async Task<string> ProcessTravelRequest(string request)
    {
        try
        {
            // Initialize MCP service if needed
            await _mcpService.InitializeAsync();
            
            // For demo purposes, return a mock response
            // In a real implementation, you'd call the actual MCP travel agent
            return $"Travel request processed: {request}. Flight options and itinerary generated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process travel request");
            return $"Failed to process travel request: {ex.Message}";
        }
    }

    private async Task<string> ProcessResearchRequest(string request)
    {
        try
        {
            // Initialize MCP service if needed
            await _mcpService.InitializeAsync();
            
            // For demo purposes, return a mock response
            // In a real implementation, you'd call the actual MCP research agent
            return $"Research completed: {request}. Comprehensive analysis available.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process research request");
            return $"Failed to process research request: {ex.Message}";
        }
    }
}
