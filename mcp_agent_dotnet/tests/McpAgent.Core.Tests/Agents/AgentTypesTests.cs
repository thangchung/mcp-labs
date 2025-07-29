using McpAgent.Core.Agents;
using Moq;

namespace McpAgent.Core.Tests.Agents;

public class AgentTypesTests
{
    [Fact]
    public void AgentContext_ShouldBeCreatedWithRequiredProperties()
    {
        // Arrange & Act
        var session = new Mock<IAgentSession>();
        var context = new AgentContext
        {
            RequestId = "test-request-id",
            ToolName = "test-tool",
            Arguments = new Dictionary<string, object?> { ["param"] = "value" },
            Session = session.Object
        };

        // Assert
        Assert.Equal("test-request-id", context.RequestId);
        Assert.Equal("test-tool", context.ToolName);
        Assert.Equal("value", context.Arguments["param"]);
        Assert.NotNull(context.Session);
    }

    [Fact]
    public void ElicitationResult_ShouldBeCreatedWithRequiredProperties()
    {
        // Arrange & Act
        var result = new ElicitationResult
        {
            Action = "accept",
            Content = new Dictionary<string, object?> { ["confirm"] = true }
        };

        // Assert
        Assert.Equal("accept", result.Action);
        Assert.True((bool)result.Content["confirm"]!);
    }

    [Fact]
    public void SamplingResult_ShouldBeCreatedWithRequiredProperties()
    {
        // Arrange & Act
        var content = new SamplingContent
        {
            Type = "text",
            Text = "Test response"
        };

        var result = new SamplingResult
        {
            Role = "assistant",
            Content = content,
            Model = "test-model",
            StopReason = "endTurn"
        };

        // Assert
        Assert.Equal("assistant", result.Role);
        Assert.Equal("text", result.Content.Type);
        Assert.Equal("Test response", result.Content.Text);
        Assert.Equal("test-model", result.Model);
        Assert.Equal("endTurn", result.StopReason);
    }

    [Fact]
    public void SamplingMessage_ShouldBeCreatedWithRequiredProperties()
    {
        // Arrange & Act
        var content = new SamplingContent
        {
            Type = "text",
            Text = "Test message"
        };

        var message = new SamplingMessage
        {
            Role = "user",
            Content = content
        };

        // Assert
        Assert.Equal("user", message.Role);
        Assert.Equal("text", message.Content.Type);
        Assert.Equal("Test message", message.Content.Text);
    }

    [Fact]
    public void PriceConfirmationSchema_ShouldHaveCorrectStructure()
    {
        // Arrange & Act
        var schema = new PriceConfirmationSchema();

        // Assert
        Assert.Equal("object", schema.Type);
        Assert.Contains("confirm", schema.Properties.Keys);
        Assert.Contains("notes", schema.Properties.Keys);
        Assert.Single(schema.Required);
        Assert.Equal("confirm", schema.Required[0]);
    }
}
