namespace Nlb.Workshop.Infrastructure.Options;

// Root configuration object for transport/serialization/replay settings.
public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public string Provider { get; set; } = "EventHubs";

    public EventHubsOptions EventHubs { get; set; } = new();

    public KafkaOptions Kafka { get; set; } = new();

    public SerializationOptions Serialization { get; set; } = new();

    public ReplayOptions Replay { get; set; } = new();
}
