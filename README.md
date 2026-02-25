# NLB Workshop: Event-Driven Architecture in .NET

This repository contains the **complete instructor implementation** for the workshop:

- Event-driven architecture fundamentals in .NET
- Pragmatic CQRS (commands + projections)
- Azure Event Hubs integration (`Azure.Messaging.EventHubs`)
- Kafka protocol integration (`Confluent.Kafka`)
- Event replay from earliest position
- Partition keys and consumer groups
- JSON (core) and Avro (advanced) payload serialization
- Clean Architecture project layout

## Project Structure

```text
Nlb.Workshop.Api/                 # Producer service (HTTP -> events)
Nlb.Workshop.Consumer.Worker/     # Consumer service (events -> projections)
Nlb.Workshop.Domain/
Nlb.Workshop.Application/
Nlb.Workshop.Contracts/
Nlb.Workshop.Infrastructure/
Nlb.Workshop.Tools.Replay/        # Replay/reset utility
compose.yaml
README.md
docs/
```

## Clean Architecture Mapping

- `Nlb.Workshop.Domain`: domain entities (`OrderReadModel`, `ProcessedEvent`)
- `Nlb.Workshop.Application`: use cases + ports (`IEventPublisher`, `IEventConsumer`, `IProjectionRepository`, `IReplayCoordinator`, `IEventSerializer`)
- `Nlb.Workshop.Infrastructure`: adapters for Event Hubs, Kafka, serialization, persistence
- `Nlb.Workshop.Api`: producer-facing HTTP endpoints
- `Nlb.Workshop.Consumer.Worker`: consumer runtime and projection updates
- `Nlb.Workshop.Tools.Replay`: replay utility for read-model rebuild

## Key Endpoints

- `POST /orders`
- `POST /orders/bulk`
- `GET /read-model/orders/{orderId}`

See `Nlb.Workshop.Api/Nlb.Workshop.Api.http` for ready-to-run samples.

## Local Infrastructure

`compose.yaml` includes:

- Event Hubs emulator
- Azurite (Blob checkpoint store)
- Schema Registry (advanced module)
- Kafka UI (topic/consumer group browser)
- API container
- Consumer container

Start everything:

```bash
docker compose up -d
```

Run only infrastructure:

```bash
docker compose up -d eventhubs-emulator azurite schema-registry kafka-ui
```

Kafka UI:
- `http://localhost:8085`

## Run Locally (without containers for .NET services)

Default local persistence uses one shared SQLite file at repo root:
- `nlb-workshop.db` (configured as `Data Source=../nlb-workshop.db` in API, Worker, Replay)

1. Start infra via Docker compose.
2. Run consumer:

```bash
dotnet run --project Nlb.Workshop.Consumer.Worker
```

3. Run API:

```bash
dotnet run --project Nlb.Workshop.Api
```

4. Publish events using `.http` file or curl.
5. Verify projection with `GET /read-model/orders/{orderId}`.

## Replay

Replay from earliest offsets and rebuild projection:

```bash
dotnet run --project Nlb.Workshop.Tools.Replay -- --reset-read-model
```

## Transport Switching (Event Hubs SDK vs Kafka Protocol)

Set in `appsettings.json` or environment variables:

- `Messaging:Provider=EventHubs` -> uses `Azure.Messaging.EventHubs`
- `Messaging:Provider=Kafka` -> uses `Confluent.Kafka`
- Default Kafka broker for local runs is `localhost:9092` (Event Hubs emulator Kafka endpoint).

## Serialization Switching (JSON vs Avro)

Set:

- `Messaging:Serialization:DefaultFormat=json` (core path)
- `Messaging:Serialization:DefaultFormat=avro` (advanced path)

## Workshop Agenda and Labs

- Agenda: `docs/workshop-agenda.md`
- Lab guide: `docs/lab-guide.md`

## Build

```bash
dotnet build nlb-workshop-evh.slnx
```
