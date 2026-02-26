using Nlb.Workshop.Application.Interfaces;

namespace Nlb.Workshop.Infrastructure.Messaging.Partitioning;

public sealed class OrderPartitionKeyResolver : IPartitionKeyResolver
{
  public string ResolveForOrder(string customerId)
  {
    return string.IsNullOrWhiteSpace(customerId) ? "unknown-customer" : customerId.Trim().ToLowerInvariant();
  }
}