using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Application.Models;
using Nlb.Workshop.Infrastructure.Messaging.Common;
using Nlb.Workshop.Infrastructure.Messaging.Kafka;
using Nlb.Workshop.Infrastructure.Options;

namespace Nlb.Workshop.Infrastructure.Replay;

public sealed class KafkaReplayCoordinator : IReplayCoordinator
{
  private readonly KafkaOptions _kafkaOptions;
  private readonly ReplayOptions _replayOptions;
  private readonly string _defaultPayloadFormat;

  public KafkaReplayCoordinator(IOptions<MessagingOptions> messagingOptions)
  {
    var options = messagingOptions.Value;
    _kafkaOptions = options.Kafka;
    _replayOptions = options.Replay;
    _defaultPayloadFormat = options.Serialization.DefaultFormat;
  }

  public async Task<ReplayResult> ReplayAsync(Func<ConsumedEventContext, CancellationToken, Task> handleEventAsync,
      CancellationToken cancellationToken = default)
  {
    // Ločen replay group izolira replay napredek od "normalnega" consumerja.
    var groupId = string.IsNullOrWhiteSpace(_kafkaOptions.ReplayConsumerGroup)
        ? $"replay-{Guid.NewGuid():N}"
        : _kafkaOptions.ReplayConsumerGroup;

    var consumerConfig = KafkaClientFactory.CreateConsumerConfig(_kafkaOptions, groupId);

    using var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
    consumer.Subscribe(_kafkaOptions.Topic);

    var processedEvents = 0;
    var partitionsVisited = 0;
    var seenPartitions = new HashSet<string>();
    var emptyReads = 0;

    while (!cancellationToken.IsCancellationRequested)
    {
      var consumeResult = consumer.Consume(TimeSpan.FromSeconds(Math.Max(1, _replayOptions.MaximumWaitTimeSeconds)));
      if (consumeResult?.Message?.Value is null)
      {
        // End replay after configurable number of empty polls.
        emptyReads++;
        if (emptyReads >= Math.Max(1, _replayOptions.ConsecutiveEmptyReadsToStop))
          break;

        continue;
      }

      emptyReads = 0;
      var partitionId = consumeResult.Partition.Value.ToString();
      if (seenPartitions.Add(partitionId))
        partitionsVisited++;

      // Kafka record mapiramo v enoten model, ki ga uporabi projection use case.
      var eventContext = new ConsumedEventContext(
          consumeResult.Message.Value,
          GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.EventType) ?? "unknown",
          ParseInt(GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.EventVersion), 1),
          consumeResult.Message.Key ??
          GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.PartitionKey) ??
          string.Empty,
          partitionId,
          consumeResult.Offset.Value,
          GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.PayloadFormat) ?? _defaultPayloadFormat,
          GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.CorrelationId));

      await handleEventAsync(eventContext, cancellationToken);
      processedEvents++;
    }

    consumer.Close();

    return new ReplayResult(processedEvents, partitionsVisited);
  }

  private static string? GetHeaderValue(Headers headers, string name)
  {
    var header = headers.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase));
    if (header is null)
      return null;

    var bytes = header.GetValueBytes();
    return bytes.Length == 0 ? null : Encoding.UTF8.GetString(bytes);
  }

  private static int ParseInt(string? value, int fallback)
  {
    return int.TryParse(value, out var parsed) ? parsed : fallback;
  }
}