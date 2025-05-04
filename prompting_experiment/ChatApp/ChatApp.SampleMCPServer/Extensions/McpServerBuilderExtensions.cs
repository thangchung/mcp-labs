using ChatApp.SampleMCPServer.Tools;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System.Reflection;

namespace MCPServer;

public static class McpServerBuilderExtensions
{
    public static IMcpServerBuilder WithSKPlugins(this IMcpServerBuilder builder)
    {
        // If no plugins are provided explicitly, add all functions from the kernel plugins registered in DI container as tools
        builder.Services.AddSingleton<IEnumerable<McpServerTool>>(services =>
        {
            IEnumerable<KernelPlugin> plugins = services.GetServices<KernelPlugin>();
            Kernel kernel = services.GetRequiredService<Kernel>();

            List<McpServerTool> tools = [];

            foreach (var plugin in plugins)
            {
                foreach (var function in plugin)
                {
                    tools.Add(McpServerTool.Create(function.AsAIFunction(kernel)));
                }
            }

            foreach (var toolType in new[] { typeof(TimeTool), typeof(CalculationTool) })
            {
                if (toolType is not null)
                {
                    foreach (var toolMethod in toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        if (toolMethod.GetCustomAttribute<McpServerToolAttribute>() is not null)
                        {
                            tools.Add(toolMethod.IsStatic 
                                ? McpServerTool.Create(toolMethod, options: new() { Services = services }) 
                                : McpServerTool.Create(toolMethod, toolType, new() { Services = services }));
                        }
                    }
                }
            }

            return tools;
        });

        return builder;
    }
}