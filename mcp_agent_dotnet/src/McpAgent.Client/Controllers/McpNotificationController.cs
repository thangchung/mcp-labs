using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using McpAgent.Client.Hubs;
using McpAgent.Client.Models;

namespace McpAgent.Client.Controllers;

/// <summary>
/// Controller to handle MCP notifications from the server
/// </summary>
[ApiController]
[Route("mcp")]
public class McpNotificationController : ControllerBase
{
    private readonly ILogger<McpNotificationController> _logger;
    private readonly IHubContext<McpNotificationHub> _hubContext;

    public McpNotificationController(ILogger<McpNotificationController> logger, IHubContext<McpNotificationHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Handle all MCP notification requests from the server
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleMcpNotification()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            
            _logger.LogDebug("📨 Received MCP notification: {RequestBody}", requestBody);

            // Parse the JSON-RPC request
            var jsonDoc = JsonDocument.Parse(requestBody);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("method", out var methodElement))
            {
                var method = methodElement.GetString();
                _logger.LogInformation("🔔 MCP notification method: {Method}", method);

                switch (method)
                {
                    case "notifications/progress":
                        await HandleProgressNotification(root);
                        break;
                    case "notifications/log":
                        await HandleLogNotification(root);
                        break;
                    case "sampling/createMessage":
                    case "elicitation/request":
                        return await HandleElicitationRequest(root);
                    default:
                        _logger.LogWarning("⚠️ Unknown MCP notification method: {Method}", method);
                        break;
                }
            }

            // Return success response
            return Ok(new { jsonrpc = "2.0", result = new { } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error handling MCP notification");
            return StatusCode(500, new { 
                jsonrpc = "2.0", 
                error = new { 
                    code = -32603, 
                    message = "Internal error",
                    data = ex.Message 
                } 
            });
        }
    }

    private async Task HandleProgressNotification(JsonElement root)
    {
        try
        {
            if (root.TryGetProperty("params", out var paramsElement))
            {
                var progress = 0;
                var total = 100;
                var message = "Processing...";
                var progressToken = "";

                if (paramsElement.TryGetProperty("progress", out var progressElement))
                    progress = progressElement.GetInt32();
                
                if (paramsElement.TryGetProperty("total", out var totalElement))
                    total = totalElement.GetInt32();
                
                if (paramsElement.TryGetProperty("message", out var messageElement))
                    message = messageElement.GetString() ?? "Processing...";

                if (paramsElement.TryGetProperty("progressToken", out var tokenElement))
                    progressToken = tokenElement.GetString() ?? "";

                _logger.LogInformation("📊 Progress: {Progress}/{Total} - {Message}", progress, total, message);

                // Send to UI via SignalR
                var notification = new McpProgressNotification
                {
                    Progress = progress,
                    Total = total,
                    Message = message,
                    ProgressToken = progressToken
                };

                await _hubContext.Clients.All.SendAsync("ProgressUpdate", notification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error parsing progress notification");
        }
    }

    private async Task HandleLogNotification(JsonElement root)
    {
        try
        {
            if (root.TryGetProperty("params", out var paramsElement))
            {
                var level = "info";
                var message = "No message";
                var logger = "";

                if (paramsElement.TryGetProperty("level", out var levelElement))
                    level = levelElement.GetString() ?? "info";
                
                if (paramsElement.TryGetProperty("data", out var dataElement))
                    message = dataElement.GetString() ?? "No message";
                else if (paramsElement.TryGetProperty("message", out var messageElement))
                    message = messageElement.GetString() ?? "No message";

                if (paramsElement.TryGetProperty("logger", out var loggerElement))
                    logger = loggerElement.GetString() ?? "";

                _logger.LogInformation("📝 Server log [{Level}]: {Message}", level.ToUpper(), message);

                // Send to UI via SignalR
                var notification = new McpLogNotification
                {
                    Level = level,
                    Message = message,
                    Logger = logger
                };

                await _hubContext.Clients.All.SendAsync("LogUpdate", notification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error parsing log notification");
        }
    }

    private async Task<IActionResult> HandleElicitationRequest(JsonElement root)
    {
        try
        {
            _logger.LogInformation("❓ Received elicitation request from server");
            
            var prompt = "Please provide input";
            var options = new List<string>();
            
            if (root.TryGetProperty("params", out var paramsElement))
            {
                if (paramsElement.TryGetProperty("prompt", out var promptElement))
                    prompt = promptElement.GetString() ?? prompt;
                
                if (paramsElement.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                {
                    options = optionsElement.EnumerateArray()
                        .Select(o => o.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
            }

            var elicitationRequest = new McpSamplingRequest
            {
                Id = Guid.NewGuid().ToString(),
                Prompt = prompt,
                Options = options,
                RequestedAt = DateTime.UtcNow
            };

            _logger.LogInformation("🎯 Broadcasting elicitation request: {Prompt}", prompt);
            
            // Broadcast to all connected clients via SignalR
            await _hubContext.Clients.All.SendAsync("ElicitationRequest", elicitationRequest);

            // Return success response
            var response = new
            {
                jsonrpc = "2.0",
                id = root.TryGetProperty("id", out var idElement) ? idElement.GetString() : "unknown",
                result = new
                {
                    status = "broadcasted",
                    requestId = elicitationRequest.Id
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error handling elicitation request");
            return StatusCode(500, new { 
                jsonrpc = "2.0", 
                error = new { 
                    code = -32603, 
                    message = "Internal error",
                    data = ex.Message 
                } 
            });
        }
    }
}
