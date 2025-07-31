using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;

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

        var openAiModeOption = new Option<bool>(
            aliases: ["--openai", "-o"],
            description: "Use OpenAI-powered client");

        var openAiApiKeyOption = new Option<string>(
            aliases: ["--openai-key", "-ok"],
            description: "OpenAI API key (required for OpenAI mode)");

        var openAiConfigOption = new Option<string>(
            aliases: ["--openai-config", "-oc"],
            description: "OpenAI configuration in format 'model:endpoint' (default: gpt-4o-mini:https://api.openai.com/v1)",
            getDefaultValue: () => "gpt-4o-mini:https://api.openai.com/v1");

        var rootCommand = new RootCommand("MCP Agent Client - Interactive client for MCP agent server")
        {
            serverUrlOption,
            verboseOption,
            clearSessionOption,
            basicModeOption,
            geminiModeOption,
            geminiEnhancedModeOption,
            geminiApiKeyOption,
            openAiModeOption,
            openAiApiKeyOption,
            openAiConfigOption
        };

        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            // Extract all parameter values from context
            var serverUrl = context.ParseResult.GetValueForOption(serverUrlOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var clearSession = context.ParseResult.GetValueForOption(clearSessionOption);
            var basicMode = context.ParseResult.GetValueForOption(basicModeOption);
            var geminiMode = context.ParseResult.GetValueForOption(geminiModeOption);
            var geminiEnhancedMode = context.ParseResult.GetValueForOption(geminiEnhancedModeOption);
            var geminiApiKey = context.ParseResult.GetValueForOption(geminiApiKeyOption);
            var openAiMode = context.ParseResult.GetValueForOption(openAiModeOption);
            var openAiApiKey = context.ParseResult.GetValueForOption(openAiApiKeyOption);
            var openAiConfig = context.ParseResult.GetValueForOption(openAiConfigOption);

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

                if (openAiMode)
                {
                    if (string.IsNullOrWhiteSpace(openAiApiKey))
                    {
                        logger.LogError("❌ OpenAI API key is required when using --openai mode. Use --openai-key option.");
                        Environment.Exit(1);
                        return;
                    }

                    // Parse OpenAI configuration
                    var configParts = openAiConfig.Split(':');
                    var model = configParts.Length > 0 ? configParts[0] : "gpt-4o-mini";
                    var endpoint = configParts.Length > 1 ? string.Join(":", configParts.Skip(1)) : "https://api.openai.com/v1";

                    logger.LogInformation("🧠 Using OpenAI-powered client with model: {Model}, endpoint: {Endpoint}", model, endpoint);
                    var openAiClient = new OpenAiMcpClientEnhanced(serverUrl, loggerFactory, openAiApiKey, model, endpoint);
                    await openAiClient.RunAsync();
                }
                else if (geminiEnhancedMode)
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
        });

        return await rootCommand.InvokeAsync(args);
    }
}
