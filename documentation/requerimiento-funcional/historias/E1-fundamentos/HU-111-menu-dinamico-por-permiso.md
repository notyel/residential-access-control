# HU-111 · Menú dinámico y rutas por permiso

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** En curso · **Depende de:** HU-109
**Requerimiento:** §3 completo, §8.7

## Historia

Como usuario, quiero ver solo las opciones y acciones que puedo usar, y que se actualicen cuando
cambien mis permisos, para tener una navegación acorde a mi rol.

## Criterios de aceptación

- [ ] Cada opción de menú declara el permiso que la habilita; `GET menu` devuelve las opciones cuyo permiso está entre los efectivos del usuario, agrupadas y ordenadas.
- [ ] `GET auth/me` devuelve usuario, conjunto, roles y permisos efectivos; el frontend los guarda en un estado con signals, no en `localStorage`.
- [ ] Directiva `*appIfPermission="'units.write'"` reemplaza a `*appIfRole`; todas las acciones de crear, editar, anular y exportar la usan.
- [ ] Guarda `permissionGuard` aplicada con `canActivateChild` a todo `/access-control`; cada ruta declara `data.permission`. Una URL sin permiso muestra la página "Sin acceso", no redirige en silencio.
- [ ] Las acciones del buscador por comando (`Ctrl K`) también se filtran por permiso.
- [ ] Menú y permisos se recargan al renovar el token y al volver a la pestaña; un cambio hecho por el administrador se refleja sin cerrar sesión.
- [ ] Si la vinculación residencial del usuario termina (HU-117), sus opciones "Mi residencia / Mis propiedades" desaparecen en la siguiente recarga.
- [ ] El menú admite grupos (p. ej. "Usuarios y roles" → Usuarios, Roles) y mantiene el comportamiento responsive actual.
- [ ] Menús por defecto sembrados según §3 para los roles de sistema; las opciones de módulos aún no construidos se añaden en la historia que crea cada pantalla.

## Diseño técnico

- `Menu` gana `Permission`, `ParentId` y `Group`; se elimina `RoleMenu`.
- `PermissionsStore` (signal) en `core/services`; `MenuService` y la directiva dependen de él.
- La ruta por defecto tras el login es la primera opción del menú del usuario, no `/dashboard` fija.

## Verificación

Con dos sesiones abiertas: el administrador quita `visits.read` al rol Portería; al volver a la
pestaña del guarda desaparece "Visitas" y la URL directa muestra "Sin acceso". Prueba unitaria de
`permissionGuard` y de la directiva.

## Avance (2026-10-10)

Hecho junto con HU-109: `GET auth/me`, permisos en memoria (solo el token queda en `localStorage`), `*appIfPermission`, `permissionGuard` con página "Sin acceso", buscador por comando filtrado por permiso, menú filtrado por permiso en el servidor. Columna `Menu.Group` y migración `MenuGroups` creadas en el backend.

Pendiente: pintar los grupos en el sidebar, recargar menú y permisos al volver a la pestaña, y que la ruta inicial sea la primera opción del menú en vez de `/dashboard`.
