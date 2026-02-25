using Nlb.Workshop.Contracts.Events;

namespace Nlb.Workshop.Application.Interfaces;

// Transport-agnostic publishing contract used by command use-cases.
public interface IEventPublisher
{
    Task PublishAsync<TPayload>(EventEnvelope<TPayload> envelope, CancellationToken cancellationToken = default);

    Task PublishBatchAsync<TPayload>(IReadOnlyCollection<EventEnvelope<TPayload>> envelopes,
        CancellationToken cancellationToken = default);
}
