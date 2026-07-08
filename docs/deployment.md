# Production Deployment Guide

This guide covers deploying Sentinel to Kubernetes using the production Helm chart and Argo CD GitOps workflow.

## Prerequisites

- Kubernetes 1.28+ cluster with:
  - Ingress controller (nginx recommended)
  - cert-manager (for TLS)
  - Metrics server (for HPA)
  - Storage class for persistent volumes
- Helm 3.14+
- Argo CD 2.10+ (optional, recommended)
- Container images published to your registry (see [Release pipeline](../.github/workflows/release.yml))

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  sentinel-platform namespace                            │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐                   │
│  │   Web   │  │   API   │  │ Worker  │                   │
│  └────┬────┘  └────┬────┘  └────┬────┘                   │
└───────┼────────────┼────────────┼─────────────────────────┘
        │            │            │
┌───────┼────────────┼────────────┼─────────────────────────┐
│  sentinel-system namespace                              │
│  ┌──────────┐ ┌───────────┐ ┌───────┐ ┌──────────┐      │
│  │PostgreSQL│ │ ClickHouse│ │ Redis │ │ RabbitMQ │      │
│  └──────────┘ └───────────┘ └───────┘ └──────────┘      │
└─────────────────────────────────────────────────────────┘
```

## Quick Start (Helm)

### 1. Create secrets values file

```bash
cp kubernetes/helm/sentinel/values.yaml my-values.yaml
```

Edit `my-values.yaml`:

```yaml
secrets:
  jwtSecretKey: "your-production-secret-min-32-characters-long"

ingress:
  hosts:
    - host: sentinel.yourdomain.com
      paths:
        - path: /api
          pathType: Prefix
          service: api
        - path: /
          pathType: Prefix
          service: web
  tls:
    - secretName: sentinel-tls
      hosts:
        - sentinel.yourdomain.com

image:
  registry: ghcr.io
  repository: your-org/sentinel
  tag: "0.7.0"
```

### 2. Install the chart

```bash
helm lint kubernetes/helm/sentinel -f my-values.yaml

helm upgrade --install sentinel kubernetes/helm/sentinel \
  -f my-values.yaml \
  --namespace sentinel-platform \
  --create-namespace
```

### 3. Verify deployment

```bash
kubectl get pods -n sentinel-platform
kubectl get pods -n sentinel-system
curl -k https://sentinel.yourdomain.com/health
```

## External Database Configuration

For production, use managed database services instead of embedded charts:

```yaml
postgresql:
  enabled: false

externalDatabase:
  postgresql:
    enabled: true
    host: mydb.abc123.us-east-1.rds.amazonaws.com
    port: 5432
    database: sentinel
    username: sentinel
    existingSecret: sentinel-db-credentials
    existingSecretPasswordKey: password

clickhouse:
  enabled: false

externalDatabase:
  clickhouse:
    enabled: true
    host: clickhouse.example.com
    port: 8123
    database: sentinel
    username: default
    existingSecret: sentinel-ch-credentials
    existingSecretPasswordKey: password
```

Apply the same pattern for Redis and RabbitMQ.

## Argo CD Deployment

1. Update `kubernetes/argocd/application.yaml` with your repository URL.
2. Configure secrets via Argo CD Vault Plugin or External Secrets Operator.
3. Apply the application:

```bash
kubectl apply -f kubernetes/argocd/application.yaml
argocd app sync sentinel
argocd app wait sentinel --health
```

## Resource Sizing

| Component | Min Replicas | CPU Request | Memory Request |
|-----------|-------------|-------------|----------------|
| API | 2 | 250m | 512Mi |
| Worker | 2 | 250m | 512Mi |
| Web | 2 | 100m | 128Mi |
| PostgreSQL | 1 | 250m | 512Mi |
| ClickHouse | 1 | 500m | 2Gi |

Adjust `values.yaml` based on your ingestion volume. Run k6 benchmarks (see `benchmarks/README.md`) to validate sizing.

## TLS Configuration

The chart enables TLS via ingress annotations:

```yaml
ingress:
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
```

Ensure cert-manager ClusterIssuer `letsencrypt-prod` exists, or replace with your issuer.

## Autoscaling

HPA is enabled by default for API, worker, and web. Requires metrics-server:

```bash
kubectl get hpa -n sentinel-platform
```

Tune targets in `values.yaml` under `api.autoscaling`, `worker.autoscaling`, `web.autoscaling`.

## Network Policies

Enable with `networkPolicy.enabled: true` (default). Platform pods can only reach system namespace data services on required ports.

Label namespaces if using custom names:

```bash
kubectl label namespace sentinel-platform kubernetes.io/metadata.name=sentinel-platform
kubectl label namespace sentinel-system kubernetes.io/metadata.name=sentinel-system
```

## Post-Deployment Checklist

- [ ] `/health` returns 200
- [ ] `/ready` returns 200 (all dependencies healthy)
- [ ] Login works via web UI
- [ ] Log ingestion accepts test batch
- [ ] Search returns results
- [ ] TLS certificate is valid
- [ ] Backups configured (see `scripts/backup-*.sh`)
- [ ] Monitoring/alerting wired to `/ready` failures

## Upgrading

```bash
helm upgrade sentinel kubernetes/helm/sentinel \
  -f my-values.yaml \
  --namespace sentinel-platform \
  --set image.tag=0.8.0
```

Rolling updates are handled by Kubernetes deployments. PDBs ensure minimum availability during upgrades.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| API pod CrashLoopBackOff | `kubectl logs -n sentinel-platform deploy/sentinel-api` |
| /ready returns 503 | Individual health checks: postgres, clickhouse, redis, rabbitmq |
| Ingress 502 | Service endpoints: `kubectl get endpoints -n sentinel-platform` |
| HPA not scaling | `kubectl top pods -n sentinel-platform`; verify metrics-server |

See [operations.md](operations.md) for the full runbook.
