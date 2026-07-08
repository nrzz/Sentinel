# Disaster Recovery Plan

Recovery procedures for Sentinel production environments.

## Recovery Objectives

| Metric | Target | Notes |
|--------|--------|-------|
| RPO (Recovery Point Objective) | 1 hour | Based on hourly backup schedule |
| RTO (Recovery Time Objective) | 4 hours | Full platform restore |
| RLO (Recovery Level Objective) | Full service | API, workers, web, all data |

## Backup Inventory

| Component | Method | Frequency | Retention | Location |
|-----------|--------|-----------|-----------|----------|
| PostgreSQL | `scripts/backup-postgres.sh` | Hourly | 7 days | S3 / offsite |
| ClickHouse | `scripts/backup-clickhouse.sh` | Daily | 7 days | S3 / offsite |
| Helm values | Git repository | Every change | Indefinite | Git remote |
| Kubernetes secrets | External Secrets Operator | Synced | Versioned | Vault / AWS SM |
| Container images | GHCR | Every release | Tagged + latest | ghcr.io |

## Disaster Scenarios

### Scenario 1: Single pod failure

**Impact**: Minimal — Kubernetes restarts pod automatically.

**Recovery**:
1. Verify pod restarts: `kubectl get pods -n sentinel-platform`
2. Check PDB allowed disruption
3. No manual intervention unless CrashLoopBackOff

**RTO**: < 5 minutes (automatic)

### Scenario 2: Database corruption (PostgreSQL)

**Impact**: Auth, config, and metadata unavailable.

**Recovery**:
1. Scale API and workers to 0
2. Identify latest clean backup:
   ```bash
   ls -lt /backups/postgres/sentinel-pg-*.sql.gz | head -1
   ```
3. Restore:
   ```bash
   export PGPASSWORD="..."
   gunzip -c /backups/postgres/sentinel-pg-YYYYMMDD.sql.gz | \
     psql -h $PGHOST -U sentinel -d sentinel
   ```
4. Verify schema: `\dt identity.*`
5. Scale services back up
6. Verify `/ready` and login

**RTO**: 1–2 hours

### Scenario 3: ClickHouse data loss

**Impact**: Historical logs, metrics, traces unavailable. Ingestion may queue in RabbitMQ.

**Recovery**:
1. Stop workers to prevent queue overflow:
   ```bash
   kubectl scale deployment sentinel-worker -n sentinel-platform --replicas=0
   ```
2. Restore ClickHouse tables from backup:
   ```bash
   ./scripts/backup-clickhouse.sh  # verify backup exists first
   # Re-import native format files via clickhouse-client
   ```
3. Re-run ClickHouse migrations if schema changed:
   ```bash
   # API startup runs migrations automatically
   kubectl rollout restart deployment/sentinel-api -n sentinel-platform
   ```
4. Scale workers back up
5. Monitor RabbitMQ queue drain

**RTO**: 2–4 hours
**Data loss**: Up to 24 hours of telemetry (daily backup cadence)

### Scenario 4: Complete cluster loss

**Impact**: Total service outage.

**Recovery**:
1. Provision new Kubernetes cluster
2. Install ingress controller, cert-manager, metrics-server
3. Restore secrets from vault/backup
4. Deploy via Argo CD or Helm:
   ```bash
   helm upgrade --install sentinel kubernetes/helm/sentinel -f production-values.yaml
   ```
5. Restore PostgreSQL from latest backup
6. Restore ClickHouse from latest backup
7. Update DNS to point to new ingress IP
8. Run smoke tests and k6 benchmarks

**RTO**: 4 hours

### Scenario 5: Region failure (multi-region future)

**Impact**: Total outage in primary region.

**Current state**: Single-region deployment. Failover requires manual restore in secondary region.

**Future**: Active-passive with cross-region PostgreSQL replication and ClickHouse backup replication.

## Recovery Verification Checklist

After any recovery procedure:

- [ ] `GET /health` returns 200
- [ ] `GET /ready` returns 200
- [ ] User login succeeds
- [ ] Log ingestion accepts test batch (202)
- [ ] Search returns historical data
- [ ] Alerts and dashboards load
- [ ] Worker processes RabbitMQ backlog
- [ ] No errors in API/worker logs for 15 minutes
- [ ] k6 smoke test passes thresholds

## Backup Testing

Test restores quarterly:

```bash
# 1. Spin up isolated PostgreSQL instance
docker run -d --name pg-restore-test -e POSTGRES_PASSWORD=test -p 5433:5432 postgres:17-alpine

# 2. Restore latest backup
gunzip -c backups/postgres/sentinel-pg-latest.sql.gz | \
  PGPASSWORD=test psql -h localhost -p 5433 -U postgres

# 3. Verify row counts
PGPASSWORD=test psql -h localhost -p 5433 -U postgres -c \
  "SELECT count(*) FROM identity.users;"
```

Document results in incident log.

## Communication Plan

| Phase | Action | Audience |
|-------|--------|----------|
| Detection | Page on-call | Engineering |
| Triage (15 min) | Assess scope, declare incident | Engineering + stakeholders |
| Recovery | Execute DR procedure | Engineering |
| Verification | Run checklist | Engineering + QA |
| Post-mortem (48h) | Blameless review | All teams |

## Contact Information

Update with your organization's details:

| Role | Contact |
|------|---------|
| On-call rotation | PagerDuty / Opsgenie |
| Infrastructure | platform-team@example.com |
| Security | security@example.com |
| Management | cto@example.com |

## Related Documents

- [deployment.md](deployment.md) — Initial deployment
- [operations.md](operations.md) — Day-2 runbook
- [security/threat-model.md](../security/threat-model.md) — Security context
- [scripts/backup-postgres.sh](../scripts/backup-postgres.sh) — Backup scripts
