namespace McpAgent.Core.Events;

/// <summary>
/// Represents an event callback function for event replay.
/// </summary>
/// <param name="eventData">The event data to process.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task EventCallback(object eventData);

/// <summary>
/// Interface for event store operations supporting session resumption.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Stores an event in the specified stream and returns the event ID.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="eventData">The event data to store.</param>
    /// <returns>The unique event identifier.</returns>
    Task<string> StoreEventAsync(string streamId, object eventData);

    /// <summary>
    /// Replays events after the specified event ID for session resumption.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="lastEventId">The last known event ID. If null, replays all events.</param>
    /// <param name="callback">The callback to invoke for each event.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReplayEventsAfterAsync(string streamId, string? lastEventId, EventCallback callback);

    /// <summary>
    /// Gets the last event ID for the specified stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>The last event ID, or null if no events exist.</returns>
    Task<string?> GetLastEventIdAsync(string streamId);

    /// <summary>
    /// Gets all events for the specified stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>The collection of events for the stream.</returns>
    Task<IEnumerable<object>> GetEventsAsync(string streamId);
}
