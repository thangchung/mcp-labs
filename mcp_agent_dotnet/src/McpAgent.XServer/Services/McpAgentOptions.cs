namespace McpAgent.XServer.Services;

/// <summary>
/// Configuration options for the MCP Agent
/// </summary>
public class McpAgentOptions
{
    /// <summary>
    /// URL of the MCP server
    /// </summary>
    public string? McpServerUrl { get; set; }
    
    /// <summary>
    /// OpenAI API Key
    /// </summary>
    public string? OpenAiApiKey { get; set; }
    
    /// <summary>
    /// OpenAI model to use (default: gpt-4o-mini)
    /// </summary>
    public string? OpenAiModel { get; set; }
    
    /// <summary>
    /// OpenAI API endpoint (default: https://api.openai.com/v1)
    /// </summary>
    public string? OpenAiEndpoint { get; set; }
    
    /// <summary>
    /// Enable enhanced MCP features (sampling, notifications, progress, elicitation)
    /// </summary>
    public bool EnableEnhancedFeatures { get; set; } = true;
    
    /// <summary>
    /// Maximum number of notifications to keep in memory
    /// </summary>
    public int MaxNotifications { get; set; } = 1000;
    
    /// <summary>
    /// Maximum number of progress contexts to keep active
    /// </summary>
    public int MaxActiveProgress { get; set; } = 100;
    
    /// <summary>
    /// Default timeout for MCP operations in seconds
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300;
    
    /// <summary>
    /// Enable verbose logging for MCP operations
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = true;
}
