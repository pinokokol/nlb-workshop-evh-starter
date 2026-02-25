using Nlb.Workshop.Contracts.Events;

namespace Nlb.Workshop.Application.Interfaces;

// Envelope serializer abstraction (json/avro).
public interface IEventSerializer
{
    string Format { get; }

    byte[] Serialize<TPayload>(EventEnvelope<TPayload> envelope);

    EventEnvelope<TPayload> Deserialize<TPayload>(byte[] payload);
}
