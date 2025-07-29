namespace McpAgent.Core.Agents;

/// <summary>
/// Interface for MCP agent tools.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Executes the agent tool with the provided context.
    /// </summary>
    /// <param name="context">The execution context.</param>
    /// <returns>The result of the agent execution.</returns>
    Task<string> ExecuteAsync(AgentContext context);
}

/// <summary>
/// Base class for MCP agents providing common functionality.
/// </summary>
public abstract class AgentBase : IAgent
{
    public abstract Task<string> ExecuteAsync(AgentContext context);

    protected static async Task SimulateWork(int durationMs = 2000)
    {
        await Task.Delay(durationMs);
    }

    protected static void ValidateArgument(Dictionary<string, object?> arguments, string key)
    {
        if (!arguments.ContainsKey(key) || arguments[key] == null)
        {
            throw new ArgumentException($"Missing required argument: {key}");
        }
    }
}
