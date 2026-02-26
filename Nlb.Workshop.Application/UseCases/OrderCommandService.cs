using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Contracts.Api;
using Nlb.Workshop.Contracts.Events;

namespace Nlb.Workshop.Application.UseCases;

public sealed class OrderCommandService
{
  private readonly IEventPublisher _eventPublisher;

  private readonly IPartitionKeyResolver _partitionKeyResolver;

  private readonly IEventSerializerResolver _eventSerializerResolver;

  private readonly TimeProvider _timeProvider;

  public OrderCommandService(IEventPublisher eventPublisher,
        IPartitionKeyResolver partitionKeyResolver,
        IEventSerializerResolver eventSerializerResolver,
        TimeProvider timeProvider)
  {
    _eventPublisher = eventPublisher;
    _partitionKeyResolver = partitionKeyResolver;
    _eventSerializerResolver = eventSerializerResolver;
    _timeProvider = timeProvider;
  }

  public async Task<PublishOrderResponse> PublishOrderAsync(CreateOrderRequest request,
  CancellationToken cancellationToken = default)
  {
    ValidateRequest(request);

    var serializer = _eventSerializerResolver.GetSerializer();
    var orderId = string.IsNullOrWhiteSpace(request.OrderId) ? Guid.NewGuid().ToString("N") : request.OrderId;
    var partitionKey = _partitionKeyResolver.ResolveForOrder(request.CustomerId);
    var now = _timeProvider.GetUtcNow();

    if (request.UseV2)
    {
      var payload = new OrderCreatedV2(
        orderId,
        request.CustomerId,
        request.Amount,
        request.Currency,
        now,
        "api",
        request.SourceSystem ?? "Nlb.Workshop.Api",
        new Dictionary<string, string> { ["workshop"] = "eda-dotnet" }
      );

      var envelope = EventEnvelopeFactory.Create(EventTypeNames.OrderCreated, 2,
      partitionKey, payload, request.CorrelationId);

      await _eventPublisher.PublishAsync(envelope, cancellationToken);

      return new PublishOrderResponse(orderId, envelope.EventId, envelope.EventType,
    envelope.Version, envelope.PartitionKey, serializer.Format);
    }

    var v1Payload = new OrderCreatedV1(
      orderId,
      request.CustomerId,
      request.Amount,
      request.Currency,
      now,
      "api"
    );

    var v1Envelope = EventEnvelopeFactory.Create(EventTypeNames.OrderCreated, 1, partitionKey, v1Payload,
            request.CorrelationId);

    await _eventPublisher.PublishAsync(v1Envelope, cancellationToken);

    return new PublishOrderResponse(orderId, v1Envelope.EventId, v1Envelope.EventType,
    v1Envelope.Version, v1Envelope.PartitionKey, serializer.Format);
  }

  public async Task<IReadOnlyCollection<PublishOrderResponse>> PublishBulkAsync(CreateOrdersBulkRequest request,
  CancellationToken cancellationToken = default)
  {
    if (request.Orders.Count == 0)
      return Array.Empty<PublishOrderResponse>();

    var responses = new List<PublishOrderResponse>(request.Orders.Count);

    foreach (var order in request.Orders)
    {
      var response = await PublishOrderAsync(order, cancellationToken);

      responses.Add(response);
    }

    return responses;
  }

  private static void ValidateRequest(CreateOrderRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.CustomerId))
      throw new ArgumentException("CustomerId is required.");

    if (request.Amount <= 0)
      throw new ArgumentException("Amount must be greater than zero.");

    if (string.IsNullOrWhiteSpace(request.Currency))
      throw new ArgumentException("Currency is required.");
  }
}