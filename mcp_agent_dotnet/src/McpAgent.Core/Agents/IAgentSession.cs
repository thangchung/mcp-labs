using McpAgent.Core.Events;

namespace McpAgent.Core.Agents;

/// <summary>
/// Interface for agent session operations.
/// </summary>
public interface IAgentSession
{
    /// <summary>
    /// Gets the session identifier
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the agent type for this session
    /// </summary>
    string AgentType { get; }

    /// <summary>
    /// Gets whether the session is currently active
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the events recorded for this session
    /// </summary>
    IReadOnlyList<IEvent> Events { get; }

    /// <summary>
    /// Initializes the session
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Executes an agent with the provided context
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <returns>The result of the execution</returns>
    Task<string> ExecuteAsync(AgentContext context);

    /// <summary>
    /// Restores the session from a collection of events
    /// </summary>
    /// <param name="events">The events to restore from</param>
    Task RestoreFromEventsAsync(IEnumerable<IEvent> events);

    /// <summary>
    /// Ends the session
    /// </summary>
    Task EndAsync();

    /// <summary>
    /// Records an event for this session
    /// </summary>
    /// <param name="eventData">The event to record</param>
    Task RecordEventAsync(IEvent eventData);

    /// <summary>
    /// Sends a progress notification to the client.
    /// </summary>
    Task SendProgressNotificationAsync(string progressToken, int progress, int total, string message, string? relatedRequestId = null);

    /// <summary>
    /// Sends a log message to the client.
    /// </summary>
    Task SendLogMessageAsync(string level, string data, string logger, string? relatedRequestId = null);

    /// <summary>
    /// Requests user input via elicitation.
    /// </summary>
    Task<ElicitationResult?> ElicitAsync(string message, object? requestedSchema = null, string? relatedRequestId = null);

    /// <summary>
    /// Requests AI assistance via sampling.
    /// </summary>
    Task<SamplingResult?> CreateMessageAsync(IEnumerable<SamplingMessage> messages, int? maxTokens = null, string? relatedRequestId = null);
}
