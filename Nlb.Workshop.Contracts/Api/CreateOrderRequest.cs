namespace Nlb.Workshop.Contracts.Api;

// Command payload for publishing a single order-created event.
public sealed record CreateOrderRequest(
    string? OrderId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string? CorrelationId,
    bool UseV2,
    string? SourceSystem
);
