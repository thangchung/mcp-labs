using System.Collections.Concurrent;
using System.Text.Json;

namespace McpAgent.Core.Events;

/// <summary>
/// Simple in-memory event store implementation for MCP session resumption.
/// Based on the Python implementation from the MCP agents sample.
/// </summary>
public class SimpleEventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, List<StoredEvent>> _streams = new();
    private long _eventIdCounter = 0;

    /// <inheritdoc />
    public Task<string> StoreEventAsync(string streamId, object eventData)
    {
        if (string.IsNullOrEmpty(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(streamId));

        var eventId = Interlocked.Increment(ref _eventIdCounter).ToString();
        var storedEvent = new StoredEvent(eventId, eventData, DateTimeOffset.UtcNow);

        _streams.AddOrUpdate(
            streamId,
            [storedEvent],
            (_, existingEvents) =>
            {
                lock (existingEvents)
                {
                    existingEvents.Add(storedEvent);
                    return existingEvents;
                }
            });

        return Task.FromResult(eventId);
    }

    /// <inheritdoc />
    public Task ReplayEventsAfterAsync(string streamId, string? lastEventId, EventCallback callback)
    {
        if (string.IsNullOrEmpty(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(streamId));

        if (!_streams.TryGetValue(streamId, out var events))
            return Task.CompletedTask;

        lock (events)
        {
            var eventsToReplay = lastEventId == null
                ? events
                : events.SkipWhile(e => e.EventId != lastEventId).Skip(1);

            foreach (var storedEvent in eventsToReplay)
            {
                callback(storedEvent.EventData);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetLastEventIdAsync(string streamId)
    {
        if (string.IsNullOrEmpty(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(streamId));

        if (!_streams.TryGetValue(streamId, out var events))
            return Task.FromResult<string?>(null);

        lock (events)
        {
            return Task.FromResult(events.LastOrDefault()?.EventId);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<object>> GetEventsAsync(string streamId)
    {
        if (string.IsNullOrEmpty(streamId))
            throw new ArgumentException("Stream ID cannot be null or empty.", nameof(streamId));

        if (!_streams.TryGetValue(streamId, out var events))
            return Task.FromResult(Enumerable.Empty<object>());

        lock (events)
        {
            return Task.FromResult(events.Select(e => e.EventData));
        }
    }

    /// <summary>
    /// Gets the total number of events across all streams.
    /// </summary>
    public int TotalEventCount => _streams.Values.Sum(events =>
    {
        lock (events)
        {
            return events.Count;
        }
    });

    /// <summary>
    /// Gets the number of streams.
    /// </summary>
    public int StreamCount => _streams.Count;

    /// <summary>
    /// Clears all events from the store.
    /// </summary>
    public void Clear()
    {
        _streams.Clear();
        Interlocked.Exchange(ref _eventIdCounter, 0);
    }

    private record StoredEvent(string EventId, object EventData, DateTimeOffset Timestamp);
}
