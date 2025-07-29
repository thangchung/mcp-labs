using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using McpAgent.Server.Services;
using System.ComponentModel;
using System.Text.Json;

namespace McpAgent.Server.Tools;

/// <summary>
/// Travel agent MCP tool
/// </summary>
public class TravelAgentTool
{
    private readonly IMcpAgentServerExtended _agentServer;
    private readonly ILogger<TravelAgentTool> _logger;

    public TravelAgentTool(IMcpAgentServerExtended agentServer, ILogger<TravelAgentTool> logger)
    {
        _agentServer = agentServer ?? throw new ArgumentNullException(nameof(agentServer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Description("Book travel with price confirmation and interactive decision making")]
    public async Task<string> ExecuteAsync(
        [Description("Travel destination")] string destination,
        [Description("Check-in date")] string checkIn,
        [Description("Check-out date")] string checkOut,
        [Description("Number of guests")] int guests)
    {
        _logger.LogInformation("Executing travel_agent tool with destination: {Destination}", destination);

        var arguments = new Dictionary<string, object?>
        {
            { "destination", destination },
            { "checkIn", checkIn },
            { "checkOut", checkOut },
            { "guests", guests }
        };

        var requestId = Guid.NewGuid().ToString();
        return await _agentServer.ExecuteAgentAsync("travel_agent", arguments, requestId);
    }
}

/// <summary>
/// Research agent MCP tool
/// </summary>
public class ResearchAgentTool
{
    private readonly IMcpAgentServerExtended _agentServer;
    private readonly ILogger<ResearchAgentTool> _logger;

    public ResearchAgentTool(IMcpAgentServerExtended agentServer, ILogger<ResearchAgentTool> logger)
    {
        _agentServer = agentServer ?? throw new ArgumentNullException(nameof(agentServer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Description("Research topics with AI-assisted summaries and analysis")]
    public async Task<string> ExecuteAsync(
        [Description("Research topic")] string topic,
        [Description("Research depth (basic, detailed, comprehensive)")] string depth = "basic")
    {
        _logger.LogInformation("Executing research_agent tool with topic: {Topic}", topic);

        var arguments = new Dictionary<string, object?>
        {
            { "topic", topic },
            { "depth", depth }
        };

        var requestId = Guid.NewGuid().ToString();
        return await _agentServer.ExecuteAgentAsync("research_agent", arguments, requestId);
    }
}
