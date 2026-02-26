using Microsoft.Extensions.DependencyInjection;

namespace Nlb.Workshop.Application.UseCases;

public static class ApplicationServiceCollectionExtensions
{
  public static IServiceCollection AddWorkshopApplication(this IServiceCollection services)
  {
    services.AddSingleton(TimeProvider.System);

    services.AddSingleton<OrderCommandService>();
    services.AddSingleton<OrderProjectionService>();
    services.AddSingleton<ReadModelQueryService>();

    return services;
  }
}
