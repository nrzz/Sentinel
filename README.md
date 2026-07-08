# Sentinel

Sentinel is an open-source observability platform for logs, metrics, traces, alerts, and incident management. It provides a unified view of your distributed systems with real-time ingestion, powerful querying, and an extensible plugin architecture.

## Features

- **Logs** — High-throughput log ingestion with live streaming via SignalR
- **Metrics** — Time-series metrics with PromQL-style querying
- **Traces** — Distributed tracing with span visualization
- **Alerts** — Rule-based alerting with silencing and escalation
- **Incidents** — Incident lifecycle management linked to alerts
- **Dashboards** — Customizable dashboards for your observability data
- **Plugins** — Extensible architecture for integrations and custom processors
- **AI** — Built-in AI assistance for log analysis and root-cause investigation

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│  sentinel-  │────▶│  sentinel-   │────▶│  PostgreSQL │
│    web      │     │    api       │     │  ClickHouse │
│  (React)    │     │  (.NET 10)   │     │  Redis      │
└─────────────┘     └──────┬───────┘     │  RabbitMQ   │
                           │             └─────────────┘
                    ┌──────▼───────┐
                    │  sentinel-   │
                    │   worker     │
                    └──────────────┘
```

See [docs/architecture.md](docs/architecture.md) for detailed system design.

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Docker](https://www.docker.com/) and Docker Compose

### Run with Docker Compose

```bash
cd docker
cp .env.example .env
docker compose up -d
```

| Service       | URL                          |
|---------------|------------------------------|
| Web UI        | http://localhost:3000        |
| API           | http://localhost:5018        |
| API Docs      | http://localhost:5018/scalar |
| RabbitMQ Mgmt | http://localhost:15672       |

### Local Development

**Backend:**

```bash
dotnet restore Sentinel.slnx
dotnet run --project src/Sentinel.Api
```

**Frontend:**

```bash
cd src/sentinel-web
npm install
npm run dev
```

The Vite dev server proxies `/api` and `/hubs` to the API at `http://localhost:5018`.

## Project Structure

```
Sentinel/
├── src/
│   ├── Sentinel.Api/          # ASP.NET Core API
│   ├── Sentinel.Domain/       # Domain models and interfaces
│   ├── Sentinel.Infrastructure/ # Data access, messaging, health checks
│   ├── Sentinel.Workers/      # Background processing workers
│   └── sentinel-web/          # React frontend
├── tests/
│   ├── Sentinel.UnitTests/
│   ├── Sentinel.IntegrationTests/
│   └── Sentinel.Benchmarks/
├── docker/                    # Dockerfiles and compose
├── design/adr/                # Architecture Decision Records
└── docs/                      # Documentation
```

## Documentation

- [Architecture](docs/architecture.md)
- [Contributing](CONTRIBUTING.md)
- [Roadmap](ROADMAP.md)
- [Changelog](CHANGELOG.md)
- [Security Policy](SECURITY.md)
- [Support](SUPPORT.md)

## License

MIT — see [LICENSE](LICENSE).
