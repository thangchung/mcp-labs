using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace PongService.Services;

public class McpClientService : IMcpClientService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<McpClientService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HttpClient _httpClient;

    public McpClientService(
        IConfiguration configuration,
        ILogger<McpClientService> logger,
        ILoggerFactory loggerFactory,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _httpClient = httpClient;
    }

    public async Task<McpResponse> CallMcpServerAsync(string jwtToken, string message, string userEmail)
    {
        try
        {
            _logger.LogInformation("Calling MCP Server for user: {UserEmail}", userEmail);

            // Validate JWT token and extract user role
            var userRole = ExtractUserRoleFromToken(jwtToken);
            var isAdmin = userRole.ToLowerInvariant() == "admin";

            _logger.LogInformation("User role extracted: {UserRole}, IsAdmin: {IsAdmin}", userRole, isAdmin);

            // Get MCP server configuration
            var serverUrl = _configuration["McpServer:Url"] ?? "http://localhost:5002";
            var clientName = _configuration["McpServer:ClientName"] ?? "PongService";

            _logger.LogInformation("Connecting to MCP server at: {ServerUrl}", serverUrl);

            // Create HTTP client with JWT token in Authorization header
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");

            // Create MCP client transport without OAuth
            var transport = new SseClientTransport(new()
            {
                Endpoint = new Uri($"{serverUrl}/mcp"),
                Name = clientName
            }, _httpClient, _loggerFactory);

            // Create MCP client using the official factory
            await using var client = await McpClientFactory.CreateAsync(transport, loggerFactory: _loggerFactory);

            _logger.LogInformation("MCP client connected successfully");

            // List available tools
            var tools = await client.ListToolsAsync();
            _logger.LogInformation("Found {ToolCount} tools on the server", tools.Count);

            // Look for the PingProcessor tool
            var pingTool = tools.FirstOrDefault(t => t.Name == "ping_processor");
            if (pingTool == null)
            {
                _logger.LogWarning("ping_processor tool not found on MCP server");
                return new McpResponse
                {
                    Success = false,
                    AdminAccess = isAdmin,
                    ToolExecuted = false,
                    ErrorMessage = "ping_processor tool not available on MCP server",
                    Timestamp = DateTime.UtcNow
                };
            }

            _logger.LogInformation("Calling ping_processor tool with message: {Message}", message);

            // Call the tool using the official MCP client
            var result = await client.CallToolAsync(
                "ping_processor",
                new Dictionary<string, object?>
                {
                    { "message", message },
                    { "userEmail", userEmail },
                    { "isAdmin", isAdmin },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                });

            _logger.LogInformation("MCP tool call successful");

            // Extract response content
            var responseContent = result.Content?.Count > 0 
                ? ((TextContentBlock)result.Content[0]).Text 
                : "No response content";

            return new McpResponse
            {
                Success = true,
                AdminAccess = isAdmin,
                ToolExecuted = true,
                ResponseContent = responseContent,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception mcpEx) when (mcpEx.GetType().Name == "McpException")
        {
            _logger.LogError(mcpEx, "MCP protocol error for user: {UserEmail}", userEmail);
            
            return new McpResponse
            {
                Success = false,
                AdminAccess = false,
                ToolExecuted = false,
                ErrorMessage = $"MCP protocol error: {mcpEx.Message}",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MCP server for user: {UserEmail}", userEmail);
            
            return new McpResponse
            {
                Success = false,
                AdminAccess = false,
                ToolExecuted = false,
                ErrorMessage = $"MCP communication error: {ex.Message}",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private string ExtractUserRoleFromToken(string jwtToken)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(jwtToken);
            
            var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "scp");
            return roleClaim?.Value ?? "User";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract role from JWT token");
            return "User";
        }
    }
}
