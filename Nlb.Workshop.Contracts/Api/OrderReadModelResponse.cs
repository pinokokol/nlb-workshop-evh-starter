namespace Nlb.Workshop.Contracts.Api;

// Query response for the denormalized order read model.
public sealed record OrderReadModelResponse(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? SourceSystem,
    int LastEventVersion,
    Guid LastEventId
);
