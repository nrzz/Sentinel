# Support

## Getting Help

### Documentation

- [README](README.md) — Quick start and project overview
- [Architecture](docs/architecture.md) — System design and data flow
- [Contributing](CONTRIBUTING.md) — Development setup and guidelines
- [Roadmap](ROADMAP.md) — Planned features and milestones

### Community

- **GitHub Issues** — Bug reports and feature requests
- **GitHub Discussions** — Questions, ideas, and community support

### Commercial Support

Enterprise support, custom integrations, and SLA-backed deployments are available. Contact **support@sentinel-observability.dev** for details.

## Troubleshooting

### API won't start

1. Verify infrastructure services are healthy:
   ```bash
   cd docker && docker compose ps
   ```
2. Check connection strings in `appsettings.json` or environment variables.
3. Review API logs: `docker compose logs sentinel-api`

### Frontend can't reach API

1. Ensure the API is running on port 5018.
2. In development, the Vite proxy handles `/api` routing — no CORS config needed.
3. In production, verify nginx proxy settings in `docker/nginx.conf`.

### Database migration failures

1. Ensure PostgreSQL and ClickHouse are healthy before starting the API.
2. Check credentials match between `.env` and connection strings.
3. Review migration logs in API startup output.

### SignalR connection issues

1. Verify JWT token is valid and not expired.
2. Check that `/hubs/logs` is proxied correctly (WebSocket upgrade required).
3. Review browser console for connection errors.

## Version Compatibility

| Component    | Minimum Version |
|--------------|-----------------|
| .NET SDK     | 10.0            |
| Node.js      | 22.0            |
| PostgreSQL   | 16              |
| ClickHouse   | 24.x            |
| Redis        | 7.0             |
| RabbitMQ     | 3.13            |
| Docker       | 24.0            |
