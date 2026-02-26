using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Application.Models;
using Nlb.Workshop.Infrastructure.Messaging.Common;
using Nlb.Workshop.Infrastructure.Options;

namespace Nlb.Workshop.Infrastructure.Messaging.EventHubs;

public sealed class EventHubsEventConsumer : IEventConsumer
{
  private readonly EventHubsOptions _eventHubsOptions;
  private readonly string _defaultPayloadFormat;
  private readonly ILogger<EventHubsEventConsumer> _logger;

  public async Task StartAsync(Func<ConsumedEventContext, CancellationToken, Task> handleEventAsync,
      CancellationToken cancellationToken)
  {
    var checkpointStore = new BlobContainerClient(_eventHubsOptions.CheckpointBlobConnectionString,
      _eventHubsOptions.CheckpointContainerName);
    await checkpointStore.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

    var processor = new EventProcessorClient(
      checkpointStore,
      _eventHubsOptions.ConsumerGroup,
      _eventHubsOptions.ConnectionString,
      _eventHubsOptions.EventHubName
    );

    processor.ProcessEventAsync += async args =>
        {
          if (args.Data is null)
            return;

          var eventContext = new ConsumedEventContext(
              args.Data.EventBody.ToArray(),
              GetStringProperty(args.Data.Properties, EventHeaderNames.EventType, "unknown"),
              GetIntProperty(args.Data.Properties, EventHeaderNames.EventVersion, 1),
              GetStringProperty(args.Data.Properties, EventHeaderNames.PartitionKey, string.Empty),
              args.Partition.PartitionId,
              ParseOffset(args.Data.OffsetString),
              GetStringProperty(args.Data.Properties, EventHeaderNames.PayloadFormat, _defaultPayloadFormat),
              GetNullableStringProperty(args.Data.Properties, EventHeaderNames.CorrelationId));

          await handleEventAsync(eventContext, args.CancellationToken);
          await args.UpdateCheckpointAsync(args.CancellationToken);
        };

    processor.ProcessErrorAsync += args =>
        {
          _logger.LogError(args.Exception,
              "Event Hubs processor error. Partition: {PartitionId}, Operation: {Operation}.",
              args.PartitionId,
              args.Operation);
          return Task.CompletedTask;
        };

    await processor.StartProcessingAsync(cancellationToken);

    try
    {
      await Task.Delay(Timeout.Infinite, cancellationToken);
    }
    catch
    {
      // we do nothing
    }
    finally
    {
      await processor.StopProcessingAsync(CancellationToken.None);
    }
  }

  public EventHubsEventConsumer(IOptions<MessagingOptions> messagingOptions,
      ILogger<EventHubsEventConsumer> logger)
  {
    var options = messagingOptions.Value;
    _eventHubsOptions = options.EventHubs;
    _defaultPayloadFormat = options.Serialization.DefaultFormat;
    _logger = logger;
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