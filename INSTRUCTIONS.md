# Attendee Playground: Event-Driven Workshop (.NET)

This guide contains hands-on examples so you can keep practicing after the workshop.

Repository root used in examples:
- `/Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter`

## 0) Start Infrastructure

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
docker compose up -d eventhubs-emulator azurite schema-registry kafka-ui
```

Check containers:

```bash
docker compose ps
```

Kafka UI:
- `http://localhost:8085`

## 1) Start Services (Event Hubs default)

Terminal 1 (consumer):

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
dotnet run --project Nlb.Workshop.Consumer.Worker
```

Terminal 2 (API):

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
dotnet run --project Nlb.Workshop.Api --urls http://localhost:5001
```

Swagger:
- `http://localhost:5001/swagger`

## 2) Basic End-to-End Flow

Health check:

```bash
curl -s http://localhost:5001/health
```

Publish one order and capture `ORDER_ID`:

```bash
RESP=$(curl -s -X POST http://localhost:5001/orders \
  -H "Content-Type: application/json" \
  -d '{"orderId":null,"customerId":"customer-001","amount":130.5,"currency":"EUR","correlationId":null,"useV2":false,"sourceSystem":null}')
echo "$RESP"
ORDER_ID=$(echo "$RESP" | sed -n 's/.*"orderId":"\([^"]*\)".*/\1/p')
echo "$ORDER_ID"
```

Read projection:

```bash
curl -s http://localhost:5001/read-model/orders/$ORDER_ID
```

Note:
- first read can briefly be empty/404 due to eventual consistency; retry after 1 second.

## 3) Keys and Partitions

### 3.1 Same key ordering

```bash
for i in {1..6}; do
  curl -s -X POST http://localhost:5001/orders \
    -H "Content-Type: application/json" \
    -d '{"orderId":null,"customerId":"customer-001","amount":10,"currency":"EUR","correlationId":null,"useV2":false,"sourceSystem":null}' > /dev/null
done
```

Check consumer logs:
- same key should usually stay on same partition
- offsets for that partition should move forward

### 3.2 Different keys across partitions

```bash
for c in customer-001 customer-002 customer-003 customer-004; do
  curl -s -X POST http://localhost:5001/orders \
    -H "Content-Type: application/json" \
    -d "{\"orderId\":null,\"customerId\":\"$c\",\"amount\":20,\"currency\":\"EUR\",\"correlationId\":null,\"useV2\":false,\"sourceSystem\":null}" > /dev/null
done
```

### 3.3 Bulk fan-out

```bash
curl -s -X POST http://localhost:5001/orders/bulk \
  -H "Content-Type: application/json" \
  -d '{"orders":[{"orderId":null,"customerId":"customer-101","amount":31,"currency":"EUR","correlationId":"bulk-1","useV2":false,"sourceSystem":"bulk"},{"orderId":null,"customerId":"customer-102","amount":32,"currency":"EUR","correlationId":"bulk-2","useV2":false,"sourceSystem":"bulk"},{"orderId":null,"customerId":"customer-103","amount":33,"currency":"EUR","correlationId":"bulk-3","useV2":false,"sourceSystem":"bulk"},{"orderId":null,"customerId":"customer-104","amount":34,"currency":"EUR","correlationId":"bulk-4","useV2":false,"sourceSystem":"bulk"}]}'
```

## 4) Consumer Group Scaling (two worker instances)

Start a second consumer in Terminal 3:

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
dotnet run --project Nlb.Workshop.Consumer.Worker
```

Publish traffic:

```bash
for i in {1..30}; do
  c=$(( (i % 6) + 1 ))
  curl -s -X POST http://localhost:5001/orders \
    -H "Content-Type: application/json" \
    -d "{\"orderId\":null,\"customerId\":\"customer-00$c\",\"amount\":$((10 + i)),\"currency\":\"EUR\",\"correlationId\":\"cg-demo-$i\",\"useV2\":false,\"sourceSystem\":\"cg-lab\"}" > /dev/null
done
```

Expected:
- both worker terminals print event logs
- partition work is shared between instances in same group

## 5) Replay (rebuild read model)

Run replay tool:

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
dotnet run --project Nlb.Workshop.Tools.Replay -- --reset-read-model
```

Expected summary:
- read model reset
- replay starts and finishes
- processed events / visited partitions numbers printed

Verify a known order again:

```bash
curl -s http://localhost:5001/read-model/orders/$ORDER_ID
```

## 6) Kafka Transport Mode

Optional refresh of infra:

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
docker compose up -d --force-recreate eventhubs-emulator kafka-ui
```

Terminal 1 (Kafka consumer):

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
Messaging__Provider=Kafka dotnet run --project Nlb.Workshop.Consumer.Worker
```

Terminal 2 (Kafka producer API):

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
Messaging__Provider=Kafka dotnet run --project Nlb.Workshop.Api --urls http://localhost:5001
```

Verify Kafka mode:

```bash
RESP=$(curl -s -X POST http://localhost:5001/orders \
  -H "Content-Type: application/json" \
  -d '{"orderId":null,"customerId":"customer-kafka-001","amount":42,"currency":"EUR","correlationId":"kafka-json","useV2":false,"sourceSystem":"kafka-demo"}')
echo "$RESP"
```

Expected:
- response contains `"payloadFormat":"json"`
- API logs show `Published Kafka event ...`
- worker logs show event received and projected

## 7) Avro Mode (over Kafka provider)

Terminal 1:

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
Messaging__Provider=Kafka Messaging__Serialization__DefaultFormat=avro dotnet run --project Nlb.Workshop.Consumer.Worker
```

Terminal 2:

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
Messaging__Provider=Kafka Messaging__Serialization__DefaultFormat=avro dotnet run --project Nlb.Workshop.Api --urls http://localhost:5001
```

Verify Avro mode:

```bash
RESP=$(curl -s -X POST http://localhost:5001/orders \
  -H "Content-Type: application/json" \
  -d '{"orderId":null,"customerId":"customer-avro-001","amount":84,"currency":"EUR","correlationId":"kafka-avro","useV2":true,"sourceSystem":"avro-demo"}')
echo "$RESP"
```

Expected:
- response contains `"payloadFormat":"avro"`
- worker logs show `format=avro`
- read model endpoint still returns normal JSON projection

## 8) Quick SQLite Checks

Show latest 10 orders:

```bash
cd /Users/pinokokol/Documents/GitHub/nlb-workshop-evh-starter
sqlite3 nlb-workshop.db "select OrderId, CustomerId, Amount, Currency, LastEventVersion from Orders order by UpdatedAt desc limit 10;"
```

Counts:

```bash
sqlite3 nlb-workshop.db "select count(*) as orders from Orders; select count(*) as processed_events from ProcessedEvents;"
```

## 9) Common Problems

### Docker image pull timeout / proxy

Symptom:
- cannot pull images from `mcr.microsoft.com`

Fix:
- configure Docker Desktop HTTPS proxy
- or pre-pull images manually

```bash
docker pull mcr.microsoft.com/azure-messaging/eventhubs-emulator:latest
docker pull mcr.microsoft.com/azure-storage/azurite:latest
docker pull apicurio/apicurio-registry-mem:2.6.2.Final
docker pull provectuslabs/kafka-ui:latest
```

### Kafka disconnect (`1/1 brokers are down`)

This usually means protocol/auth mismatch. In this repo Kafka settings are preconfigured for Event Hubs emulator (SASL/PLAIN with connection string). If needed, recreate infra:

```bash
docker compose up -d --force-recreate eventhubs-emulator kafka-ui
```

## 10) Stop Everything

```bash
docker compose down
```

To also remove read model volume:

```bash
docker compose down -v
```
