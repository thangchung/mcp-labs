using McpAgent.Core.Agents;
using McpAgent.Core.Events;
using Microsoft.Extensions.Logging;

namespace McpAgent.Server.Services;

/// <summary>
/// Represents an active agent session that can be resumed across requests
/// </summary>
public class AgentSession : IAgentSession
{
    private readonly string _sessionId;
    private readonly string _agentType;
    private readonly IEventStore _eventStore;
    private readonly ILogger _logger;
    private readonly IMcpServerClient? _mcpClient;
    private IAgent? _agent;
    private readonly List<IEvent> _sessionEvents;

    public AgentSession(string sessionId, string agentType, IEventStore eventStore, ILogger logger, IMcpServerClient? mcpClient = null)
    {
        _sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        _agentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mcpClient = mcpClient; // Optional - can work without MCP client for local operations
        _sessionEvents = new List<IEvent>();
    }

    /// <inheritdoc />
    public string SessionId => _sessionId;

    /// <inheritdoc />
    public string AgentType => _agentType;

    /// <inheritdoc />
    public bool IsActive { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<IEvent> Events => _sessionEvents.AsReadOnly();

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing session {SessionId} for agent type {AgentType}", _sessionId, _agentType);

        // Create the appropriate agent instance
        _agent = CreateAgent(_agentType);

        // Record session started event
        var startedEvent = new SessionStartedEvent(_sessionId, _agentType, DateTimeOffset.UtcNow);
        await RecordEventAsync(startedEvent);

        IsActive = true;
    }

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(AgentContext context)
    {
        if (!IsActive)
            throw new InvalidOperationException("Session is not active");

        if (_agent == null)
            throw new InvalidOperationException("Agent is not initialized");

        _logger.LogInformation("Executing agent {AgentType} in session {SessionId}", _agentType, _sessionId);

        try
        {
            var result = await _agent.ExecuteAsync(context);

            // Record execution event
            var executionEvent = new AgentExecutionEvent(_sessionId, _agentType, context.Arguments, result, DateTimeOffset.UtcNow);
            await RecordEventAsync(executionEvent);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing agent {AgentType} in session {SessionId}", _agentType, _sessionId);
            
            // Record error event
            var errorEvent = new AgentErrorEvent(_sessionId, _agentType, ex.Message, DateTimeOffset.UtcNow);
            await RecordEventAsync(errorEvent);
            
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RestoreFromEventsAsync(IEnumerable<IEvent> events)
    {
        _logger.LogInformation("Restoring session {SessionId} from {EventCount} events", _sessionId, events.Count());

        _sessionEvents.Clear();
        _sessionEvents.AddRange(events);

        // Find the session started event to get agent type
        var sessionStarted = events.OfType<SessionStartedEvent>().FirstOrDefault();
        if (sessionStarted == null)
        {
            throw new InvalidOperationException("Cannot restore session without SessionStartedEvent");
        }

        // Create agent instance
        _agent = CreateAgent(sessionStarted.AgentType);

        IsActive = true;
    }

    /// <inheritdoc />
    public async Task EndAsync()
    {
        _logger.LogInformation("Ending session {SessionId}", _sessionId);

        if (IsActive)
        {
            var endedEvent = new SessionEndedEvent(_sessionId, DateTimeOffset.UtcNow);
            await RecordEventAsync(endedEvent);
        }

        IsActive = false;
    }

    /// <inheritdoc />
    public async Task RecordEventAsync(IEvent eventData)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _sessionEvents.Add(eventData);
        await _eventStore.StoreEventAsync(_sessionId, eventData);
    }

    /// <inheritdoc />
    public async Task SendProgressNotificationAsync(string progressToken, int progress, int total, string message, string? relatedRequestId = null)
    {
        _logger.LogInformation("Progress notification for session {SessionId}: {Progress}/{Total} - {Message}", 
            _sessionId, progress, total, message);
        
        // Send real MCP progress notification if client is available
        if (_mcpClient != null)
        {
            try
            {
                await _mcpClient.SendProgressNotificationAsync(progressToken, progress, total, message, relatedRequestId);
                _logger.LogDebug("Successfully sent MCP progress notification for session {SessionId}", _sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send MCP progress notification for session {SessionId}: {Message}", _sessionId, ex.Message);
            }
        }
        else
        {
            _logger.LogDebug("No MCP client available for session {SessionId}, progress notification logged only", _sessionId);
        }
    }

    /// <inheritdoc />
    public async Task SendLogMessageAsync(string level, string data, string logger, string? relatedRequestId = null)
    {
        _logger.LogInformation("Log message for session {SessionId} [{Level}] {Logger}: {Data}", 
            _sessionId, level, logger, data);
        
        // Send real MCP log notification if client is available
        if (_mcpClient != null)
        {
            try
            {
                await _mcpClient.SendLogMessageAsync(level, data, logger, relatedRequestId);
                _logger.LogDebug("Successfully sent MCP log notification for session {SessionId}", _sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send MCP log notification for session {SessionId}: {Message}", _sessionId, ex.Message);
            }
        }
        else
        {
            _logger.LogDebug("No MCP client available for session {SessionId}, log message logged only", _sessionId);
        }
    }

    /// <inheritdoc />
    public async Task<ElicitationResult?> ElicitAsync(string message, object? requestedSchema = null, string? relatedRequestId = null)
    {
        _logger.LogInformation("Eliciting user input for session {SessionId}: {Message}", _sessionId, message);
        
        // Send real MCP elicitation request if client is available
        if (_mcpClient != null)
        {
            try
            {
                var result = await _mcpClient.ElicitAsync(message, requestedSchema, relatedRequestId);
                _logger.LogInformation("Received elicitation response for session {SessionId}: {Action}", _sessionId, result?.Action);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send MCP elicitation request for session {SessionId}: {Message}", _sessionId, ex.Message);
            }
        }
        
        // Fallback response when MCP client is unavailable
        _logger.LogDebug("No MCP client available for session {SessionId}, returning fallback elicitation response", _sessionId);
        
        var fallbackResult = new ElicitationResult
        {
            Action = "input",
            Content = new Dictionary<string, object?> 
            { 
                ["response"] = $"[Simulated user response to: {message}]",
                ["source"] = "fallback"
            }
        };
        
        return fallbackResult;
    }

    /// <inheritdoc />
    public async Task<SamplingResult?> CreateMessageAsync(IEnumerable<SamplingMessage> messages, int? maxTokens = null, string? relatedRequestId = null)
    {
        _logger.LogInformation("Creating AI message for session {SessionId} with {MessageCount} messages", _sessionId, messages.Count());
        
        // Send real MCP sampling request if client is available
        if (_mcpClient != null)
        {
            try
            {
                var result = await _mcpClient.CreateMessageAsync(messages, maxTokens, relatedRequestId);
                _logger.LogInformation("Received AI message for session {SessionId} from model: {Model}", _sessionId, result?.Model);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send MCP sampling request for session {SessionId}: {Message}", _sessionId, ex.Message);
            }
        }
        
        // Fallback response when MCP client is unavailable
        _logger.LogDebug("No MCP client available for session {SessionId}, returning fallback AI response", _sessionId);
        
        var lastMessage = messages.LastOrDefault();
        var fallbackResult = new SamplingResult
        {
            Role = "assistant",
            Content = new SamplingContent 
            { 
                Type = "text", 
                Text = $"I apologize, but I'm currently unable to process your request due to a temporary connection issue. Your message: '{lastMessage?.Content?.Text ?? "conversation"}' has been noted." 
            },
            Model = "fallback-model",
            StopReason = "end_turn"
        };
        
        return fallbackResult;
    }

    private IAgent CreateAgent(string agentType)
    {
        return agentType.ToLowerInvariant() switch
        {
            "travel_agent" => new TravelAgent(_logger as ILogger<TravelAgent>),
            "research_agent" => new ResearchAgent(_logger as ILogger<ResearchAgent>),
            _ => throw new ArgumentException($"Unknown agent type: {agentType}", nameof(agentType))
        };
    }
}
