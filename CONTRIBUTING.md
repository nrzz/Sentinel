# Contributing to Sentinel

Thank you for your interest in contributing to Sentinel! This document provides guidelines for contributing to the project.

## Getting Started

1. Fork the repository and clone your fork.
2. Install prerequisites: .NET 10 SDK, Node.js 22+, Docker.
3. Copy `docker/.env.example` to `docker/.env` and start infrastructure:
   ```bash
   cd docker && docker compose up -d postgres clickhouse redis rabbitmq
   ```
4. Run the API and frontend locally (see [README](README.md#local-development)).

## Development Workflow

1. Create a feature branch from `develop`:
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/your-feature-name
   ```
2. Make your changes following the coding standards below.
3. Run tests locally:
   ```bash
   dotnet test Sentinel.slnx
   dotnet format Sentinel.slnx --verify-no-changes
   cd src/sentinel-web && npm run build
   ```
4. Commit with clear, descriptive messages.
5. Open a pull request against `develop`.

## Coding Standards

### C# / .NET

- Follow [.NET coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- Use `dotnet format` before committing — CI enforces formatting.
- Write unit tests for new business logic.
- Use vertical slice architecture for API features (`Features/` folder).
- Keep domain logic in `Sentinel.Domain`, infrastructure in `Sentinel.Infrastructure`.

### TypeScript / React

- Use functional components with hooks.
- Colocate API clients in `src/lib/api/`.
- Use TanStack Query for server state, Zustand for client state.
- Follow existing Tailwind CSS patterns (dark mode default).

### Architecture Decisions

Significant design changes require an [Architecture Decision Record](design/adr/). Use the next available ADR number and follow the existing format.

## Pull Request Guidelines

- Keep PRs focused — one feature or fix per PR.
- Include a clear description of what changed and why.
- Reference related issues (e.g., `Closes #42`).
- Ensure CI passes before requesting review.
- Add or update tests for behavioral changes.

## Reporting Issues

- Use GitHub Issues with the appropriate template.
- Include reproduction steps, expected vs. actual behavior, and environment details.
- For security vulnerabilities, see [SECURITY.md](SECURITY.md).

## Code of Conduct

All contributors are expected to follow our [Code of Conduct](CODE_OF_CONDUCT.md).
