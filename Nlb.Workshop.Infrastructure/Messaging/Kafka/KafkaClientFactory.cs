using Confluent.Kafka;
using Nlb.Workshop.Infrastructure.Options;

namespace Nlb.Workshop.Infrastructure.Messaging.Kafka;

internal static class KafkaClientFactory
{
  public static ProducerConfig CreateProducerConfig(KafkaOptions options)
  {
    var config = new ProducerConfig
    {
      BootstrapServers = options.BootstrapServers,
      Acks = Acks.All,
      MessageSendMaxRetries = 3,
      EnableIdempotence = true
    };

    ApplySecurityOption(config, options);

    return config;
  }

  public static ConsumerConfig CreateConsumerConfig(KafkaOptions options, string groupId)
  {
    var config = new ConsumerConfig
    {
      BootstrapServers = options.BootstrapServers,
      GroupId = groupId,
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false,
      EnablePartitionEof = false
    };

    ApplySecurityOption(config, options);

    return config;
  }

  private static void ApplySecurityOption(ClientConfig config, KafkaOptions options)
  {
    if (!Enum.TryParse<SecurityProtocol>(options.SecurityProtocol, true, out var securityProtocol))
      securityProtocol = SecurityProtocol.Plaintext;

    config.SecurityProtocol = securityProtocol;

    if (string.IsNullOrWhiteSpace(options.SaslMechanism) ||
        string.IsNullOrWhiteSpace(options.SaslUsername) ||
        string.IsNullOrWhiteSpace(options.SaslPassword))
      return;

    if (Enum.TryParse<SaslMechanism>(options.SaslMechanism, true, out var saslMechanism))
      config.SaslMechanism = saslMechanism;

    config.SaslUsername = options.SaslUsername;
    config.SaslPassword = options.SaslPassword;
  }
}