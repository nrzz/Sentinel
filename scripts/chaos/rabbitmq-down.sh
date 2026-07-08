#!/usr/bin/env bash
# Chaos scenario: simulate RabbitMQ outage.
# Usage: ./scripts/chaos/rabbitmq-down.sh [namespace] [duration_seconds]
#
# Requires kubectl access to the target cluster.
set -euo pipefail

NAMESPACE="${1:-sentinel-system}"
RELEASE="${RELEASE:-sentinel}"
DURATION="${2:-120}"
STATEFULSET="${RELEASE}-rabbitmq"

echo "=== Chaos: RabbitMQ Down ==="
echo "Namespace: ${NAMESPACE}"
echo "StatefulSet: ${STATEFULSET}"
echo "Duration: ${DURATION}s"

if ! kubectl get statefulset "${STATEFULSET}" -n "${NAMESPACE}" &>/dev/null; then
  echo "ERROR: StatefulSet ${STATEFULSET} not found in ${NAMESPACE}" >&2
  exit 1
fi

ORIGINAL_REPLICAS="$(kubectl get statefulset "${STATEFULSET}" -n "${NAMESPACE}" -o jsonpath='{.spec.replicas}')"
echo "Original replicas: ${ORIGINAL_REPLICAS}"

echo "Scaling RabbitMQ to 0..."
kubectl scale statefulset "${STATEFULSET}" -n "${NAMESPACE}" --replicas=0

echo "RabbitMQ is down. Waiting ${DURATION}s..."
echo "Monitor API /ready endpoint and worker logs during this window."

sleep "${DURATION}"

echo "Restoring RabbitMQ to ${ORIGINAL_REPLICAS} replicas..."
kubectl scale statefulset "${STATEFULSET}" -n "${NAMESPACE}" --replicas="${ORIGINAL_REPLICAS}"

echo "Waiting for RabbitMQ pod to be ready..."
kubectl rollout status statefulset "${STATEFULSET}" -n "${NAMESPACE}" --timeout=300s

echo "=== Chaos scenario complete ==="
echo "Verify:"
echo "  1. API /ready returns healthy"
echo "  2. Queued messages are processed"
echo "  3. No data loss in ClickHouse"
