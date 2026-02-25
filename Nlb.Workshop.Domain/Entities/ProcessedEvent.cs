namespace Nlb.Workshop.Domain.Entities;

// Idempotency marker table so duplicate deliveries do not reapply projections.
public sealed class ProcessedEvent
{
    public Guid EventId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int Version { get; set; }

    public string PartitionId { get; set; } = string.Empty;

    public long? Offset { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}
