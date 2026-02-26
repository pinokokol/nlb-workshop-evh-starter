using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Contracts.Events;
using Nlb.Workshop.Infrastructure.Messaging.Common;
using Nlb.Workshop.Infrastructure.Options;

namespace Nlb.Workshop.Infrastructure.Messaging.EventHubs;

public sealed class EventHubsEventPublisher : IEventPublisher, IAsyncDisposable
{
  private readonly EventHubProducerClient _producerClient;
  private readonly IEventSerializerResolver _eventSerializerResolver;
  private readonly ILogger<EventHubsEventPublisher> _logger;

  public EventHubsEventPublisher(IOptions<MessagingOptions> messagingOptions,
      IEventSerializerResolver eventSerializerResolver,
      ILogger<EventHubsEventPublisher> logger)
  {
    var options = messagingOptions.Value.EventHubs;
    _producerClient = new EventHubProducerClient(options.ConnectionString, options.EventHubName);
    _eventSerializerResolver = eventSerializerResolver;
    _logger = logger;
  }

  public async Task PublishAsync<TPayload>(EventEnvelope<TPayload> envelope,
    CancellationToken cancellationToken = default)
  {
    var serializer = _eventSerializerResolver.GetSerializer();
    var eventData = CreateEventData(envelope, serializer);

    using var batch = await _producerClient.CreateBatchAsync(
      new CreateBatchOptions { PartitionKey = envelope.PartitionKey }, cancellationToken
    );

    if (!batch.TryAdd(eventData))
      throw new InvalidOperationException("Event does not fit into a single Event Hubs batch.");

    await _producerClient.SendAsync(batch, cancellationToken);

    _logger.LogInformation(
            "Published Event Hubs event {EventId} ({EventType} v{Version}) on key {PartitionKey}.",
            envelope.EventId,
            envelope.EventType,
            envelope.Version,
            envelope.PartitionKey);
  }

  public async Task PublishBatchAsync<TPayload>(IReadOnlyCollection<EventEnvelope<TPayload>> envelopes,
  CancellationToken cancellationToken = default)
  {
    foreach (var envelope in envelopes)
      await PublishAsync(envelope, cancellationToken);
  }

  public ValueTask DisposeAsync()
  {
    return _producerClient.DisposeAsync();
  }

  public static EventData CreateEventData<TPayload>(EventEnvelope<TPayload> envelope, IEventSerializer serializer)
  {
    var payload = serializer.Serialize(envelope);
    var eventData = new EventData(payload);

    eventData.Properties[EventHeaderNames.EventType] = envelope.EventType;
    eventData.Properties[EventHeaderNames.EventVersion] = envelope.Version;
    eventData.Properties[EventHeaderNames.CorrelationId] = envelope.CorrelationId;
    eventData.Properties[EventHeaderNames.PartitionKey] = envelope.PartitionKey;
    eventData.Properties[EventHeaderNames.PayloadFormat] = serializer.Format;

    return eventData;
  }
}