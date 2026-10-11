#!/usr/bin/env bash
# Copia completa de la base de datos, cifrada, a un almacenamiento distinto del proveedor.
#
# Variables de entorno (ninguna vive en el repositorio; se definen en el servidor o en los
# secretos del CI):
#   BACKUP_DATABASE_URL   cadena de conexión de solo lectura, formato postgresql://usuario:clave@host/base
#   BACKUP_PASSPHRASE     frase con la que se cifra la copia (gpg simétrico, AES-256)
#   BACKUP_DIR            carpeta local donde se dejan las copias (por defecto ./backups)
#   BACKUP_REMOTE         destino de rclone, opcional (p. ej. "almacen:control-acceso/backups")
#   BACKUP_RETENTION_DAYS días que se conservan las copias locales (por defecto 30)
#
# Uso: ./backup.sh        (programado a diario; ver documentation/operacion/copias-de-seguridad.md)
set -euo pipefail

: "${BACKUP_DATABASE_URL:?Falta BACKUP_DATABASE_URL}"
: "${BACKUP_PASSPHRASE:?Falta BACKUP_PASSPHRASE}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"

mkdir -p "$BACKUP_DIR"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
file="$BACKUP_DIR/control-acceso-$stamp.dump.gpg"

# formato "custom" de pg_dump: comprimido y restaurable por partes con pg_restore
pg_dump --format=custom --no-owner --no-privileges "$BACKUP_DATABASE_URL" \
  | gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$BACKUP_PASSPHRASE" --output "$file"

# una copia vacía o truncada no sirve: mejor fallar aquí que descubrirlo al restaurar
size=$(wc -c < "$file")
if [ "$size" -lt 10240 ]; then
  echo "ERROR: la copia $file pesa $size bytes; se descarta." >&2
  rm -f "$file"
  exit 1
fi

if [ -n "${BACKUP_REMOTE:-}" ]; then
  rclone copy "$file" "$BACKUP_REMOTE"
fi

find "$BACKUP_DIR" -name 'control-acceso-*.dump.gpg' -mtime "+$BACKUP_RETENTION_DAYS" -delete

echo "OK $file ($size bytes)"
