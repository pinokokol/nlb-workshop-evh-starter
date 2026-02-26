// TODO(workshop)
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Application.Models;
using Nlb.Workshop.Infrastructure.Messaging.Common;
using Nlb.Workshop.Infrastructure.Options;

namespace Nlb.Workshop.Infrastructure.Messaging.Kafka;

public sealed class KafkaEventConsumer : IEventConsumer
{
  private readonly KafkaOptions _kafkaOptions;
  private readonly string _defaultPayloadFormat;
  private readonly ILogger<KafkaEventConsumer> _logger;

  public KafkaEventConsumer(IOptions<MessagingOptions> messagingOptions,
      ILogger<KafkaEventConsumer> logger)
  {
    var options = messagingOptions.Value;
    _kafkaOptions = options.Kafka;
    _defaultPayloadFormat = options.Serialization.DefaultFormat;
    _logger = logger;
  }

  public async Task StartAsync(Func<ConsumedEventContext, CancellationToken, Task> handleEventAsync,
      CancellationToken cancellationToken)
  {
    // GroupId določa deljenje particij med več instancami workerja.
    var consumerConfig = KafkaClientFactory.CreateConsumerConfig(_kafkaOptions, _kafkaOptions.ConsumerGroup);

    using var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
    consumer.Subscribe(_kafkaOptions.Topic);

    try
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        var consumeResult = consumer.Consume(cancellationToken);
        if (consumeResult?.Message?.Value is null)
          continue;

        // Kafka record normaliziramo v app-level model dogodka.
        var context = new ConsumedEventContext(
            consumeResult.Message.Value,
            GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.EventType) ?? "unknown",
            ParseInt(GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.EventVersion), 1),
            consumeResult.Message.Key ??
            GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.PartitionKey) ??
            string.Empty,
            consumeResult.Partition.Value.ToString(),
            consumeResult.Offset.Value,
            GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.PayloadFormat) ??
            _defaultPayloadFormat,
            GetHeaderValue(consumeResult.Message.Headers, EventHeaderNames.CorrelationId));

        await handleEventAsync(context, cancellationToken);
        // Commit šele po uspešni obdelavi => at-least-once semantika.
        consumer.Commit(consumeResult);
      }
    }
    catch (OperationCanceledException)
    {
      // expected during graceful shutdown
    }
    catch (ConsumeException consumeException)
    {
      _logger.LogError(consumeException,
          "Kafka consume exception. Error code: {Code}.",
          consumeException.Error.Code);
      throw;
    }
    finally
    {
      consumer.Close();
    }
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