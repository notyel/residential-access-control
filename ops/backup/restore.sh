#!/usr/bin/env bash
# Restaura una copia hecha con backup.sh en una base de datos VACÍA.
#
# Variables de entorno:
#   RESTORE_DATABASE_URL  base de destino (vacía). Nunca la de producción sin antes renombrarla.
#   BACKUP_PASSPHRASE     la misma frase con la que se cifró la copia
#
# Uso: ./restore.sh backups/control-acceso-20260101T030000Z.dump.gpg
set -euo pipefail

file="${1:?Indique el archivo de la copia}"
: "${RESTORE_DATABASE_URL:?Falta RESTORE_DATABASE_URL}"
: "${BACKUP_PASSPHRASE:?Falta BACKUP_PASSPHRASE}"

tables=$(psql "$RESTORE_DATABASE_URL" -Atc "select count(*) from information_schema.tables where table_schema = 'public'")
if [ "$tables" != "0" ]; then
  echo "ERROR: la base de destino no está vacía ($tables tablas). No se restaura encima de datos." >&2
  exit 1
fi

start=$(date +%s)
gpg --batch --quiet --decrypt --passphrase "$BACKUP_PASSPHRASE" "$file" \
  | pg_restore --no-owner --no-privileges --exit-on-error --dbname "$RESTORE_DATABASE_URL"

echo "Restaurada en $(( $(date +%s) - start )) s."
"$(dirname "$0")/verify.sh"
