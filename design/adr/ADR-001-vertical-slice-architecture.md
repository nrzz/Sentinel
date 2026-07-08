# ADR-001: Use Vertical Slice Architecture for the API

## Status

Accepted

## Context

Sentinel's API needs to support multiple observability domains (logs, metrics, traces, alerts, incidents, dashboards, plugins). We need an architecture that:

- Keeps related code together (endpoints, handlers, validators, DTOs)
- Scales as we add new features without creating deep folder hierarchies
- Allows independent development of feature areas
- Aligns with ASP.NET Core minimal API patterns

## Decision

We will organize the API using **vertical slice architecture**, with each feature domain in its own folder under `src/Sentinel.Api/Features/`:

```
Features/
├── Authentication/
├── Logs/
├── Metrics/
├── Traces/
├── Alerts/
├── Incidents/
├── Dashboards/
└── Plugins/
```

Each slice contains its own endpoints, request/response models, validators, and service registrations via extension methods (e.g., `AddLogFeatures()`, `MapLogEndpoints()`).

Shared infrastructure (database access, messaging, caching) lives in `Sentinel.Infrastructure`. Domain models and interfaces live in `Sentinel.Domain`.

## Consequences

**Positive:**
- Features are self-contained and easy to navigate
- New features can be added without modifying unrelated code
- Clear boundaries for testing and code review
- Natural fit for minimal APIs and endpoint groups

**Negative:**
- Some code duplication across slices (mitigated by shared infrastructure)
- Cross-cutting concerns (auth, validation) require consistent patterns across slices
- Requires discipline to avoid slices depending on each other directly

## Alternatives Considered

- **Layered architecture (Controllers → Services → Repositories)** — Rejected due to excessive indirection and files scattered across layers for a single feature.
- **CQRS with MediatR** — Considered for v0.2; may adopt for complex query/command separation later.
