using A2A;
using PongService.Services;
using System.Diagnostics;
using System.Text.Json;
using System.Security.Claims;

namespace PongService.Agents;

/// <summary>
/// A2A Agent that receives ping messages and processes them via MCP server for admin users
/// </summary>
public class PongAgent
{
    private ITaskManager? _taskManager;
    private readonly IMcpClientService _mcpClientService;
    private readonly ILogger<PongAgent> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public static readonly ActivitySource ActivitySource = new("A2A.PongAgent", "1.0.0");

    public PongAgent(IMcpClientService mcpClientService, ILogger<PongAgent> logger, IHttpContextAccessor httpContextAccessor)
    {
        _mcpClientService = mcpClientService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Attach(ITaskManager taskManager)
    {
        _taskManager = taskManager;
        _taskManager.OnTaskCreated = OnTaskCreatedAsync;
        _taskManager.OnTaskUpdated = OnTaskUpdatedAsync;
        _taskManager.OnAgentCardQuery = GetAgentCardAsync;
    }

    private async Task OnTaskCreatedAsync(AgentTask task, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("OnTaskCreated", ActivityKind.Server);
        activity?.SetTag("task.id", task.Id);

        _logger.LogInformation("Task created with ID: {TaskId}", task.Id);
        await ProcessTaskAsync(task, cancellationToken);
    }

    private async Task OnTaskUpdatedAsync(AgentTask task, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("OnTaskUpdated", ActivityKind.Server);
        activity?.SetTag("task.id", task.Id);

        _logger.LogInformation("Task updated with ID: {TaskId}", task.Id);
        await ProcessTaskAsync(task, cancellationToken);
    }

    private async Task ProcessTaskAsync(AgentTask task, CancellationToken cancellationToken)
    {
        if (_taskManager == null)
        {
            throw new InvalidOperationException("TaskManager is not attached.");
        }

        try
        {
            // Extract the message from task history
            var lastMessage = task.History?.LastOrDefault();
            if (lastMessage?.Parts == null)
            {
                await _taskManager.UpdateStatusAsync(
                    task.Id,
                    TaskState.Failed,
                    new Message
                    {
                        Parts = [new TextPart { Text = "No message content found in task" }]
                    },
                    final: true,
                    cancellationToken: cancellationToken);
                return;
            }

            var messageText = lastMessage.Parts.OfType<TextPart>().FirstOrDefault()?.Text;
            if (string.IsNullOrEmpty(messageText))
            {
                await _taskManager.UpdateStatusAsync(
                    task.Id,
                    TaskState.Failed,
                    new Message
                    {
                        Parts = [new TextPart { Text = "No text content found in message" }]
                    },
                    final: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Extract metadata for JWT token and user email from HttpContext instead of message metadata
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                await _taskManager.UpdateStatusAsync(
                    task.Id,
                    TaskState.Failed,
                    new Message
                    {
                        Parts = [new TextPart { Text = "User is not authenticated" }]
                    },
                    final: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Extract JWT token from Authorization header
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            string? jwtToken = null;
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                jwtToken = authHeader.Substring("Bearer ".Length).Trim();
            }

            // Get user email from claims
            var userEmail = httpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value;

            if (string.IsNullOrEmpty(jwtToken) || string.IsNullOrEmpty(userEmail))
            {
                await _taskManager.UpdateStatusAsync(
                    task.Id,
                    TaskState.Failed,
                    new Message
                    {
                        Parts = [new TextPart { Text = $"Missing authentication information - JWT token: {(jwtToken != null ? "present" : "missing")}, User email: {(userEmail != null ? "present" : "missing")}" }]
                    },
                    final: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Update task status to Working
            await _taskManager.UpdateStatusAsync(
                task.Id,
                TaskState.Working,
                new Message
                {
                    Parts = [new TextPart { Text = $"Processing ping message and communicating with MCP server: {messageText}" }]
                },
                cancellationToken: cancellationToken);

            // Call MCP Server directly
            _logger.LogInformation("Processing ping message via MCP Server for user: {UserEmail}", userEmail);
            var mcpResponse = await _mcpClientService.CallMcpServerAsync(jwtToken, messageText, userEmail);

            _logger.LogInformation("MCP call completed - Success: {Success}, AdminAccess: {AdminAccess}", 
                mcpResponse.Success, mcpResponse.AdminAccess);

            // Build a clean, readable response
            var responseMessage = $"Pong! Message '{messageText}' processed successfully via MCP Server";
            if (mcpResponse.Success && !string.IsNullOrEmpty(mcpResponse.ResponseContent))
            {
                // Try to parse the MCP response content to extract meaningful information
                try
                {
                    var mcpContent = JsonSerializer.Deserialize<JsonElement>(mcpResponse.ResponseContent);
                    if (mcpContent.TryGetProperty("action", out var action) && action.GetString() == "Pong Response")
                    {
                        if (mcpContent.TryGetProperty("enhancedMessage", out var enhanced))
                        {
                            responseMessage = $"Pong! {enhanced.GetString()}";
                        }
                    }
                }
                catch
                {
                    // If parsing fails, use the original response content
                    responseMessage = $"Pong! {mcpResponse.ResponseContent}";
                }
            }

            // Return the response as a simple, clean artifact
            await _taskManager.ReturnArtifactAsync(task.Id, new Artifact
            {
                Parts = [new TextPart { 
                    Text = responseMessage
                }]
            }, cancellationToken);

            // Complete the task
            await _taskManager.UpdateStatusAsync(
                task.Id,
                TaskState.Completed,
                new Message
                {
                    Parts = [new TextPart { Text = "Ping message processed successfully via MCP server" }]
                },
                final: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Task {TaskId} completed successfully", task.Id);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt for task {TaskId}", task.Id);
            
            await _taskManager.UpdateStatusAsync(
                task.Id,
                TaskState.Failed,
                new Message
                {
                    Parts = [new TextPart { Text = $"Unauthorized: {ex.Message}" }]
                },
                final: true,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing task {TaskId}", task.Id);
            
            await _taskManager.UpdateStatusAsync(
                task.Id,
                TaskState.Failed,
                new Message
                {
                    Parts = [new TextPart { Text = $"Error processing ping message: {ex.Message}" }]
                },
                final: true,
                cancellationToken: cancellationToken);
        }
    }

    private Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AgentCard>(cancellationToken);
        }

        var capabilities = new AgentCapabilities
        {
            Streaming = true,
            PushNotifications = false,
        };

        // Note: Authentication is implemented at the HTTP transport level using Microsoft Entra ID
        // JWT Bearer tokens are required for all endpoints and are validated by the middleware
        // The authentication scheme used is "Bearer" with JWT tokens containing required scopes
        return Task.FromResult(new AgentCard
        {
            Name = "Pong Service Agent",
            Description = "A2A server agent that processes ping messages and integrates with MCP server for admin users. " +
                         "AUTHENTICATION REQUIRED: This agent requires Microsoft Entra ID JWT Bearer token authentication " +
                         "with 'access_as_user' scope. All requests must include valid JWT tokens in the Authorization header.",
            Url = agentUrl,
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [
                new AgentSkill
                {
                    Name = "process_ping",
                    Description = "Process ping messages and communicate with MCP server for admin users. " +
                                 "Requires JWT authentication with admin role and 'access_as_user' scope."
                }
            ],
        });
    }
}
