using McpAgent.Core.Events;

namespace McpAgent.Core.Tests.Events;

public class SimpleEventStoreTests
{
    [Fact]
    public async Task StoreEvent_ShouldReturnEventId()
    {
        // Arrange
        var eventStore = new SimpleEventStore();
        var streamId = "test-stream";
        var message = new { Type = "test", Data = "test-data" };

        // Act
        var eventId = await eventStore.StoreEventAsync(streamId, message);

        // Assert
        Assert.NotNull(eventId);
        Assert.NotEmpty(eventId);
    }

    [Fact]
    public async Task ReplayEventsAfter_ShouldReplayStoredEvents()
    {
        // Arrange
        var eventStore = new SimpleEventStore();
        var streamId = "test-stream";
        var message1 = new { Type = "test1", Data = "data1" };
        var message2 = new { Type = "test2", Data = "data2" };

        var eventId1 = await eventStore.StoreEventAsync(streamId, message1);
        var eventId2 = await eventStore.StoreEventAsync(streamId, message2);

        var replayedEvents = new List<object>();

        // Act
        await eventStore.ReplayEventsAfterAsync(streamId, eventId1, (evt) =>
        {
            replayedEvents.Add(evt);
            return Task.CompletedTask;
        });

        // Assert
        Assert.Single(replayedEvents);
    }

    [Fact]
    public async Task ReplayEventsAfter_WithNullLastEventId_ShouldReplayAllEvents()
    {
        // Arrange
        var eventStore = new SimpleEventStore();
        var streamId = "test-stream";
        var message1 = new { Type = "test1", Data = "data1" };
        var message2 = new { Type = "test2", Data = "data2" };

        await eventStore.StoreEventAsync(streamId, message1);
        await eventStore.StoreEventAsync(streamId, message2);

        var replayedEvents = new List<object>();

        // Act
        await eventStore.ReplayEventsAfterAsync(streamId, null, (evt) =>
        {
            replayedEvents.Add(evt);
            return Task.CompletedTask;
        });

        // Assert
        Assert.Equal(2, replayedEvents.Count);
    }

    [Fact]
    public async Task GetLastEventId_ShouldReturnLatestEventId()
    {
        // Arrange
        var eventStore = new SimpleEventStore();
        var streamId = "test-stream";
        var message1 = new { Type = "test1", Data = "data1" };
        var message2 = new { Type = "test2", Data = "data2" };

        var eventId1 = await eventStore.StoreEventAsync(streamId, message1);
        var eventId2 = await eventStore.StoreEventAsync(streamId, message2);

        // Act
        var lastEventId = await eventStore.GetLastEventIdAsync(streamId);

        // Assert
        Assert.Equal(eventId2, lastEventId);
    }

    [Fact]
    public async Task GetLastEventId_ForNonExistentStream_ShouldReturnNull()
    {
        // Arrange
        var eventStore = new SimpleEventStore();

        // Act
        var lastEventId = await eventStore.GetLastEventIdAsync("non-existent-stream");

        // Assert
        Assert.Null(lastEventId);
    }
}
