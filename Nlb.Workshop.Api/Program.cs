using Microsoft.AspNetCore.Mvc;
using Nlb.Workshop.Application.Interfaces;
using Nlb.Workshop.Application.UseCases;
using Nlb.Workshop.Contracts.Api;
using Nlb.Workshop.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWorkshopApplication();
builder.Services.AddWorkshopInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "v1"); });
}

using (var scope = app.Services.CreateScope())
{
  var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
  await initializer.EnsureCreatedAsync();
}

app.MapPost("/orders",
        async ([FromBody] CreateOrderRequest request,
            [FromServices] OrderCommandService commandService,
            CancellationToken cancellationToken) =>
        {
          try
          {
            var response = await commandService.PublishOrderAsync(request, cancellationToken);
            return Results.Created($"/read-model/orders/{response.OrderId}", response);
          }
          catch (ArgumentException ex)
          {
            return Results.BadRequest(new { error = ex.Message });
          }
        })
    .WithName("PublishOrder")
    .WithTags("Orders");

app.MapPost("/orders/bulk",
        async ([FromBody] CreateOrdersBulkRequest request,
            [FromServices] OrderCommandService commandService,
            CancellationToken cancellationToken) =>
        {
          var response = await commandService.PublishBulkAsync(request, cancellationToken);
          return Results.Ok(response);
        })
    .WithName("PublishOrdersBulk")
    .WithTags("Orders");

app.MapGet("/read-model/orders/{orderId}",
        async (string orderId,
            [FromServices] ReadModelQueryService queryService,
            CancellationToken cancellationToken) =>
        {
          var response = await queryService.GetOrderAsync(orderId, cancellationToken);
          return response is null ? Results.NotFound() : Results.Ok(response);
        })
    .WithName("GetOrderReadModel")
    .WithTags("Read Model");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health")
    .WithTags("System");

await app.RunAsync();
