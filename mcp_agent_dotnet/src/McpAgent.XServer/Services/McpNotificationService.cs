using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace McpAgent.XServer.Services;

/// <summary>
/// Service for handling MCP notifications
/// </summary>
public class McpNotificationService
{
    private readonly ILogger<McpNotificationService> _logger;
    private readonly ConcurrentQueue<McpNotification> _notifications;
    private readonly List<INotificationHandler> _handlers;
    private int _notificationId;

    public McpNotificationService(ILogger<McpNotificationService> logger)
    {
        _logger = logger;
        _notifications = new ConcurrentQueue<McpNotification>();
        _handlers = new List<INotificationHandler>();
        _notificationId = 0;
    }

    /// <summary>
    /// Add a notification to the system
    /// </summary>
    public async Task AddNotificationAsync(string message, NotificationType type, Dictionary<string, object?>? metadata = null)
    {
        var notification = new McpNotification
        {
            Id = Interlocked.Increment(ref _notificationId),
            Message = message,
            Type = type,
            Timestamp = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object?>(),
            IsRead = false
        };

        _notifications.Enqueue(notification);
        _logger.LogInformation("📬 Notification #{Id} added: {Type} - {Message}", notification.Id, type, message);

        // Notify all handlers
        await NotifyHandlersAsync(notification);
    }

    /// <summary>
    /// Register a notification handler
    /// </summary>
    public void RegisterHandler(INotificationHandler handler)
    {
        _handlers.Add(handler);
        _logger.LogInformation("📬 Notification handler registered: {HandlerType}", handler.GetType().Name);
    }

    /// <summary>
    /// Get all notifications
    /// </summary>
    public IEnumerable<McpNotification> GetNotifications()
    {
        return _notifications.ToArray().OrderByDescending(n => n.Timestamp);
    }

    /// <summary>
    /// Get unread notifications
    /// </summary>
    public IEnumerable<McpNotification> GetUnreadNotifications()
    {
        return _notifications.Where(n => !n.IsRead).OrderByDescending(n => n.Timestamp);
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public async Task MarkAsReadAsync(int notificationId)
    {
        var notifications = _notifications.ToArray();
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
        
        if (notification != null)
        {
            notification.IsRead = true;
            _logger.LogInformation("📬 Notification #{Id} marked as read", notificationId);
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    public async Task MarkAllAsReadAsync()
    {
        var notifications = _notifications.ToArray();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }
        
        _logger.LogInformation("📬 All {Count} notifications marked as read", notifications.Length);
    }

    /// <summary>
    /// Clear all notifications
    /// </summary>
    public async Task ClearAllNotificationsAsync()
    {
        var count = _notifications.Count;
        while (_notifications.TryDequeue(out _)) { }
        
        _logger.LogInformation("📬 All {Count} notifications cleared", count);
    }

    /// <summary>
    /// Get notification statistics
    /// </summary>
    public NotificationStatistics GetStatistics()
    {
        var notifications = _notifications.ToArray();
        
        return new NotificationStatistics
        {
            TotalNotifications = notifications.Length,
            UnreadNotifications = notifications.Count(n => !n.IsRead),
            NotificationsByType = notifications
                .GroupBy(n => n.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            LatestNotification = notifications.OrderByDescending(n => n.Timestamp).FirstOrDefault()
        };
    }

    /// <summary>
    /// Notify all registered handlers about a new notification
    /// </summary>
    private async Task NotifyHandlersAsync(McpNotification notification)
    {
        var tasks = _handlers.Select(handler => NotifyHandlerSafelyAsync(handler, notification));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Safely notify a handler (with error handling)
    /// </summary>
    private async Task NotifyHandlerSafelyAsync(INotificationHandler handler, McpNotification notification)
    {
        try
        {
            await handler.HandleNotificationAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in notification handler {HandlerType}", handler.GetType().Name);
        }
    }
}

/// <summary>
/// Represents an MCP notification
/// </summary>
public class McpNotification
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = new();
}

/// <summary>
/// Notification types
/// </summary>
public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success,
    Progress,
    UserAction,
    SystemEvent,
    AIAnalysis,
    Sampling,
    Elicitation,
    ToolExecution,
    SessionEvent
}

/// <summary>
/// Interface for notification handlers
/// </summary>
public interface INotificationHandler
{
    Task HandleNotificationAsync(McpNotification notification);
}

/// <summary>
/// Notification statistics
/// </summary>
public class NotificationStatistics
{
    public int TotalNotifications { get; set; }
    public int UnreadNotifications { get; set; }
    public Dictionary<NotificationType, int> NotificationsByType { get; set; } = new();
    public McpNotification? LatestNotification { get; set; }
}

/// <summary>
/// Console notification handler - logs notifications to console
/// </summary>
public class ConsoleNotificationHandler : INotificationHandler
{
    private readonly ILogger<ConsoleNotificationHandler> _logger;

    public ConsoleNotificationHandler(ILogger<ConsoleNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleNotificationAsync(McpNotification notification)
    {
        var icon = GetNotificationIcon(notification.Type);
        var color = GetNotificationColor(notification.Type);
        
        Console.WriteLine($"{icon} [{notification.Timestamp:HH:mm:ss}] {notification.Message}");
        
        if (notification.Metadata.Any())
        {
            Console.WriteLine($"   Metadata: {string.Join(", ", notification.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        }
        
        await Task.CompletedTask;
    }

    private string GetNotificationIcon(NotificationType type)
    {
        return type switch
        {
            NotificationType.Info => "ℹ️",
            NotificationType.Warning => "⚠️",
            NotificationType.Error => "❌",
            NotificationType.Success => "✅",
            NotificationType.Progress => "⏳",
            NotificationType.UserAction => "👤",
            NotificationType.SystemEvent => "🔧",
            NotificationType.AIAnalysis => "🧠",
            NotificationType.Sampling => "🎲",
            NotificationType.Elicitation => "🎯",
            NotificationType.ToolExecution => "🛠️",
            NotificationType.SessionEvent => "📋",
            _ => "📬"
        };
    }

    private ConsoleColor GetNotificationColor(NotificationType type)
    {
        return type switch
        {
            NotificationType.Error => ConsoleColor.Red,
            NotificationType.Warning => ConsoleColor.Yellow,
            NotificationType.Success => ConsoleColor.Green,
            NotificationType.Info => ConsoleColor.Cyan,
            NotificationType.Progress => ConsoleColor.Blue,
            _ => ConsoleColor.White
        };
    }
}

/// <summary>
/// SignalR notification handler - sends notifications to connected clients
/// </summary>
public class SignalRNotificationHandler : INotificationHandler
{
    private readonly ILogger<SignalRNotificationHandler> _logger;
    // Note: In a real implementation, you'd inject IHubContext<ChatHub> here

    public SignalRNotificationHandler(ILogger<SignalRNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleNotificationAsync(McpNotification notification)
    {
        _logger.LogInformation("📡 Sending notification #{Id} to SignalR clients", notification.Id);
        
        // In a real implementation, you would send to SignalR clients here
        // await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
        
        await Task.CompletedTask;
    }
}
