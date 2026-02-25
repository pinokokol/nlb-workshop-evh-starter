namespace Nlb.Workshop.Application.Interfaces;

// Encapsulates partition-key strategy so ordering/scaling behavior is explicit.
public interface IPartitionKeyResolver
{
    string ResolveForOrder(string customerId);
}
