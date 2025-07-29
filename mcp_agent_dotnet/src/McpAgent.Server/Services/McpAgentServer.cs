using McpAgent.Core.Agents;
using McpAgent.Core.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace McpAgent.Server.Services;

/// <summary>
/// Extended interface that adds server-specific functionality to the base MCP agent server
/// </summary>
public interface IMcpAgentServerExtended : IMcpAgentServer
{
    /// <summary>
    /// Gets the available agent types registered in the server
    /// </summary>
    IReadOnlyDictionary<string, Type> AvailableAgents { get; }
    
    /// <summary>
    /// Executes an agent with the given parameters
    /// </summary>
    Task<string> ExecuteAgentAsync(string agentType, Dictionary<string, object?> arguments, string requestId);
}

/// <summary>
/// Implementation of MCP Agent Server that manages agent sessions and executes agent operations
/// </summary>
public class McpAgentServer : IMcpAgentServerExtended
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<McpAgentServer> _logger;
    private readonly ConcurrentDictionary<string, IAgentSession> _activeSessions;
    private readonly Dictionary<string, Type> _availableAgents;

    public McpAgentServer(IEventStore eventStore, ILogger<McpAgentServer> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activeSessions = new ConcurrentDictionary<string, IAgentSession>();
        _availableAgents = new Dictionary<string, Type>
        {
            { "travel_agent", typeof(TravelAgent) },
            { "research_agent", typeof(ResearchAgent) }
        };
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Type> AvailableAgents => _availableAgents.AsReadOnly();

    /// <inheritdoc />
    public async Task<IAgentSession> CreateSessionAsync(string agentType, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(agentType))
            throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));
        
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        if (!_availableAgents.TryGetValue(agentType, out var agentTypeClass))
        {
            throw new ArgumentException($"Unknown agent type: {agentType}", nameof(agentType));
        }

        _logger.LogInformation("Creating session {SessionId} for agent type {AgentType}", sessionId, agentType);

        var session = new AgentSession(sessionId, agentType, _eventStore, _logger);
        await session.InitializeAsync();

        _activeSessions.TryAdd(sessionId, session);

        return session;
    }

    /// <inheritdoc />
    public async Task<IAgentSession?> GetSessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            return session;
        }

        // Try to restore session from event store
        var events = await _eventStore.GetEventsAsync(sessionId);
        if (events.Any())
        {
            var firstEvent = events.First();
            if (firstEvent is SessionStartedEvent sessionStarted)
            {
                _logger.LogInformation("Restoring session {SessionId} for agent type {AgentType}", sessionId, sessionStarted.AgentType);
                
                var restoredSession = new AgentSession(sessionId, sessionStarted.AgentType, _eventStore, _logger);
                await restoredSession.RestoreFromEventsAsync(events.Cast<IEvent>());
                
                _activeSessions.TryAdd(sessionId, restoredSession);
                return restoredSession;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> EndSessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        _logger.LogInformation("Ending session {SessionId}", sessionId);

        if (_activeSessions.TryRemove(sessionId, out var session))
        {
            await session.EndAsync();
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetActiveSessionsAsync()
    {
        return Task.FromResult(_activeSessions.Keys.AsEnumerable());
    }

    /// <inheritdoc />
    public async Task<string> ExecuteAgentAsync(string agentType, Dictionary<string, object?> arguments, string requestId)
    {
        if (string.IsNullOrWhiteSpace(agentType))
            throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));

        if (!_availableAgents.ContainsKey(agentType))
        {
            throw new ArgumentException($"Unknown agent type: {agentType}", nameof(agentType));
        }

        var sessionId = requestId ?? Guid.NewGuid().ToString();
        
        try
        {
            // Create or get existing session
            var session = await GetSessionAsync(sessionId) ?? await CreateSessionAsync(agentType, sessionId);
            
            // Create agent context with required properties
            var context = new AgentContext 
            { 
                RequestId = requestId,
                ToolName = agentType,
                Arguments = arguments,
                Session = session
            };
            
            // Execute the agent
            var result = await session.ExecuteAsync(context);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent {AgentType} with request {RequestId}", agentType, requestId);
            throw;
        }
    }
}
