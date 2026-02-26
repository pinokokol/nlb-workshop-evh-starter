using Nlb.Workshop.Application.UseCases;
using Nlb.Workshop.Consumer.Worker;
using Nlb.Workshop.Infrastructure.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkshopApplication();
builder.Services.AddWorkshopInfrastructure(builder.Configuration);
builder.Services.AddHostedService<EventConsumerWorker>();

var host = builder.Build();
await host.RunAsync();
