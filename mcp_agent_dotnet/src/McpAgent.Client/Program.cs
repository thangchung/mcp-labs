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

        var geminiModeOption = new Option<bool>(
            aliases: ["--gemini", "-g"],
            description: "Use Google Gemini AI-powered client");

        var geminiEnhancedModeOption = new Option<bool>(
            aliases: ["--gemini-enhanced", "-ge"],
            description: "Use enhanced Google Gemini AI client with session management and notifications");

        var geminiApiKeyOption = new Option<string>(
            aliases: ["--gemini-key", "-k"],
            description: "Google Gemini API key (required for Gemini modes)");

        var rootCommand = new RootCommand("MCP Agent Client - Interactive client for MCP agent server")
        {
            serverUrlOption,
            verboseOption,
            clearSessionOption,
            basicModeOption,
            geminiModeOption,
            geminiEnhancedModeOption,
            geminiApiKeyOption
        };

        rootCommand.SetHandler(async (serverUrl, verbose, clearSession, basicMode, geminiMode, geminiEnhancedMode, geminiApiKey) =>
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

                if (geminiEnhancedMode)
                {
                    if (string.IsNullOrWhiteSpace(geminiApiKey))
                    {
                        logger.LogError("❌ Gemini API key is required when using --gemini-enhanced mode. Use --gemini-key option.");
                        Environment.Exit(1);
                        return;
                    }

                    logger.LogInformation("🚀 Using Enhanced Google Gemini AI client with session management");
                    var geminiEnhancedClient = new GeminiMcpClientEnhanced(serverUrl, loggerFactory, geminiApiKey);
                    await geminiEnhancedClient.RunAsync();
                }
                else if (geminiMode)
                {
                    if (string.IsNullOrWhiteSpace(geminiApiKey))
                    {
                        logger.LogError("❌ Gemini API key is required when using --gemini mode. Use --gemini-key option.");
                        Environment.Exit(1);
                        return;
                    }

                    logger.LogInformation("🧠 Using Google Gemini AI-powered client");
                    var geminiClient = new GeminiMcpClient(serverUrl, loggerFactory, geminiApiKey);
                    await geminiClient.RunAsync();
                }
                else if (basicMode)
                {
                    logger.LogInformation("🔧 Using basic client mode");
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
        }, serverUrlOption, verboseOption, clearSessionOption, basicModeOption, geminiModeOption, geminiEnhancedModeOption, geminiApiKeyOption);

        return await rootCommand.InvokeAsync(args);
    }
}
