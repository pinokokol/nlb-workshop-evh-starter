namespace Nlb.Workshop.Contracts.Events;

// Standard event envelope wrapping versioned payload with transport metadata.
public sealed record EventEnvelope<TPayload>(
    Guid EventId,
    string EventType,
    int Version,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string PartitionKey,
    TPayload Payload
);
