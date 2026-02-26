using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Contracts.Api;

namespace Nlb.Workshop.Application.UseCases;

public sealed class ReadModelQueryService
{
  private readonly IProjectionRepository _projectionRepository;

  public ReadModelQueryService(IProjectionRepository projectionRepository)
  {
    _projectionRepository = projectionRepository;
  }

  public async Task<OrderReadModelResponse?> GetOrderAsync(string orderId,
    CancellationToken cancellationToken = default)
  {
    var order = await _projectionRepository.GetOrderAsync(orderId, cancellationToken);
    if (order is null)
      return null;

    return new OrderReadModelResponse(
      order.OrderId,
      order.CustomerId,
      order.Amount,
      order.Currency,
      order.CreatedAt,
      order.UpdatedAt,
      order.SourceSystem,
      order.LastEventVersion,
      order.LastEventId
    );
  }
}