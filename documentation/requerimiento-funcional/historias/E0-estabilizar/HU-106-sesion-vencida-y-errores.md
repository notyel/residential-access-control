# HU-106 · Sesión vencida y errores del API

**Épica:** E7 · **Etapa:** 0 Estabilizar · **Estado:** Hecha · **Depende de:** —
**Requerimiento:** §7.1, §7.6 · **Defecto:** D7

## Historia

Como usuario, quiero que la aplicación me diga con claridad qué salió mal y me lleve al login cuando
mi sesión venza, para no quedarme en una pantalla que no responde.

## Situación actual

- Cada acción de controlador envuelve todo en `try/catch` y responde 500 con `ex.Message`. Un
  documento duplicado al registrar una visita o un `PersonId` inexistente salen como error interno.
- Los DTO no tienen validaciones.
- El frontend no trata el 401: con el token vencido (240 min) las pantallas quedan en error.
- `AuthService` del frontend considera autenticado a cualquiera que tenga algo en `localStorage`.
- El login no consulta `User.IsActive`.

## Criterios de aceptación

- [x] Un manejador global traduce las excepciones: validación → 400, no encontrado → 404, conflicto o duplicado → 409, lo demás → 500 con mensaje genérico y el detalle solo en el log.
- [x] Se eliminan los `try/catch` repetidos de los controladores.
- [x] Los DTO de entrada declaran obligatorios y longitudes; el 400 incluye los errores por campo dentro de `ResponseModel.errors`.
- [x] Ante un 401 el frontend cierra la sesión, lleva a `/login` y, tras entrar, vuelve a la ruta donde estaba.
- [x] `authGuard` rechaza un token vencido (lee `exp`) sin esperar a la primera llamada.
- [x] Un usuario con `IsActive = false` no puede iniciar sesión.
- [x] Ante un 403 se muestra "No tiene permiso para esta acción" y no se cierra la sesión.

## Diseño técnico

- Backend: `IExceptionHandler` + `ProblemDetails` adaptado a `ResponseModel`; excepciones de dominio (`NotFoundException`, `ConflictException`, `ValidationException`) en `AccessControl.Application`.
- Frontend: interceptor funcional (`withInterceptors`) que añade el token y trata 401/403; reemplaza el interceptor de clase.

## Archivos

`backend/AccessControl.API/Program.cs`, controladores, servicios que hoy lanzan
`InvalidOperationException`/`ArgumentException`; `front/src/app/core/interceptors/*`,
`core/guards/auth.guard.ts`, `core/services/auth.service.ts`, `core/config/app.config.ts`.

## Verificación

Registrar una visita con un documento ya existente como "persona nueva" (409 con mensaje legible).
Alterar la fecha del token en `localStorage` y navegar: redirige al login y regresa a la ruta.

## Hallazgos al implementar

- Al validar los DTO apareció un defecto que los datos simulados ocultaban: el formulario de visita envía `""` en correo y teléfono cuando no se diligencian, y `[EmailAddress]` lo rechazaba. Se creó `[OptionalEmailAddress]` y los opcionales en blanco se guardan como `null`.
