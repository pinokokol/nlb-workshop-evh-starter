namespace Nlb.Workshop.Domain.Entities;

// Denormalized projection used by query endpoints.
public sealed class OrderReadModel
{
    public string OrderId { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? SourceSystem { get; set; }

    public int LastEventVersion { get; set; }

    public Guid LastEventId { get; set; }
}
