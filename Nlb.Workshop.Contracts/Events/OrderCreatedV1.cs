namespace Nlb.Workshop.Contracts.Events;

// Initial contract version for order-created events.
public sealed record OrderCreatedV1(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAt,
    string CreatedBy
);
