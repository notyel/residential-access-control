# HU-132 · Copias de seguridad y recuperación

**Épica:** E7 · **Etapa:** 6 Consultas y operación · **Estado:** Hecha con pendientes · **Depende de:** HU-101, HU-108
**Requerimiento:** §7.8

## Historia

Como responsable de la plataforma, quiero copias de seguridad automáticas y un procedimiento de
restauración probado, para recuperar la información ante un fallo o un borrado accidental.

## Situación actual

La base de datos es un PostgreSQL gestionado. No hay procedimiento de respaldo ni de restauración
documentado, ni se sabe qué retención ofrece el plan contratado.

## Criterios de aceptación

- [x] Documento `documentation/operacion/copias-de-seguridad.md` con: qué se respalda, cada cuánto, dónde se guarda, cuánto se conserva y quién es responsable.
- [ ] Objetivos escritos y acordados: pérdida máxima de datos (RPO) y tiempo máximo de recuperación (RTO). **Pendiente:** están propuestos (RPO 24 h, RTO 4 h); falta acordarlos.
- [ ] Copia automática diaria completa con retención mínima de 30 días, más recuperación a un punto en el tiempo si el proveedor la ofrece; se confirma y documenta lo que da el plan actual. **Pendiente:** confirmar y anotar lo que ofrece el plan del proveedor.
- [ ] Una copia adicional fuera del proveedor de la base de datos (`pg_dump` programado a un almacenamiento distinto, cifrado). **Pendiente:** el script existe; falta el almacenamiento de destino y programarlo.
- [ ] Procedimiento de restauración paso a paso, ejecutado al menos una vez contra una base vacía; se anota la fecha y el tiempo que tomó. **Pendiente:** escrito, sin ejecutar; no hay fecha ni tiempo que anotar.
- [ ] Verificación automática: una tarea periódica restaura la última copia en una base temporal y comprueba que las migraciones coinciden y que hay datos recientes. **Pendiente:** `verify.sh` existe; falta la tarea periódica que restaure y lo ejecute.
- [ ] Alerta cuando una copia falla o tiene más de 26 horas. **Pendiente:** depende de dónde se programe la tarea.
- [ ] Procedimiento para restaurar la información de **un solo conjunto** sin afectar a los demás. **Parcial:** documentado, sin ensayar.
- [x] Las credenciales de respaldo no están en el repositorio.
- [x] Endpoint `GET health` (API y base de datos) para monitoreo.

## Diseño técnico

- Scripts en `ops/backup/` y tarea programada (cron del servidor o flujo programado del CI).
- La restauración por conjunto se apoya en `ComplexId` (HU-108): exportar e importar filtrando por esa columna.

## Verificación

Simulacro: borrar una visita en un entorno de prueba, restaurar desde la copia y recuperarla;
registrar el resultado en el documento de operación.

## Cómo quedó

- `GET /api/health` (sin sesión): estado del API, de la base y migraciones pendientes; 503 si la base no responde.
- Documento [documentation/operacion/copias-de-seguridad.md](../../../operacion/copias-de-seguridad.md) con qué se respalda, objetivos propuestos, procedimiento de restauración completa y de un solo conjunto.
- Scripts en `ops/backup/`: `backup.sh` (`pg_dump` cifrado con gpg, envío con rclone, retención de 30 días), `restore.sh` (solo sobre una base vacía) y `verify.sh` (migraciones y antigüedad de los datos).
- **Nada de esto se ha ejecutado contra la infraestructura real.** Lo que sigue abierto depende de quien opera el servicio.
