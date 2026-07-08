#!/usr/bin/env bash
# Backup ClickHouse data for Sentinel.
# Usage: ./scripts/backup-clickhouse.sh [output_dir]
set -euo pipefail

OUTPUT_DIR="${1:-./backups/clickhouse}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_DIR="${OUTPUT_DIR}/sentinel-ch-${TIMESTAMP}"

CH_HOST="${CH_HOST:-localhost}"
CH_PORT="${CH_PORT:-8123}"
CH_DATABASE="${CH_DATABASE:-sentinel}"
CH_USER="${CH_USER:-default}"
CH_PASSWORD="${CH_PASSWORD:-}"

mkdir -p "${BACKUP_DIR}"

echo "Starting ClickHouse backup: ${CH_DATABASE}@${CH_HOST}:${CH_PORT}"
echo "Output directory: ${BACKUP_DIR}"

TABLES=(
  "logs"
  "metrics"
  "traces"
)

for table in "${TABLES[@]}"; do
  OUTFILE="${BACKUP_DIR}/${table}.native.gz"
  echo "Exporting table: ${table}"

  curl -sf \
    --user "${CH_USER}:${CH_PASSWORD}" \
    "http://${CH_HOST}:${CH_PORT}/?database=${CH_DATABASE}&query=SELECT%20*%20FROM%20${table}%20FORMAT%20Native" \
    | gzip -9 > "${OUTFILE}"

  echo "  -> ${OUTFILE} ($(du -h "${OUTFILE}" | cut -f1))"
done

# Export schema DDL
curl -sf \
  --user "${CH_USER}:${CH_PASSWORD}" \
  "http://${CH_HOST}:${CH_PORT}/?database=${CH_DATABASE}&query=SHOW%20CREATE%20TABLE%20logs" \
  > "${BACKUP_DIR}/schema-logs.sql" 2>/dev/null || true

echo "Backup complete: ${BACKUP_DIR}"

# Retention: keep last 7 backups
find "${OUTPUT_DIR}" -maxdepth 1 -name 'sentinel-ch-*' -mtime +7 -exec rm -rf {} + 2>/dev/null || true

echo "Done."
