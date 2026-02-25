namespace Nlb.Workshop.Application.Interfaces;

// Creates/updates local projection storage before services start consuming events.
public interface IDatabaseInitializer
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}
