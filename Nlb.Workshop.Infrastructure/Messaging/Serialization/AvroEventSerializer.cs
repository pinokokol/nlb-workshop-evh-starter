using System.Text.Json;
using Avro;
using Avro.Generic;
using Avro.IO;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Contracts.Events;

namespace Nlb.Workshop.Infrastructure.Messaging.Serialization;

public sealed class AvroEventSerializer : IEventSerializer
{
  private const string EnvelopeSchemaJson = """
                                              {
                                                "type": "record",
                                                "name": "WorkshopEnvelope",
                                                "namespace": "Nlb.Workshop.Contracts.Events",
                                                "fields": [
                                                  { "name": "eventId", "type": "string" },
                                                  { "name": "eventType", "type": "string" },
                                                  { "name": "version", "type": "int" },
                                                  { "name": "occurredAt", "type": "string" },
                                                  { "name": "correlationId", "type": "string" },
                                                  { "name": "partitionKey", "type": "string" },
                                                  { "name": "payloadJson", "type": "string" }
                                                ]
                                              }
                                              """;

  private static readonly RecordSchema EnvelopeSchema =
      (RecordSchema)Schema.Parse(EnvelopeSchemaJson);

  private static readonly GenericWriter<GenericRecord> Writer = new(EnvelopeSchema);

  private static readonly GenericReader<GenericRecord> Reader = new(EnvelopeSchema, EnvelopeSchema);

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = false
  };

  public string Format => "avro";

  public byte[] Serialize<TPayload>(EventEnvelope<TPayload> envelope)
  {
    // Polja envelope mapiramo v Avro GenericRecord.
    var record = new GenericRecord(EnvelopeSchema);
    record.Add("eventId", envelope.EventId.ToString("D"));
    record.Add("eventType", envelope.EventType);
    record.Add("version", envelope.Version);
    record.Add("occurredAt", envelope.OccurredAt.ToString("O"));
    record.Add("correlationId", envelope.CorrelationId);
    record.Add("partitionKey", envelope.PartitionKey);
    record.Add("payloadJson", JsonSerializer.Serialize(envelope.Payload, JsonOptions));

    using var memoryStream = new MemoryStream();
    var encoder = new BinaryEncoder(memoryStream);
    Writer.Write(record, encoder);

    return memoryStream.ToArray();
  }

  public EventEnvelope<TPayload> Deserialize<TPayload>(byte[] payload)
  {
    // Iz binarnega Avro payloada rekonstruiramo strongly-typed envelope.
    using var memoryStream = new MemoryStream(payload);
    var decoder = new BinaryDecoder(memoryStream);
    var record = Reader.Read(default!, decoder);

    var eventId = Guid.Parse(record["eventId"]!.ToString()!);
    var eventType = record["eventType"]!.ToString()!;
    var version = Convert.ToInt32(record["version"]!);
    var occurredAt = DateTimeOffset.Parse(record["occurredAt"]!.ToString()!);
    var correlationId = record["correlationId"]!.ToString()!;
    var partitionKey = record["partitionKey"]!.ToString()!;
    var payloadJson = record["payloadJson"]!.ToString()!;

    // Payload je znotraj Avro-ja serializiran kot JSON besedilo.
    var deserializedPayload = JsonSerializer.Deserialize<TPayload>(payloadJson, JsonOptions);
    if (deserializedPayload is null)
      throw new InvalidOperationException("Unable to deserialize Avro payload JSON into target type.");

    return new EventEnvelope<TPayload>(
        eventId,
        eventType,
        version,
        occurredAt,
        correlationId,
        partitionKey,
        deserializedPayload);
  }
}