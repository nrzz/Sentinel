# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial project scaffolding with .NET 10 API, Workers, Domain, and Infrastructure layers
- React 19 frontend with Vite, TypeScript, and Tailwind CSS
- Dark mode UI with sidebar navigation and command palette (Ctrl+K)
- Pages: Login, Overview, Logs, Metrics, Traces, Alerts, Incidents, Dashboards, Plugins, Settings
- JWT authentication store with in-memory tokens and refresh flow
- API client module for `/api/v1` endpoints
- SignalR integration for live log streaming
- Virtualized log table with `@tanstack/react-virtual`
- Metrics charts with Recharts
- Docker Compose stack: PostgreSQL, ClickHouse, Redis, RabbitMQ, API, Worker, Web
- Multi-stage Dockerfiles with non-root users and health checks
- GitHub Actions CI: .NET build/test/format, frontend build, integration tests
- Governance docs: README, CONTRIBUTING, CODE_OF_CONDUCT, SECURITY, SUPPORT
- Architecture Decision Records (ADR-001 through ADR-007)
- Architecture documentation

### Changed

- N/A (initial release)

### Fixed

- N/A (initial release)

## [0.1.0] — TBD

First public release. See [ROADMAP.md](ROADMAP.md) for planned features.

[Unreleased]: https://github.com/sentinel-observability/sentinel/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/sentinel-observability/sentinel/releases/tag/v0.1.0
