using Microsoft.Extensions.Options;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Infrastructure.Options;

namespace Nlb.Workshop.Infrastructure.Messaging.Serialization;

public sealed class EventSerializerResolver : IEventSerializerResolver
{
  private readonly Dictionary<string, IEventSerializer> _serializers;
  private readonly string _defaultFormat;

  public EventSerializerResolver(IEnumerable<IEventSerializer> serializers,
      IOptions<MessagingOptions> messagingOptions)
  {
    _serializers = serializers.ToDictionary(x => x.Format, StringComparer.OrdinalIgnoreCase);
    _defaultFormat = messagingOptions.Value.Serialization.DefaultFormat;
  }

  public IEventSerializer GetSerializer(string? format = null)
  {
    var formatToResolve = string.IsNullOrWhiteSpace(format) ? _defaultFormat : format;

    if (_serializers.TryGetValue(formatToResolve, out var serializer))
      return serializer;

    throw new InvalidOperationException($"No serializer registered for format '{formatToResolve}'.");
  }
}
