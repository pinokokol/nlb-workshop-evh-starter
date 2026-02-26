using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Application.UseCases;
using Nlb.Workshop.Infrastructure.DependencyInjection;

var resetReadModel = args.Any(x => string.Equals(x, "--reset-read-model", StringComparison.OrdinalIgnoreCase));

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
  Args = args,
  ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddWorkshopApplication();
builder.Services.AddWorkshopInfrastructure(builder.Configuration);

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ReplayTool");
var initializer = host.Services.GetRequiredService<IDatabaseInitializer>();
var projectionRepository = host.Services.GetRequiredService<IProjectionRepository>();
var projectionService = host.Services.GetRequiredService<OrderProjectionService>();
var replayCoordinator = host.Services.GetRequiredService<IReplayCoordinator>();

await initializer.EnsureCreatedAsync();

if (resetReadModel)
{
  logger.LogInformation("Resetting read model before replay.");
  await projectionRepository.ResetAsync();
}

logger.LogInformation("Starting replay.");
var replayResult = await replayCoordinator.ReplayAsync(projectionService.HandleAsync);
logger.LogInformation("Replay finished. Events processed: {Count}, partitions visited: {Partitions}.",
    replayResult.ProcessedEvents,
    replayResult.PartitionsVisited);
