using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace A2AMcpAuth.Tests;

/// <summary>
/// Integration tests for the complete A2A-MCP authentication flow:
/// 1. Send ping message to Ping Service
/// 2. Ping Service uses A2A protocol to communicate with Pong Service
/// 3. Pong Service calls MCP Server using MCP protocol
/// 4. All services authenticate via Microsoft Entra ID
/// 5. Only admin role can access MCP Server tools
/// </summary>
public class A2AMcpAuthenticationFlowTests : IClassFixture<A2AMcpTestFixture>
{
    private readonly A2AMcpTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public A2AMcpAuthenticationFlowTests(A2AMcpTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task SendPingMessage_WhenUserIsAdmin_ShouldSuccessfullyProcessThroughAllServices()
    {
        // Arrange - Create admin user JWT token
        var adminToken = CreateAdminJwtToken();
        var pingRequest = new
        {
            Message = "Hello from integration test",
            UserId = "admin@example.com"
        };

        // Act - Send ping message to Ping Service
        _fixture.PingServiceClient.DefaultRequestHeaders.Clear();
        _fixture.PingServiceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _fixture.PingServiceClient.PostAsJsonAsync("/api/ping", pingRequest);

        // Assert - Verify successful response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {responseContent}");

        var result = JsonSerializer.Deserialize<PingResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Message.Should().Contain("Hello from integration test");
        result.A2AResponse.Should().NotBeNull();
        result.A2AResponse!.Success.Should().BeTrue();
        result.McpResponse.Should().NotBeNull();
        result.McpResponse!.Success.Should().BeTrue();
        result.McpResponse.ToolExecuted.Should().BeTrue();
        result.McpResponse.AdminAccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendPingMessage_WhenUserIsNotAdmin_ShouldFailAtMcpServerLevel()
    {
        // Arrange - Create regular user JWT token
        var userToken = CreateUserJwtToken();
        var pingRequest = new
        {
            Message = "Hello from non-admin user",
            UserId = "user@example.com"
        };

        // Act - Send ping message to Ping Service
        _fixture.PingServiceClient.DefaultRequestHeaders.Clear();
        _fixture.PingServiceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var response = await _fixture.PingServiceClient.PostAsJsonAsync("/api/ping", pingRequest);

        // Assert - Should succeed at A2A level but fail at MCP level
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PingResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.A2AResponse.Should().NotBeNull();
        result.A2AResponse!.Success.Should().BeTrue();
        result.McpResponse.Should().NotBeNull();
        result.McpResponse!.Success.Should().BeFalse();
        result.McpResponse.ErrorMessage.Should().Contain("Access denied");
        result.McpResponse.AdminAccess.Should().BeFalse();
    }

    [Fact]
    public async Task SendPingMessage_WithInvalidToken_ShouldFailAtAuthentication()
    {
        // Arrange - Create invalid JWT token
        var invalidToken = "invalid.jwt.token";
        var pingRequest = new
        {
            Message = "Hello with invalid token",
            UserId = "test@example.com"
        };

        // Act - Send ping message to Ping Service
        _fixture.PingServiceClient.DefaultRequestHeaders.Clear();
        _fixture.PingServiceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);
        var response = await _fixture.PingServiceClient.PostAsJsonAsync("/api/ping", pingRequest);

        // Assert - Should fail at authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendPingMessage_WithoutToken_ShouldFailAtAuthentication()
    {
        // Arrange
        var pingRequest = new
        {
            Message = "Hello without token",
            UserId = "test@example.com"
        };

        // Act - Send ping message without authorization header
        var response = await _fixture.PingServiceClient.PostAsJsonAsync("/api/ping", pingRequest);

        // Assert - Should fail at authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A2AProtocol_ShouldPassJwtTokenCorrectly()
    {
        // Arrange - Create admin token
        var adminToken = CreateAdminJwtToken();
        
        // Act - Direct call to Pong Service with A2A protocol
        var a2aRequest = new
        {
            jsonrpc = "2.0",
            method = "message/send",
            id = "test-123",
            @params = new
            {
                message = new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { kind = "text", text = "Test A2A message" }
                    }
                }
            }
        };

        _fixture.PongServiceClient.DefaultRequestHeaders.Clear();
        _fixture.PongServiceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _fixture.PongServiceClient.PostAsJsonAsync("/a2a", a2aRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        result.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        result.GetProperty("id").GetString().Should().Be("test-123");
        result.TryGetProperty("result", out var resultProperty).Should().BeTrue();
    }

    [Fact]
    public async Task McpServer_ShouldValidateAdminRoleCorrectly()
    {
        // Arrange - Create admin token
        var adminToken = CreateAdminJwtToken();
        
        // Act - Direct call to MCP Server
        var mcpRequest = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "test-mcp-123"
        };

        _fixture.McpServerClient.DefaultRequestHeaders.Clear();
        _fixture.McpServerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _fixture.McpServerClient.PostAsJsonAsync("/mcp", mcpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"MCP Response: {responseContent}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        result.TryGetProperty("result", out var resultProperty).Should().BeTrue();
        resultProperty.TryGetProperty("tools", out var toolsProperty).Should().BeTrue();
    }

    [Fact]
    public async Task McpServer_ShouldRejectNonAdminUsers()
    {
        // Arrange - Create user token (non-admin)
        var userToken = CreateUserJwtToken();
        
        // Act - Direct call to MCP Server
        var mcpRequest = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "test-mcp-456"
        };

        _fixture.McpServerClient.DefaultRequestHeaders.Clear();
        _fixture.McpServerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var response = await _fixture.McpServerClient.PostAsJsonAsync("/mcp", mcpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompleteFlow_ShouldLogAllAuthenticationSteps()
    {
        // Arrange
        var adminToken = CreateAdminJwtToken();
        var pingRequest = new
        {
            Message = "Test complete flow logging",
            UserId = "admin@example.com"
        };

        // Act
        _fixture.PingServiceClient.DefaultRequestHeaders.Clear();
        _fixture.PingServiceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _fixture.PingServiceClient.PostAsJsonAsync("/api/ping", pingRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify that authentication steps were logged
        var logs = _fixture.GetCapturedLogs();
        logs.Should().Contain(log => log.Contains("JWT token validated"));
        logs.Should().Contain(log => log.Contains("Sending A2A message to Pong Service"));
        logs.Should().Contain(log => log.Contains("Processing MCP method: tools/list"));
        logs.Should().Contain(log => log.Contains("MCP call completed - Success: True"));
    }

    private string CreateAdminJwtToken()
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_fixture.JwtSecretKey);
        
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin@example.com"),
                new Claim(ClaimTypes.Email, "admin@example.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("scope", "mcp:tools"),
                new Claim("aud", "demo-client")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "https://login.microsoftonline.com/test-tenant",
            Audience = "demo-client",
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private string CreateUserJwtToken()
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_fixture.JwtSecretKey);
        
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "user@example.com"),
                new Claim(ClaimTypes.Email, "user@example.com"),
                new Claim(ClaimTypes.Role, "User"),
                new Claim("aud", "demo-client")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "https://login.microsoftonline.com/test-tenant",
            Audience = "demo-client",
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }
}

// Response DTOs for testing
public class PingResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public A2AResponseData? A2AResponse { get; set; }
    public McpResponseData? McpResponse { get; set; }
}

public class A2AResponseData
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class McpResponseData
{
    public bool Success { get; set; }
    public bool ToolExecuted { get; set; }
    public bool AdminAccess { get; set; }
    public string? ErrorMessage { get; set; }
}
