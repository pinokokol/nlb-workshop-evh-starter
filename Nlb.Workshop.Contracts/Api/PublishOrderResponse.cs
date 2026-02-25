namespace Nlb.Workshop.Contracts.Api;

// API response with event metadata used for tracing and demos.
public sealed record PublishOrderResponse(
    string OrderId,
    Guid EventId,
    string EventType,
    int Version,
    string PartitionKey,
    string PayloadFormat
);
