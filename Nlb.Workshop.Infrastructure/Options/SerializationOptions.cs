namespace Nlb.Workshop.Infrastructure.Options;

// Default payload format when headers do not explicitly provide one.
public sealed class SerializationOptions
{
    public string DefaultFormat { get; set; } = "json";
}
