using McpAgent.Core.Events;
using Microsoft.Extensions.Logging;

namespace McpAgent.EventStore;

/// <summary>
/// An extended event store implementation that adds management capabilities to the base SimpleEventStore
/// </summary>
public class ManagedEventStore : IEventStore
{
    private readonly SimpleEventStore _innerStore;
    private readonly ILogger<ManagedEventStore>? _logger;

    public ManagedEventStore() 
    {
        _innerStore = new SimpleEventStore();
    }

    public ManagedEventStore(ILogger<ManagedEventStore> logger) 
    {
        _innerStore = new SimpleEventStore();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> StoreEventAsync(string streamId, object eventData)
    {
        _logger?.LogDebug("Storing event {EventType} to stream {StreamId}", eventData.GetType().Name, streamId);
        var eventId = await _innerStore.StoreEventAsync(streamId, eventData);
        _logger?.LogDebug("Stored event {EventId} to stream {StreamId}", eventId, streamId);
        return eventId;
    }

    /// <inheritdoc />
    public async Task ReplayEventsAfterAsync(string streamId, string? lastEventId, EventCallback callback)
    {
        _logger?.LogDebug("Replaying events for stream {StreamId} after event {LastEventId}", streamId, lastEventId);
        await _innerStore.ReplayEventsAfterAsync(streamId, lastEventId, callback);
        _logger?.LogDebug("Completed replaying events for stream {StreamId}", streamId);
    }

    /// <inheritdoc />
    public async Task<string?> GetLastEventIdAsync(string streamId)
    {
        return await _innerStore.GetLastEventIdAsync(streamId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<object>> GetEventsAsync(string streamId)
    {
        _logger?.LogDebug("Retrieving events for stream {StreamId}", streamId);
        var events = await _innerStore.GetEventsAsync(streamId);
        _logger?.LogDebug("Retrieved {EventCount} events for stream {StreamId}", events.Count(), streamId);
        return events;
    }

    /// <summary>
    /// Gets the total number of events across all streams
    /// </summary>
    public int TotalEventCount => _innerStore.TotalEventCount;

    /// <summary>
    /// Gets the number of streams
    /// </summary>
    public int StreamCount => _innerStore.StreamCount;

    /// <summary>
    /// Clears all events from all streams
    /// </summary>
    public Task ClearAllAsync()
    {
        _logger?.LogInformation("Clearing all streams");
        _innerStore.Clear();
        return Task.CompletedTask;
    }
}
