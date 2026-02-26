using Microsoft.EntityFrameworkCore;
using Nlb.Workshop.Domain.Entities;

namespace Nlb.Workshop.Infrastructure.Data;

public sealed class WorkshopDbContext : DbContext
{
  public WorkshopDbContext(DbContextOptions<WorkshopDbContext> options)
        : base(options)
  {
  }

  public DbSet<OrderReadModel> Orders => Set<OrderReadModel>();

  public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<OrderReadModel>(entity =>
    {
      entity.HasKey(x => x.OrderId);
      entity.Property(x => x.OrderId).IsRequired();
      entity.Property(x => x.CustomerId).IsRequired();
      entity.Property(x => x.Currency).IsRequired();
      entity.Property(x => x.Amount).IsRequired();
    });

    modelBuilder.Entity<ProcessedEvent>(entity =>
    {
      entity.HasKey(x => x.EventId);
      entity.Property(x => x.EventType).IsRequired();
      entity.Property(x => x.PartitionId).IsRequired();
      entity.HasIndex(x => new { x.EventType, x.PartitionId, x.Offset });
    });
  }
}