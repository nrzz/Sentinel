# ADR-007: GitHub Actions CI Pipeline

## Status

Accepted

## Context

Sentinel is a polyglot project (.NET backend, React frontend, Docker deployment). We need automated quality gates on every push and pull request to:

- Catch build failures early
- Enforce code formatting consistency
- Run unit and integration tests
- Verify frontend builds successfully
- Smoke-test Docker images on main branch merges

## Decision

Implement CI via `.github/workflows/ci.yml` with four jobs:

### 1. dotnet-build
- Restore, build, and run unit tests
- Enforce formatting with `dotnet format --verify-no-changes`
- Collect code coverage via XPlat Code Coverage

### 2. frontend-build
- Install npm dependencies
- Type-check and build the React app (`tsc -b && vite build`)

### 3. integration-tests
- Depends on `dotnet-build`
- Spins up PostgreSQL and Redis as GitHub Actions services
- Runs integration tests against real database instances

### 4. docker-build (main branch only)
- Depends on `dotnet-build` and `frontend-build`
- Builds all three Docker images as a smoke test

Triggers: push and PR to `main` and `develop` branches.

## Consequences

**Positive:**
- Fast feedback on PRs (~5-10 minutes for full pipeline)
- Formatting enforced automatically — no style debates in code review
- Integration tests run against real services, not mocks
- Docker smoke test catches Dockerfile breakage before deployment

**Negative:**
- Integration tests limited to PostgreSQL and Redis (ClickHouse/RabbitMQ not in CI services — may add Testcontainers later)
- No deployment automation yet (planned for v0.2)
- Docker build job only runs on main — PRs don't verify Docker builds

## Alternatives Considered

- **Azure DevOps Pipelines** — Team uses GitHub; keeping CI close to source.
- **Testcontainers in CI** — More complete but slower and more complex; deferred to v0.2.
- **Single monolithic CI job** — Slower feedback; parallel jobs provide faster results.

## Future Enhancements

- [ ] Add Testcontainers for ClickHouse and RabbitMQ in integration tests
- [ ] Deploy to staging on merge to `develop`
- [ ] npm audit and `dotnet list package --vulnerable` security scans
- [ ] Performance regression benchmarks in CI
