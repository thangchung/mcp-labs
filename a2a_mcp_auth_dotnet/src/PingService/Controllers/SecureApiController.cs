using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace PingService.Controllers;

/// <summary>
/// Secure API controller demonstrating Microsoft Entra ID authentication and authorization features.
/// This controller provides endpoints for testing authentication, retrieving user information,
/// and demonstrating different authorization policies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[SwaggerTag("Secure API endpoints demonstrating authentication and authorization")]
public class SecureApiController : ControllerBase
{
    private readonly ILogger<SecureApiController> _logger;

    public SecureApiController(ILogger<SecureApiController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the current user's information and claims from the JWT token.
    /// This endpoint demonstrates how to extract user identity and claims for personalization.
    /// </summary>
    /// <returns>User information including identity, email, roles, and token claims</returns>
    [HttpGet("userinfo")]
    [SwaggerOperation(
        Summary = "Get current user information",
        Description = "Returns the authenticated user's identity, claims, and roles extracted from the JWT token."
    )]
    [SwaggerResponse(200, "User information retrieved successfully")]
    [SwaggerResponse(401, "User is not authenticated")]
    public IActionResult GetUserInfo()
    {
        try
        {
            var userInfo = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                Name = User.Identity?.Name,
                Email = User.FindFirst(ClaimTypes.Email)?.Value ??
                       User.FindFirst("preferred_username")?.Value ??
                       User.FindFirst("unique_name")?.Value,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("sub")?.Value ??
                        User.FindFirst("oid")?.Value,
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                TenantId = User.FindFirst("tid")?.Value,
                AppId = User.FindFirst("appid")?.Value ?? User.FindFirst("aud")?.Value,
                Scopes = User.FindFirst("scp")?.Value?.Split(' ') ?? Array.Empty<string>(),
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };

            _logger.LogInformation("User info requested by {UserId} with email {Email} from tenant {TenantId}", 
                userInfo.UserId, userInfo.Email, userInfo.TenantId);
            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user information for user {UserId}", 
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");
            return StatusCode(500, new { Error = "Failed to retrieve user information" });
        }
    }

    /// <summary>
    /// Basic authentication test endpoint.
    /// This endpoint simply confirms that the user is authenticated and returns a success message.
    /// </summary>
    /// <returns>Success message for authenticated users</returns>
    [HttpGet("test")]
    [SwaggerOperation(
        Summary = "Test authentication",
        Description = "A simple endpoint to verify that JWT authentication is working correctly."
    )]
    [SwaggerResponse(200, "Authentication test successful")]
    [SwaggerResponse(401, "User is not authenticated")]
    public IActionResult TestAuth()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
        _logger.LogInformation("Authentication test successful for user {UserId}", userId);
        
        return Ok(new 
        { 
            Message = "Authentication successful!", 
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Service = "PingService"
        });
    }

    /// <summary>
    /// Health check endpoint specific to secure API functionality.
    /// This endpoint provides detailed health information for authentication-related services.
    /// </summary>
    /// <returns>Health status of secure API components</returns>
    [HttpGet("health")]
    [SwaggerOperation(
        Summary = "Secure API health check",
        Description = "Returns health status information for authentication and authorization components."
    )]
    [SwaggerResponse(200, "Health check successful")]
    [SwaggerResponse(401, "User is not authenticated")]
    public IActionResult SecureHealth()
    {
        var healthInfo = new
        {
            Status = "Healthy",
            Service = "PingService - Secure API",
            Authentication = new
            {
                Scheme = "Microsoft Entra ID",
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                TokenPresent = Request.Headers.ContainsKey("Authorization")
            },
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        };

        _logger.LogInformation("Secure API health check performed");
        return Ok(healthInfo);
    }
}
