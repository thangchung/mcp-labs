using A2A;
using PingService.Services;
using System.Diagnostics;

namespace PingService.Agents;

/// <summary>
/// A2A Agent that sends ping messages through the A2A protocol to the Pong Service
/// </summary>
public class PingAgent
{
    private ITaskManager? _taskManager;
    private readonly IA2AClientService _a2aClientService;
    private readonly ILogger<PingAgent> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public static readonly ActivitySource ActivitySource = new("A2A.PingAgent", "1.0.0");

    public PingAgent(IA2AClientService a2aClientService, ILogger<PingAgent> logger, IHttpContextAccessor httpContextAccessor)
    {
        _a2aClientService = a2aClientService;
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
                    Parts = [new TextPart { Text = $"Processing ping message via A2A protocol: {messageText}" }]
                },
                cancellationToken: cancellationToken);

            // Send message via A2A protocol to Pong Service
            _logger.LogInformation("Sending A2A message to Pong Service for user: {UserEmail}", userEmail);
            var a2aResponse = await _a2aClientService.SendMessageAsync(messageText, jwtToken, userEmail);

            // Extract the response content in a readable format
            string responseText;
            if (a2aResponse.Success && a2aResponse.Data != null)
            {
                // Try to extract the actual response from the task
                if (a2aResponse.Data.GetType().GetProperty("Response")?.GetValue(a2aResponse.Data) is string taskResponse)
                {
                    responseText = $"Success! Pong Service responded: {taskResponse}";
                }
                else
                {
                    responseText = $"A2A task completed successfully. Task ID: {a2aResponse.Data}";
                }
            }
            else
            {
                responseText = $"A2A communication failed: {a2aResponse.Message ?? "Unknown error"}";
            }

            // Return a clean, readable response
            await _taskManager.ReturnArtifactAsync(task.Id, new Artifact
            {
                Parts = [new TextPart { Text = responseText }]
            }, cancellationToken);

            // Complete the task
            await _taskManager.UpdateStatusAsync(
                task.Id,
                TaskState.Completed,
                new Message
                {
                    Parts = [new TextPart { Text = "Ping message sent successfully via A2A protocol" }]
                },
                final: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Task {TaskId} completed successfully", task.Id);
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
            Name = "Ping Service Agent",
            Description = "A2A client agent that sends ping messages through the A2A protocol to the Pong Service. " +
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
                    Name = "send_ping",
                    Description = "Send ping messages via A2A protocol to Pong Service with MCP integration. " +
                                 "Requires JWT authentication with valid user identity and 'access_as_user' scope."
                }
            ],
        });
    }
}
