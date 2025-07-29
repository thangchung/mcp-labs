using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpAgent.Client;

/// <summary>
/// Interactive MCP client that connects to the agent server and provides a command-line interface
/// for interacting with travel and research agents through the MCP protocol.
/// </summary>
public class McpAgentClient
{
    private readonly string _serverUrl;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpAgentClient> _logger;
    private readonly SessionManager _sessionManager;

    public McpAgentClient(string serverUrl, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new ArgumentException("Server URL cannot be null or empty", nameof(serverUrl));
        
        _serverUrl = serverUrl;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<McpAgentClient>();
        _sessionManager = new SessionManager(_loggerFactory);
    }

    /// <summary>
    /// Runs the interactive client session
    /// </summary>
    public async Task RunAsync()
    {
        try
        {
            await ConnectAndRunInteractively();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during client execution");
            throw;
        }
    }

    private async Task ConnectAndRunInteractively()
    {
        // Create HTTP client for direct JSON-RPC communication
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_serverUrl.Replace("/mcp", ""));
        
        // Instead of using the MCP SDK transport (which expects streaming), 
        // we'll use direct HTTP communication with our JSON-RPC endpoint
        var client = new DirectHttpMcpClient(httpClient, _serverUrl, _loggerFactory);
        
        // Initialize connection
        await client.InitializeAsync();
        _logger.LogInformation("✅ Connected to MCP server");

        try
        {
            // Check for existing session tokens
            var existingSession = await _sessionManager.LoadExistingSessionAsync();
            
            if (existingSession != null)
            {
                _logger.LogInformation("🔄 Found existing session, attempting to resume...");
                await ResumeExistingSession(client, existingSession);
            }
            else
            {
                _logger.LogInformation("🆕 Starting new interactive session");
                await StartNewInteractiveSession(client);
            }
        }
        finally
        {
            client.Dispose();
        }
    }

    private async Task ResumeExistingSession(DirectHttpMcpClient client, SessionInfo sessionInfo)
    {
        _logger.LogInformation("📋 Resuming session for tool: {ToolName}", sessionInfo.LastTool);
        
        try
        {
            // Try to resume the previous tool execution
            var result = await client.CallToolAsync(sessionInfo.LastTool, sessionInfo.LastArgs);
            
            _logger.LogInformation("✅ Session resumed successfully");
            DisplayToolResult(result);
            
            // Clear the session since it completed
            await _sessionManager.ClearSessionAsync();
            
            // Continue with interactive mode
            await RunInteractiveLoop(client);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resume session, starting fresh");
            await _sessionManager.ClearSessionAsync();
            await StartNewInteractiveSession(client);
        }
    }

    private async Task StartNewInteractiveSession(DirectHttpMcpClient client)
    {
        // List available tools
        var toolsResult = await client.ListToolsAsync();
        
        if (!toolsResult.TryGetProperty("tools", out var toolsArray) || toolsArray.GetArrayLength() == 0)
        {
            _logger.LogWarning("No tools available on the server");
            return;
        }

        _logger.LogInformation("📚 Available tools:");
        foreach (var tool in toolsArray.EnumerateArray())
        {
            if (tool.TryGetProperty("name", out var nameElement) && tool.TryGetProperty("description", out var descElement))
            {
                var name = nameElement.GetString();
                var description = descElement.GetString();
                _logger.LogInformation("  • {Name}: {Description}", name, description ?? "No description");
            }
        }

        await RunInteractiveLoop(client);
    }

    private async Task RunInteractiveLoop(DirectHttpMcpClient client)
    {
        _logger.LogInformation("\n🎮 Interactive mode started. Type 'exit' to quit, 'help' for commands.");

        while (true)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                ShowHelp();
                continue;
            }

            if (input.Equals("tools", StringComparison.OrdinalIgnoreCase))
            {
                await ListTools(client);
                continue;
            }

            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                await _sessionManager.ClearSessionAsync();
                _logger.LogInformation("🗑️ Session cleared");
                continue;
            }

            // Handle tool execution commands
            await ProcessToolCommand(client, input);
        }
    }

    private void ShowHelp()
    {
        Console.WriteLine("\n📖 Available commands:");
        Console.WriteLine("  help                    - Show this help message");
        Console.WriteLine("  tools                   - List available tools");
        Console.WriteLine("  travel <destination>    - Start travel booking process");
        Console.WriteLine("  research <topic>        - Start research process");
        Console.WriteLine("  clear                   - Clear current session");
        Console.WriteLine("  exit                    - Exit the client");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  travel Paris");
        Console.WriteLine("  research \"AI agent communication\"");
    }

    private async Task ListTools(DirectHttpMcpClient client)
    {
        try
        {
            var toolsResult = await client.ListToolsAsync();
            
            Console.WriteLine("\n📚 Available tools:");
            if (toolsResult.TryGetProperty("tools", out var toolsArray))
            {
                foreach (var tool in toolsArray.EnumerateArray())
                {
                    if (tool.TryGetProperty("name", out var nameElement))
                    {
                        var name = nameElement.GetString();
                        Console.WriteLine($"  • {name}");
                        
                        if (tool.TryGetProperty("description", out var descElement))
                        {
                            var description = descElement.GetString();
                            if (!string.IsNullOrEmpty(description))
                            {
                                Console.WriteLine($"    {description}");
                            }
                        }
                        
                        // Show input schema if available
                        if (tool.TryGetProperty("inputSchema", out var schema) && 
                            schema.ValueKind == JsonValueKind.Object &&
                            schema.TryGetProperty("properties", out var properties))
                        {
                            Console.WriteLine("    Parameters:");
                            foreach (var prop in properties.EnumerateObject())
                            {
                                Console.WriteLine($"      - {prop.Name}");
                            }
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list tools");
        }
    }

    private async Task ProcessToolCommand(DirectHttpMcpClient client, string input)
    {
        try
        {
            var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            var command = parts[0].ToLowerInvariant();
            var argument = parts.Length > 1 ? parts[1] : "";

            Dictionary<string, object?> args;
            string toolName;

            switch (command)
            {
                case "travel":
                    if (string.IsNullOrWhiteSpace(argument))
                    {
                        _logger.LogWarning("Please specify a destination: travel <destination>");
                        return;
                    }
                    toolName = "travel_agent";
                    args = new Dictionary<string, object?> { { "destination", argument } };
                    break;

                case "research":
                    if (string.IsNullOrWhiteSpace(argument))
                    {
                        _logger.LogWarning("Please specify a research topic: research <topic>");
                        return;
                    }
                    toolName = "research_agent";
                    args = new Dictionary<string, object?> { { "topic", argument } };
                    break;

                default:
                    _logger.LogWarning("Unknown command: {Command}. Type 'help' for available commands.", command);
                    return;
            }

            await ExecuteToolWithCallbacks(client, toolName, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command: {Input}", input);
        }
    }

    private async Task ExecuteToolWithCallbacks(DirectHttpMcpClient client, string toolName, Dictionary<string, object?> args)
    {
        try
        {
            _logger.LogInformation("🚀 Executing {ToolName} with arguments: {Args}", 
                toolName, JsonSerializer.Serialize(args));

            // Save session info for potential resumption
            await _sessionManager.SaveSessionAsync(toolName, args);

            // Note: Progress notifications are handled directly by our server
            // and logged through the server's console output

            // Execute the tool
            var result = await client.CallToolAsync(toolName, args);
            
            _logger.LogInformation("✅ Tool execution completed");
            DisplayToolResult(result);

            // Clear session on successful completion
            await _sessionManager.ClearSessionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool execution failed");
            // Keep session for potential retry/resumption
        }
    }

    private void DisplayToolResult(JsonElement result)
    {
        Console.WriteLine("\n📋 Tool Result:");
        Console.WriteLine("═══════════════");
        
        if (result.TryGetProperty("content", out var contentArray))
        {
            foreach (var content in contentArray.EnumerateArray())
            {
                if (content.TryGetProperty("type", out var typeElement) && 
                    typeElement.GetString() == "text" &&
                    content.TryGetProperty("text", out var textElement))
                {
                    Console.WriteLine(textElement.GetString());
                }
                else
                {
                    // Handle other content types
                    Console.WriteLine(content.GetRawText());
                }
            }
        }
        else
        {
            // Fallback for different result formats
            Console.WriteLine(result.GetRawText());
        }
        
        Console.WriteLine("═══════════════");
    }
}
