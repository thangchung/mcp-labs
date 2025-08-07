using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace McpAgent.XServer.Services;

/// <summary>
/// Service for handling MCP progress updates
/// </summary>
public class McpProgressService
{
    private readonly ILogger<McpProgressService> _logger;
    private readonly ConcurrentDictionary<string, ProgressContext> _activeProgress;
    private readonly List<IProgressHandler> _handlers;

    public McpProgressService(ILogger<McpProgressService> logger)
    {
        _logger = logger;
        _activeProgress = new ConcurrentDictionary<string, ProgressContext>();
        _handlers = new List<IProgressHandler>();
    }

    /// <summary>
    /// Register a progress handler
    /// </summary>
    public void RegisterHandler(IProgressHandler handler)
    {
        _handlers.Add(handler);
        _logger.LogInformation("📊 Progress handler registered: {HandlerType}", handler.GetType().Name);
    }

    /// <summary>
    /// Start a new progress tracking context
    /// </summary>
    public async Task<string> StartProgressAsync(string operationName, int totalSteps = 100, string? description = null)
    {
        var progressId = Guid.NewGuid().ToString("N")[..8];
        var context = new ProgressContext
        {
            Id = progressId,
            OperationName = operationName,
            Description = description,
            TotalSteps = totalSteps,
            CurrentStep = 0,
            StartTime = DateTime.UtcNow,
            Status = ProgressStatus.InProgress
        };

        _activeProgress.TryAdd(progressId, context);
        _logger.LogInformation("📊 Progress tracking started: {OperationName} (ID: {ProgressId})", operationName, progressId);

        await NotifyHandlersAsync(context, ProgressEventType.Started);
        return progressId;
    }

    /// <summary>
    /// Update progress for an existing context
    /// </summary>
    public async Task UpdateProgressAsync(string progressId, int currentStep, string? statusMessage = null, Dictionary<string, object?>? metadata = null)
    {
        if (_activeProgress.TryGetValue(progressId, out var context))
        {
            context.CurrentStep = Math.Min(currentStep, context.TotalSteps);
            context.LastUpdated = DateTime.UtcNow;
            context.StatusMessage = statusMessage;
            
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    context.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var percentage = (double)context.CurrentStep / context.TotalSteps * 100;
            _logger.LogInformation("📊 Progress update: {OperationName} - {Percentage:F1}% ({CurrentStep}/{TotalSteps})",
                context.OperationName, percentage, context.CurrentStep, context.TotalSteps);

            await NotifyHandlersAsync(context, ProgressEventType.Updated);

            // Auto-complete if we've reached the total steps
            if (context.CurrentStep >= context.TotalSteps && context.Status == ProgressStatus.InProgress)
            {
                await CompleteProgressAsync(progressId, "Operation completed successfully");
            }
        }
        else
        {
            _logger.LogWarning("⚠️ Progress update attempted for unknown ID: {ProgressId}", progressId);
        }
    }

    /// <summary>
    /// Complete a progress tracking context
    /// </summary>
    public async Task CompleteProgressAsync(string progressId, string? completionMessage = null)
    {
        if (_activeProgress.TryGetValue(progressId, out var context))
        {
            context.Status = ProgressStatus.Completed;
            context.CompletedAt = DateTime.UtcNow;
            context.StatusMessage = completionMessage ?? "Operation completed";
            context.CurrentStep = context.TotalSteps; // Ensure we're at 100%

            var duration = context.CompletedAt.Value - context.StartTime;
            _logger.LogInformation("✅ Progress completed: {OperationName} in {Duration:F2}s", 
                context.OperationName, duration.TotalSeconds);

            await NotifyHandlersAsync(context, ProgressEventType.Completed);
            
            // Remove from active progress after a delay to allow handlers to process
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(task => 
                _activeProgress.TryRemove(progressId, out var removed));
        }
        else
        {
            _logger.LogWarning("⚠️ Progress completion attempted for unknown ID: {ProgressId}", progressId);
        }
    }

    /// <summary>
    /// Fail a progress tracking context
    /// </summary>
    public async Task FailProgressAsync(string progressId, string errorMessage, Exception? exception = null)
    {
        if (_activeProgress.TryGetValue(progressId, out var context))
        {
            context.Status = ProgressStatus.Failed;
            context.CompletedAt = DateTime.UtcNow;
            context.StatusMessage = errorMessage;
            context.Error = exception?.ToString();

            var duration = context.CompletedAt.Value - context.StartTime;
            _logger.LogError("❌ Progress failed: {OperationName} after {Duration:F2}s - {ErrorMessage}", 
                context.OperationName, duration.TotalSeconds, errorMessage);

            await NotifyHandlersAsync(context, ProgressEventType.Failed);
            
            // Remove from active progress after a delay
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(task => 
                _activeProgress.TryRemove(progressId, out var removed));
        }
        else
        {
            _logger.LogWarning("⚠️ Progress failure attempted for unknown ID: {ProgressId}", progressId);
        }
    }

    /// <summary>
    /// Get progress context by ID
    /// </summary>
    public ProgressContext? GetProgress(string progressId)
    {
        _activeProgress.TryGetValue(progressId, out var context);
        return context;
    }

    /// <summary>
    /// Get all active progress contexts
    /// </summary>
    public IEnumerable<ProgressContext> GetActiveProgress()
    {
        return _activeProgress.Values.Where(p => p.Status == ProgressStatus.InProgress);
    }

    /// <summary>
    /// Get progress statistics
    /// </summary>
    public ProgressStatistics GetStatistics()
    {
        var contexts = _activeProgress.Values.ToArray();
        
        return new ProgressStatistics
        {
            ActiveOperations = contexts.Count(c => c.Status == ProgressStatus.InProgress),
            CompletedOperations = contexts.Count(c => c.Status == ProgressStatus.Completed),
            FailedOperations = contexts.Count(c => c.Status == ProgressStatus.Failed),
            AverageCompletionTime = contexts
                .Where(c => c.CompletedAt.HasValue)
                .Select(c => (c.CompletedAt!.Value - c.StartTime).TotalSeconds)
                .DefaultIfEmpty(0)
                .Average()
        };
    }

    /// <summary>
    /// Create a progress bar display for console output
    /// </summary>
    public async Task DisplayProgressBarAsync(string progressId, int width = 50)
    {
        if (_activeProgress.TryGetValue(progressId, out var context))
        {
            var percentage = (double)context.CurrentStep / context.TotalSteps;
            var filledWidth = (int)(percentage * width);
            var emptyWidth = width - filledWidth;

            var progressBar = $"[{'█'.ToString().PadLeft(filledWidth, '█')}{'░'.ToString().PadLeft(emptyWidth, '░')}]";
            var percentageText = $"{percentage:P1}";
            var statusText = context.StatusMessage ?? context.OperationName;

            Console.Write($"\r{progressBar} {percentageText} - {statusText}");
            
            if (context.Status != ProgressStatus.InProgress)
            {
                Console.WriteLine(); // New line when completed
            }
        }
    }

    /// <summary>
    /// Notify all registered handlers about progress events
    /// </summary>
    private async Task NotifyHandlersAsync(ProgressContext context, ProgressEventType eventType)
    {
        var tasks = _handlers.Select(handler => NotifyHandlerSafelyAsync(handler, context, eventType));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Safely notify a handler (with error handling)
    /// </summary>
    private async Task NotifyHandlerSafelyAsync(IProgressHandler handler, ProgressContext context, ProgressEventType eventType)
    {
        try
        {
            await handler.HandleProgressAsync(context, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in progress handler {HandlerType}", handler.GetType().Name);
        }
    }
}

/// <summary>
/// Represents a progress tracking context
/// </summary>
public class ProgressContext
{
    public string Id { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalSteps { get; set; }
    public int CurrentStep { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ProgressStatus Status { get; set; }
    public string? StatusMessage { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = new();

    public double Percentage => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;
    public TimeSpan Elapsed => (CompletedAt ?? DateTime.UtcNow) - StartTime;
}

/// <summary>
/// Progress status enumeration
/// </summary>
public enum ProgressStatus
{
    InProgress,
    Completed,
    Failed
}

/// <summary>
/// Progress event types
/// </summary>
public enum ProgressEventType
{
    Started,
    Updated,
    Completed,
    Failed
}

/// <summary>
/// Interface for progress handlers
/// </summary>
public interface IProgressHandler
{
    Task HandleProgressAsync(ProgressContext context, ProgressEventType eventType);
}

/// <summary>
/// Progress statistics
/// </summary>
public class ProgressStatistics
{
    public int ActiveOperations { get; set; }
    public int CompletedOperations { get; set; }
    public int FailedOperations { get; set; }
    public double AverageCompletionTime { get; set; }
}

/// <summary>
/// Console progress handler - displays progress in the console
/// </summary>
public class ConsoleProgressHandler : IProgressHandler
{
    private readonly ILogger<ConsoleProgressHandler> _logger;

    public ConsoleProgressHandler(ILogger<ConsoleProgressHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleProgressAsync(ProgressContext context, ProgressEventType eventType)
    {
        switch (eventType)
        {
            case ProgressEventType.Started:
                Console.WriteLine($"🚀 Started: {context.OperationName}");
                break;
                
            case ProgressEventType.Updated:
                Console.Write($"\r📊 {context.OperationName}: {context.Percentage:F1}%");
                if (!string.IsNullOrEmpty(context.StatusMessage))
                {
                    Console.Write($" - {context.StatusMessage}");
                }
                break;
                
            case ProgressEventType.Completed:
                Console.WriteLine($"\r✅ Completed: {context.OperationName} in {context.Elapsed.TotalSeconds:F2}s");
                break;
                
            case ProgressEventType.Failed:
                Console.WriteLine($"\r❌ Failed: {context.OperationName} after {context.Elapsed.TotalSeconds:F2}s");
                if (!string.IsNullOrEmpty(context.StatusMessage))
                {
                    Console.WriteLine($"   Error: {context.StatusMessage}");
                }
                break;
        }
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// SignalR progress handler - sends progress updates to connected clients
/// </summary>
public class SignalRProgressHandler : IProgressHandler
{
    private readonly ILogger<SignalRProgressHandler> _logger;
    // Note: In a real implementation, you'd inject IHubContext<ChatHub> here

    public SignalRProgressHandler(ILogger<SignalRProgressHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleProgressAsync(ProgressContext context, ProgressEventType eventType)
    {
        _logger.LogInformation("📡 Sending progress update for {OperationName} to SignalR clients", context.OperationName);
        
        // In a real implementation, you would send to SignalR clients here
        // await _hubContext.Clients.All.SendAsync("ReceiveProgress", context);
        
        await Task.CompletedTask;
    }
}
