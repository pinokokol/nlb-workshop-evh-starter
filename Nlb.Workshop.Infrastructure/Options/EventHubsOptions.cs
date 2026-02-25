namespace Nlb.Workshop.Infrastructure.Options;

// Event Hubs transport settings (supports local emulator defaults).
public sealed class EventHubsOptions
{
    public string ConnectionString { get; set; } =
        "Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public string EventHubName { get; set; } = "orders";

    public string ConsumerGroup { get; set; } = "projection";

    public string ReplayConsumerGroup { get; set; } = "replay";

    public string CheckpointBlobConnectionString { get; set; } =
        "UseDevelopmentStorage=true";

    public string CheckpointContainerName { get; set; } = "checkpoints";
}
