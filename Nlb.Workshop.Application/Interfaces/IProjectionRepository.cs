using Nlb.Workshop.Domain.Entities;

namespace Nlb.Workshop.Application.Interfaces;

// Persistence contract for read-model and idempotency state.
public interface IProjectionRepository
{
    Task<OrderReadModel?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default);

    Task<bool> UpsertOrderIfEventNotProcessedAsync(OrderReadModel order,
        ProcessedEvent processedEvent,
        CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);
}
