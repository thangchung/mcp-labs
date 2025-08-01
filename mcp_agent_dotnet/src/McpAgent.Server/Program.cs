using McpAgent.Core.Events;
using McpAgent.Core.Agents;
using McpAgent.EventStore;
using McpAgent.Server.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSingleton<IEventStore, ManagedEventStore>();

// Add HTTP client for MCP communication
builder.Services.AddHttpClient();

// Add MCP client service
builder.Services.AddScoped<IMcpServerClient>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    
    // Get MCP client endpoint from configuration or use default
    var mcpClientEndpoint = configuration.GetValue<string>("McpClient:Endpoint") ?? "http://localhost:8007/mcp";
    
    var httpClient = httpClientFactory.CreateClient("McpClient");
    httpClient.Timeout = TimeSpan.FromSeconds(30);
    
    return new HttpMcpServerClient(httpClient, mcpClientEndpoint, loggerFactory);
});

builder.Services.AddScoped<IMcpAgentServerExtended, McpAgentServer>();
builder.Services.AddScoped<IMcpAgentServer>(provider => provider.GetRequiredService<IMcpAgentServerExtended>());

// Configure logging to stderr for better compatibility with MCP clients
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Add MCP server with HTTP transport
builder.Services.AddMcpServer().WithHttpTransport();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors();
app.UseRouting();

// Map MCP endpoints
app.MapMcp();

// Manual MCP endpoint implementation for testing
app.MapPost("/mcp", async (HttpContext context, IMcpAgentServerExtended agentServer) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        
        // Parse JSON-RPC request
        var request = JsonSerializer.Deserialize<JsonElement>(body);
        
        var method = request.GetProperty("method").GetString();
        var id = request.TryGetProperty("id", out var idProp) ? idProp : (JsonElement?)null;
        
        object? result = null;
        
        switch (method)
        {
            case "initialize":
                result = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        tools = new { }
                    },
                    serverInfo = new
                    {
                        name = "mcp-agent-server",
                        version = "1.0.0"
                    }
                };
                break;
                
            case "tools/list":
                result = new
                {
                    tools = agentServer.AvailableAgents.Keys.Select(key => new
                    {
                        name = key,
                        description = key == "travel_agent" 
                            ? "Book travel with price confirmation"
                            : "Research topics with AI summaries",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new { },
                            required = new string[] { }
                        }
                    }).ToArray()
                };
                break;
                
            case "tools/call":
                var @params = request.GetProperty("params");
                var toolName = @params.GetProperty("name").GetString();
                var arguments = @params.TryGetProperty("arguments", out var argsProp) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object?>>(argsProp.GetRawText())
                    : new Dictionary<string, object?>();
                
                if (toolName != null && agentServer.AvailableAgents.ContainsKey(toolName))
                {
                    var requestId = Guid.NewGuid().ToString();
                    var toolResult = await agentServer.ExecuteAgentAsync(toolName, arguments ?? new(), requestId);
                    
                    result = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = toolResult
                            }
                        }
                    };
                }
                else
                {
                    throw new ArgumentException($"Unknown tool: {toolName}");
                }
                break;
                
            default:
                throw new NotImplementedException($"Method {method} not implemented");
        }
        
        var response = new
        {
            jsonrpc = "2.0",
            id = id?.ToString(),
            result
        };
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
    catch (Exception ex)
    {
        var errorResponse = new
        {
            jsonrpc = "2.0",
            id = (string?)null,
            error = new
            {
                code = -32603,
                message = ex.Message
            }
        };
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
});

// Add a health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

// Add an endpoint to show available tools
app.MapGet("/tools", (IMcpAgentServerExtended agentServer) =>
{
    var tools = agentServer.AvailableAgents.Keys ?? Array.Empty<string>();
    return Results.Ok(new { tools = tools.ToArray() });
});

var port = args.Length > 0 && args[0] == "--port" && args.Length > 1 ? int.Parse(args[1]) : 8006;

// Check if demo mode is requested
if (args.Contains("--demo-injection"))
{
    Console.WriteLine("🧪 Running MCP Client Injection Demo...");
    Console.WriteLine();
    await McpAgent.Server.Services.McpInjectionDemo.RunDemoAsync();
    return;
}

app.Urls.Add($"http://localhost:{port}");

Console.WriteLine($"🚀 MCP Agent Server starting on port {port}");
Console.WriteLine($"📡 MCP endpoint: http://localhost:{port}/mcp");
Console.WriteLine($"❤️  Health check: http://localhost:{port}/health");
Console.WriteLine($"🔧 Available tools: http://localhost:{port}/tools");
Console.WriteLine();
Console.WriteLine("Available agent tools:");
Console.WriteLine("  - travel_agent: Book travel with price confirmation");
Console.WriteLine("  - research_agent: Research topics with AI summaries");
Console.WriteLine();

await app.RunAsync();
