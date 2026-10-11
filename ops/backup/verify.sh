#!/usr/bin/env bash
# Comprueba que una base restaurada sirve: tiene las migraciones esperadas y datos recientes.
# Lo usan restore.sh y la verificación periódica (restaurar la última copia en una base temporal).
#
# Variables de entorno:
#   RESTORE_DATABASE_URL      base restaurada que se revisa
#   EXPECTED_LAST_MIGRATION   opcional: id de la última migración del código desplegado
#   MAX_AGE_HOURS             antigüedad máxima del último registro (por defecto 26)
set -euo pipefail

: "${RESTORE_DATABASE_URL:?Falta RESTORE_DATABASE_URL}"
MAX_AGE_HOURS="${MAX_AGE_HOURS:-26}"
q() { psql "$RESTORE_DATABASE_URL" -Atc "$1"; }

last_migration=$(q 'select "MigrationId" from "__EFMigrationsHistory" order by "MigrationId" desc limit 1')
echo "Última migración: $last_migration"
if [ -n "${EXPECTED_LAST_MIGRATION:-}" ] && [ "$last_migration" != "$EXPECTED_LAST_MIGRATION" ]; then
  echo "ERROR: se esperaba $EXPECTED_LAST_MIGRATION." >&2
  exit 1
fi

echo "Conjuntos: $(q 'select count(*) from "Complexes"') · Unidades: $(q 'select count(*) from "Units"') · Eventos de acceso: $(q 'select count(*) from "AccessEvents"')"

# la bitácora recibe una fila con cada cambio: si lo último es viejo, la copia también lo es
age=$(q 'select coalesce(floor(extract(epoch from now() - max("OccurredAt")) / 3600), 999999) from "AuditLogs"')
echo "Último registro de la bitácora: hace $age h"
if [ "$age" -gt "$MAX_AGE_HOURS" ]; then
  echo "ERROR: la copia tiene más de $MAX_AGE_HOURS horas." >&2
  exit 1
fi

echo "OK"
