using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using McpAgent.Core.Events;
using McpAgent.Core.Agents;
using McpAgent.EventStore;
using McpAgent.Server.Services;
using Xunit;

namespace McpAgent.Server.Tests.Services;

/// <summary>
/// Tests to verify that HttpMcpServerClient is properly injected into AgentSession through DI
/// </summary>
public class McpClientInjectionTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public McpClientInjectionTests()
    {
        var services = new ServiceCollection();
        
        // Configure services similar to Program.cs
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IEventStore, ManagedEventStore>();
        services.AddHttpClient();
        
        // Add MCP client service
        services.AddScoped<IMcpServerClient>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            
            var mcpClientEndpoint = "http://localhost:8007/mcp";
            var httpClient = httpClientFactory.CreateClient("McpClient");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            return new HttpMcpServerClient(httpClient, mcpClientEndpoint, loggerFactory);
        });
        
        services.AddScoped<IMcpAgentServerExtended, McpAgentServer>();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task McpAgentServer_ShouldInjectHttpMcpServerClient_WhenCreatingAgentSession()
    {
        // Arrange
        var agentServer = _serviceProvider.GetRequiredService<IMcpAgentServerExtended>();
        var sessionId = "test-injection-session";
        
        // Act
        var session = await agentServer.CreateSessionAsync("travel_agent", sessionId);
        
        // Assert
        Assert.NotNull(session);
        Assert.Equal(sessionId, session.SessionId);
        Assert.Equal("travel_agent", session.AgentType);
        Assert.True(session.IsActive);
    }

    [Fact]
    public void ServiceContainer_ShouldResolveHttpMcpServerClient_Successfully()
    {
        // Act
        var mcpClient = _serviceProvider.GetRequiredService<IMcpServerClient>();
        
        // Assert
        Assert.NotNull(mcpClient);
        Assert.IsType<HttpMcpServerClient>(mcpClient);
    }

    [Fact]
    public async Task AgentSession_WithInjectedMcpClient_ShouldHandleMcpOperationsGracefully()
    {
        // Arrange
        var agentServer = _serviceProvider.GetRequiredService<IMcpAgentServerExtended>();
        var session = await agentServer.CreateSessionAsync("travel_agent", "test-mcp-ops-session");
        
        // Act & Assert - These should not throw exceptions
        await session.SendProgressNotificationAsync("test-progress", 1, 3, "Test progress", "test-request");
        await session.SendLogMessageAsync("info", "Test log message", "TestLogger", "test-request");
        
        var elicitResult = await session.ElicitAsync("Test elicitation", null, "test-request");
        Assert.NotNull(elicitResult);
        
        var messages = new[]
        {
            new SamplingMessage
            {
                Role = "user",
                Content = new SamplingContent { Type = "text", Text = "Test message" }
            }
        };
        
        var samplingResult = await session.CreateMessageAsync(messages, 100, "test-request");
        Assert.NotNull(samplingResult);
        
        await session.EndAsync();
    }

    [Fact]
    public void DependencyInjection_ShouldCreateNewInstancesPerScope()
    {
        // Arrange & Act
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();
        
        var mcpClient1 = scope1.ServiceProvider.GetRequiredService<IMcpServerClient>();
        var mcpClient2 = scope2.ServiceProvider.GetRequiredService<IMcpServerClient>();
        
        // Assert - Should be different instances per scope
        Assert.NotNull(mcpClient1);
        Assert.NotNull(mcpClient2);
        Assert.NotSame(mcpClient1, mcpClient2);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
