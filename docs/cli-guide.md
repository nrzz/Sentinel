# Sentinel CLI Guide

The Sentinel CLI (`sentinel`) is a .NET global tool for authenticating, querying logs, managing alerts, inspecting incidents, and listing tenants from the command line.

## Installation

### From this repository

```bash
dotnet pack cli/Sentinel.CLI/Sentinel.CLI.csproj -c Release
dotnet tool install --global --add-source cli/Sentinel.CLI/bin/Release sentinel
```

### Local development

```bash
dotnet run --project cli/Sentinel.CLI -- --help
```

## Global Options

| Option | Default | Description |
|--------|---------|-------------|
| `--base-url` | `http://localhost:5000` | Sentinel API base URL |
| `--output` | `json` | Output format: `json` or `yaml` |

## Authentication

### Login

Authenticate and persist credentials to `~/.sentinel/config.json`:

```bash
sentinel auth login --email user@example.com --password secret --tenant-id <guid>
```

With YAML output:

```bash
sentinel auth login --email user@example.com --password secret --output yaml
```

Subsequent commands read the stored access token, refresh token, and tenant ID automatically.

## Commands

### Search logs

```bash
sentinel logs search --query "error" --level error --service api --limit 50
```

Supported filters:

- `--query` — full-text search
- `--level` — log level
- `--service` — service name
- `--environment` — deployment environment
- `--from` / `--to` — ISO 8601 timestamps
- `--limit` / `--offset` — pagination

### List alerts

```bash
sentinel alerts list
sentinel alerts list --output yaml
```

### Show incident

```bash
sentinel incidents show <incident-id>
```

### List tenants

```bash
sentinel tenants list
```

## Output Formats

### JSON (default)

```bash
sentinel tenants list --output json
```

### YAML

```bash
sentinel logs search --query timeout --output yaml
```

## Configuration File

After `auth login`, credentials are stored at:

```
~/.sentinel/config.json
```

Example structure:

```json
{
  "baseUrl": "http://localhost:5000",
  "accessToken": "<jwt>",
  "refreshToken": "<refresh>",
  "tenantId": "<guid>",
  "email": "user@example.com"
}
```

## Environment Overrides

Use `--base-url` to target a different Sentinel deployment without editing the config file:

```bash
sentinel --base-url https://sentinel.example.com tenants list
```

## Exit Codes

- `0` — success
- `1` — API or validation error (error details written to stdout in the selected output format)

## Related Documentation

- [SDK Guide](./sdk-guide.md) — programmatic access with .NET, Node, Go, and Python SDKs
- [Architecture](./architecture.md) — platform overview
