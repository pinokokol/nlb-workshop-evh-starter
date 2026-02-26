using Microsoft.Extensions.Logging;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Application.Models;
using Nlb.Workshop.Contracts.Events;
using Nlb.Workshop.Domain.Entities;

namespace Nlb.Workshop.Application.UseCases;

public sealed class OrderProjectionService
{
  private readonly IProjectionRepository _projectionRepository;
  private readonly IEventSerializerResolver _eventSerializerResolver;
  private readonly ILogger<OrderProjectionService> _logger;

  public OrderProjectionService(IProjectionRepository projectionRepository,
        IEventSerializerResolver eventSerializerResolver,
        ILogger<OrderProjectionService> logger)
  {
    _projectionRepository = projectionRepository;
    _eventSerializerResolver = eventSerializerResolver;
    _logger = logger;
  }

  public async Task HandleAsync(ConsumedEventContext consumedEventContext,
    CancellationToken cancellationToken = default)
  {
    if (!string.Equals(consumedEventContext.EventType, EventTypeNames.OrderCreated,
      StringComparison.OrdinalIgnoreCase))
    {
      _logger.LogDebug("Skipping unsupported event type '{EventType}'.", consumedEventContext.EventType);
      return;
    }

    var serializer = _eventSerializerResolver.GetSerializer(consumedEventContext.PayloadFormat);

    switch (consumedEventContext.Version)
    {
      case 1:
        // V1

        var envelopeV1 = serializer.Deserialize<OrderCreatedV1>(consumedEventContext.Body);

        await ApplyProjectionAsync(envelopeV1.EventId,
          envelopeV1.EventType,
          envelopeV1.Version,
          consumedEventContext,
          envelopeV1.Payload.OrderId,
          envelopeV1.Payload.CustomerId,
          envelopeV1.Payload.Amount,
          envelopeV1.Payload.Currency,
          envelopeV1.Payload.CreatedAt,
          "legacy",
          cancellationToken
        );

        break;

      case 2:
        // V2

        var envelopeV2 = serializer.Deserialize<OrderCreatedV2>(consumedEventContext.Body);

        await ApplyProjectionAsync(envelopeV2.EventId,
          envelopeV2.EventType,
          envelopeV2.Version,
          consumedEventContext,
          envelopeV2.Payload.OrderId,
          envelopeV2.Payload.CustomerId,
          envelopeV2.Payload.Amount,
          envelopeV2.Payload.Currency,
          envelopeV2.Payload.CreatedAt,
          envelopeV2.Payload.SourceSystem,
          cancellationToken
        );

        break;

      default:
        _logger.LogWarning("Unsupported event version {Version} for {EventType}.",
                      consumedEventContext.Version,
                      consumedEventContext.EventType);
        break;
    }
  }

  private async Task ApplyProjectionAsync(Guid eventId,
    string eventType,
    int version,
    ConsumedEventContext consumedEventContext,
    string orderId,
    string customerId,
    decimal amount,
    string currency,
    DateTimeOffset createdAt,
    string? sourceSystem,
    CancellationToken cancellationToken
    )
  {
    var projection = new OrderReadModel
    {
      OrderId = orderId,
      CustomerId = customerId,
      Amount = amount,
      Currency = currency,
      CreatedAt = createdAt,
      UpdatedAt = DateTimeOffset.UtcNow,
      SourceSystem = sourceSystem,
      LastEventVersion = version,
      LastEventId = eventId
    };

    var processedEvent = new ProcessedEvent
    {
      EventId = eventId,
      EventType = eventType,
      Version = version,
      PartitionId = consumedEventContext.PartitionId,
      Offset = consumedEventContext.Offset,
      ProcessedAt = DateTimeOffset.UtcNow
    };

    var applied = await _projectionRepository.UpsertOrderIfEventNotProcessedAsync(projection,
    processedEvent, cancellationToken);

    if (!applied)
    {
      _logger.LogInformation("Skipping duplicate event {EventId}.", eventId);
      return;
    }

    _logger.LogInformation(
            "[PROJECTION] upserted order={OrderId} from event={EventId} v{Version} partition={PartitionId} offset={Offset}",
            orderId,
            eventId,
            version,
            consumedEventContext.PartitionId,
            consumedEventContext.Offset);
  }
}