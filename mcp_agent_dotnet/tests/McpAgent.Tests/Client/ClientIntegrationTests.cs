using Microsoft.Extensions.Logging;
using McpAgent.Client;
using Xunit;
using Xunit.Abstractions;

namespace McpAgent.Tests.Client;

/// <summary>
/// Integration tests for the MCP client components
/// These tests require the MCP server to be running on the default port
/// </summary>
public class ClientIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;
    private const string TestServerUrl = "http://127.0.0.1:8006/mcp";

    public ClientIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddXUnit(output).SetMinimumLevel(LogLevel.Debug));
    }

    [Fact]
    public async Task ValidationClient_ShouldConnectToServer()
    {
        // Skip test if server is not running
        if (!await IsServerRunning())
        {
            _output.WriteLine("⚠️ Skipping integration test - MCP server not running");
            return;
        }

        // Arrange
        var validationClient = new ValidationClient(TestServerUrl, _loggerFactory);

        // Act
        var isValid = await validationClient.ValidateAsync();

        // Assert
        Assert.True(isValid, "Validation should pass when server is running");
    }

    [Fact]
    public async Task QuickConnectivityTest_WithRunningServer_ShouldReturnTrue()
    {
        // Skip test if server is not running
        if (!await IsServerRunning())
        {
            _output.WriteLine("⚠️ Skipping integration test - MCP server not running");
            return;
        }

        // Act
        var result = await ValidationClient.QuickConnectivityTestAsync(TestServerUrl, _loggerFactory);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task QuickConnectivityTest_WithInvalidServer_ShouldReturnFalse()
    {
        // Arrange - Use a port that should not be in use
        var invalidUrl = "http://127.0.0.1:9999/mcp";

        // Act
        var result = await ValidationClient.QuickConnectivityTestAsync(invalidUrl, _loggerFactory);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("http://127.0.0.1:8006/mcp")]
    [InlineData("http://localhost:8006/mcp")]
    public async Task ValidationClient_ShouldHandleDifferentUrlFormats(string serverUrl)
    {
        // Skip test if server is not running
        if (!await IsServerRunning())
        {
            _output.WriteLine("⚠️ Skipping integration test - MCP server not running");
            return;
        }

        // Arrange
        var validationClient = new ValidationClient(serverUrl, _loggerFactory);

        // Act & Assert
        var isValid = await validationClient.ValidateAsync();
        
        // Should work with different URL formats when server is running
        Assert.True(isValid, $"Validation should pass for URL format: {serverUrl}");
    }

    /// <summary>
    /// Helper method to check if the MCP server is running
    /// </summary>
    private async Task<bool> IsServerRunning()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            // Try to connect to the server endpoint
            var response = await httpClient.GetAsync($"{TestServerUrl.Replace("/mcp", "")}");
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Unit tests for client components that don't require server connectivity
/// </summary>
public class ClientUnitTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;

    public ClientUnitTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddXUnit(output).SetMinimumLevel(LogLevel.Debug));
    }

    [Fact]
    public void McpAgentClient_ShouldInitializeWithValidParameters()
    {
        // Arrange & Act
        var client = new McpAgentClient("http://test.com/mcp", _loggerFactory);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void EnhancedMcpClient_ShouldInitializeWithValidParameters()
    {
        // Arrange & Act
        var client = new EnhancedMcpClient("http://test.com/mcp", _loggerFactory);

        // Assert
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void McpAgentClient_WithInvalidUrl_ShouldThrow(string? invalidUrl)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new McpAgentClient(invalidUrl!, _loggerFactory));
    }

    [Fact]
    public void SessionInfo_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var originalSession = new SessionInfo
        {
            SessionId = Guid.NewGuid().ToString(),
            LastTool = "test_tool",
            LastArgs = new Dictionary<string, object?>
            {
                { "param1", "value1" },
                { "param2", 42 },
                { "param3", true }
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(originalSession);
        var deserializedSession = System.Text.Json.JsonSerializer.Deserialize<SessionInfo>(json);

        // Assert
        Assert.NotNull(deserializedSession);
        Assert.Equal(originalSession.SessionId, deserializedSession.SessionId);
        Assert.Equal(originalSession.LastTool, deserializedSession.LastTool);
        Assert.Equal(originalSession.Timestamp, deserializedSession.Timestamp);
        
        // Check args (note: JSON deserialization may change types)
        Assert.Equal(3, deserializedSession.LastArgs.Count);
        Assert.True(deserializedSession.LastArgs.ContainsKey("param1"));
        Assert.True(deserializedSession.LastArgs.ContainsKey("param2"));
        Assert.True(deserializedSession.LastArgs.ContainsKey("param3"));
    }
}
