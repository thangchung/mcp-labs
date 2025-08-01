using McpAgent.Client.Components;
using McpAgent.Client.Hubs;
using McpAgent.Client.Services;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.ResponseCompression;

// Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// Parse command line for port override
var port = 8007; // default port
for (int i = 0; i < args.Length - 1; i++)
{
    if ((args[i] == "--port" || args[i] == "-p") && int.TryParse(args[i + 1], out var parsedPort))
    {
        port = parsedPort;
        break;
    }
}

// Manual configuration binding - create a simple configuration class inline
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var mcpSection = config.GetSection("McpAgent");
    
    return new McpAgentConfig
    {
        HostPort = port, // Use command line override
        GeminiApiKey = mcpSection["GeminiApiKey"],
        OpenAI = new OpenAIConfig
        {
            ApiKey = mcpSection["OpenAI:ApiKey"],
            Model = mcpSection["OpenAI:Model"] ?? "gpt-4o-mini",
            Endpoint = mcpSection["OpenAI:Endpoint"] ?? "https://api.openai.com/v1"
        },
        McpServer = new McpServerConfig
        {
            Endpoint = mcpSection["McpServer:Endpoint"] ?? "http://localhost:8006/mcp",
            TimeoutSeconds = int.TryParse(mcpSection["McpServer:TimeoutSeconds"], out var timeout) ? timeout : 30
        }
    };
});

// Configure logging
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Debug;
});

// Add Blazor Server services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Add response compression for SignalR (optional, mainly for WebAssembly)
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

// Add HTTP client for MCP server communication
builder.Services.AddHttpClient("McpServer", (serviceProvider, httpClient) =>
{
    var config = serviceProvider.GetRequiredService<McpAgentConfig>();
    httpClient.Timeout = TimeSpan.FromSeconds(config.McpServer.TimeoutSeconds);
    httpClient.DefaultRequestHeaders.Add("User-Agent", "McpAgent-Client/1.0");
});

// Add required services as singletons to prevent reinitialization
builder.Services.AddSingleton<McpChatService>();
builder.Services.AddSingleton<McpIntegratedChatService>();

// Configure the host URL
builder.WebHost.UseUrls($"http://localhost:{port}");

var app = builder.Build();

// Use response compression for SignalR (optional)
app.UseResponseCompression();

var logger = app.Logger;

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Map Razor components using Blazor Server pattern
app.MapRazorComponents<McpAgent.Client.Components.App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub for MCP notifications
app.MapHub<McpNotificationHub>("/mcpHub");

// Log startup information
logger.LogInformation("🌐 Starting Blazor Web Interface with Streaming Chat");
logger.LogInformation("🔗 Web UI: http://localhost:{Port}", port);
logger.LogInformation("🌐 Blazor Server starting on port {Port}", port);
logger.LogInformation("🌍 Web UI: http://localhost:{Port}", port);
logger.LogInformation("Press Ctrl+C to shut down");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogError(ex, "💥 Fatal error occurred");
    return 1;
}

logger.LogInformation("👋 Client shutting down");
return 0;

// Simple configuration classes to avoid namespace issues
public class McpAgentConfig
{
    public int HostPort { get; set; } = 8007;
    public string? GeminiApiKey { get; set; }
    public OpenAIConfig OpenAI { get; set; } = new();
    public McpServerConfig McpServer { get; set; } = new();
}

public class OpenAIConfig
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
}

public class McpServerConfig
{
    public string Endpoint { get; set; } = "http://localhost:8006/mcp";
    public int TimeoutSeconds { get; set; } = 30;
}
