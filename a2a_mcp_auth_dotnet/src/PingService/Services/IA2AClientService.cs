using PingService.Models;

namespace PingService.Services;

public interface IA2AClientService
{
    Task<A2AServiceResponse> SendMessageAsync(string message, string jwtToken, string userEmail);
}

public class A2AServiceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? Error { get; set; }
    public A2AResponseData A2AResponse { get; set; } = new();
    public McpResponseData McpResponse { get; set; } = new();
}
