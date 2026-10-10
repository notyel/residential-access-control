# HU-112 · Bitácora de auditoría

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Pendiente · **Depende de:** HU-108
**Requerimiento:** §2 criterio 6, §7.5

## Historia

Como administrador o auditor, quiero consultar quién hizo cada cambio importante, qué cambió y
cuándo, para poder reconstruir lo ocurrido ante una reclamación.

## Criterios de aceptación

- [ ] Toda entidad auditable registra `CreatedById` y `UpdatedById` además de las fechas que ya tiene.
- [ ] Tabla `AuditLog` de solo inserción: conjunto, usuario, fecha, acción, entidad, id, valores antes y después (solo los campos que cambiaron), IP.
- [ ] Se registran automáticamente altas, cambios y cierres de vigencia de todas las entidades de negocio.
- [ ] Se registran además, de forma explícita: inicio de sesión correcto y fallido, cambio y recuperación de contraseña, cambios de roles y permisos, activación, desactivación y revocación de usuarios, cambios de políticas del conjunto.
- [ ] Nunca se guardan contraseñas, hashes ni tokens en la bitácora.
- [ ] El API no ofrece ninguna operación para editar o borrar entradas.
- [ ] Pantalla "Bitácora" con filtros por fecha, usuario, entidad y acción, y detalle antes/después en el panel lateral; exige `audit.read`.
- [ ] La bitácora respeta la separación por conjunto; el superadministrador ve las acciones globales.

## Diseño técnico

- `SaveChangesInterceptor` de EF Core que recorre el `ChangeTracker` y escribe `AuditLog` en la misma transacción; atributo `[NotAudited]` para campos sensibles.
- `IAuditService.Record(action, …)` para los eventos que no son cambios de entidad (login, rechazo de permisos).
- Valores antes/después en `jsonb`.
- No confundir con el registro de accesos físicos (HU-120): esto audita lo que hacen los usuarios en la plataforma.

## Verificación

Editar un usuario y cambiar los permisos de un rol; la bitácora muestra ambas entradas con autor,
fecha y diferencias. Prueba de integración que verifica que un cambio fallido (rollback) no deja
entrada.
