#!/usr/bin/env bash
set -euo pipefail

# Local backup of the Supabase/Postgres database. Manual/interactive use.
#
# The daily automated backup does NOT run this file - it runs a standalone
# copy at ~/.local/bin/srfm-db-backup.sh via the com.srfm.dbbackup launchd
# agent. macOS's TCC privacy protection blocks headless/background processes
# from reading anything under ~/Documents (Desktop/Downloads too) - including
# launchd just opening a script file there to execute it ("Operation not
# permitted", no prompt, since there's no GUI session to grant access
# through). Since this repo lives under ~/Documents, the entry point launchd
# invokes has to live elsewhere. If you change the backup logic here, mirror
# the change in ~/.local/bin/srfm-db-backup.sh.
#
# Reads the Npgsql-style connection string (SUPABASE_DB_CONNECTION), runs
# pg_dump against it, and writes a timestamped custom-format dump.
#
# Connection string is resolved in this order:
#   1. $SUPABASE_DB_CONNECTION if already set in the environment
#   2. ~/.config/srfm/db-backup.env - what the launchd copy uses (update this
#      file manually if the DB password ever rotates)
#   3. api/.env relative to the repo - used for manual/interactive runs here.
#
# Backups land in ~/srfm-db-backups (NOT inside the repo/Documents), so both
# copies of the script write to the same place.
#
# Usage:
#   scripts/backup-db.sh              # dump using the resolved connection, keep last 14
#   scripts/backup-db.sh --keep=30    # keep last 30 dumps instead
#   SUPABASE_DB_CONNECTION="..." scripts/backup-db.sh   # override the connection string

KEEP=14
BACKUP_DIR="${SRFM_BACKUP_DIR:-$HOME/srfm-db-backups}"

for arg in "$@"; do
  case "${arg}" in
    --keep=*) KEEP="${arg#*=}" ;;
    -h|--help)
      echo "Usage: $0 [--keep=N]"
      echo "  --keep=N   Number of most recent backups to retain (default: 14)"
      exit 0
      ;;
    *) echo "Unknown option: ${arg}" >&2; exit 1 ;;
  esac
done

cd "$(dirname "$0")/.."

# ---- Locate pg_dump ----
PG_DUMP="pg_dump"
if ! command -v pg_dump >/dev/null 2>&1; then
  if command -v brew >/dev/null 2>&1 && brew --prefix libpq >/dev/null 2>&1; then
    CANDIDATE="$(brew --prefix libpq)/bin/pg_dump"
    if [[ -x "${CANDIDATE}" ]]; then
      PG_DUMP="${CANDIDATE}"
    fi
  fi
fi
if ! command -v "${PG_DUMP}" >/dev/null 2>&1 && [[ ! -x "${PG_DUMP}" ]]; then
  echo "ERROR: pg_dump not found." >&2
  echo "Install it with: brew install libpq" >&2
  echo "(libpq's pg_dump isn't linked onto PATH by default; this script finds it via 'brew --prefix libpq' automatically once installed.)" >&2
  exit 1
fi

# ---- Load SUPABASE_DB_CONNECTION ----
if [[ -z "${SUPABASE_DB_CONNECTION:-}" && -f "$HOME/.config/srfm/db-backup.env" ]]; then
  SUPABASE_DB_CONNECTION="$(grep -E '^SUPABASE_DB_CONNECTION=' "$HOME/.config/srfm/db-backup.env" | head -1 | cut -d= -f2-)"
fi
if [[ -z "${SUPABASE_DB_CONNECTION:-}" && -f api/.env ]]; then
  SUPABASE_DB_CONNECTION="$(grep -E '^SUPABASE_DB_CONNECTION=' api/.env | head -1 | cut -d= -f2-)"
fi
if [[ -z "${SUPABASE_DB_CONNECTION:-}" ]]; then
  echo "ERROR: SUPABASE_DB_CONNECTION not set, and not found in ~/.config/srfm/db-backup.env or api/.env" >&2
  exit 1
fi

# ---- Parse the Npgsql-style "Key=Value;Key=Value;..." connection string ----
# (Plain-array parsing, not associative arrays, so this stays compatible with
# macOS's stock bash 3.2 as well as bash 4+.)
conn_get() {
  # Usage: conn_get key1 [key2 ...] - returns the value of the first matching key (case-insensitive)
  local pair key key_lower val wanted
  IFS=';' read -ra PAIRS <<< "${SUPABASE_DB_CONNECTION}"
  for wanted in "$@"; do
    for pair in "${PAIRS[@]}"; do
      [[ -z "${pair}" ]] && continue
      key="${pair%%=*}"
      val="${pair#*=}"
      key_lower="$(echo "${key}" | tr '[:upper:]' '[:lower:]' | tr -d ' ')"
      if [[ "${key_lower}" == "${wanted}" ]]; then
        echo "${val}"
        return 0
      fi
    done
  done
}

DB_HOST="$(conn_get host server)"
DB_PORT="$(conn_get port)"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="$(conn_get database)"
DB_NAME="${DB_NAME:-postgres}"
DB_USER="$(conn_get username userid uid user)"
DB_PASSWORD="$(conn_get password pwd)"

if [[ -z "${DB_HOST}" || -z "${DB_USER}" ]]; then
  echo "ERROR: Could not parse host/username out of SUPABASE_DB_CONNECTION" >&2
  exit 1
fi

# ---- Run the dump ----
mkdir -p "${BACKUP_DIR}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT_FILE="${BACKUP_DIR}/srfm_${TIMESTAMP}.dump"

echo "Backing up ${DB_NAME}@${DB_HOST} -> ${OUT_FILE}"
PGPASSWORD="${DB_PASSWORD}" "${PG_DUMP}" \
  --host="${DB_HOST}" \
  --port="${DB_PORT}" \
  --username="${DB_USER}" \
  --dbname="${DB_NAME}" \
  --no-owner \
  --no-privileges \
  --format=custom \
  --file="${OUT_FILE}"

SIZE="$(du -h "${OUT_FILE}" | cut -f1)"
echo "Backup complete: ${OUT_FILE} (${SIZE})"

# ---- Prune old backups ----
BACKUP_COUNT="$(ls -1 "${BACKUP_DIR}"/srfm_*.dump 2>/dev/null | wc -l | tr -d ' ')"
if (( BACKUP_COUNT > KEEP )); then
  TO_DELETE=$((BACKUP_COUNT - KEEP))
  echo "Pruning ${TO_DELETE} old backup(s), keeping the most recent ${KEEP}..."
  ls -1t "${BACKUP_DIR}"/srfm_*.dump | tail -n "${TO_DELETE}" | xargs rm -f
fi

echo "Done. $(ls -1 "${BACKUP_DIR}"/srfm_*.dump | wc -l | tr -d ' ') backup(s) on disk."
