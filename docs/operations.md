# Operations Runbook

Day-2 operations guide for Sentinel production deployments.

## Service Level Objectives

| SLO | Target | Measurement |
|-----|--------|-------------|
| API availability | 99.9% | `/health` uptime |
| Log ingestion p95 | < 500 ms | k6 `log-ingestion.js` |
| Search p95 | < 800 ms | k6 `search.js` |
| Data durability | 99.99% | Backup restore tests |

## Health Endpoints

| Endpoint | Purpose | Expected |
|----------|---------|----------|
| `GET /health` | Liveness | `200 {"status":"healthy"}` |
| `GET /ready` | Readiness (all deps) | `200` when healthy, `503` otherwise |
| `GET /live` | Placeholder | Always `200` |

```bash
kubectl port-forward -n sentinel-platform svc/sentinel-api 8080:8080
curl http://localhost:8080/health
curl http://localhost:8080/ready
```

## Common Operations

### View pod status

```bash
kubectl get pods -n sentinel-platform -o wide
kubectl get pods -n sentinel-system -o wide
```

### Scale API manually

```bash
kubectl scale deployment sentinel-api -n sentinel-platform --replicas=5
```

HPA will reconcile if enabled. Disable HPA temporarily for manual scaling:

```bash
kubectl delete hpa sentinel-api -n sentinel-platform
```

### Restart API (rolling)

```bash
kubectl rollout restart deployment/sentinel-api -n sentinel-platform
kubectl rollout status deployment/sentinel-api -n sentinel-platform
```

### View logs

```bash
# API logs
kubectl logs -n sentinel-platform -l app.kubernetes.io/component=api --tail=100 -f

# Worker logs
kubectl logs -n sentinel-platform -l app.kubernetes.io/component=worker --tail=100 -f
```

### Database migrations

Migrations run automatically on API startup. For manual migration in maintenance window:

1. Scale API to 0
2. Run a one-off migration job or port-forward to PostgreSQL
3. Scale API back up

```bash
kubectl scale deployment sentinel-api -n sentinel-platform --replicas=0
# Run migration via job or local dotnet run with production connection string
kubectl scale deployment sentinel-api -n sentinel-platform --replicas=2
```

## Backup Procedures

### PostgreSQL

```bash
export PGPASSWORD="$(kubectl get secret sentinel-secrets -n sentinel-platform -o jsonpath='{.data.postgres-password}' | base64 -d)"
export PGHOST="$(kubectl get svc sentinel-postgresql -n sentinel-system -o jsonpath='{.spec.clusterIP}')"

./scripts/backup-postgres.sh /backups/postgres
```

Schedule via CronJob in Kubernetes or external scheduler. Retention: 7 days default.

### ClickHouse

```bash
export CH_HOST="$(kubectl get svc sentinel-clickhouse -n sentinel-system -o jsonpath='{.spec.clusterIP}')"
export CH_PASSWORD="$(kubectl get secret sentinel-secrets -n sentinel-platform -o jsonpath='{.data.clickhouse-password}' | base64 -d)"

./scripts/backup-clickhouse.sh /backups/clickhouse
```

## Monitoring Alerts

Recommended alert rules:

| Alert | Condition | Severity |
|-------|-----------|----------|
| APIDown | `/health` failing 2 min | Critical |
| APINotReady | `/ready` failing 5 min | Warning |
| HighIngestLatency | p95 > 1s for 10 min | Warning |
| RabbitMQQueueDepth | > 100k messages | Warning |
| ClickHouseDiskUsage | > 80% | Warning |
| PostgreSQLConnections | > 80% max | Warning |

## Incident Response

### API returning 503 on /ready

1. Check which health check fails:
   ```bash
   curl -s http://localhost:8080/ready | jq .
   ```
2. Verify data layer pods in `sentinel-system`
3. Check connection strings in ConfigMap/Secrets
4. Review recent deployments: `kubectl rollout history deployment/sentinel-api -n sentinel-platform`

### High ingestion latency

1. Check RabbitMQ queue depth
2. Scale workers: `kubectl scale deployment sentinel-worker --replicas=4 -n sentinel-platform`
3. Check ClickHouse insert performance
4. Run k6 smoke test to measure baseline

### RabbitMQ outage

1. Check pod status: `kubectl get pods -n sentinel-system -l app.kubernetes.io/component=rabbitmq`
2. Review logs: `kubectl logs sentinel-rabbitmq-0 -n sentinel-system`
3. If persistent corruption, restore from backup and replay from dead-letter queue

### Chaos testing

Run controlled failure scenarios:

```bash
./scripts/chaos/rabbitmq-down.sh sentinel-system 120
```

Document results and verify SLO recovery time.

## Secret Rotation

### JWT signing key

1. Generate new key (min 32 chars)
2. Update Kubernetes secret
3. Rolling restart API pods
4. All users must re-login (existing tokens invalidated)

```bash
kubectl create secret generic sentinel-secrets \
  --from-literal=jwt-secret-key="NEW_KEY_HERE" \
  --dry-run=client -o yaml | kubectl apply -n sentinel-platform -f -

kubectl rollout restart deployment/sentinel-api -n sentinel-platform
```

### Database passwords

1. Update password in database
2. Update Kubernetes secret
3. Rolling restart affected pods

## Maintenance Windows

1. Notify users 24h in advance
2. Scale workers to 0 (stop processing)
3. Wait for RabbitMQ queues to drain
4. Perform maintenance (upgrade, backup restore)
5. Scale workers back up
6. Verify `/ready` and run smoke tests

## Escalation

| Level | Contact | When |
|-------|---------|------|
| L1 | On-call engineer | SLO breach, pod restarts |
| L2 | Platform team | Data loss, security incident |
| L3 | Engineering lead | Multi-service outage > 1 hour |

See [disaster-recovery.md](disaster-recovery.md) for full DR procedures.
