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
    private IAgent? _agent;
    private readonly List<IEvent> _sessionEvents;

    public AgentSession(string sessionId, string agentType, IEventStore eventStore, ILogger logger)
    {
        _sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        _agentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        
        // In a real implementation, this would send a progress notification to the MCP client
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SendLogMessageAsync(string level, string data, string logger, string? relatedRequestId = null)
    {
        _logger.LogInformation("Log message for session {SessionId} [{Level}] {Logger}: {Data}", 
            _sessionId, level, logger, data);
        
        // In a real implementation, this would send a log message to the MCP client
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<ElicitationResult?> ElicitAsync(string message, object? requestedSchema = null, string? relatedRequestId = null)
    {
        _logger.LogInformation("Eliciting user input for session {SessionId}: {Message}", _sessionId, message);
        
        // For now, return a simulated response
        // In a real implementation, this would send a request to the MCP client
        await Task.Delay(100); // Simulate network delay
        
        var result = new ElicitationResult
        {
            Action = "input",
            Content = new Dictionary<string, object?> 
            { 
                ["response"] = $"[Simulated user response to: {message}]" 
            }
        };
        
        _logger.LogInformation("Received elicitation response for session {SessionId}", _sessionId);
        
        return result;
    }

    /// <inheritdoc />
    public async Task<SamplingResult?> CreateMessageAsync(IEnumerable<SamplingMessage> messages, int? maxTokens = null, string? relatedRequestId = null)
    {
        _logger.LogInformation("Creating AI message for session {SessionId} with {MessageCount} messages", _sessionId, messages.Count());
        
        // For now, return a simulated AI response
        // In a real implementation, this would use the MCP sampling capability
        await Task.Delay(200); // Simulate AI processing delay
        
        var lastMessage = messages.LastOrDefault();
        var result = new SamplingResult
        {
            Role = "assistant",
            Content = new SamplingContent 
            { 
                Type = "text", 
                Text = $"[AI Response based on: {lastMessage?.Content?.Text ?? "conversation"}]" 
            },
            Model = "simulated-model",
            StopReason = "end_turn"
        };
        
        _logger.LogInformation("Generated AI message for session {SessionId}", _sessionId);
        
        return result;
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
