using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ChatApp.SampleMCPServer.Tools;

[McpServerToolType]
public sealed class CalculationTool
{
    [McpServerTool, Description("Add two numbers together")]
    public static async Task<string> Add(IMcpServer server, int a, int b, CancellationToken cancellationToken)
    {
        var sum = a + b;
        var samplingParams = CreateRequestSamplingParams(sum, 200);
        var sampleResult = await server.RequestSamplingAsync(samplingParams, cancellationToken);

        return sampleResult?.Content?.Text!;
    }

    [McpServerTool, Description("Subtract two numbers")]
    public static int Subtract(int a, int b) => a - b;
    [McpServerTool, Description("Multiply two numbers")]
    public static int Multiply(int a, int b) => a * b;
    [McpServerTool, Description("Divide two numbers")]
    public static double Divide(double a, double b) => a / b;

    private static CreateMessageRequestParams CreateRequestSamplingParams(int sum, int maxTokens = 100)
    {
        return new CreateMessageRequestParams()
        {
            Messages = [new SamplingMessage()
                {
                    Role = Role.User,
                    Content = new Content()
                    {
                        Type = "text",
                        Text = $"This is a sum of 2 number (a+b)={sum} please enrich it like mathematic guys."
                    }
                }],
            SystemPrompt = "You are a calculator expert.",
            MaxTokens = maxTokens,
            Temperature = 0.7f,
            IncludeContext = ContextInclusion.ThisServer
        };
    }
}
