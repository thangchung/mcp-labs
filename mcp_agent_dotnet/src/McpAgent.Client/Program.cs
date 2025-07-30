using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.CommandLine;
using System.Text.Json;

namespace McpAgent.Client;

public class Program
{
    private static readonly string DefaultServerUrl = "http://127.0.0.1:8006/mcp";
    
    public static async Task<int> Main(string[] args)
    {
        // Create command line interface
        var serverUrlOption = new Option<string>(
            aliases: ["--url", "-u"],
            description: "MCP server URL",
            getDefaultValue: () => DefaultServerUrl);

        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Enable verbose logging");

        var clearSessionOption = new Option<bool>(
            aliases: ["--clear-session", "-c"],
            description: "Clear existing session before starting");

        var basicModeOption = new Option<bool>(
            aliases: ["--basic", "-b"],
            description: "Use basic client instead of enhanced client");

        var rootCommand = new RootCommand("MCP Agent Client - Interactive client for MCP agent server")
        {
            serverUrlOption,
            verboseOption,
            clearSessionOption,
            basicModeOption
        };

        rootCommand.SetHandler(async (serverUrl, verbose, clearSession, basicMode) =>
        {
            // Configure logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole()
                       .SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
            });

            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                logger.LogInformation("🤖 Starting MCP Agent Client");
                logger.LogInformation("🔗 Connecting to: {ServerUrl}", serverUrl);

                if (clearSession)
                {
                    var sessionManager = new SessionManager(loggerFactory);
                    await sessionManager.ClearSessionAsync();
                    logger.LogInformation("🗑️ Existing session cleared");
                }

                if (basicMode)
                {
                    logger.LogInformation("� Using basic client mode");
                    var basicClient = new McpAgentClient(serverUrl, loggerFactory);
                    await basicClient.RunAsync();
                }
                else
                {
                    logger.LogInformation("🚀 Using enhanced client mode");
                    var enhancedClient = new EnhancedMcpClientFixed(serverUrl, loggerFactory);
                    await enhancedClient.RunAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 Fatal error occurred");
                Environment.Exit(1);
            }

            logger.LogInformation("👋 Client shutting down");
        }, serverUrlOption, verboseOption, clearSessionOption, basicModeOption);

        return await rootCommand.InvokeAsync(args);
    }
}
