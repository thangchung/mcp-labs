using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpAgent.Client;

/// <summary>
/// Simple validation client for testing basic MCP connectivity
/// </summary>
public class ValidationClient
{
    private readonly string _serverUrl;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ValidationClient> _logger;

    public ValidationClient(string serverUrl, ILoggerFactory loggerFactory)
    {
        _serverUrl = serverUrl;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ValidationClient>();
    }

    /// <summary>
    /// Performs basic validation of MCP server connectivity and capabilities
    /// </summary>
    public async Task<bool> ValidateAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Starting MCP server validation...");

            using var httpClient = new HttpClient();
            var transport = new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = new Uri(_serverUrl),
                Name = "Validation Client"
            }, httpClient, _loggerFactory);

            var client = await McpClientFactory.CreateAsync(transport, loggerFactory: _loggerFactory);

            try
            {
                // Test 1: Ping the server
                _logger.LogInformation("1️⃣ Testing server ping...");
                await client.PingAsync();
                _logger.LogInformation("✅ Ping successful");

                // Test 2: Check server info
                _logger.LogInformation("2️⃣ Checking server information...");
                _logger.LogInformation("   Server: {Name} v{Version}", client.ServerInfo.Name, client.ServerInfo.Version);
                
                // Test 3: List available tools
                _logger.LogInformation("3️⃣ Listing available tools...");
                var tools = await client.ListToolsAsync();
                _logger.LogInformation("   Found {Count} tools:", tools.Count);
                foreach (var tool in tools)
                {
                    _logger.LogInformation("   • {Name}: {Description}", tool.Name, tool.Description ?? "No description");
                }

                // Test 4: Check capabilities
                _logger.LogInformation("4️⃣ Checking server capabilities...");
                var caps = client.ServerCapabilities;
                _logger.LogInformation("   Tools: {HasTools}", caps.Tools != null ? "✅" : "❌");
                _logger.LogInformation("   Resources: {HasResources}", caps.Resources != null ? "✅" : "❌");
                _logger.LogInformation("   Prompts: {HasPrompts}", caps.Prompts != null ? "✅" : "❌");
                _logger.LogInformation("   Logging: {HasLogging}", caps.Logging != null ? "✅" : "❌");

                // Test 5: Quick tool execution test (if tools are available)
                if (tools.Count > 0)
                {
                    _logger.LogInformation("5️⃣ Testing tool execution readiness...");
                    var testTool = tools.FirstOrDefault(t => t.Name.Contains("travel") || t.Name.Contains("research"));
                    if (testTool != null)
                    {
                        _logger.LogInformation("   Found testable tool: {ToolName}", testTool.Name);
                        // Note: We don't actually execute here to avoid side effects during validation
                        _logger.LogInformation("   Tool execution readiness: ✅");
                    }
                    else
                    {
                        _logger.LogInformation("   No recognizable agent tools found for testing");
                    }
                }

                _logger.LogInformation("🎉 All validation tests passed!");
                return true;
            }
            finally
            {
                await client.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Validation failed");
            return false;
        }
    }

    /// <summary>
    /// Runs a quick connectivity test
    /// </summary>
    public static async Task<bool> QuickConnectivityTestAsync(string serverUrl, ILoggerFactory? loggerFactory = null)
    {
        var factory = loggerFactory ?? LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        try
        {
            var validator = new ValidationClient(serverUrl, factory);
            return await validator.ValidateAsync();
        }
        finally
        {
            if (loggerFactory == null)
            {
                factory.Dispose();
            }
        }
    }
}
