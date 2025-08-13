namespace PongService.Services;

public interface IMcpClientService
{
    Task<McpResponse> CallMcpServerAsync(string jwtToken, string message, string userEmail);
}

public class McpResponse
{
    public bool Success { get; set; }
    public bool AdminAccess { get; set; }
    public bool ToolExecuted { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResponseContent { get; set; }
    public DateTime Timestamp { get; set; }
}
