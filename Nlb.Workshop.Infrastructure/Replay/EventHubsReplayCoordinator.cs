using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Options;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Application.Models;
using Nlb.Workshop.Infrastructure.Messaging.Common;
using Nlb.Workshop.Infrastructure.Options;

namespace Nlb.Workshop.Infrastructure.Replay;

public sealed class EventHubsReplayCoordinator : IReplayCoordinator
{
  private readonly EventHubsOptions _eventHubsOptions;
  private readonly ReplayOptions _replayOptions;
  private readonly string _defaultPayloadFormat;

  public EventHubsReplayCoordinator(IOptions<MessagingOptions> messagingOptions)
  {
    var options = messagingOptions.Value;
    _eventHubsOptions = options.EventHubs;
    _replayOptions = options.Replay;
    _defaultPayloadFormat = options.Serialization.DefaultFormat;
  }

  public async Task<ReplayResult> ReplayAsync(Func<ConsumedEventContext, CancellationToken, Task> handleEventAsync,
        CancellationToken cancellationToken = default)
  {
    await using var consumerClient = new EventHubConsumerClient(
      _eventHubsOptions.ReplayConsumerGroup,
      _eventHubsOptions.ConnectionString,
      _eventHubsOptions.EventHubName);

    var processedEvents = 0;
    var partitionsVisited = 0;
    var partitionIds = await consumerClient.GetPartitionIdsAsync(cancellationToken);

    foreach (var partitionId in partitionIds)
    {
      partitionsVisited++;
      var consecutiveEmptyReads = 0;

      var readOptions = new ReadEventOptions
      {
        MaximumWaitTime = TimeSpan.FromSeconds(Math.Max(1, _replayOptions.MaximumWaitTimeSeconds))
      };

      await foreach (var partitionEvent in consumerClient.ReadEventsFromPartitionAsync(partitionId,
        EventPosition.Earliest,
        readOptions,
        cancellationToken))
      {
        if (partitionEvent.Data is null)
        {
          consecutiveEmptyReads++;
          if (consecutiveEmptyReads >= Math.Max(1, _replayOptions.ConsecutiveEmptyReadsToStop))
            break;

          continue;
        }

        consecutiveEmptyReads = 0;

        var context = new ConsumedEventContext(partitionEvent.Data.EventBody.ToArray(),
              GetStringProperty(partitionEvent.Data.Properties, EventHeaderNames.EventType, "unknown"),
              GetIntProperty(partitionEvent.Data.Properties, EventHeaderNames.EventVersion, 1),
              GetStringProperty(partitionEvent.Data.Properties, EventHeaderNames.PartitionKey, string.Empty),
              partitionId,
              ParseOffset(partitionEvent.Data.OffsetString),
              GetStringProperty(partitionEvent.Data.Properties, EventHeaderNames.PayloadFormat, _defaultPayloadFormat),
              GetNullableStringProperty(partitionEvent.Data.Properties, EventHeaderNames.CorrelationId));

        await handleEventAsync(context, cancellationToken);
        processedEvents++;
      }
    }

    return new ReplayResult(processedEvents, partitionsVisited);
  }

  private static string GetStringProperty(IDictionary<string, object> properties,
          string key,
          string fallback)
  {
    if (!properties.TryGetValue(key, out var value))
      return fallback;

    return value?.ToString() ?? fallback;
  }

  private static string? GetNullableStringProperty(IDictionary<string, object> properties, string key)
  {
    if (!properties.TryGetValue(key, out var value))
      return null;

    return value?.ToString();
  }

  private static int GetIntProperty(IDictionary<string, object> properties, string key, int fallback)
  {
    if (!properties.TryGetValue(key, out var value))
      return fallback;

    return value switch
    {
      int intValue => intValue,
      long longValue => (int)longValue,
      string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
      _ => fallback
    };
  }

  private static long? ParseOffset(string? value)
  {
    if (long.TryParse(value, out var parsed))
      return parsed;

    return null;
  }
}