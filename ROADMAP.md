# Roadmap

## v0.1.0 — Foundation (Current)

- [x] Core API with vertical slice architecture
- [x] PostgreSQL for metadata, ClickHouse for telemetry
- [x] RabbitMQ message bus for async processing
- [x] JWT authentication with refresh tokens
- [x] React frontend with dark mode UI
- [x] Docker Compose development environment
- [x] CI pipeline (build, test, format check)
- [ ] Log ingestion via gRPC and HTTP
- [ ] Basic log search and live streaming
- [ ] Metric ingestion and querying
- [ ] Trace ingestion and visualization

## v0.2.0 — Observability Core

- [ ] Alert rule engine with threshold and anomaly detection
- [ ] Incident management workflow
- [ ] Custom dashboards with drag-and-drop panels
- [ ] Saved searches and query history
- [ ] Service map from trace data
- [ ] OpenTelemetry collector integration guide

## v0.3.0 — Scale & Performance

- [ ] Horizontal API scaling with Redis-backed sessions
- [ ] ClickHouse partitioning and retention policies
- [ ] Log sampling and rate limiting
- [ ] Query result caching
- [ ] Benchmark suite with performance baselines

## v0.4.0 — Extensibility

- [ ] Plugin SDK for custom processors and exporters
- [ ] Webhook notifications (Slack, PagerDuty, Teams)
- [ ] SSO integration (OIDC, SAML)
- [ ] Multi-tenant isolation
- [ ] RBAC with role-based API authorization

## v0.5.0 — AI & Intelligence

- [ ] AI-powered log anomaly detection
- [ ] Natural language query interface
- [ ] Automated root-cause analysis
- [ ] Incident summarization and postmortem drafts
- [ ] Predictive alerting

## v1.0.0 — Production Ready

- [ ] High-availability deployment guide
- [ ] Backup and disaster recovery procedures
- [ ] Comprehensive API documentation
- [ ] Helm charts for Kubernetes deployment
- [ ] SOC 2 compliance documentation
- [ ] Performance SLA benchmarks

## How to Influence the Roadmap

- Open a GitHub Discussion with the `roadmap` label
- Vote on existing feature requests
- Contribute ADRs for proposed architectural changes

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines.
