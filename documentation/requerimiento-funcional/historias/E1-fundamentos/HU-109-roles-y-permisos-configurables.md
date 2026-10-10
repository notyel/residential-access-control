# HU-109 · Roles y permisos configurables

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Hecha · **Depende de:** HU-108
**Requerimiento:** §2 completo (criterios 1 y 2), §8.6

## Historia

Como administrador del conjunto, quiero definir roles y decidir qué permisos tiene cada uno, para
ajustar lo que puede hacer cada tipo de usuario sin pedir un cambio en el programa.

## Criterios de aceptación

- [x] Catálogo de permisos definido en código con la forma `modulo.accion` (p. ej. `units.read`, `units.write`, `visits.checkin`, `users.manage`, `reports.read`); cada historia posterior añade los suyos.
- [x] `Role` pasa de enum a entidad por conjunto (nombre, descripción, activo, `IsSystem`), con sus permisos en `RolePermission`.
- [x] Un usuario puede tener varios roles (`UserRole`); sus permisos efectivos son la unión.
- [x] Cada conjunto nuevo nace con seis roles de sistema y sus permisos por defecto: Superadministrador (global), Administrador del conjunto, Portería, Propietario, Residente o arrendatario, Auditor.
- [x] El rol Auditor solo tiene permisos de lectura.
- [x] El administrador puede crear roles, clonarlos, cambiar sus permisos y desactivarlos; los de sistema no se pueden borrar.
- [x] Nadie puede otorgar un permiso que no tiene, ni quitarse a sí mismo `roles.manage` si es el último que lo posee.
- [x] Todos los endpoints se protegen por permiso, no por nombre de rol; desaparecen las políticas `AdminOnly`, `GuardOnly`, `OwnerOnly`, `AdminOrGuard`.
- [x] Un cambio de permisos surte efecto, como máximo, en la siguiente renovación del token (ver HU-113), sin esperar los 240 minutos actuales.
- [x] Pantalla "Usuarios y roles → Roles": lista de roles y matriz de permisos agrupada por módulo.
- [x] Los usuarios existentes se migran: Admin → Administrador del conjunto, Guard → Portería, Owner → Propietario.

## Diseño técnico

- `[HasPermission("visits.checkin")]` → `IAuthorizationPolicyProvider` + `AuthorizationHandler` que consulta los permisos efectivos (en caché por usuario, invalidada al cambiar roles).
- El token lleva id de usuario, conjunto y una versión de permisos; los permisos se resuelven en el servidor, no se confía en una lista dentro del token.
- `RoleMenu` deja de usarse; el menú se resuelve en HU-111.

## Verificación

Pruebas de integración: quitar `visits.checkin` al rol Portería y comprobar 403 al registrar; crear
un rol propio con un solo permiso y comprobar que solo esa operación responde 200; usuario con dos
roles obtiene la unión.
