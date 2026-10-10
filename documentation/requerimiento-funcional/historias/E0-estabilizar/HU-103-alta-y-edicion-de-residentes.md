# HU-103 · Alta y edición de residentes contra el API

**Épica:** E7 · **Etapa:** 0 Estabilizar · **Estado:** Hecha · **Depende de:** —
**Requerimiento:** §1.2 · **Defecto:** D2

## Historia

Como administrador, quiero crear y editar residentes desde las pantallas que ya existen, para
mantener el directorio mientras se construye el modelo completo de unidades y ocupantes.

## Situación actual

`ResidentsService.createResident` hace `POST users` y `updateResident` hace `PUT users/{id}`.
`UsersController` solo expone `GET`. La creación de usuarios existe en `POST auth/register`, con
otra forma de respuesta. Las pantallas Nuevo residente y Editar residente no guardan.

## Criterios de aceptación

- [x] `POST api/users` crea el usuario (correo, nombre, apellido, apartamento, contraseña, rol) y devuelve `ResponseModel<UserDto>`; correo duplicado → 409.
- [x] `PUT api/users/{id}` actualiza nombre, apellido y apartamento; no cambia contraseña ni rol; id inexistente → 404.
- [x] Ambos exigen rol Admin.
- [x] Los DTO validan obligatorios, formato de correo y longitud mínima de contraseña; los errores vuelven como 400 con el detalle por campo.
- [x] El frontend lee `response.data` en crear y editar y muestra el mensaje del API si falla.
- [x] `POST auth/register` se elimina o delega en el mismo servicio, para no tener dos formas de crear usuarios.

## Diseño técnico

- Mover `CreateUserAsync` de `AuthService` a `UsersService`; añadir `UpdateUserAsync`.
- Es un arreglo sobre el modelo actual; el alta definitiva de usuarios es HU-110 y la de propietarios HU-116.

## Archivos

`backend/AccessControl.API/Controllers/UsersController.cs`, `AuthController.cs`,
`backend/AccessControl.Application/Services/UsersService.cs`,
`front/.../residents/services/residents.service.ts`, páginas `new-resident` y `edit-resident`.

## Verificación

Crear un residente, verlo en la lista, editar su apartamento, recargar y confirmar el cambio.
Repetir el alta con el mismo correo y ver el mensaje de duplicado.
