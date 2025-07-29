using McpAgent.Core.Agents;
using Moq;

namespace McpAgent.Core.Tests.Agents;

public class TravelAgentTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSendProgressNotifications()
    {
        // Arrange
        var mockSession = new Mock<IAgentSession>();
        var agent = new TravelAgent();
        var context = new AgentContext
        {
            RequestId = "test-request",
            ToolName = "travel_agent",
            Arguments = new Dictionary<string, object?> { ["destination"] = "Paris" },
            Session = mockSession.Object
        };

        mockSession.Setup(s => s.ElicitAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<string?>()))
                  .ReturnsAsync(new ElicitationResult { Action = "accept", Content = new Dictionary<string, object?> { ["confirm"] = true } });

        // Act
        var result = await agent.ExecuteAsync(context);

        // Assert
        mockSession.Verify(s => s.SendProgressNotificationAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>()), 
            Times.AtLeast(1));
        Assert.NotNull(result);
        Assert.Contains("booking confirmed", result.ToLowerInvariant());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDeclines_ShouldCancelBooking()
    {
        // Arrange
        var mockSession = new Mock<IAgentSession>();
        var agent = new TravelAgent();
        var context = new AgentContext
        {
            RequestId = "test-request",
            ToolName = "travel_agent",
            Arguments = new Dictionary<string, object?> { ["destination"] = "Tokyo" },
            Session = mockSession.Object
        };

        mockSession.Setup(s => s.ElicitAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<string?>()))
                  .ReturnsAsync(new ElicitationResult { Action = "decline", Content = new Dictionary<string, object?> { ["confirm"] = false } });

        // Act
        var result = await agent.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("booking cancelled", result.ToLowerInvariant());
    }

    [Fact]
    public async Task ExecuteAsync_WithoutDestination_ShouldThrowArgumentException()
    {
        // Arrange
        var mockSession = new Mock<IAgentSession>();
        var agent = new TravelAgent();
        var context = new AgentContext
        {
            RequestId = "test-request",
            ToolName = "travel_agent",
            Arguments = new Dictionary<string, object?>(),
            Session = mockSession.Object
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => agent.ExecuteAsync(context));
    }
}
