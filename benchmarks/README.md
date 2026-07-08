# Sentinel Load Test Benchmarks

k6 scripts for validating production SLOs before and after deployments.

## Prerequisites

- [k6](https://k6.io/docs/get-started/installation/) v0.50+
- Running Sentinel API (local, staging, or production)
- Valid tenant ID and (for search) JWT access token

## Performance Targets

| Scenario | Metric | Target | Script |
|----------|--------|--------|--------|
| Log ingestion | p95 latency | < 500 ms | `log-ingestion.js` |
| Log ingestion | Error rate | < 1% | `log-ingestion.js` |
| Log ingestion | Throughput | ≥ 5,000 logs/sec @ 100 VUs | `log-ingestion.js` |
| Log search | p95 latency | < 800 ms | `search.js` |
| Log search | Error rate | < 2% | `search.js` |
| Log search | Concurrent users | 30 VUs sustained 5 min | `search.js` |

## Running Tests

### Log Ingestion

```bash
k6 run benchmarks/k6/log-ingestion.js \
  -e BASE_URL=https://sentinel.example.com \
  -e TENANT_ID=your-tenant-uuid \
  -e BATCH_SIZE=50
```

### Log Search

Obtain a JWT by logging in via `/api/v1/auth/login`, then:

```bash
k6 run benchmarks/k6/search.js \
  -e BASE_URL=https://sentinel.example.com \
  -e AUTH_TOKEN=eyJhbGciOiJIUzI1NiIs... \
  -e TENANT_ID=your-tenant-uuid
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `BASE_URL` | `http://localhost:8080` | Sentinel API base URL |
| `TENANT_ID` | default seed tenant | Tenant UUID header |
| `BATCH_SIZE` | `50` | Logs per ingestion request |
| `AUTH_TOKEN` | _(required for search)_ | JWT bearer token |

## CI Integration

Add a nightly workflow step:

```yaml
- name: Run k6 smoke test
  run: |
    k6 run benchmarks/k6/log-ingestion.js \
      -e BASE_URL=${{ secrets.STAGING_URL }} \
      --duration 1m --vus 10
```

## Interpreting Results

k6 prints threshold pass/fail at the end. Key metrics:

- `http_req_duration` — end-to-end request time
- `http_req_failed` — HTTP 4xx/5xx rate
- `ingest_duration` / `search_duration` — custom application-level timers
- `logs_ingested` — total log records accepted (ingestion only)

## Related

- BenchmarkDotNet micro-benchmarks: `tests/Sentinel.Benchmarks/`
- Operations runbook: `docs/operations.md`
