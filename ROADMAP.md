# Roadmap

## v0.7.0 — Current Release

Shipped capabilities:

- [x] Core API with vertical slice architecture
- [x] PostgreSQL for metadata, ClickHouse for telemetry
- [x] RabbitMQ message bus for async processing
- [x] JWT authentication with refresh tokens and RBAC permission policies
- [x] Tenant API key authentication for ingestion endpoints
- [x] Log, metric, and trace ingestion via HTTP and gRPC
- [x] Log search, live streaming (SignalR), and saved searches
- [x] Metric and trace querying
- [x] Alert rules, executions, and silencing
- [x] Incident management with timeline and comments
- [x] Custom dashboards
- [x] Plugin install/toggle lifecycle
- [x] AI-assisted search, summarization, and correlation
- [x] React 19 frontend with dark mode UI
- [x] Docker Compose development environment
- [x] Helm chart for Kubernetes (v0.7.0)
- [x] CI pipeline (build, test, format, CodeQL, vulnerable-package check)
- [x] Release pipeline (Docker images, Helm package)
- [x] Operations and disaster recovery documentation

## v0.8.0 — Reliability & Runtime

- [ ] DB-driven alert evaluation worker
- [ ] Plugin runtime host for custom processors
- [ ] Redis-backed query result caching
- [ ] Testcontainers-based integration test suite
- [ ] Frontend test framework buildout

## v0.9.0 — Scale & Performance

- [ ] Horizontal API scaling with Redis-backed sessions
- [ ] ClickHouse partitioning and retention policies
- [ ] Log sampling and rate limiting
- [ ] Benchmark suite with performance baselines and k6 load tests

## v1.0.0 — Production Hardening

- [ ] SSO integration (OIDC, SAML)
- [ ] Webhook notifications (Slack, PagerDuty, Teams)
- [ ] Service map from trace data
- [ ] OpenTelemetry collector integration guide
- [ ] SOC 2 compliance documentation
- [ ] Performance SLA benchmarks

## How to Influence the Roadmap

- Open a GitHub Discussion with the `roadmap` label
- Vote on existing feature requests
- Contribute ADRs for proposed architectural changes

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines.
