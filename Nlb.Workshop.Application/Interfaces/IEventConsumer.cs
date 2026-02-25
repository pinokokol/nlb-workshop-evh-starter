using Nlb.Workshop.Application.Models;

namespace Nlb.Workshop.Application.Interfaces;

// Transport-agnostic event consumption contract.
public interface IEventConsumer
{
    Task StartAsync(Func<ConsumedEventContext, CancellationToken, Task> handleEventAsync,
        CancellationToken cancellationToken);
}
