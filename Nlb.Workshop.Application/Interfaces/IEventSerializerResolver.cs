namespace Nlb.Workshop.Application.Interfaces;

// Resolves serializer implementation by format name.
public interface IEventSerializerResolver
{
    IEventSerializer GetSerializer(string? format = null);
}
