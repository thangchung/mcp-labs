namespace McpAgent.Client.Models;

/// <summary>
/// MCP Progress notification
/// </summary>
public class McpProgressNotification
{
    public int Progress { get; set; }
    public int Total { get; set; } = 100;
    public string Message { get; set; } = "";
    public string? ProgressToken { get; set; }
}

/// <summary>
/// MCP Log notification
/// </summary>
public class McpLogNotification
{
    public string Level { get; set; } = "info";
    public string Message { get; set; } = "";
    public string? Logger { get; set; }
}

/// <summary>
/// MCP Sampling request
/// </summary>
public class McpSamplingRequest
{
    public string Id { get; set; } = "";
    public string Prompt { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// MCP Sampling response
/// </summary>
public class McpSamplingResponse
{
    public string RequestId { get; set; } = "";
    public string Response { get; set; } = "";
    public string Action { get; set; } = "";
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
}
