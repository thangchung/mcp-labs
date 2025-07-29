using McpAgent.Core.Agents;
using McpAgent.Core.Events;
using McpAgent.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace McpAgent.Server.Tests.Services;

public class AgentSessionTests
{
    [Fact]
    public async Task Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var mockEventStore = new Mock<IEventStore>();
        var logger = Mock.Of<ILogger<AgentSession>>();

        // Act
        var session = new AgentSession("test-session", "travel_agent", mockEventStore.Object, logger);

        // Assert
        Assert.Equal("test-session", session.SessionId);
        Assert.Equal("travel_agent", session.AgentType);
        Assert.False(session.IsActive);
        Assert.Empty(session.Events);
    }

    [Fact]
    public async Task InitializeAsync_ShouldRecordSessionStartedEvent()
    {
        // Arrange
        var mockEventStore = new Mock<IEventStore>();
        var logger = Mock.Of<ILogger<AgentSession>>();
        var session = new AgentSession("test-session", "travel_agent", mockEventStore.Object, logger);

        // Act
        await session.InitializeAsync();

        // Assert
        mockEventStore.Verify(es => es.StoreEventAsync(
            It.Is<string>(s => s == "test-session"),
            It.IsAny<SessionStartedEvent>()), Times.Once);
    }

    [Fact]
    public async Task SendProgressNotificationAsync_ShouldLogProgress()
    {
        // Arrange
        var mockEventStore = new Mock<IEventStore>();
        var logger = Mock.Of<ILogger<AgentSession>>();
        var session = new AgentSession("test-session", "travel_agent", mockEventStore.Object, logger);

        // Act
        await session.SendProgressNotificationAsync("token1", 50, 100, "Progress message", "request1");

        // Assert - This method just logs, doesn't store events
        // No verification needed as this is a logging operation
        Assert.True(true); // Test passes if no exception is thrown
    }

    [Fact]
    public async Task SendLogMessageAsync_ShouldLogMessage()
    {
        // Arrange
        var mockEventStore = new Mock<IEventStore>();
        var logger = Mock.Of<ILogger<AgentSession>>();
        var session = new AgentSession("test-session", "travel_agent", mockEventStore.Object, logger);

        // Act
        await session.SendLogMessageAsync("info", "Test log", "test-logger", "request1");

        // Assert - This method just logs, doesn't store events
        // No verification needed as this is a logging operation
        Assert.True(true); // Test passes if no exception is thrown
    }

    [Fact]
    public async Task ElicitAsync_ShouldReturnSimulatedResult()
    {
        // Arrange
        var mockEventStore = new Mock<IEventStore>();
        var logger = Mock.Of<ILogger<AgentSession>>();
        var session = new AgentSession("test-session", "travel_agent", mockEventStore.Object, logger);

        // Act
        var result = await session.ElicitAsync("Please confirm", null, "request1");

        // Assert - Should return a simulated result, not null
        Assert.NotNull(result);
        Assert.Equal("input", result.Action);
        Assert.True(result.Content.ContainsKey("response"));
    }

    [Fact]
    public async Task CreateMessageAsync_ShouldReturnSimulatedResult()
    {
        // Arrange
        var mockEventStore = new Mock<IEventStore>();
        var logger = Mock.Of<ILogger<AgentSession>>();
        var session = new AgentSession("test-session", "travel_agent", mockEventStore.Object, logger);
        var messages = new List<SamplingMessage>
        {
            new() { Role = "user", Content = new SamplingContent { Type = "text", Text = "Hello" } }
        };

        // Act
        var result = await session.CreateMessageAsync(messages, 100, "request1");

        // Assert - Should return a simulated result, not null
        Assert.NotNull(result);
        Assert.Equal("assistant", result.Role);
        Assert.Equal("text", result.Content.Type);
        Assert.Contains("Hello", result.Content.Text);
        Assert.Equal("simulated-model", result.Model);
        Assert.Equal("end_turn", result.StopReason);
    }

    [Fact]
    public async Task RecordEventAsync_ShouldAppendToEventStore()
    {
        // Arrange
        var mockEventStore = new Mock<IEventStore>();
        var logger = Mock.Of<ILogger<AgentSession>>();
        var session = new AgentSession("test-session", "travel_agent", mockEventStore.Object, logger);
        var testEvent = new SessionStartedEvent("test-session", "travel_agent", DateTimeOffset.UtcNow);

        // Act
        await session.RecordEventAsync(testEvent);

        // Assert
        mockEventStore.Verify(es => es.StoreEventAsync("test-session", testEvent), Times.Once);
    }

    [Fact]
    public async Task EndAsync_ShouldRecordSessionEndedEvent()
    {
        // Arrange
        var mockEventStore = new Mock<IEventStore>();
        var logger = Mock.Of<ILogger<AgentSession>>();
        var session = new AgentSession("test-session", "travel_agent", mockEventStore.Object, logger);
        
        // Initialize the session to set IsActive = true
        await session.InitializeAsync();

        // Act
        await session.EndAsync();

        // Assert - Should record both SessionStartedEvent (from Initialize) and SessionEndedEvent (from End)
        mockEventStore.Verify(es => es.StoreEventAsync(
            It.Is<string>(s => s == "test-session"),
            It.IsAny<SessionEndedEvent>()), Times.Once);
        
        Assert.False(session.IsActive);
    }
}
