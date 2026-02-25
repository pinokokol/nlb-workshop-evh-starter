namespace Nlb.Workshop.Infrastructure.Options;

// Replay loop controls to prevent endless waiting on empty streams.
public sealed class ReplayOptions
{
    public int MaximumWaitTimeSeconds { get; set; } = 2;

    public int ConsecutiveEmptyReadsToStop { get; set; } = 3;
}
