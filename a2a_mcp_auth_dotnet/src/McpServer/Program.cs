using Microsoft.Identity.Web;
using McpServer.Tools;

namespace McpServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Microsoft Entra ID authentication
        builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

        // Configure authorization with proper MCP policies following official patterns
        builder.Services.AddAuthorization(options =>
        {
            // Admin policy for administrative tools - using Microsoft Entra ID scope claim
            options.AddPolicy("AdminOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", "admin");
            });
        });

        // Configure MCP server using official SDK pattern
        builder.Services.AddMcpServer()
            .WithTools<McpTools>()
            .WithHttpTransport();
        
        // Configure logging
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        // Map MCP endpoint with authentication
        app.MapMcp("/mcp").RequireAuthorization("AdminOnly");

        // Health check endpoint
        app.MapGet("/health", () => new { Status = "Healthy", Service = "McpServer", Timestamp = DateTime.UtcNow });

        app.Run();
    }
}
