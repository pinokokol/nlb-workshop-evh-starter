using System.Text.Json;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Contracts.Events;

namespace Nlb.Workshop.Infrastructure.Messaging.Serialization;

public sealed class JsonEventSerializer : IEventSerializer
{
  private static readonly JsonSerializerOptions serializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = false
  };

  public string Format => "json";

  public byte[] Serialize<TPayload>(EventEnvelope<TPayload> envelope)
  {
    return JsonSerializer.SerializeToUtf8Bytes(envelope, serializerOptions);
  }

  public EventEnvelope<TPayload> Deserialize<TPayload>(byte[] payload)
  {
    var envelope = JsonSerializer.Deserialize<EventEnvelope<TPayload>>(payload, serializerOptions);
    if (envelope is null)
      throw new InvalidOperationException("Unable to deserialize JSON event envelope.");

    return envelope;
  }
}