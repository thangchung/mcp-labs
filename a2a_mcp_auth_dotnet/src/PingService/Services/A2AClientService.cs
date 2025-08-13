using A2A;
using PingService.Models;
using System.Text.Json;

namespace PingService.Services;

public class A2AClientService : IA2AClientService
{
    private readonly ILogger<A2AClientService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _pongServiceUrl;

    public A2AClientService(ILogger<A2AClientService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _pongServiceUrl = configuration["PongService:Url"] ?? "http://localhost:5001";
    }

    public async Task<A2AServiceResponse> SendMessageAsync(string message, string jwtToken, string userEmail)
    {
        try
        {
            _logger.LogInformation("Initiating A2A protocol communication for user: {UserEmail}", userEmail);

            // Create HTTP client with proper authentication headers
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");

            // Create A2A client with authenticated HTTP client
            var a2aClient = new A2AClient(new Uri($"{_pongServiceUrl}/pong"), httpClient);

            // Create A2A message with minimal metadata (authentication is in HTTP headers now)
            var a2aMessage = new Message
            {
                Role = MessageRole.User,
                MessageId = Guid.NewGuid().ToString(),
                ContextId = Guid.NewGuid().ToString(),
                Parts = [new TextPart { Text = message }],
                Metadata = new Dictionary<string, JsonElement>
                {
                    ["user_email"] = JsonSerializer.SerializeToElement(userEmail),
                    ["user_id"] = JsonSerializer.SerializeToElement(userEmail),
                    ["timestamp"] = JsonSerializer.SerializeToElement(DateTime.UtcNow.ToString("O"))
                }
            };

            // Create MessageSendParams for A2A protocol
            var messageSendParams = new MessageSendParams
            {
                Message = a2aMessage,
                Configuration = new MessageSendConfiguration
                {
                    AcceptedOutputModes = ["text"],
                    Blocking = true
                }
            };

            _logger.LogInformation("Sending A2A message with authentication in HTTP headers");

            // Send message via A2A protocol with authenticated HTTP client
            var response = await a2aClient.SendMessageAsync(messageSendParams);

            if (response is AgentTask task)
            {
                _logger.LogInformation("Received A2A task response with ID: {TaskId}", task.Id);

                return new A2AServiceResponse
                {
                    Success = true,
                    Message = "A2A task created successfully",
                    Data = new
                    {
                        TaskId = task.Id,
                        Status = task.Status.State.ToString(),
                        Response = task.Artifacts?.FirstOrDefault()?.Parts?.OfType<TextPart>()?.FirstOrDefault()?.Text ?? "Task created"
                    }
                };
            }
            else if (response is Message messageResponse)
            {
                _logger.LogInformation("Received A2A message response");

                var responseText = messageResponse.Parts?.OfType<TextPart>()?.FirstOrDefault()?.Text ?? "No response content";

                return new A2AServiceResponse
                {
                    Success = true,
                    Message = "A2A message sent successfully",
                    Data = new
                    {
                        Response = responseText,
                        MessageId = messageResponse.MessageId
                    }
                };
            }
            else
            {
                _logger.LogWarning("Unexpected A2A response type: {ResponseType}", response?.GetType().Name ?? "null");
                
                return new A2AServiceResponse
                {
                    Success = false,
                    Message = "Unexpected response type from A2A protocol",
                    Error = $"Unknown response format: {response?.GetType().Name ?? "null"}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A2A protocol communication failed for user: {UserEmail}", userEmail);
            
            return new A2AServiceResponse
            {
                Success = false,
                Message = "A2A protocol communication failed",
                Error = ex.Message
            };
        }
    }
}
