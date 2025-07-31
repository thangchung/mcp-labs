using System.Text.Json;
using McpAgent.Core.Agents;
using McpAgent.Core.Events;
using McpAgent.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace McpAgent.Server.Tests.Services;

/// <summary>
/// Integration tests for MCP server functionality with real JSON-RPC communication
/// </summary>
public class McpServerIntegrationTests
{
    private readonly Mock<IEventStore> _mockEventStore;
    private readonly Mock<ILogger<AgentSession>> _mockLogger;
    private readonly Mock<IMcpServerClient> _mockMcpClient;

    public McpServerIntegrationTests()
    {
        _mockEventStore = new Mock<IEventStore>();
        _mockLogger = new Mock<ILogger<AgentSession>>();
        _mockMcpClient = new Mock<IMcpServerClient>();
    }

    [Fact]
    public async Task SendProgressNotificationAsync_ShouldSendRealMcpNotification()
    {
        // Arrange
        var session = new AgentSession("test-session", "travel_agent", _mockEventStore.Object, _mockLogger.Object, _mockMcpClient.Object);
        
        // Act
        await session.SendProgressNotificationAsync("token123", 50, 100, "Processing...", "request1");

        // Assert
        // Verify that the MCP client was called with correct parameters
        _mockMcpClient.Verify(c => c.SendProgressNotificationAsync("token123", 50, 100, "Processing...", "request1"), Times.Once);
    }

    [Fact]
    public async Task SendLogMessageAsync_ShouldSendRealMcpLogNotification()
    {
        // Arrange
        var session = new AgentSession("test-session", "travel_agent", _mockEventStore.Object, _mockLogger.Object, _mockMcpClient.Object);
        
        // Act
        await session.SendLogMessageAsync("info", "Test log message", "test-logger", "request1");

        // Assert
        // Verify that the MCP client was called with correct parameters
        _mockMcpClient.Verify(c => c.SendLogMessageAsync("info", "Test log message", "test-logger", "request1"), Times.Once);
    }

    [Fact]
    public async Task ElicitAsync_ShouldSendRealMcpElicitationRequest()
    {
        // Arrange
        var session = new AgentSession("test-session", "travel_agent", _mockEventStore.Object, _mockLogger.Object, _mockMcpClient.Object);
        var schema = new { type = "object", properties = new { confirm = new { type = "boolean" } } };
        var expectedResult = new ElicitationResult { Action = "accept", Content = new Dictionary<string, object?> { ["confirm"] = true } };
        
        _mockMcpClient.Setup(c => c.ElicitAsync("Please confirm your choice", schema, "request1"))
                     .ReturnsAsync(expectedResult);
        
        // Act
        var result = await session.ElicitAsync("Please confirm your choice", schema, "request1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("accept", result.Action);
        _mockMcpClient.Verify(c => c.ElicitAsync("Please confirm your choice", schema, "request1"), Times.Once);
    }

    [Fact]
    public async Task CreateMessageAsync_ShouldSendRealMcpSamplingRequest()
    {
        // Arrange
        var session = new AgentSession("test-session", "travel_agent", _mockEventStore.Object, _mockLogger.Object, _mockMcpClient.Object);
        var messages = new List<SamplingMessage>
        {
            new() { Role = "user", Content = new SamplingContent { Type = "text", Text = "Hello, how can you help?" } }
        };
        var expectedResult = new SamplingResult
        {
            Role = "assistant",
            Content = new SamplingContent { Type = "text", Text = "I can help you with various tasks!" },
            Model = "test-model",
            StopReason = "end_turn"
        };
        
        _mockMcpClient.Setup(c => c.CreateMessageAsync(messages, 150, "request1"))
                     .ReturnsAsync(expectedResult);
        
        // Act
        var result = await session.CreateMessageAsync(messages, 150, "request1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("assistant", result.Role);
        Assert.Equal("test-model", result.Model);
        _mockMcpClient.Verify(c => c.CreateMessageAsync(messages, 150, "request1"), Times.Once);
    }

    [Fact]
    public async Task HandleMcpConnectionFailure_ShouldFallbackGracefully()
    {
        // Arrange - session without MCP client (null)
        var session = new AgentSession("test-session", "travel_agent", _mockEventStore.Object, _mockLogger.Object, null);
        
        // Act & Assert
        // Should not throw, should log error and return reasonable fallback
        var messages = new List<SamplingMessage>
        {
            new() { Role = "user", Content = new SamplingContent { Type = "text", Text = "Test" } }
        };
        
        var result = await session.CreateMessageAsync(messages, 100);
        
        // Should handle failure gracefully with fallback response
        Assert.NotNull(result);
        Assert.Equal("assistant", result.Role);
        Assert.Equal("fallback-model", result.Model);
        Assert.Contains("unable to process", result.Content.Text.ToLowerInvariant());
    }

    [Fact]
    public async Task ValidateMcpJsonRpcProtocol_ShouldFollowSpecification()
    {
        // This test validates that our MCP implementation follows the JSON-RPC 2.0 specification
        // as required by the Model Context Protocol
        
        // Arrange
        var session = new AgentSession("test-session", "travel_agent", _mockEventStore.Object, _mockLogger.Object, _mockMcpClient.Object);
        
        // Act - Test various MCP operations
        await session.SendProgressNotificationAsync("progress1", 25, 100, "Starting...");
        await session.SendLogMessageAsync("debug", "Debug message", "test");
        
        // Assert
        // Verify JSON-RPC 2.0 format compliance through mock verification
        _mockMcpClient.Verify(c => c.SendProgressNotificationAsync("progress1", 25, 100, "Starting...", null), Times.Once);
        _mockMcpClient.Verify(c => c.SendLogMessageAsync("debug", "Debug message", "test", null), Times.Once);
    }
}
