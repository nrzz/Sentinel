# ADR-002: PostgreSQL for Metadata, ClickHouse for Telemetry

## Status

Accepted

## Context

Sentinel stores two categories of data:

1. **Metadata** — Users, tenants, alert rules, dashboards, incidents, plugin configs. Relatively low volume, requires ACID transactions, relational queries, and foreign key constraints.
2. **Telemetry** — Logs, metrics, traces. High volume, append-heavy, time-range queries, columnar analytics.

Using a single database for both would force compromises on either transactional integrity or analytical performance.

## Decision

- **PostgreSQL 17** for all metadata and configuration data
- **ClickHouse 24** for all telemetry data (logs, metrics, traces)

PostgreSQL handles:
- User/tenant management
- Alert rules and incident records
- Dashboard definitions
- Plugin registry
- Refresh tokens and sessions

ClickHouse handles:
- Log entries (partitioned by date, ordered by timestamp)
- Metric data points (aggregated time series)
- Trace spans (ordered by trace ID and timestamp)

## Consequences

**Positive:**
- Each database optimized for its workload
- ClickHouse provides sub-second queries over billions of log rows
- PostgreSQL provides reliable transactions for critical metadata
- Clear data lifecycle — telemetry retention policies independent of metadata

**Negative:**
- Two databases to operate, monitor, and back up
- Cross-database queries not possible (must join in application layer)
- Schema migrations needed for both databases
- Increased infrastructure complexity in development and deployment

## Alternatives Considered

- **PostgreSQL only with TimescaleDB** — Good for metrics but less optimal for high-cardinality log search at scale.
- **Elasticsearch** — Strong log search but operational complexity and resource usage are high for a self-hosted platform.
- **Single ClickHouse** — Lacks ACID transactions needed for user management and incident workflows.
