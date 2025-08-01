using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpAgent.Client;

/// <summary>
/// Manages session state for resumable MCP operations
/// </summary>
public class SessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly string _sessionFilePath;

    public SessionManager(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SessionManager>();
        _sessionFilePath = Path.Combine(Directory.GetCurrentDirectory(), "mcp_session.json");
    }

    /// <summary>
    /// Saves session information for potential resumption
    /// </summary>
    public async Task SaveSessionAsync(string toolName, Dictionary<string, object?> args)
    {
        try
        {
            var sessionInfo = new SessionInfo
            {
                SessionId = Guid.NewGuid().ToString(),
                LastTool = toolName,
                LastArgs = args,
                Timestamp = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(sessionInfo, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_sessionFilePath, json);
            _logger.LogDebug("Session saved: {ToolName}", toolName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save session");
        }
    }

    /// <summary>
    /// Loads existing session information if available
    /// </summary>
    public async Task<SessionInfo?> LoadExistingSessionAsync()
    {
        try
        {
            if (!File.Exists(_sessionFilePath))
                return null;

            var json = await File.ReadAllTextAsync(_sessionFilePath);
            var sessionInfo = JsonSerializer.Deserialize<SessionInfo>(json);

            // Check if session is not too old (avoid resuming very old sessions)
            if (sessionInfo != null && 
                DateTimeOffset.UtcNow - sessionInfo.Timestamp < TimeSpan.FromHours(24))
            {
                _logger.LogDebug("Loaded existing session: {ToolName}", sessionInfo.LastTool);
                return sessionInfo;
            }

            // Remove old session file
            await ClearSessionAsync();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load session");
            return null;
        }
    }

    /// <summary>
    /// Clears the current session
    /// </summary>
    public async Task ClearSessionAsync()
    {
        try
        {
            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
                _logger.LogDebug("Session cleared");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear session");
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Information about a saved session for resumption
/// </summary>
public class SessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string LastTool { get; set; } = string.Empty;
    public Dictionary<string, object?> LastArgs { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; }
}
