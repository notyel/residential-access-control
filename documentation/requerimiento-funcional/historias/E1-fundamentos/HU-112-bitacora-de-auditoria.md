# HU-112 · Bitácora de auditoría

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Hecha · **Depende de:** HU-108
**Requerimiento:** §2 criterio 6, §7.5

## Historia

Como administrador o auditor, quiero consultar quién hizo cada cambio importante, qué cambió y
cuándo, para poder reconstruir lo ocurrido ante una reclamación.

## Criterios de aceptación

- [x] Toda entidad auditable registra `CreatedById` y `UpdatedById` además de las fechas que ya tiene.
- [x] Tabla `AuditLog` de solo inserción: conjunto, usuario, fecha, acción, entidad, id, valores antes y después (solo los campos que cambiaron), IP.
- [x] Se registran automáticamente altas, cambios y cierres de vigencia de todas las entidades de negocio.
- [x] Se registran además, de forma explícita: inicio de sesión correcto y fallido, cambio y recuperación de contraseña, cambios de roles y permisos, activación, desactivación y revocación de usuarios, cambios de políticas del conjunto.
- [x] Nunca se guardan contraseñas, hashes ni tokens en la bitácora.
- [x] El API no ofrece ninguna operación para editar o borrar entradas.
- [x] Pantalla "Bitácora" con filtros por fecha, usuario, entidad y acción, y detalle antes/después en el panel lateral; exige `audit.read`.
- [x] La bitácora respeta la separación por conjunto; el superadministrador ve las acciones globales.

## Diseño técnico

- `SaveChangesInterceptor` de EF Core que recorre el `ChangeTracker` y escribe `AuditLog` en la misma transacción; atributo `[NotAudited]` para campos sensibles.
- `IAuditService.Record(action, …)` para los eventos que no son cambios de entidad (login, rechazo de permisos).
- Valores antes/después en `jsonb`.
- No confundir con el registro de accesos físicos (HU-120): esto audita lo que hacen los usuarios en la plataforma.

## Verificación

Editar un usuario y cambiar los permisos de un rol; la bitácora muestra ambas entradas con autor,
fecha y diferencias. Prueba de integración que verifica que un cambio fallido (rollback) no deja
entrada.

## Cómo quedó

- `AuditableEntity` lleva `CreatedById` y `UpdatedById`. `ApplicationDbContext` escribe una fila en `AuditLogs` por cada alta, cambio o baja, en la misma transacción; los campos marcados `[NotAudited]` (hash de contraseña, sello de seguridad) nunca se copian.
- No se usó un `SaveChangesInterceptor` aparte: la lógica vive en `ApplicationDbContext.Audit.cs`, junto a la que fija conjunto y autoría, para que la bitácora los vea ya resueltos.
- Hechos explícitos con `IAuditService.Record`: inicios de sesión correctos, fallidos y bloqueados, invitaciones, recuperación de contraseña, revocación de accesos, cierre de sesión y salida de visitas.
- `GET api/audit` (permiso `audit.read`) y pantalla "Bitácora" con filtros y detalle antes → después. No existe ninguna operación para editar o borrar.
- 6 pruebas en `AuditTests`, incluida la que verifica que ninguna entrada contiene contraseñas, hashes ni tokens.
