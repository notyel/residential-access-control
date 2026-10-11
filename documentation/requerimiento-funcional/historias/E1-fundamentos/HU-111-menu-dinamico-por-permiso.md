# HU-111 · Menú dinámico y rutas por permiso

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Hecha con pendientes · **Depende de:** HU-109
**Requerimiento:** §3 completo, §8.7

## Historia

Como usuario, quiero ver solo las opciones y acciones que puedo usar, y que se actualicen cuando
cambien mis permisos, para tener una navegación acorde a mi rol.

## Criterios de aceptación

- [x] Cada opción de menú declara el permiso que la habilita; `GET menu` devuelve las opciones cuyo permiso está entre los efectivos del usuario, agrupadas y ordenadas.
- [x] `GET auth/me` devuelve usuario, conjunto, roles y permisos efectivos; el frontend los guarda en un estado con signals, no en `localStorage`.
- [x] Directiva `*appIfPermission="'units.write'"` reemplaza a `*appIfRole`; todas las acciones de crear, editar, anular y exportar la usan.
- [x] Guarda `permissionGuard` aplicada con `canActivateChild` a todo `/access-control`; cada ruta declara `data.permission`. Una URL sin permiso muestra la página "Sin acceso", no redirige en silencio.
- [x] Las acciones del buscador por comando (`Ctrl K`) también se filtran por permiso.
- [x] Menú y permisos se recargan al renovar el token y al volver a la pestaña; un cambio hecho por el administrador se refleja sin cerrar sesión.
- [ ] Si la vinculación residencial del usuario termina (HU-117), sus opciones "Mi residencia / Mis propiedades" desaparecen en la siguiente recarga. **Parcial:** las opciones dependen del rol, no del vínculo: al terminar la vinculación siguen en el menú, pero ya no muestran ninguna unidad (el servidor solo entrega las de vínculo vigente).
- [x] El menú admite grupos (p. ej. "Usuarios y roles" → Usuarios, Roles) y mantiene el comportamiento responsive actual.
- [x] Menús por defecto sembrados según §3 para los roles de sistema; las opciones de módulos aún no construidos se añaden en la historia que crea cada pantalla.

## Diseño técnico

- `Menu` gana `Permission`, `ParentId` y `Group`; se elimina `RoleMenu`.
- `PermissionsStore` (signal) en `core/services`; `MenuService` y la directiva dependen de él.
- La ruta por defecto tras el login es la primera opción del menú del usuario, no `/dashboard` fija.

## Verificación

Con dos sesiones abiertas: el administrador quita `visits.read` al rol Portería; al volver a la
pestaña del guarda desaparece "Visitas" y la URL directa muestra "Sin acceso". Prueba unitaria de
`permissionGuard` y de la directiva.

## Cómo quedó

- `GET auth/me`; en el navegador solo se guarda el token, el usuario y sus permisos viven en memoria y se cargan antes de la primera ruta.
- `permissionGuard` (`data.permission`, `superAdminOnly`, `public`) con página "Sin acceso"; `*appIfPermission`; buscador por comando filtrado por permiso.
- El menú se filtra en el servidor por permiso y trae `group`; el sidebar pinta los grupos.
- Al volver a la pestaña se piden de nuevo permisos y menú. La entrada a `/access-control` lleva a la primera opción del menú del usuario.
- Cambio respecto al diseño: no hay `ParentId`; un nivel de agrupación (`Menu.Group`) cubre lo que pide §3.
