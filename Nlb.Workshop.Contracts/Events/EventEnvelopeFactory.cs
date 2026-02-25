namespace Nlb.Workshop.Contracts.Events;

// Helper for consistent envelope creation in command flows.
public static class EventEnvelopeFactory
{
    public static EventEnvelope<TPayload> Create<TPayload>(
        string eventType,
        int version,
        string partitionKey,
        TPayload payload,
        string? correlationId = null)
    {
        return new EventEnvelope<TPayload>(
            Guid.NewGuid(),
            eventType,
            version,
            DateTimeOffset.UtcNow,
            correlationId ?? Guid.NewGuid().ToString("N"),
            partitionKey,
            payload);
    }
}
