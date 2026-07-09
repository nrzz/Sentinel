# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.7.0] — 2026-07-09

### Added

- API-key authentication for log/metric/trace ingestion (`X-Api-Key` or `Bearer sent_*`)
- RBAC permission policies enforced per API feature group
- Production configuration validation (JWT secrets, CORS, ingestion auth, seeded admin)
- `POST /api/v1/alerts/executions/{id}/silence` and `GET /api/v1/alerts/executions`
- `PATCH /api/v1/plugins/{id}` toggle endpoint
- `PATCH /api/v1/incidents/{id}` partial update endpoint
- CodeQL security scanning workflow
- CI gate for vulnerable NuGet packages (`dotnet list package --vulnerable`)
- Helm chart v0.7.0 and release pipeline

### Changed

- Frontend aligned to backend API contracts (alert rules/executions, plugin status, incident fields)
- API enums serialized as camelCase strings (integers still accepted on input)
- CORS restricted to configured origins in production; permissive only in Development
- Open registration and default admin seeding gated behind `Security` config flags
- Release workflow integration tests are now blocking

### Fixed

- Frontend/backend mismatches for alerts, plugins, and incidents
- `release.yml` masking integration test failures with `continue-on-error`

## [0.1.0] — 2026-03-01

Initial public scaffolding: .NET 10 API, React frontend, Docker Compose stack, JWT auth, and CI pipeline.

[Unreleased]: https://github.com/nrzz/Sentinel/compare/v0.7.0...HEAD
[0.7.0]: https://github.com/nrzz/Sentinel/compare/v0.1.0...v0.7.0
[0.1.0]: https://github.com/nrzz/Sentinel/releases/tag/v0.1.0
