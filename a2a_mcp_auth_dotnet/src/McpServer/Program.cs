using Microsoft.Identity.Web;
using McpServer.Tools;
using McpServer.Middleware;
using ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace McpServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure structured logging with JSON formatter
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            options.UseUtcTimestamp = true;
            options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
            {
                Indented = false
            };
        });

        // Add OpenTelemetry logging
        builder.AddObservabilityLogging("McpServer");

        // Configure Microsoft Entra ID authentication with optimized caching
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

        // Add observability (OpenTelemetry)
        builder.Services.AddObservability("McpServer", builder.Configuration, builder.Environment);

        builder.Services.AddServiceDefaults(builder.Configuration);

        // Configure logging
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        var app = builder.Build();

        // Add JSON-RPC tracing middleware early in the pipeline
        app.UseMiddleware<JsonRpcTracingMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        // Map MCP endpoint with authentication
        app.MapMcp("/mcp").RequireAuthorization("AdminOnly");

        app.MapDefaultEndpoints("McpServer");

        app.Run();
    }
}
