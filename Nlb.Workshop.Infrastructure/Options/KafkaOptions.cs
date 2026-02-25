namespace Nlb.Workshop.Infrastructure.Options;

// Kafka protocol settings (used by Confluent.Kafka adapters).
public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "orders";

    public string ConsumerGroup { get; set; } = "projection";

    public string ReplayConsumerGroup { get; set; } = "replay";

    public string SecurityProtocol { get; set; } = "Plaintext";

    public string? SaslMechanism { get; set; }

    public string? SaslUsername { get; set; }

    public string? SaslPassword { get; set; }
}
