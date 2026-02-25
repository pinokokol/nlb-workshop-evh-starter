namespace Nlb.Workshop.Application.Models;

// Normalized consume metadata passed from transport adapters into projection use-cases.
public sealed record ConsumedEventContext(
    byte[] Body,
    string EventType,
    int Version,
    string PartitionKey,
    string PartitionId,
    long? Offset,
    string PayloadFormat,
    string? CorrelationId
);
