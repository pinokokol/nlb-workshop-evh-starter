using Microsoft.EntityFrameworkCore;
using Nlb.Workshop.Application.Interfaces;

namespace Nlb.Workshop.Infrastructure.Data;

public sealed class DatabaseInitializer : IDatabaseInitializer
{
  private readonly IDbContextFactory<WorkshopDbContext> _dbContextFactory;

  public DatabaseInitializer(IDbContextFactory<WorkshopDbContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
  {
    await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
    await dbContext.Database.EnsureCreatedAsync(cancellationToken);
  }
}
