using Microsoft.Extensions.Logging;
using McpAgent.Client;
using System.Text.Json;
using Xunit.Abstractions;

namespace McpAgent.Client.Tests;

public class SessionManagerTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _testSessionPath;

    public SessionManagerTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Use a test-specific session file
        _testSessionPath = Path.Combine(Path.GetTempPath(), $"test_session_{Guid.NewGuid():N}.json");
    }

    [Fact]
    public async Task SaveAndLoadSession_ShouldPersistSessionInfo()
    {
        // Arrange
        var sessionManager = new TestSessionManager(_loggerFactory, _testSessionPath);
        var toolName = "test_tool";
        var args = new Dictionary<string, object?> { { "param1", "value1" }, { "param2", 42 } };

        try
        {
            // Act - Save session
            await sessionManager.SaveSessionAsync(toolName, args);

            // Assert - Session file should exist
            Assert.True(File.Exists(_testSessionPath));

            // Act - Load session
            var loadedSession = await sessionManager.LoadExistingSessionAsync();

            // Assert - Session should be loaded correctly
            Assert.NotNull(loadedSession);
            Assert.Equal(toolName, loadedSession.LastTool);
            Assert.Equal("value1", loadedSession.LastArgs["param1"]);
            Assert.Equal(42, ((JsonElement)loadedSession.LastArgs["param2"]!).GetInt32());
            Assert.True(DateTimeOffset.UtcNow - loadedSession.Timestamp < TimeSpan.FromMinutes(1));
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testSessionPath))
            {
                File.Delete(_testSessionPath);
            }
        }
    }

    [Fact]
    public async Task LoadExistingSession_WithNoFile_ShouldReturnNull()
    {
        // Arrange
        var sessionManager = new TestSessionManager(_loggerFactory, _testSessionPath);

        // Act
        var session = await sessionManager.LoadExistingSessionAsync();

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public async Task LoadExistingSession_WithOldSession_ShouldReturnNullAndCleanup()
    {
        // Arrange
        var sessionManager = new TestSessionManager(_loggerFactory, _testSessionPath);
        var oldSession = new SessionInfo
        {
            SessionId = Guid.NewGuid().ToString(),
            LastTool = "old_tool",
            LastArgs = new Dictionary<string, object?>(),
            Timestamp = DateTimeOffset.UtcNow.AddDays(-2) // 2 days old
        };

        // Create old session file
        var json = JsonSerializer.Serialize(oldSession);
        await File.WriteAllTextAsync(_testSessionPath, json);

        try
        {
            // Act
            var session = await sessionManager.LoadExistingSessionAsync();

            // Assert
            Assert.Null(session);
            Assert.False(File.Exists(_testSessionPath)); // Should be cleaned up
        }
        finally
        {
            // Cleanup
            if (File.Exists(_testSessionPath))
            {
                File.Delete(_testSessionPath);
            }
        }
    }

    [Fact]
    public async Task ClearSession_ShouldDeleteSessionFile()
    {
        // Arrange
        var sessionManager = new TestSessionManager(_loggerFactory, _testSessionPath);
        await sessionManager.SaveSessionAsync("test_tool", new Dictionary<string, object?>());
        
        Assert.True(File.Exists(_testSessionPath));

        // Act
        await sessionManager.ClearSessionAsync();

        // Assert
        Assert.False(File.Exists(_testSessionPath));
    }

    /// <summary>
    /// Test-specific SessionManager that allows custom file path
    /// </summary>
    private class TestSessionManager : SessionManager
    {
        private readonly string _customPath;

        public TestSessionManager(ILoggerFactory loggerFactory, string customPath) 
            : base(loggerFactory)
        {
            _customPath = customPath;
        }

        // Override the file path for testing
        public new async Task SaveSessionAsync(string toolName, Dictionary<string, object?> args)
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

            await File.WriteAllTextAsync(_customPath, json);
        }

        public new async Task<SessionInfo?> LoadExistingSessionAsync()
        {
            if (!File.Exists(_customPath))
                return null;

            var json = await File.ReadAllTextAsync(_customPath);
            var sessionInfo = JsonSerializer.Deserialize<SessionInfo>(json);

            if (sessionInfo != null && 
                DateTimeOffset.UtcNow - sessionInfo.Timestamp < TimeSpan.FromHours(24))
            {
                return sessionInfo;
            }

            await ClearSessionAsync();
            return null;
        }

        public new async Task ClearSessionAsync()
        {
            if (File.Exists(_customPath))
            {
                File.Delete(_customPath);
            }
            await Task.CompletedTask;
        }
    }
}
