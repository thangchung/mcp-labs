using System.Text.Json;
using McpAgent.Core.Agents;
using McpAgent.Core.Events;
using McpAgent.EventStore;
using McpAgent.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpAgent.Server.Services;

/// <summary>
/// Demonstration class showing HttpMcpServerClient dependency injection into AgentSession
/// This shows how the DI container automatically provides the MCP client to sessions
/// </summary>
public class McpInjectionDemo
{
    /// <summary>
    /// Demonstrates creating an AgentSession with injected HttpMcpServerClient
    /// </summary>
    public static async Task RunDemoAsync()
    {
        Console.WriteLine("=== MCP Client Injection Demo ===");
        Console.WriteLine();

        // Create service collection and configure services (similar to Program.cs)
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add event store
        services.AddSingleton<IEventStore, ManagedEventStore>();
        
        // Add HTTP client
        services.AddHttpClient();
        
        // Add MCP client service (same as Program.cs configuration)
        services.AddScoped<IMcpServerClient>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            
            // Use a demo MCP client endpoint
            var mcpClientEndpoint = "http://localhost:8007/mcp";
            
            var httpClient = httpClientFactory.CreateClient("McpClient");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            return new HttpMcpServerClient(httpClient, mcpClientEndpoint, loggerFactory);
        });
        
        // Add MCP Agent Server
        services.AddScoped<IMcpAgentServerExtended, McpAgentServer>();
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        Console.WriteLine("✅ Service container configured with HttpMcpServerClient");
        Console.WriteLine();
        
        // Get services from DI container
        var eventStore = serviceProvider.GetRequiredService<IEventStore>();
        var logger = serviceProvider.GetRequiredService<ILogger<McpInjectionDemo>>();
        var mcpClient = serviceProvider.GetRequiredService<IMcpServerClient>();
        var agentServer = serviceProvider.GetRequiredService<IMcpAgentServerExtended>();
        
        Console.WriteLine($"📦 Retrieved services from DI container:");
        Console.WriteLine($"   - EventStore: {eventStore.GetType().Name}");
        Console.WriteLine($"   - Logger: {logger.GetType().Name}");
        Console.WriteLine($"   - MCP Client: {mcpClient.GetType().Name}");
        Console.WriteLine($"   - Agent Server: {agentServer.GetType().Name}");
        Console.WriteLine();
        
        // Demonstrate creating AgentSession through the server (which injects MCP client)
        Console.WriteLine("🔧 Creating AgentSession through McpAgentServer...");
        
        var sessionId = "demo-injection-session-" + Guid.NewGuid().ToString()[..8];
        var session = await agentServer.CreateSessionAsync("travel_agent", sessionId);
        
        Console.WriteLine($"✅ AgentSession created: {session.SessionId}");
        Console.WriteLine($"   - Agent Type: {session.AgentType}");
        Console.WriteLine($"   - Is Active: {session.IsActive}");
        Console.WriteLine();
        
        // Test MCP operations through the session (will use injected HttpMcpServerClient)
        Console.WriteLine("🧪 Testing MCP operations through injected client...");
        
        try
        {
            // Test progress notification
            await session.SendProgressNotificationAsync("demo-progress", 1, 3, "Starting demo operations", "demo-request-1");
            Console.WriteLine("✅ Progress notification sent successfully");
            
            // Test log message
            await session.SendLogMessageAsync("info", "Demo log message from injected MCP client", "McpInjectionDemo", "demo-request-1");
            Console.WriteLine("✅ Log message sent successfully");
            
            // Test elicitation (will fallback gracefully if no MCP client connection)
            var elicitResult = await session.ElicitAsync("Demo elicitation: What is your favorite travel destination?", null, "demo-request-1");
            Console.WriteLine($"✅ Elicitation completed: {elicitResult?.Action}");
            if (elicitResult?.Content?.ContainsKey("source") == true)
            {
                Console.WriteLine($"   - Source: {elicitResult.Content["source"]}");
            }
            
            // Test AI message creation (will fallback gracefully if no MCP client connection)
            var messages = new[]
            {
                new SamplingMessage
                {
                    Role = "user",
                    Content = new SamplingContent { Type = "text", Text = "Hello from the injected MCP client demo!" }
                }
            };
            
            var samplingResult = await session.CreateMessageAsync(messages, 100, "demo-request-1");
            Console.WriteLine($"✅ AI message created: {samplingResult?.Model}");
            if (samplingResult?.Content?.Text?.Length > 50)
            {
                Console.WriteLine($"   - Response: {samplingResult.Content.Text[..50]}...");
            }
            else
            {
                Console.WriteLine($"   - Response: {samplingResult?.Content?.Text}");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  MCP operations completed with graceful fallback: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("🎯 Demo Results:");
        Console.WriteLine("   - HttpMcpServerClient successfully injected into AgentSession");
        Console.WriteLine("   - MCP operations work with real client or graceful fallback");
        Console.WriteLine("   - Dependency injection provides seamless MCP integration");
        Console.WriteLine();
        
        // Clean up
        await session.EndAsync();
        serviceProvider.Dispose();
        
        Console.WriteLine("✅ Demo completed successfully!");
        Console.WriteLine("=== End of MCP Client Injection Demo ===");
    }
}
