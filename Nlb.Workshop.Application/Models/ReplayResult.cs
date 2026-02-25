namespace Nlb.Workshop.Application.Models;

// Replay execution summary for CLI/log output.
public sealed record ReplayResult(int ProcessedEvents, int PartitionsVisited);
