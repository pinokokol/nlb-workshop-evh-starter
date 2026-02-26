using Microsoft.EntityFrameworkCore;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Domain.Entities;
using Nlb.Workshop.Infrastructure.Data;

namespace Nlb.Workshop.Infrastructure.Repositories;

public sealed class SqliteProjectionRepository : IProjectionRepository
{
  private readonly IDbContextFactory<WorkshopDbContext> _dbContextFactory;

  public SqliteProjectionRepository(IDbContextFactory<WorkshopDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public async Task<OrderReadModel?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
  {
    await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    return await dbContext.Orders
          .AsNoTracking()
          .SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
  }

  public async Task<bool> UpsertOrderIfEventNotProcessedAsync(OrderReadModel order,
    ProcessedEvent processedEvent,
    CancellationToken cancellationToken = default)
  {
    await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    try
    {
      var alreadyProcessed = await dbContext.ProcessedEvents.AsNoTracking()
          .AnyAsync(x => x.EventId == processedEvent.EventId, cancellationToken);

      if (alreadyProcessed)
      {
        return false;
      }

      dbContext.ProcessedEvents.Add(processedEvent);

      var existingProjection = await dbContext.Orders
        .SingleOrDefaultAsync(x => x.OrderId == order.OrderId, cancellationToken);

      if (existingProjection is null)
      {
        dbContext.Orders.Add(order);
      }
      else
      {
        existingProjection.CustomerId = order.CustomerId;
        existingProjection.Amount = order.Amount;
        existingProjection.Currency = order.Currency;
        existingProjection.CreatedAt = order.CreatedAt;
        existingProjection.UpdatedAt = order.UpdatedAt;
        existingProjection.SourceSystem = order.SourceSystem;
        existingProjection.LastEventVersion = order.LastEventVersion;
        existingProjection.LastEventId = order.LastEventId;
      }

      await dbContext.SaveChangesAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);

      return true;
    }
    catch (DbUpdateException)
    {
      await transaction.RollbackAsync(cancellationToken);
      return false;
    }
  }

  public async Task ResetAsync(CancellationToken cancellationToken = default)
  {
    await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

    dbContext.Orders.RemoveRange(dbContext.Orders);
    dbContext.ProcessedEvents.RemoveRange(dbContext.ProcessedEvents);
    await dbContext.SaveChangesAsync(cancellationToken);
  }
}