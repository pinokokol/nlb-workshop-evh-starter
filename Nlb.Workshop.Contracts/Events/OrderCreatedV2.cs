namespace Nlb.Workshop.Contracts.Events;

// Evolved contract version with source-system and extensible attributes.
public sealed record OrderCreatedV2(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string SourceSystem,
    IReadOnlyDictionary<string, string>? Attributes
);
