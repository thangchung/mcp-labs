using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServer.Tools;

[McpServerToolType]
public sealed class McpTools
{
    [McpServerTool, Description("Process ping messages and return enhanced pong responses")]
    public static string PingProcessor(
        [Description("The message to enhance")] string message,
        [Description("User email address")] string userEmail = "",
        [Description("Whether user has admin privileges")] bool isAdmin = false,
        [Description("Request timestamp")] string timestamp = "")
    {
        // Simply concatenate an enhancing string to the message
        var enhancedMessage = $"Enhanced: {message} - Processed with love!";
        
        var response = new
        {
            Action = "Pong Response",
            OriginalMessage = message,
            EnhancedMessage = enhancedMessage,
            ProcessedBy = "McpServer",
            UserEmail = userEmail,
            AdminAccess = isAdmin,
            ProcessedAt = DateTime.UtcNow.ToString("O"),
            RequestTimestamp = timestamp,
            Status = "Success"
        };

        return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
}
