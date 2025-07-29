using Microsoft.Extensions.Logging;

namespace McpAgent.Core.Agents;

/// <summary>
/// Research agent that performs research tasks with AI-assisted summaries via sampling.
/// Based on the Python implementation from the MCP agents sample.
/// </summary>
public class ResearchAgent : AgentBase
{
    private readonly ILogger<ResearchAgent>? _logger;

    public ResearchAgent(ILogger<ResearchAgent>? logger = null)
    {
        _logger = logger;
    }

    public override async Task<string> ExecuteAsync(AgentContext context)
    {
        ValidateArgument(context.Arguments, "topic");
        
        var topic = context.Arguments["topic"]?.ToString()!;
        _logger?.LogInformation("Starting research on topic: {Topic}", topic);

        // Define research steps
        var steps = new[]
        {
            "Collecting sources and references...",
            "Analyzing research papers...",
            "Gathering expert opinions...",
            "Synthesizing findings..."
        };

        // Send progress notifications for each step
        for (int i = 0; i < steps.Length; i++)
        {
            await context.Session.SendProgressNotificationAsync(
                progressToken: context.RequestId,
                progress: i * 25,
                total: 100,
                message: steps[i],
                relatedRequestId: context.RequestId);

            await context.Session.SendLogMessageAsync(
                level: "info",
                data: $"Processing step {i + 1}/{steps.Length} ({(i + 1) * 25}%)",
                logger: "research_agent",
                relatedRequestId: context.RequestId);

            await SimulateWork(2000); // Simulate research work
        }

        // Request AI assistance for summarizing research findings via sampling
        _logger?.LogInformation("Requesting AI summary for research on: {Topic}", topic);

        var samplingMessages = new[]
        {
            new SamplingMessage
            {
                Role = "user",
                Content = new SamplingContent
                {
                    Type = "text",
                    Text = $"Please summarize the key findings for research on: {topic}. " +
                           "Focus on recent developments, practical applications, and future prospects."
                }
            }
        };

        var samplingResult = await context.Session.CreateMessageAsync(
            messages: samplingMessages,
            maxTokens: 200,
            relatedRequestId: context.RequestId);

        string summary = "Research completed without AI summary.";
        if (samplingResult?.Content?.Text != null)
        {
            summary = samplingResult.Content.Text;
            _logger?.LogInformation("Received AI summary: {Summary}", summary);
        }

        // Complete the research
        await context.Session.SendProgressNotificationAsync(
            progressToken: context.RequestId,
            progress: 100,
            total: 100,
            message: "Research completed!",
            relatedRequestId: context.RequestId);

        var result = $"Research completed on '{topic}'.\n\nKey Findings Summary:\n{summary}\n\n" +
                    "This research summary was generated using AI assistance to provide comprehensive insights.";

        await context.Session.SendLogMessageAsync(
            level: "info",
            data: $"Research task completed for topic: {topic}",
            logger: "research_agent",
            relatedRequestId: context.RequestId);

        return result;
    }
}
