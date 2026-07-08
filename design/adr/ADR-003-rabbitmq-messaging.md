# ADR-003: RabbitMQ for Async Message Processing

## Status

Accepted

## Context

Sentinel ingests telemetry data (logs, metrics, traces) at high throughput. The API must respond quickly to ingestion requests while processing (parsing, enrichment, indexing, alerting) happens asynchronously. We need a message broker that supports:

- Reliable delivery with at-least-once semantics
- Multiple consumer types (workers) for different processing pipelines
- Dead-letter queues for failed messages
- Topic-based routing for different event types

## Decision

Use **RabbitMQ 4** as the message broker between the API and worker services.

Message flow:
```
Ingestion API → RabbitMQ Exchange → Queues → Workers
                                          ├── Log Processor
                                          ├── Metric Aggregator
                                          ├── Trace Indexer
                                          └── Alert Evaluator
```

Topology is initialized at API startup via `IRabbitMqTopologyInitializer`. Workers consume from dedicated queues bound to topic exchanges.

## Consequences

**Positive:**
- Decouples ingestion from processing — API stays responsive
- Workers can scale independently based on queue depth
- Dead-letter queues capture failed messages for investigation
- RabbitMQ management UI aids debugging in development
- Mature ecosystem with well-understood operational patterns

**Negative:**
- Additional infrastructure component to deploy and monitor
- Message ordering not guaranteed across consumers (acceptable for telemetry)
- At-least-once delivery requires idempotent processing in workers
- Network partition scenarios need careful handling

## Alternatives Considered

- **In-process channels (System.Threading.Channels)** — No persistence; data loss on crash. Insufficient for production.
- **Redis Streams** — Simpler but less feature-rich for routing and dead-letter handling.
- **Apache Kafka** — Overkill for initial scale; higher operational complexity.
