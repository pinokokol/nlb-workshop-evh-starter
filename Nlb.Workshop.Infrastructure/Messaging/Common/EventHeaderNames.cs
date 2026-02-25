namespace Nlb.Workshop.Infrastructure.Messaging.Common;

// Shared message header keys used across Event Hubs and Kafka adapters.
public static class EventHeaderNames
{
    public const string EventType = "event-type";

    public const string EventVersion = "event-version";

    public const string CorrelationId = "correlation-id";

    public const string PartitionKey = "partition-key";

    public const string PayloadFormat = "payload-format";
}
