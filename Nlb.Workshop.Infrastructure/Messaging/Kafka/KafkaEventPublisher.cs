using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Contracts.Events;
using Nlb.Workshop.Infrastructure.Messaging.Common;
using Nlb.Workshop.Infrastructure.Options;

namespace Nlb.Workshop.Infrastructure.Messaging.Kafka;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
  private readonly KafkaOptions _kafkaOptions;
  private readonly IEventSerializerResolver _eventSerializerResolver;
  private readonly ILogger<KafkaEventPublisher> _logger;
  private readonly IProducer<string, byte[]> _producer;

  public KafkaEventPublisher(IOptions<MessagingOptions> messagingOptions,
      IEventSerializerResolver eventSerializerResolver,
      ILogger<KafkaEventPublisher> logger)
  {
    _kafkaOptions = messagingOptions.Value.Kafka;
    _eventSerializerResolver = eventSerializerResolver;
    _logger = logger;
    _producer = new ProducerBuilder<string, byte[]>(KafkaClientFactory.CreateProducerConfig(_kafkaOptions)).Build();
  }

  public async Task PublishAsync<TPayload>(EventEnvelope<TPayload> envelope,
      CancellationToken cancellationToken = default)
  {
    var serializer = _eventSerializerResolver.GetSerializer();
    var message = CreateMessage(envelope, serializer);

    await _producer.ProduceAsync(_kafkaOptions.Topic, message, cancellationToken);

    _logger.LogInformation("Published Kafka event {EventId} ({EventType} v{Version}) key {PartitionKey}.",
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

  public void Dispose()
  {
    _producer.Flush(TimeSpan.FromSeconds(3));
    _producer.Dispose();
  }

  private static Message<string, byte[]> CreateMessage<TPayload>(EventEnvelope<TPayload> envelope,
      IEventSerializer serializer)
  {
    var headers = new Headers
        {
            { EventHeaderNames.EventType, Encoding.UTF8.GetBytes(envelope.EventType) },
            { EventHeaderNames.EventVersion, Encoding.UTF8.GetBytes(envelope.Version.ToString()) },
            { EventHeaderNames.CorrelationId, Encoding.UTF8.GetBytes(envelope.CorrelationId) },
            { EventHeaderNames.PartitionKey, Encoding.UTF8.GetBytes(envelope.PartitionKey) },
            { EventHeaderNames.PayloadFormat, Encoding.UTF8.GetBytes(serializer.Format) }
        };

    return new Message<string, byte[]>
    {
      Key = envelope.PartitionKey,
      Value = serializer.Serialize(envelope),
      Headers = headers,
      Timestamp = new Timestamp(envelope.OccurredAt.UtcDateTime)
    };
  }
}