# Sentinel SDK Guide

Sentinel provides official client libraries for .NET, Node.js, Go, and Python. All SDKs expose the same API surface for authentication, log ingestion and search, metrics, alerts, incidents, and tenant management.

## Installation

### .NET

```bash
dotnet add package Sentinel.Sdk
```

For local development in this repository:

```bash
dotnet add reference ../../sdk/dotnet/Sentinel.Sdk/Sentinel.Sdk.csproj
```

### Node.js

```bash
npm install @sentinel/sdk
```

### Go

```bash
go get github.com/sentinel-observability/sentinel-go
```

### Python

```bash
pip install sentinel-sdk
```

## Quick Start

### .NET

```csharp
using Sentinel.Sdk;
using Sentinel.Sdk.Models;

var client = new SentinelClient(new SentinelClientOptions
{
    BaseUrl = "http://localhost:5000"
});

var auth = await client.LoginAsync(new LoginRequest("user@example.com", "password"));
client.SetAccessToken(auth.AccessToken, auth.User.TenantId);

await client.IngestLogsAsync(new IngestLogsRequest(
[
    new LogEntryInput("api", "production", "info", "Request completed")
]));

var logs = await client.SearchLogsAsync(new LogSearchQuery(Query: "completed", Limit: 20));
```

### Node.js

```typescript
import { SentinelClient } from '@sentinel/sdk';

const client = new SentinelClient({ baseUrl: 'http://localhost:5000' });
await client.login({ email: 'user@example.com', password: 'password' });

await client.ingestLogs([
  { service: 'api', environment: 'production', level: 'info', message: 'Request completed' }
]);

const logs = await client.searchLogs({ query: 'completed', limit: 20 });
```

### Go

```go
client := sentinel.NewClient(sentinel.ClientOptions{BaseURL: "http://localhost:5000"})
auth, err := client.Login(ctx, sentinel.LoginRequest{
    Email:    "user@example.com",
    Password: "password",
})

_, err = client.IngestLogs(ctx, []sentinel.LogEntryInput{
    {Service: "api", Environment: "production", Level: "info", Message: "Request completed"},
})
```

### Python

```python
from sentinel_sdk import LogEntryInput, LoginRequest, SentinelClient

client = SentinelClient(base_url="http://localhost:5000")
client.login(LoginRequest(email="user@example.com", password="password"))

client.ingest_logs([
    LogEntryInput(service="api", environment="production", level="info", message="Request completed")
])
```

## Authentication

All SDKs support:

- `login` / `LoginAsync` — exchange email and password for JWT access and refresh tokens
- `refreshToken` / `RefreshTokenAsync` — renew an access token
- `setAccessToken` — configure a pre-issued token and tenant ID

Tenant scoping uses the `X-Tenant-ID` header. When authenticated, the tenant claim from the JWT is used automatically if no header is supplied.

## API Coverage

| Capability | .NET | Node | Go | Python |
|------------|------|------|----|--------|
| Auth login / refresh | Yes | Yes | Yes | Yes |
| Ingest logs | Yes | Yes | Yes | Yes |
| Search logs | Yes | Yes | Yes | Yes |
| Ingest metrics | Yes | Yes | Yes | Yes |
| Query metrics | Yes | Yes | Yes | Yes |
| List / get alerts | Yes | Yes | Yes | Yes |
| List / get incidents | Yes | Yes | Yes | Yes |
| List / get tenants | Yes | Yes | Yes | Yes |

## Retry Policy

SDK clients retry transient HTTP failures (5xx, 429, network errors) with exponential backoff. Configure retry attempts via client options:

- .NET: `SentinelClientOptions.MaxRetryAttempts`
- Node: `maxRetryAttempts`
- Go: `MaxRetryAttempts`
- Python: `max_retry_attempts`

## Error Handling

Failed API responses raise typed exceptions:

- .NET: `SentinelApiException`
- Node: `SentinelApiError`
- Go: `*APIError`
- Python: `SentinelApiError`

Each includes the HTTP status code and parsed error message when available.

## Examples

Runnable sample shippers are included under `examples/`:

- `examples/dotnet-log-shipper/` — .NET console app
- `examples/node-log-shipper/` — Node.js script
- `examples/python-log-shipper/` — Python script

Set `SENTINEL_BASE_URL`, `SENTINEL_ACCESS_TOKEN`, and `SENTINEL_TENANT_ID` (or email/password credentials) before running.

## Plugin SDK

For extending Sentinel with collectors, alert channels, AI providers, storage backends, and authentication integrations, see `plugins/Sentinel.PluginSdk/` and the sample plugins under `plugins/samples/`.
