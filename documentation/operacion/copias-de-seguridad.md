# Copias de seguridad y recuperación

Historia: [HU-132](../requerimiento-funcional/historias/E6-consultas-y-operacion/HU-132-copias-de-seguridad.md)

> **Estado de este documento.** Los scripts y el procedimiento están escritos. Lo que depende de la
> infraestructura real **aún no se ha ejecutado** y está marcado como *Por confirmar*: quien opera
> el servicio debe completarlo (plan del proveedor, almacenamiento externo, tarea programada,
> simulacro de restauración). No dé las copias por buenas hasta registrar aquí el primer simulacro.

## Qué se respalda

| Qué | Dónde vive | Cómo se respalda |
| --- | --- | --- |
| Base de datos PostgreSQL (todos los conjuntos) | Proveedor gestionado (Neon) | Copia del proveedor + `pg_dump` diario externo |
| Código y migraciones | Repositorio Git | El propio repositorio |
| Secretos (cadena de conexión, clave JWT, SMTP) | Gestor de secretos del servidor / `dotnet user-secrets` en desarrollo | Custodia aparte; **no** van en las copias ni en el repositorio |

La aplicación no guarda archivos fuera de la base de datos.

## Objetivos

| Objetivo | Valor propuesto | Estado |
| --- | --- | --- |
| Pérdida máxima de datos (RPO) | 24 horas con la copia diaria; minutos si el plan del proveedor ofrece recuperación a un punto en el tiempo | *Por confirmar y acordar con la administración* |
| Tiempo máximo de recuperación (RTO) | 4 horas | *Por confirmar con el primer simulacro* |

## Copias

1. **Copia del proveedor.** *Por confirmar:* qué ofrece el plan contratado (copias automáticas,
   días de retención, recuperación a un punto en el tiempo). Anotar aquí el resultado y la fecha
   de la consulta.
2. **Copia externa diaria** con [`ops/backup/backup.sh`](../../ops/backup/backup.sh): `pg_dump`
   en formato *custom*, cifrada con `gpg` (AES-256) y enviada con `rclone` a un almacenamiento de
   otro proveedor. Conserva 30 días. Descarta la copia si pesa menos de 10 KB.
   - Frecuencia: diaria, 03:00 hora del servidor.
   - Responsable: *Por definir (nombre y suplente).*
   - Destino (`BACKUP_REMOTE`): *Por definir.*

Tarea programada de ejemplo (cron del servidor):

```cron
0 3 * * * cd /opt/control-acceso/ops/backup && ./backup.sh >> /var/log/control-acceso-backup.log 2>&1
```

Variables que necesita (se definen en el servidor o en los secretos del CI, nunca en el
repositorio): `BACKUP_DATABASE_URL` (usuario de **solo lectura**), `BACKUP_PASSPHRASE`,
`BACKUP_REMOTE`. La frase de cifrado se guarda en un lugar distinto del almacenamiento de las
copias: quien tenga las dos cosas puede leer todos los datos personales.

## Restauración completa

1. Crear una base **vacía** (nueva rama o proyecto del proveedor, o un PostgreSQL local).
2. Descargar la copia y restaurar:
   ```bash
   export RESTORE_DATABASE_URL='postgresql://…/base_vacia'
   export BACKUP_PASSPHRASE='…'
   ./ops/backup/restore.sh backups/control-acceso-AAAAMMDDTHHMMSSZ.dump.gpg
   ```
   El script se niega a restaurar sobre una base con tablas y al final ejecuta `verify.sh`.
3. Apuntar la API a la base restaurada (`ConnectionStrings__DefaultConnection`) y reiniciarla. Al
   arrancar aplica las migraciones que falten si la copia es de una versión anterior.
4. Comprobar `GET /api/health` (`status: ok`, `pendingMigrations: 0`) e iniciar sesión.

## Restaurar un solo conjunto

Todas las tablas con datos de un conjunto llevan `ComplexId`. Para recuperar un conjunto sin tocar
los demás:

1. Restaurar la copia completa en una base **temporal** (procedimiento anterior).
2. Exportar de la temporal solo las filas de ese conjunto, tabla por tabla, respetando el orden de
   dependencias (primero `Persons`, `Structures`, `Units`; después lo que apunta a ellas):
   ```bash
   psql "$TEMPORAL" -c "\copy (select * from \"Units\" where \"ComplexId\" = '<id>') to 'units.csv' csv header"
   ```
3. En la base real, dentro de **una transacción**, borrar o corregir las filas dañadas de ese
   conjunto e importar las exportadas con `\copy … from`.
4. `AccessEvents` tiene un disparador que impide modificarlos o borrarlos. Para esta operación
   excepcional se desactiva dentro de la transacción y se vuelve a activar antes de confirmar:
   ```sql
   ALTER TABLE "AccessEvents" DISABLE TRIGGER trg_access_events_immutable;
   -- … importación …
   ALTER TABLE "AccessEvents" ENABLE TRIGGER trg_access_events_immutable;
   ```
5. Dejar constancia en la bitácora de la operación (quién, cuándo, qué conjunto y desde qué copia).

Este procedimiento es manual a propósito: se ejecuta rara vez y exige revisar el resultado antes de
confirmar la transacción. *Por hacer:* ensayarlo una vez con el conjunto de demostración.

## Verificación automática y alertas

- `ops/backup/verify.sh` comprueba, sobre una base restaurada, que la última migración es la
  esperada y que el último registro de la bitácora tiene menos de 26 horas.
- *Por configurar:* tarea semanal que restaura la última copia en una base temporal y ejecuta
  `verify.sh`; y una alerta (correo o canal del equipo) cuando `backup.sh` o esa verificación
  terminan con error. Con cron basta `MAILTO=`; en un CI programado, la notificación de fallo del
  flujo.
- Monitoreo del servicio: `GET /api/health` responde sin sesión `200` con
  `{ status, api, database, pendingMigrations }`, o `503` si la base no responde.

## Simulacros

| Fecha | Qué se probó | Copia usada | Tiempo | Resultado | Quién |
| --- | --- | --- | --- | --- | --- |
| *Pendiente* | Restauración completa en base vacía | | | | |
| *Pendiente* | Borrar una visita en pruebas y recuperarla desde la copia | | | | |
| *Pendiente* | Restauración de un solo conjunto | | | | |
