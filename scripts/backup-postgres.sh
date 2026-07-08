#!/usr/bin/env bash
# Backup PostgreSQL database for Sentinel.
# Usage: ./scripts/backup-postgres.sh [output_dir]
set -euo pipefail

OUTPUT_DIR="${1:-./backups/postgres}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_FILE="${OUTPUT_DIR}/sentinel-pg-${TIMESTAMP}.sql.gz"

PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGDATABASE="${PGDATABASE:-sentinel}"
PGUSER="${PGUSER:-sentinel}"

mkdir -p "${OUTPUT_DIR}"

echo "Starting PostgreSQL backup: ${PGDATABASE}@${PGHOST}:${PGPORT}"
echo "Output: ${BACKUP_FILE}"

if [ -z "${PGPASSWORD:-}" ]; then
  echo "ERROR: PGPASSWORD environment variable is required." >&2
  exit 1
fi

pg_dump \
  --host="${PGHOST}" \
  --port="${PGPORT}" \
  --username="${PGUSER}" \
  --dbname="${PGDATABASE}" \
  --format=plain \
  --no-owner \
  --no-acl \
  --verbose \
  | gzip -9 > "${BACKUP_FILE}"

echo "Backup complete: ${BACKUP_FILE} ($(du -h "${BACKUP_FILE}" | cut -f1))"

# Retention: keep last 7 daily backups
find "${OUTPUT_DIR}" -name 'sentinel-pg-*.sql.gz' -mtime +7 -delete 2>/dev/null || true

echo "Done."
