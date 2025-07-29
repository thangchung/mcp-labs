using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace McpAgent.Client;

/// <summary>
/// Enhanced MCP client with support for elicitation, sampling, and interactive callbacks
/// following patterns from the Python reference implementation
/// </summary>
public class EnhancedMcpClient
{
    private readonly string _serverUrl;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<EnhancedMcpClient> _logger;
    private readonly SessionManager _sessionManager;

    public EnhancedMcpClient(string serverUrl, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new ArgumentException("Server URL cannot be null or empty", nameof(serverUrl));
        
        _serverUrl = serverUrl;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<EnhancedMcpClient>();
        _sessionManager = new SessionManager(_loggerFactory);
    }

    /// <summary>
    /// Runs the enhanced interactive client with full MCP capabilities
    /// </summary>
    public async Task RunAsync()
    {
        try
        {
            await ConnectWithEnhancedCapabilities();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enhanced client execution");
            throw;
        }
    }

    private async Task ConnectWithEnhancedCapabilities()
    {
        // Create HTTP transport with enhanced options
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10); // Allow for long-running operations
        
        var transport = new SseClientTransport(new SseClientTransportOptions
        {
            Endpoint = new Uri(_serverUrl),
            Name = "Enhanced MCP Agent Client"
        }, httpClient, _loggerFactory);

        // Create client with enhanced capabilities
        var clientOptions = new McpClientOptions
        {
            Capabilities = new ClientCapabilities
            {
                // Sampling = new SamplingCapability(), // Requires a handler - commenting out for now
                // Prompts = new PromptsCapability(),   // Enable prompts support (not available in current SDK)
                // Resources = new ResourcesCapability() // Enable resources support (not available in current SDK)
            },
            ClientInfo = new Implementation
            {
                Name = "MCP Agent Client",
                Version = "1.0.0"
            }
        };

        var client = await McpClientFactory.CreateAsync(transport, clientOptions, _loggerFactory);
        
        _logger.LogInformation("✅ Connected with enhanced capabilities");
        _logger.LogInformation("🔧 Server: {ServerName} v{ServerVersion}", 
            client.ServerInfo.Name, client.ServerInfo.Version);

        try
        {
            // Register global notification handlers for the session
            await RegisterGlobalHandlers(client);

            // Check for existing session
            var existingSession = await _sessionManager.LoadExistingSessionAsync();
            
            if (existingSession != null)
            {
                _logger.LogInformation("🔄 Resuming previous session...");
                await ResumeWithEnhancedSupport(client, existingSession);
            }
            else
            {
                await StartEnhancedInteractiveSession(client);
            }
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    private async Task RegisterGlobalHandlers(IMcpClient client)
    {
        // Handle progress notifications
        await using var progressHandler = client.RegisterNotificationHandler(
            NotificationMethods.ProgressNotification,
            async (notification, ct) =>
            {
                if (notification.Params?.AsObject() is { } progressData)
                {
                    var message = progressData.TryGetPropertyValue("message", out var msg) ? msg?.GetValue<string>() : "Processing...";
                    var progress = progressData.TryGetPropertyValue("progress", out var prog) ? prog?.GetValue<int>() ?? 0 : 0;
                    var total = progressData.TryGetPropertyValue("total", out var tot) ? tot?.GetValue<int>() ?? 100 : 100;
                    
                    Console.WriteLine($"🔄 {message} [{progress}/{total}] ({progress * 100 / total}%)");
                }
            });

        // Handle log messages (note: LoggingMessage might not be available in current SDK version)
        // TODO: Verify notification method name when SDK documentation is available
        // await using var logHandler = client.RegisterNotificationHandler(
        //     NotificationMethods.LoggingMessage,
        //     async (notification, ct) =>
        //     {
        //         if (notification.Params?.AsObject() is { } logData)
        //         {
        //             var level = logData.TryGetPropertyValue("level", out var lvl) ? lvl?.GetValue<string>() : "info";
        //             var data = logData.TryGetPropertyValue("data", out var d) ? d?.GetValue<string>() : "";
        //             var logger = logData.TryGetPropertyValue("logger", out var log) ? log?.GetValue<string>() : "";
        //             
        //             var emoji = level.ToLowerInvariant() switch
        //             {
        //                 "error" => "❌",
        //                 "warning" => "⚠️",
        //                 "info" => "ℹ️",
        //                 "debug" => "🔍",
        //                 _ => "📝"
        //             };
        //             
        //             Console.WriteLine($"{emoji} [{logger}] {data}");
        //         }
        //     });

        // Handle resource notifications if supported
        if (client.ServerCapabilities.Resources?.Subscribe == true)
        {
            await using var resourceHandler = client.RegisterNotificationHandler(
                NotificationMethods.ResourceUpdatedNotification,
                async (notification, ct) =>
                {
                    if (notification.Params?.AsObject() is { } resourceData)
                    {
                        var uri = resourceData.TryGetPropertyValue("uri", out var u) ? u?.GetValue<string>() : "";
                        Console.WriteLine($"📄 Resource updated: {uri}");
                    }
                });
        }
    }

    private async Task ResumeWithEnhancedSupport(IMcpClient client, SessionInfo sessionInfo)
    {
        _logger.LogInformation("📋 Resuming {ToolName} with enhanced support", sessionInfo.LastTool);
        
        try
        {
            var result = await ExecuteToolWithFullSupport(client, sessionInfo.LastTool, sessionInfo.LastArgs);
            
            if (result.IsError)
            {
                _logger.LogWarning("⚠️ Resumed tool reported an error");
            }
            else
            {
                _logger.LogInformation("✅ Session resumed successfully");
            }
            
            DisplayEnhancedResult(result);
            await _sessionManager.ClearSessionAsync();
            await StartEnhancedInteractiveSession(client);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resume, starting fresh session");
            await _sessionManager.ClearSessionAsync();
            await StartEnhancedInteractiveSession(client);
        }
    }

    private async Task StartEnhancedInteractiveSession(IMcpClient client)
    {
        // Display server capabilities
        await DisplayServerCapabilities(client);
        
        // List available tools with enhanced information
        await DisplayAvailableTools(client);
        
        // Start interactive loop
        await RunEnhancedInteractiveLoop(client);
    }

    private async Task DisplayServerCapabilities(IMcpClient client)
    {
        Console.WriteLine("\n🔧 Server Capabilities:");
        Console.WriteLine("═══════════════════════");
        
        var caps = client.ServerCapabilities;
        Console.WriteLine($"• Tools: {(caps.Tools != null ? "✅" : "❌")}");
        Console.WriteLine($"• Resources: {(caps.Resources != null ? "✅" : "❌")}");
        Console.WriteLine($"• Prompts: {(caps.Prompts != null ? "✅" : "❌")}");
        Console.WriteLine($"• Logging: {(caps.Logging != null ? "✅" : "❌")}");
        Console.WriteLine($"• Sampling: (not available in current SDK)");
        
        if (client.ServerInstructions != null)
        {
            Console.WriteLine($"\n📋 Server Instructions:");
            Console.WriteLine(client.ServerInstructions);
        }
    }

    private async Task DisplayAvailableTools(IMcpClient client)
    {
        try
        {
            var tools = await client.ListToolsAsync();
            
            Console.WriteLine("\n🛠️ Available Tools:");
            Console.WriteLine("══════════════════");
            
            foreach (var tool in tools)
            {
                Console.WriteLine($"📦 {tool.Name}");
                if (!string.IsNullOrEmpty(tool.Description))
                {
                    Console.WriteLine($"   {tool.Description}");
                }
                
                // Display input schema in a user-friendly way
                if (tool.JsonSchema.ValueKind == JsonValueKind.Object)
                {
                    DisplayToolSchema(tool.JsonSchema);
                }
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list tools");
        }
    }

    private void DisplayToolSchema(JsonElement schema)
    {
        if (schema.TryGetProperty("properties", out var properties))
        {
            Console.WriteLine("   Parameters:");
            foreach (var prop in properties.EnumerateObject())
            {
                var type = prop.Value.TryGetProperty("type", out var t) ? t.GetString() : "any";
                var description = prop.Value.TryGetProperty("description", out var d) ? d.GetString() : "";
                
                Console.WriteLine($"     • {prop.Name} ({type}): {description}");
            }
        }
    }

    private async Task RunEnhancedInteractiveLoop(IMcpClient client)
    {
        Console.WriteLine("\n🎮 Enhanced Interactive Mode");
        Console.WriteLine("════════════════════════════");
        Console.WriteLine("Available commands:");
        Console.WriteLine("  travel <destination>     - Book travel with price confirmation");
        Console.WriteLine("  research <topic>         - Research with AI assistance");
        Console.WriteLine("  tools                    - List available tools");
        Console.WriteLine("  capabilities             - Show server capabilities");
        Console.WriteLine("  clear                    - Clear current session");
        Console.WriteLine("  help                     - Show help");
        Console.WriteLine("  exit                     - Exit client");

        while (true)
        {
            Console.Write("\n🎯 > ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            await ProcessEnhancedCommand(client, input);
        }
    }

    private async Task ProcessEnhancedCommand(IMcpClient client, string input)
    {
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0].ToLowerInvariant();
        var argument = parts.Length > 1 ? parts[1] : "";

        try
        {
            switch (command)
            {
                case "help":
                    await RunEnhancedInteractiveLoop(client);
                    return;

                case "tools":
                    await DisplayAvailableTools(client);
                    break;

                case "capabilities":
                    await DisplayServerCapabilities(client);
                    break;

                case "clear":
                    await _sessionManager.ClearSessionAsync();
                    Console.WriteLine("🗑️ Session cleared");
                    break;

                case "travel":
                    if (string.IsNullOrWhiteSpace(argument))
                    {
                        Console.WriteLine("❌ Please specify a destination: travel <destination>");
                        break;
                    }
                    await ExecuteWithEnhancedFeatures(client, "travel_agent", 
                        new Dictionary<string, object?> { { "destination", argument } });
                    break;

                case "research":
                    if (string.IsNullOrWhiteSpace(argument))
                    {
                        Console.WriteLine("❌ Please specify a research topic: research <topic>");
                        break;
                    }
                    await ExecuteWithEnhancedFeatures(client, "research_agent", 
                        new Dictionary<string, object?> { { "topic", argument } });
                    break;

                default:
                    Console.WriteLine($"❌ Unknown command: {command}. Type 'help' for available commands.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command: {Command}", command);
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    private async Task ExecuteWithEnhancedFeatures(IMcpClient client, string toolName, Dictionary<string, object?> args)
    {
        Console.WriteLine($"\n🚀 Executing {toolName}...");
        Console.WriteLine($"📝 Arguments: {JsonSerializer.Serialize(args, new JsonSerializerOptions { WriteIndented = true })}");

        // Save session for resumption
        await _sessionManager.SaveSessionAsync(toolName, args);

        try
        {
            var result = await ExecuteToolWithFullSupport(client, toolName, args);
            DisplayEnhancedResult(result);

            if (!result.IsError)
            {
                await _sessionManager.ClearSessionAsync();
                Console.WriteLine("✅ Tool execution completed successfully");
            }
            else
            {
                Console.WriteLine("⚠️ Tool execution completed with errors");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool execution failed");
            Console.WriteLine($"❌ Execution failed: {ex.Message}");
            Console.WriteLine("💾 Session saved for potential resumption");
        }
    }

    private async Task<dynamic> ExecuteToolWithFullSupport(IMcpClient client, string toolName, Dictionary<string, object?> args)
    {
        // TODO: Add elicitation and sampling support when available in the C# SDK
        // The current ModelContextProtocol.Client doesn't seem to expose elicitation/sampling callbacks
        // This would need to be implemented as the SDK evolves
        
        return await client.CallToolAsync(toolName, args);
    }

    private void DisplayEnhancedResult(dynamic result)
    {
        Console.WriteLine("\n📋 Enhanced Tool Result:");
        Console.WriteLine("═══════════════════════════");
        
        if (result.Content.Count == 0)
        {
            Console.WriteLine("(No content returned)");
        }
        
        foreach (var content in result.Content)
        {
            // Use dynamic typing to handle content blocks until we resolve SDK types
            dynamic dynamicContent = content;
            string contentType = content.GetType().Name;
            
            switch (contentType)
            {
                case "TextContentBlock":
                    Console.WriteLine("📄 Text Content:");
                    Console.WriteLine(dynamicContent.Text);
                    break;
                    
                case "ImageContentBlock":
                    Console.WriteLine($"🖼️ Image Content: {dynamicContent.Source ?? dynamicContent.Data}");
                    break;
                    
                default:
                    Console.WriteLine($"📦 {content.Type} Content:");
                    Console.WriteLine(JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true }));
                    break;
            }
            Console.WriteLine();
        }
        
        if (result.IsError)
        {
            Console.WriteLine("❌ Error reported by tool");
        }
        
        Console.WriteLine("═══════════════════════════");
    }
}
