# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 0.7.x   | :white_check_mark: |
| 0.1.x   | :x:                |

## Reporting a Vulnerability

We take the security of Sentinel seriously. If you discover a security vulnerability, please report it responsibly.

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, email **security@sentinel-observability.dev** with:

- A description of the vulnerability
- Steps to reproduce the issue
- Potential impact assessment
- Any suggested fixes (optional)

You should receive a response within 48 hours. We will work with you to understand and address the issue promptly.

## Disclosure Policy

- We will confirm receipt of your report within 48 hours.
- We will provide regular updates on our progress.
- Once a fix is available, we will coordinate disclosure timing with you.
- We aim to resolve critical vulnerabilities within 7 days.

## Security Best Practices

When deploying Sentinel:

- **Change default credentials** — The dev seeder creates `admin@sentinel.local` / `Admin123!` when `Security:SeedDefaultAdmin` is true. Set `Security:SeedDefaultAdmin` to `false` in production.
- **Require API keys for ingestion** — Set `Ingestion:RequireApiKey` to `true` and `Ingestion:AllowHeaderOnlyTenant` to `false` in production.
- **Rotate JWT secrets** — Use a strong, unique `JWT_SECRET_KEY` (minimum 32 characters).
- **Enable TLS** — Terminate TLS at your reverse proxy or load balancer.
- **Restrict network access** — Do not expose database ports publicly.
- **Keep dependencies updated** — Monitor for security advisories in .NET and npm packages.
- **Use secrets management** — Prefer environment variables or a secrets manager over config files for production credentials.

## Known Security Considerations

- JWT tokens are stored in memory on the frontend (not localStorage) to reduce XSS exposure.
- SignalR connections require authentication via JWT bearer tokens.
- The API uses CORS — configure appropriately for your deployment.
- Default Docker Compose configuration is for development only.
