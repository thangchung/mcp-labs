using Microsoft.AspNetCore.SignalR;
using McpAgent.XServer.Services;

namespace McpAgent.XServer.Hubs;

public class ChatHub : Hub
{
    private readonly McpAgentService _mcpAgentService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(McpAgentService mcpAgentService, ILogger<ChatHub> logger)
    {
        _mcpAgentService = mcpAgentService;
        _logger = logger;
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task ExecuteTravelAgent(string user, string destination)
    {
        try
        {
            _logger.LogInformation("🛫 Travel request from {User} to {Destination}", user, destination);
            
            // Send immediate acknowledgment
            await Clients.All.SendAsync("ReceiveMessage", "System", $"🛫 Processing travel request to {destination}...");
            
            // Execute travel agent through MCP
            var result = await _mcpAgentService.ExecuteTravelAgentAsync(destination);
            
            // Send the result back to all clients
            await Clients.All.SendAsync("ReceiveMessage", "Travel Agent", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing travel agent");
            await Clients.All.SendAsync("ReceiveMessage", "System", $"❌ Error processing travel request: {ex.Message}");
        }
    }

    public async Task ExecuteResearchAgent(string user, string topic)
    {
        try
        {
            _logger.LogInformation("🔍 Research request from {User} for {Topic}", user, topic);
            
            // Send immediate acknowledgment
            await Clients.All.SendAsync("ReceiveMessage", "System", $"🔍 Researching {topic}...");
            
            // Execute research agent through MCP
            var result = await _mcpAgentService.ExecuteResearchAgentAsync(topic);
            
            // Send the result back to all clients
            await Clients.All.SendAsync("ReceiveMessage", "Research Agent", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing research agent");
            await Clients.All.SendAsync("ReceiveMessage", "System", $"❌ Error processing research request: {ex.Message}");
        }
    }

    public async Task SendStatus(string user)
    {
        try
        {
            var status = await _mcpAgentService.GetStatusAsync();
            await Clients.All.SendAsync("ReceiveMessage", "System", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status");
            await Clients.All.SendAsync("ReceiveMessage", "System", $"❌ Error getting status: {ex.Message}");
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("ReceiveMessage", "System", "🔗 Connected to MCP Agent SignalR Hub");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
