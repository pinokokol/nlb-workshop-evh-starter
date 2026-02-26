using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Application.Models;
using Nlb.Workshop.Application.UseCases;

namespace Nlb.Workshop.Consumer.Worker;

public sealed class EventConsumerWorker : BackgroundService
{
  private readonly IEventConsumer _eventConsumer;
  private readonly OrderProjectionService _orderProjectionService;
  private readonly IDatabaseInitializer _databaseInitializer;
  private readonly ILogger<EventConsumerWorker> _logger;

  public EventConsumerWorker(IEventConsumer eventConsumer,
      OrderProjectionService orderProjectionService,
      IDatabaseInitializer databaseInitializer,
      ILogger<EventConsumerWorker> logger)
  {
    _eventConsumer = eventConsumer;
    _orderProjectionService = orderProjectionService;
    _databaseInitializer = databaseInitializer;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await _databaseInitializer.EnsureCreatedAsync(stoppingToken);

    _logger.LogInformation("Consumer worker started.");

    await _eventConsumer.StartAsync(HandleEventAsync, stoppingToken);
  }

  private async Task HandleEventAsync(ConsumedEventContext eventContext, CancellationToken cancellationToken)
  {
    _logger.LogInformation(
           "[EVENT-IN] type={EventType} v{Version} key={PartitionKey} partition={PartitionId} offset={Offset} format={Format}",
           eventContext.EventType,
           eventContext.Version,
           eventContext.PartitionKey,
           eventContext.PartitionId,
           eventContext.Offset,
           eventContext.PayloadFormat);

    await _orderProjectionService.HandleAsync(eventContext, cancellationToken);
  }
}