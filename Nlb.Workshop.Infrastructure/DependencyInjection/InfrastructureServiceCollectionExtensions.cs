using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Infrastructure.Data;
using Nlb.Workshop.Infrastructure.Messaging.EventHubs;
using Nlb.Workshop.Infrastructure.Messaging.Kafka;
using Nlb.Workshop.Infrastructure.Messaging.Partitioning;
using Nlb.Workshop.Infrastructure.Messaging.Serialization;
using Nlb.Workshop.Infrastructure.Options;
using Nlb.Workshop.Infrastructure.Repositories;
using Nlb.Workshop.Infrastructure.Replay;

namespace Nlb.Workshop.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
  public static IServiceCollection AddWorkshopInfrastructure(this IServiceCollection services,
      IConfiguration configuration)
  {
    var readModelConnectionString =
        configuration.GetConnectionString("ReadModel") ?? "Data Source=nlb-workshop.db";

    services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

    services.AddPooledDbContextFactory<WorkshopDbContext>(options =>
        options.UseSqlite(readModelConnectionString));

    services.AddSingleton<IProjectionRepository, SqliteProjectionRepository>();
    services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
    services.AddSingleton<IPartitionKeyResolver, OrderPartitionKeyResolver>();

    // JSON = core, Avro = advanced.
    services.AddSingleton<IEventSerializer, JsonEventSerializer>();
    services.AddSingleton<IEventSerializer, AvroEventSerializer>();
    services.AddSingleton<IEventSerializerResolver, EventSerializerResolver>();

    // Publisher provider switch.
    services.AddSingleton<IEventPublisher>(serviceProvider =>
    {
      var messagingOptions = serviceProvider.GetRequiredService<IOptions<MessagingOptions>>().Value;

      return messagingOptions.Provider.Equals("Kafka", StringComparison.OrdinalIgnoreCase)
              ? ActivatorUtilities.CreateInstance<KafkaEventPublisher>(serviceProvider)
              : ActivatorUtilities.CreateInstance<EventHubsEventPublisher>(serviceProvider);
    });

    // Consumer provider switch.
    services.AddSingleton<IEventConsumer>(serviceProvider =>
    {
      var messagingOptions = serviceProvider.GetRequiredService<IOptions<MessagingOptions>>().Value;

      return messagingOptions.Provider.Equals("Kafka", StringComparison.OrdinalIgnoreCase)
              ? ActivatorUtilities.CreateInstance<KafkaEventConsumer>(serviceProvider)
              : ActivatorUtilities.CreateInstance<EventHubsEventConsumer>(serviceProvider);
    });

    // Replay provider switch.
    services.AddSingleton<IReplayCoordinator>(serviceProvider =>
    {
      var messagingOptions = serviceProvider.GetRequiredService<IOptions<MessagingOptions>>().Value;

      return messagingOptions.Provider.Equals("Kafka", StringComparison.OrdinalIgnoreCase)
              ? ActivatorUtilities.CreateInstance<KafkaReplayCoordinator>(serviceProvider)
              : ActivatorUtilities.CreateInstance<EventHubsReplayCoordinator>(serviceProvider);
    });

    return services;
  }
}