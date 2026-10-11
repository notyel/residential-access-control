# HU-140 · Fallas de autenticación (Authentication Failures)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Authentication Failures (A07 OWASP)
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 5, 6, 8

## Historia

Como evaluador de seguridad, quiero probar el inicio de sesión, la recuperación de credenciales,
la gestión de sesiones y la resistencia ante intentos de autenticación indebidos, para cerrar los
criterios 5 y 6 de la épica sobre mecanismos de autenticación.

## Qué se prueba

**A. Inicio de sesión**

1. Credenciales correctas → 200 con `token` (`POST api/auth/login`).
2. Contraseña incorrecta, usuario inexistente, usuario invitado, usuario desactivado y usuario
   de conjunto desactivado → **la misma respuesta 401** con el mismo mensaje (sin enumeración de
   usuarios). Pruebas existentes: `Login_con_contrasena_incorrecta_responde_401`,
   `Un_conjunto_desactivado_no_permite_iniciar_sesion_a_sus_usuarios`.
3. Fuerza bruta: 5 fallos consecutivos → bloqueo 15 minutos aunque la sexta sea correcta
   (`AuthService.MaxFailedLogins = 5`, `LockoutDuration = 15 min`; prueba
   `Cinco_intentos_fallidos_bloquean_el_inicio_de_sesion…`).
4. Límite de peticiones: más de `RateLimits:AuthPerMinute` (20 por defecto) desde la misma IP en
   1 minuto → 429 con mensaje genérico (`Program.cs:88-98`). **Verificar**: la fixture de tests
   sube el límite a 1000000 (`ApiFixture.cs:61`), así que esta prueba no está cubierta en CI.
5. Verificar que `logout` y `me` **no** tienen `[EnableRateLimiting]` (`AuthController.cs:63-73`).

**B. Recuperación de credenciales**

1. `POST api/auth/forgot-password` con correo existente y no existente → respuesta idéntica.
2. El enlace sirve una sola vez: reutilizar el token → 400 "El enlace no es válido o ya venció"
   (prueba `Recuperar_la_contrasena_usa_un_enlace_de_un_solo_uso…`).
3. Token vencido (60 min) y token de invitación (72 h) → rechazo; un token nuevo anula los
   anteriores (prueba `Reenviar_la_invitacion_anula_el_enlace_anterior`).
4. Restablecer la contraseña rota el `SecurityStamp` → todas las sesiones abiertas quedan en 401
   (prueba de cierre de sesiones).
5. Política de contraseña: mínimo 10 caracteres y lista de 15 contraseñas comunes
   (`AccountSecurity.cs:18-41`; prueba `La_contrasena_debe_cumplir_la_politica`). Probar además
   contraseñas que **hoy pasan**: `abcdefghijA1` (secuencial), `AdminPass123!` (la sembrada),
   la del propio correo, y verificar que no hay historial ni verificación de similitud.

**C. Sesiones**

1. Token robado/expirado → 401 y la interfaz cierra sesión (`auth.interceptor.ts:32-35`).
2. Desactivar o revocar un usuario con sesión abierta → 401 inmediato
   (`CurrentUserMiddleware.cs:30-38`; pruebas `Desactivar_cierra_la_sesion_abierta…`,
   `Revocar_quita_los_roles_y_cierra_la_sesion…`).
3. `POST api/auth/logout` invalida el token en el servidor (prueba
   `Cerrar_sesion_invalida_el_token_en_el_servidor`).
4. Verificar claims del token (`stamp`, `complex`, `super`, `exp`) y que la firma se valida en
   cada petición (`Program.cs:54-62`).
5. Comprobar que un JWT con firma alterada o `alg: none` se rechaza.

## Fundamento en el API

### Lo que sí cumple con la épica

- Respuesta única de error de login sin revelar si la cuenta existe (`AuthService.AuthenticateAsync`).
- Bloqueo por intentos (5 / 15 min) con columnas `FailedLoginCount` y `LockedUntil` y prueba.
- Límite de peticiones por IP en los 4 endpoints anónimos, con 429 controlado.
- Tokens de recuperación/invitación de un solo uso, con hash SHA-256, expiración y anulación de
  los anteriores; mismas pruebas automatizadas.
- Invalidación server-side por sello de seguridad en cada petición y cierre global al
  restablecer contraseña o desactivar usuarios.
- Política de contraseña de 10 caracteres con lista de comunes, validada en el servidor.
- Bitácora de autenticación: `auth.login`, `auth.login_failed`, `auth.login_locked`,
  `auth.logout_all`, `auth.invitation_accepted`, `auth.password_reset_requested`,
  `auth.password_reset` (`IAuditService.AuditActions`), con prueba
  `Los_inicios_de_sesion_correctos_y_fallidos_quedan_registrados`.

### Lo que no cumple o falta verificar

- `LogEmailSender` vuelca el enlace con token al log (`AccountSecurity.cs:56`): el canal de
  recuperación queda expuesto donde se guarde el log (cruza A02-3).
- Política de contraseña débil: sin complejidad, sin historial, sin similitud, con solo 15
  contraseñas vetadas; `AdminPass123!` ni siquiera está en la lista.
- Cuentas sembradas con credenciales conocidas (`DataSeeder.cs:59-101`).
- Rate limit solo por IP y sin `UseForwardedHeaders`; sin límite por cuenta y sin límite en
  `logout`/`me`.
- Sin MFA y sin segundo factor para el superadministrador.
- Sesión fija de 240 minutos sin renovación; `session.max_hours` sin aplicar (cruza A04-1).
- El token vive en `localStorage` (cruza A02-2).
- La prueba de rate limiting está desactivada en la suite (`ApiFixture.cs:61`).

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A07-1 | Enlaces de recuperación/invitación en el log | `AccountSecurity.cs:56` · `Program.cs:143` | Robo de cuenta desde el log | Crítico | SMTP real en despliegue; en desarrollo no registrar el cuerpo del enlace | P0 |
| A07-2 | Credenciales por defecto de las cuentas sembradas | `DataSeeder.cs:59-101` | Acceso inicial a superadministrador | Crítico | Retirar el seed de producto o forzar cambio en el primer ingreso | P0 |
| A07-3 | Política de contraseña débil (solo longitud + 15 comunes) | `AccountSecurity.cs:18-41` | Contraseñas predecibles o basadas en el correo | Alto | Bloquear patrones y datos del usuario, considerar diccionario ampliado y verificación de historial | P1 |
| A07-4 | Rate limit por IP sin proxy confiable ni límite por cuenta | `Program.cs:92-94` | Fuerza bruta compartida o evadida detrás de proxy | Alto | `UseForwardedHeaders` + límite por usuario/correo además del por IP | P1 |
| A07-5 | Sin MFA para superadministrador | `AuthService` | El rol más crítico depende de una sola contraseña | Medio | TOTP o segundo factor para `super` y `roles.manage` | P2 |
| A07-6 | Prueba de rate limit ausente en CI | `ApiFixture.cs:61` | El control puede romperse sin detectarse | Medio | Habilitar el límite en una prueba dedicada con política propia | P2 |

## Criterios de aceptación

- [ ] Documentadas las pruebas de login, recuperación y sesión con payload, código y respuesta
      (criterios 1 y 6).
- [ ] Verificada la no enumeración de usuarios y la política de contraseñas con casos positivos y
      negativos (criterio 5).
- [ ] Confirmado que ninguna respuesta ni registro expone credenciales o tokens (criterios 5 y 7).
- [ ] Cada hallazgo con componente, impacto, riesgo, recomendación y prioridad (criterios 2 y 8).

## Verificación

Evidencia en `documentation/owasp-top10/evidencias/A07-<n>-(peticion|respuesta).txt`. El bloque
de fuerza bruta se ejecuta en un entorno aislado (las cuentas de prueba se bloquean 15 minutos) y
el bloque de rate limit se ejecuta sin la variable de fixture que lo desactiva.
