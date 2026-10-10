# HU-132 · Copias de seguridad y recuperación

**Épica:** E7 · **Etapa:** 6 Consultas y operación · **Estado:** Pendiente · **Depende de:** HU-101, HU-108
**Requerimiento:** §7.8

## Historia

Como responsable de la plataforma, quiero copias de seguridad automáticas y un procedimiento de
restauración probado, para recuperar la información ante un fallo o un borrado accidental.

## Situación actual

La base de datos es un PostgreSQL gestionado. No hay procedimiento de respaldo ni de restauración
documentado, ni se sabe qué retención ofrece el plan contratado.

## Criterios de aceptación

- [ ] Documento `documentation/operacion/copias-de-seguridad.md` con: qué se respalda, cada cuánto, dónde se guarda, cuánto se conserva y quién es responsable.
- [ ] Objetivos escritos y acordados: pérdida máxima de datos (RPO) y tiempo máximo de recuperación (RTO).
- [ ] Copia automática diaria completa con retención mínima de 30 días, más recuperación a un punto en el tiempo si el proveedor la ofrece; se confirma y documenta lo que da el plan actual.
- [ ] Una copia adicional fuera del proveedor de la base de datos (`pg_dump` programado a un almacenamiento distinto, cifrado).
- [ ] Procedimiento de restauración paso a paso, ejecutado al menos una vez contra una base vacía; se anota la fecha y el tiempo que tomó.
- [ ] Verificación automática: una tarea periódica restaura la última copia en una base temporal y comprueba que las migraciones coinciden y que hay datos recientes.
- [ ] Alerta cuando una copia falla o tiene más de 26 horas.
- [ ] Procedimiento para restaurar la información de **un solo conjunto** sin afectar a los demás.
- [ ] Las credenciales de respaldo no están en el repositorio.
- [ ] Endpoint `GET health` (API y base de datos) para monitoreo.

## Diseño técnico

- Scripts en `ops/backup/` y tarea programada (cron del servidor o flujo programado del CI).
- La restauración por conjunto se apoya en `ComplexId` (HU-108): exportar e importar filtrando por esa columna.

## Verificación

Simulacro: borrar una visita en un entorno de prueba, restaurar desde la copia y recuperarla;
registrar el resultado en el documento de operación.
