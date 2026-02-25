namespace Nlb.Workshop.Contracts.Api;

// Command payload for publishing multiple orders in one call.
public sealed record CreateOrdersBulkRequest(IReadOnlyCollection<CreateOrderRequest> Orders);
