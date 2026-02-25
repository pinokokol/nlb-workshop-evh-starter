using Nlb.Workshop.Application.Models;

namespace Nlb.Workshop.Application.Interfaces;

// Replay contract for rebuilding projections from historical events.
public interface IReplayCoordinator
{
    Task<ReplayResult> ReplayAsync(Func<ConsumedEventContext, CancellationToken, Task> handleEventAsync,
        CancellationToken cancellationToken = default);
}
