using McpAgent.Core.Agents;
using Moq;

namespace McpAgent.Core.Tests.Agents;

public class ResearchAgentTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldRequestSamplingForSummary()
    {
        // Arrange
        var mockSession = new Mock<IAgentSession>();
        var agent = new ResearchAgent();
        var context = new AgentContext
        {
            RequestId = "test-request",
            ToolName = "research_agent",
            Arguments = new Dictionary<string, object?> { ["topic"] = "Machine Learning" },
            Session = mockSession.Object
        };

        var samplingResult = new SamplingResult
        {
            Role = "assistant",
            Content = new SamplingContent { Type = "text", Text = "Machine learning is a subset of AI..." },
            Model = "test-model",
            StopReason = "endTurn"
        };

        mockSession.Setup(s => s.CreateMessageAsync(It.IsAny<IEnumerable<SamplingMessage>>(), It.IsAny<int?>(), It.IsAny<string?>()))
                  .ReturnsAsync(samplingResult);

        // Act
        var result = await agent.ExecuteAsync(context);

        // Assert
        mockSession.Verify(s => s.CreateMessageAsync(
            It.IsAny<IEnumerable<SamplingMessage>>(), It.IsAny<int?>(), It.IsAny<string?>()), 
            Times.Once);
        Assert.NotNull(result);
        Assert.Contains("research completed", result.ToLowerInvariant());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSendProgressNotifications()
    {
        // Arrange
        var mockSession = new Mock<IAgentSession>();
        var agent = new ResearchAgent();
        var context = new AgentContext
        {
            RequestId = "test-request",
            ToolName = "research_agent",
            Arguments = new Dictionary<string, object?> { ["topic"] = "AI Ethics" },
            Session = mockSession.Object
        };

        mockSession.Setup(s => s.CreateMessageAsync(It.IsAny<IEnumerable<SamplingMessage>>(), It.IsAny<int?>(), It.IsAny<string?>()))
                  .ReturnsAsync(new SamplingResult
                  {
                      Role = "assistant",
                      Content = new SamplingContent { Type = "text", Text = "Test summary" },
                      Model = "test-model",
                      StopReason = "endTurn"
                  });

        // Act
        var result = await agent.ExecuteAsync(context);

        // Assert
        mockSession.Verify(s => s.SendProgressNotificationAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>()), 
            Times.AtLeast(1));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutTopic_ShouldThrowArgumentException()
    {
        // Arrange
        var mockSession = new Mock<IAgentSession>();
        var agent = new ResearchAgent();
        var context = new AgentContext
        {
            RequestId = "test-request",
            ToolName = "research_agent",
            Arguments = new Dictionary<string, object?>(),
            Session = mockSession.Object
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => agent.ExecuteAsync(context));
    }
}
