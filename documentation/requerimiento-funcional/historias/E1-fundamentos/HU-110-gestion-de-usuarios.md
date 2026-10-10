# HU-110 · Usuarios: alta, activación y revocación

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Pendiente · **Depende de:** HU-109
**Requerimiento:** §2 criterios 1 y 5, §3 (Usuarios y roles)

## Historia

Como administrador del conjunto, quiero crear usuarios, asignarles roles y activar, desactivar o
revocar su acceso, para controlar quién entra a la plataforma.

## Criterios de aceptación

- [ ] Pantalla "Usuarios y roles → Usuarios": lista paginada con búsqueda por nombre o correo y filtros por rol y estado.
- [ ] Alta de usuario con correo, nombre y uno o varios roles. No se escribe la contraseña: el usuario recibe un enlace para definirla (HU-113); hasta entonces su estado es "Invitado".
- [ ] Edición de datos y de roles asignados.
- [ ] Desactivar: el usuario no puede iniciar sesión y sus sesiones abiertas dejan de funcionar de inmediato. Reactivar lo devuelve a su estado anterior.
- [ ] Revocar accesos: cierra todas sus sesiones y le quita todos los roles, sin borrar el usuario ni su historial.
- [ ] Un usuario no se puede desactivar ni revocar a sí mismo; siempre queda al menos un administrador activo por conjunto.
- [ ] Los usuarios no se eliminan.
- [ ] Todas estas operaciones exigen `users.manage` y quedan en la bitácora (HU-112).
- [ ] `User.ApartmentNumber` deja de pedirse aquí; la relación con unidades se registra en HU-116 y HU-117.
- [ ] La pantalla "Residentes" actual se retira del menú cuando HU-116 esté hecha; hasta entonces sigue funcionando.

## Diseño técnico

- `User.Status` (`Invited | Active | Disabled`) reemplaza el uso de `IsActive` para usuarios.
- `User.SecurityStamp`: cambia al desactivar, revocar o cambiar contraseña; el servidor rechaza tokens con un sello anterior.
- Frontend: módulo `modules/access-control/users` con las piezas globales existentes (`data-table`, `form-section`, panel lateral para filtros).

## Verificación

Crear un usuario con rol Portería, iniciar sesión en otra ventana, desactivarlo desde el
administrador: la siguiente acción de la otra ventana lo lleva al login y no puede volver a entrar.
