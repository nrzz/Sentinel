# ADR-006: Docker Compose for Local Development

## Status

Accepted

## Context

Sentinel depends on four infrastructure services (PostgreSQL, ClickHouse, Redis, RabbitMQ) plus two application services (API, Worker) and a web frontend. Developers need a consistent, reproducible environment that:

- Starts all dependencies with one command
- Uses health checks to ensure proper startup ordering
- Persists data across restarts via named volumes
- Runs containers as non-root users where possible
- Mirrors production topology closely enough for meaningful testing

## Decision

Provide a `docker/docker-compose.yml` that orchestrates the full stack:

| Service | Image | Port | Purpose |
|---|---|---|---|
| postgres | postgres:17-alpine | 5432 | Metadata storage |
| clickhouse | clickhouse-server:24.12 | 8123/9000 | Telemetry storage |
| redis | redis:7-alpine | 6379 | Caching, pub/sub |
| rabbitmq | rabbitmq:4-management | 5672/15672 | Message broker |
| sentinel-api | Dockerfile.api | 5018 | .NET API |
| sentinel-worker | Dockerfile.worker | — | Background processing |
| sentinel-web | Dockerfile.web | 3000 | React UI via nginx |

Configuration via `docker/.env.example` (copied to `.env`). Named volumes for all stateful services. Health checks gate dependent service startup.

Developers can also run infrastructure-only:
```bash
docker compose up -d postgres clickhouse redis rabbitmq
```
Then run API and frontend locally for faster iteration.

## Consequences

**Positive:**
- One-command full stack for new contributors
- Consistent environment across team members and CI
- Health checks prevent race conditions on startup
- Named volumes preserve data between restarts
- `.env.example` documents all configuration options

**Negative:**
- Docker resource usage (~4 GB RAM for full stack)
- ClickHouse container is slow to start on first run
- Not a production deployment solution (no TLS, single-node, default credentials)
- Windows/Mac Docker performance overhead for volume mounts

## Alternatives Considered

- **Dev containers (VS Code)** — Good but requires VS Code; Docker Compose is more universal.
- **Tilt/Skaffold** — Overkill for current scale; may adopt for Kubernetes dev later.
- **Embedded databases (SQLite, LiteDB)** — Cannot replicate ClickHouse analytical performance.
