# HU-110 · Usuarios: alta, activación y revocación

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Hecha con pendientes · **Depende de:** HU-109
**Requerimiento:** §2 criterios 1 y 5, §3 (Usuarios y roles)

## Historia

Como administrador del conjunto, quiero crear usuarios, asignarles roles y activar, desactivar o
revocar su acceso, para controlar quién entra a la plataforma.

## Criterios de aceptación

- [x] Pantalla "Usuarios y roles → Usuarios": lista paginada con búsqueda por nombre o correo y filtro por estado. El filtro por rol existe en el API (`role=`) pero aún no tiene control en la pantalla.
- [x] Alta de usuario con correo, nombre y uno o varios roles. No se escribe la contraseña: el usuario recibe un enlace para definirla (HU-113); hasta entonces su estado es "Invitado".
- [x] Edición de datos y de roles asignados.
- [x] Desactivar: el usuario no puede iniciar sesión y sus sesiones abiertas dejan de funcionar de inmediato. Reactivar lo devuelve a su estado anterior.
- [x] Revocar accesos: cierra todas sus sesiones y le quita todos los roles, sin borrar el usuario ni su historial.
- [x] Un usuario no se puede desactivar ni revocar a sí mismo; siempre queda al menos un administrador activo por conjunto.
- [x] Los usuarios no se eliminan.
- [x] Todas estas operaciones exigen `users.manage` y quedan en la bitácora.
- [x] `User.ApartmentNumber` deja de pedirse aquí; la relación con unidades se registra en HU-116 y HU-117.
- [x] La pantalla "Residentes" se retiró al construir HU-116.

## Diseño técnico

- `User.Status` (`Invited | Active | Disabled`) reemplaza el uso de `IsActive` para usuarios.
- `User.SecurityStamp`: cambia al desactivar, revocar o cambiar contraseña; el servidor rechaza tokens con un sello anterior.
- Frontend: módulo `modules/access-control/users` con las piezas globales existentes (`data-table`, `form-section`, panel lateral para filtros).

## Verificación

Crear un usuario con rol Portería, iniciar sesión en otra ventana, desactivarlo desde el
administrador: la siguiente acción de la otra ventana lo lleva al login y no puede volver a entrar.

## Cómo quedó

- `User.Status` (Invited, Active, Disabled) y `User.SecurityStamp`, que viaja en el token. `CurrentUserMiddleware` responde 401 si el usuario o su conjunto ya no pueden entrar o si el sello cambió: desactivar, revocar o cambiar la contraseña cierra las sesiones en la siguiente petición. Esto cierra también el pendiente de HU-108 (tokens de un conjunto desactivado).
- Enlaces de un solo uso en `UserToken` (solo se guarda el hash). El correo sale por `IEmailSender`; la implementación por defecto lo deja en el log, de donde se toma el enlace en desarrollo. **Falta configurar un envío real (SMTP) para producción.**
- Crear un conjunto ya no pide contraseña para su administrador: también recibe invitación.
- Regla de protección: no se puede desactivar, revocar ni quitar el rol al último usuario activo capaz de administrar usuarios.
- 10 pruebas de integración en `UserTests` y recorrido en navegador (crear usuario, aceptar invitación con el enlace del log).
