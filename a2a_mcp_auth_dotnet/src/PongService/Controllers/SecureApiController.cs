using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace PongService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SecureApiController : ControllerBase
{
    private readonly ILogger<SecureApiController> _logger;

    public SecureApiController(ILogger<SecureApiController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get user information from the authenticated token
    /// </summary>
    /// <returns>User information and claims</returns>
    [HttpGet("userinfo")]
    [AuthorizeForScopes(Scopes = ["access_as_user"])]
    public ActionResult<object> GetUserInfo()
    {
        try
        {
            var userInfo = new
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Name = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("preferred_username")?.Value,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                TenantId = User.FindFirst("tid")?.Value,
                ObjectId = User.FindFirst("oid")?.Value,
                Scopes = User.FindFirst("scope")?.Value?.Split(' '),
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };

            _logger.LogInformation("Successfully retrieved user info for user: {UserId}", userInfo.UserId);
            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user information");
            return Problem("An error occurred while retrieving user information");
        }
    }

    /// <summary>
    /// Test endpoint to validate authentication and authorization
    /// </summary>
    /// <returns>Success message with timestamp</returns>
    [HttpGet("test")]
    public ActionResult<object> TestAuthentication()
    {
        var response = new
        {
            Message = "Authentication successful!",
            Timestamp = DateTime.UtcNow,
            User = User.Identity?.Name ?? "Unknown",
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            AuthenticationType = User.Identity?.AuthenticationType
        };

        _logger.LogInformation("Test authentication endpoint called by user: {User}", User.Identity?.Name);
        return Ok(response);
    }
}
