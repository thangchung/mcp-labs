namespace McpAgent.Core.Events;

/// <summary>
/// Base interface for all events in the system
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Gets the timestamp when the event occurred
    /// </summary>
    DateTimeOffset Timestamp { get; }
}

/// <summary>
/// Event fired when a new agent session is started
/// </summary>
public record SessionStartedEvent(string SessionId, string AgentType, DateTimeOffset Timestamp) : IEvent;

/// <summary>
/// Event fired when an agent execution completes successfully
/// </summary>
public record AgentExecutionEvent(string SessionId, string AgentType, Dictionary<string, object?> Arguments, string Result, DateTimeOffset Timestamp) : IEvent;

/// <summary>
/// Event fired when an agent execution encounters an error
/// </summary>
public record AgentErrorEvent(string SessionId, string AgentType, string ErrorMessage, DateTimeOffset Timestamp) : IEvent;

/// <summary>
/// Event fired when an agent session is ended
/// </summary>
public record SessionEndedEvent(string SessionId, DateTimeOffset Timestamp) : IEvent;
