namespace McpAgent.Client;

/// <summary>
/// Types of notifications in the MCP system
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// General information notification
    /// </summary>
    Info,
    
    /// <summary>
    /// Successful operation notification
    /// </summary>
    Success,
    
    /// <summary>
    /// Warning notification that doesn't prevent operation
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error notification indicating a problem
    /// </summary>
    Error,
    
    /// <summary>
    /// User action notification
    /// </summary>
    UserAction,
    
    /// <summary>
    /// Tool execution notification
    /// </summary>
    ToolExecution,
    
    /// <summary>
    /// AI analysis or insight notification
    /// </summary>
    AIAnalysis,
    
    /// <summary>
    /// Information elicitation notification
    /// </summary>
    Elicitation,
    
    /// <summary>
    /// MCP sampling notification
    /// </summary>
    Sampling
}

/// <summary>
/// Represents a notification message in the MCP system
/// </summary>
public class NotificationMessage
{
    /// <summary>
    /// The timestamp when the notification was created
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// The type of notification
    /// </summary>
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// The notification message content
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional metadata associated with the notification
    /// </summary>
    public Dictionary<string, object?>? Metadata { get; set; }
}
