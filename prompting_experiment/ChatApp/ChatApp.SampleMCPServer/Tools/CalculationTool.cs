using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ChatApp.SampleMCPServer.Tools;

[McpServerToolType]
public sealed class CalculationTool
{
    [McpServerTool, Description("Add two numbers together")]
    public static int Add(int a, int b) => a + b;
    [McpServerTool, Description("Subtract two numbers")]
    public static int Subtract(int a, int b) => a - b;
    [McpServerTool, Description("Multiply two numbers")]
    public static int Multiply(int a, int b) => a * b;
    [McpServerTool, Description("Divide two numbers")]
    public static double Divide(double a, double b) => a / b;
}
