using McpAgent.Core.Events;

namespace McpAgent.Core.Agents;

/// <summary>
/// Context for agent tool execution.
/// </summary>
public class AgentContext
{
    public required string RequestId { get; init; }
    public required string ToolName { get; init; }
    public required Dictionary<string, object?> Arguments { get; init; }
    public required IAgentSession Session { get; init; }
}

/// <summary>
/// Result of an elicitation request.
/// </summary>
public class ElicitationResult
{
    public required string Action { get; init; }
    public required Dictionary<string, object?> Content { get; init; }
}

/// <summary>
/// Result of a sampling request.
/// </summary>
public class SamplingResult
{
    public required string Role { get; init; }
    public required SamplingContent Content { get; init; }
    public required string Model { get; init; }
    public required string StopReason { get; init; }
}

/// <summary>
/// Content for sampling messages.
/// </summary>
public class SamplingContent
{
    public required string Type { get; init; }
    public required string Text { get; init; }
}

/// <summary>
/// Message for sampling requests.
/// </summary>
public class SamplingMessage
{
    public required string Role { get; init; }
    public required SamplingContent Content { get; init; }
}

/// <summary>
/// Schema for price confirmation elicitation.
/// </summary>
public class PriceConfirmationSchema
{
    public string Type => "object";
    public Dictionary<string, object> Properties => new()
    {
        ["confirm"] = new { type = "boolean", description = "Whether to confirm the price" },
        ["notes"] = new { type = "string", description = "Optional notes from the user" }
    };
    public string[] Required => ["confirm"];
}

/// <summary>
/// Interface for MCP agent server operations
/// </summary>
public interface IMcpAgentServer
{
    /// <summary>
    /// Creates a new agent session
    /// </summary>
    /// <param name="agentType">The type of agent to create</param>
    /// <param name="sessionId">The session identifier</param>
    /// <returns>The created session</returns>
    Task<IAgentSession> CreateSessionAsync(string agentType, string sessionId);

    /// <summary>
    /// Gets an existing session by ID
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <returns>The session if found, otherwise null</returns>
    Task<IAgentSession?> GetSessionAsync(string sessionId);

    /// <summary>
    /// Ends a session
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <returns>True if the session was ended, false if not found</returns>
    Task<bool> EndSessionAsync(string sessionId);

    /// <summary>
    /// Gets all active session IDs
    /// </summary>
    /// <returns>The collection of active session IDs</returns>
    Task<IEnumerable<string>> GetActiveSessionsAsync();
}
