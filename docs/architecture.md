# Sentinel Architecture

## Overview

Sentinel is a self-hosted observability platform that collects, stores, queries, and visualizes telemetry data from distributed systems. It follows a modular architecture with clear separation between ingestion, processing, storage, and presentation layers.

## System Context

```
                    ┌──────────────────────────────────────────────┐
                    │              External Systems               │
                    │  ┌─────────┐  ┌──────────┐  ┌───────────┐ │
                    │  │ OTel    │  │ App Logs │  │  Agents   │ │
                    │  │Collector│  │  (HTTP)  │  │  (gRPC)   │ │
                    │  └────┬────┘  └────┬─────┘  └─────┬─────┘ │
                    └───────┼────────────┼──────────────┼────────┘
                            │            │              │
                    ┌───────▼────────────▼──────────────▼────────┐
                    │              Sentinel Platform               │
                    │  ┌──────────┐  ┌──────────┐  ┌────────────┐ │
                    │  │   Web    │  │   API    │  │  Workers   │ │
                    │  │  (React) │  │  (.NET)  │  │  (.NET)    │ │
                    │  └──────────┘  └────┬─────┘  └─────┬──────┘ │
                    │                     │                │        │
                    │  ┌──────────────────▼────────────────▼─────┐ │
                    │  │           Infrastructure Layer           │ │
                    │  │  PostgreSQL │ ClickHouse │ Redis │ RMQ  │ │
                    │  └─────────────────────────────────────────┘ │
                    └──────────────────────────────────────────────┘
```

## Component Architecture

### Sentinel.Api

ASP.NET Core 10 minimal API serving as the primary entry point.

**Responsibilities:**
- REST API at `/api/v1/*` for all CRUD and query operations
- gRPC services for high-throughput telemetry ingestion
- SignalR hubs at `/hubs/*` for real-time streaming
- JWT authentication and authorization
- OpenTelemetry instrumentation (traces and metrics exported via OTLP)
- Database migrations on startup
- RabbitMQ topology initialization

**Feature slices** (vertical slice architecture):
| Slice | Endpoints | Description |
|---|---|---|
| Authentication | `/api/v1/auth/*` | Login, refresh, logout |
| Users | `/api/v1/users/*` | User management |
| Tenants | `/api/v1/tenants/*` | Multi-tenant support |
| Logs | `/api/v1/logs/*` | Log query and search |
| Metrics | `/api/v1/metrics/*` | Metric query |
| Traces | `/api/v1/traces/*` | Trace query and detail |
| Alerts | `/api/v1/alerts/*` | Alert management |
| Incidents | `/api/v1/incidents/*` | Incident lifecycle |
| Dashboards | `/api/v1/dashboards/*` | Dashboard CRUD |
| Plugins | `/api/v1/plugins/*` | Plugin registry |
| Search | `/api/v1/search/*` | Cross-signal search |
| AI | `/api/v1/ai/*` | AI-assisted analysis |

### Sentinel.Workers

Background service for async processing.

**Responsibilities:**
- Consume messages from RabbitMQ queues
- Process and enrich ingested telemetry
- Evaluate alert rules against incoming data
- Index logs and traces in ClickHouse
- Aggregate metrics into rollups

### Sentinel.Domain

Pure domain layer with no infrastructure dependencies.

**Contains:**
- Entity base classes (`Entity`, `Result<T>`)
- Configuration options (`SentinelOptions`, `JwtOptions`, etc.)
- Domain interfaces and value objects
- Correlation ID utilities

### Sentinel.Infrastructure

Infrastructure implementations for all external systems.

**Contains:**
- PostgreSQL connection factory and migration runner
- ClickHouse connection factory and migrator
- Redis connection factory
- RabbitMQ connection factory, publisher, and topology initializer
- Health checks for all dependencies

### sentinel-web

React 19 SPA served via nginx in production.

**Architecture:**
```
src/
├── components/     # Reusable UI (layout, command palette)
├── pages/          # Route-level page components
├── stores/         # Zustand stores (auth)
├── lib/
│   ├── api/        # REST API client modules
│   └── signalr/    # SignalR hub connections
├── hooks/          # Custom React hooks
├── types/          # TypeScript type definitions
└── routes/         # React Router configuration
```

**State management:**
- **Server state:** TanStack Query (caching, refetching, mutations)
- **Client state:** Zustand (auth tokens in memory)
- **Real-time:** SignalR hub connections for live log streaming

## Data Flow

### Log Ingestion

```
Agent/App → gRPC/HTTP → API → RabbitMQ → Worker → ClickHouse
                                              ↓
                                         SignalR Hub → Web UI (live)
```

1. Application sends log batch via gRPC (`LogIngestionGrpcService`) or HTTP
2. API validates, assigns correlation ID, publishes to RabbitMQ `logs.ingest` queue
3. Worker consumes, parses, enriches (service name, severity), writes to ClickHouse
4. Worker publishes to SignalR for real-time subscribers
5. Web UI queries historical logs via REST, receives live logs via SignalR

### Metric Ingestion

```
OTel Collector → gRPC → API → RabbitMQ → Worker → ClickHouse (aggregated)
```

1. OpenTelemetry Collector exports metrics via gRPC
2. API publishes to `metrics.ingest` queue
3. Worker aggregates data points into time-series rollups
4. Web UI queries via `/api/v1/metrics/query`

### Alert Evaluation

```
Worker (metric/log processor) → Alert Engine → RabbitMQ → Notification Worker
                                                       → SignalR Hub → Web UI
```

1. Workers evaluate alert rules against processed telemetry
2. Firing alerts published to `alerts.fired` queue
3. Alert state persisted in PostgreSQL
4. SignalR pushes alert updates to connected clients

## Storage Design

### PostgreSQL (Metadata)

| Table | Purpose |
|---|---|
| users | User accounts and credentials |
| tenants | Multi-tenant isolation |
| refresh_tokens | JWT refresh token storage |
| alert_rules | Alert rule definitions |
| incidents | Incident records and status |
| dashboards | Dashboard definitions and panels |
| plugins | Plugin registry and configuration |

### ClickHouse (Telemetry)

| Table | Engine | Partition | Order By |
|---|---|---|---|
| logs | MergeTree | toYYYYMM(timestamp) | (service, timestamp) |
| metrics | MergeTree | toYYYYMM(timestamp) | (name, labels_hash, timestamp) |
| traces | MergeTree | toYYYYMM(start_time) | (trace_id, start_time) |

Retention policies configured per table (default: 30 days logs, 90 days metrics, 7 days traces).

### Redis

- Query result caching (TTL-based)
- Rate limiting counters
- Pub/sub for cross-instance SignalR (future)

## Security

- **Authentication:** JWT bearer tokens with refresh rotation
- **Authorization:** Role-based access control (planned v0.4)
- **Transport:** TLS termination at reverse proxy (production)
- **Secrets:** Environment variables, never committed to source
- **Frontend:** Tokens in memory only, no localStorage persistence

## Deployment Topology

### Development (Docker Compose)

Single-node deployment with all services in one Docker Compose stack. See [ADR-006](../design/adr/ADR-006-docker-compose-dev-environment.md).

### Production (Future)

```
┌─────────────┐
│ Load Balancer│
└──────┬──────┘
       │
  ┌────▼─────┐    ┌──────────────┐
  │  nginx   │    │  sentinel-api │ ×N
  │  (web)   │    │  (stateless)  │
  └──────────┘    └──────┬───────┘
                         │
              ┌──────────▼──────────┐
              │  sentinel-worker ×N │
              └──────────┬──────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
    PostgreSQL     ClickHouse       RabbitMQ
    (primary +        (cluster)      (cluster)
     replica)            Redis
                      (cluster)
```

## Observability of Sentinel Itself

Sentinel practices what it preaches:

- **Self-instrumentation:** OpenTelemetry traces and metrics from the API
- **Health endpoints:** `/health` (liveness), `/ready` (readiness with dependency checks)
- **Structured logging:** Correlation IDs propagated through all layers
- **OTLP export:** Traces and metrics exported to configured OTLP endpoint

## Related Documents

- [ADR-001: Vertical Slice Architecture](../design/adr/ADR-001-vertical-slice-architecture.md)
- [ADR-002: Dual Database Storage](../design/adr/ADR-002-dual-database-storage.md)
- [ADR-003: RabbitMQ Messaging](../design/adr/ADR-003-rabbitmq-messaging.md)
- [ADR-004: JWT Authentication](../design/adr/ADR-004-jwt-authentication.md)
- [ADR-005: React Frontend Stack](../design/adr/ADR-005-react-frontend-stack.md)
- [ADR-006: Docker Compose Dev Environment](../design/adr/ADR-006-docker-compose-dev-environment.md)
- [ADR-007: GitHub Actions CI](../design/adr/ADR-007-github-actions-ci.md)
