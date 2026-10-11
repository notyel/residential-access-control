# HU-137 · Fallas criptográficas (Cryptographic Failures)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Cryptographic Failures (A02 OWASP)
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 5, 8

## Historia

Como evaluador de seguridad, quiero verificar cómo se protegen las credenciales, los tokens y los
datos personales de propietarios, residentes, visitantes, vehículos y parqueaderos durante su
almacenamiento y su transmisión, para demostrar el criterio 5 de la épica.

## Qué se prueba

1. **Almacenamiento de contraseñas.** Confirmar que solo se persiste el hash BCrypt
   (`BCrypt.Net-Next 4.0.3`, `AuthService.cs` `HashPassword`/`Verify`) y que
   `User.PasswordHash` y `User.SecurityStamp` están marcados `[NotAudited]` (`User.cs`) para no
   copiarse a la bitácora (prueba existente: `La_bitacora_nunca_contiene_contrasenas_sellos_ni_tokens`).
2. **Almacenamiento de tokens de recuperación/invitación.** Verificar en BD que solo exista el
   hash SHA-256 (`UserToken.TokenHash`, índice único) y nunca el token en claro
   (`AccountSecurity.cs:98-105,137-140`).
3. **Token de sesión (JWT).** Decodificar el JWT y confirmar claims (`nameid`, `name`, `email`,
   `stamp`, `complex`, `tz`, `super`), algoritmo HMAC-SHA256, `exp` de 240 minutos
   (`JwtSettings.ExpireMinutes`) y que la clave cumpla ≥32 bytes (se valida al arrancar,
   `Program.cs:32-36`). Probar que un JWT firmado con otra clave o con `alg: none` es rechazado.
4. **Transmisión.** Confirmar que las peticiones viajan por HTTPS en el entorno de pruebas y
   verificar si el servidor emite `Strict-Transport-Security` (hoy no hay `UseHsts()`).
5. **Exposición del token en el navegador.** Verificar que el token vive en `localStorage` bajo
   la llave `jwt` (`front/src/app/core/services/auth.service.ts:33,52,105`), es accesible para
   cualquier script de la página y no hay cookie `HttpOnly`.
6. **Token en la URL.** Verificar que los enlaces de recuperación/invitación llevan el token en
   el query string (`UserTokenService.BuildLink` → `?token=…`, `AccountSecurity.cs:134`) y que el
   frontend lo lee de `queryParamMap` (`front/.../account/account.component.ts`): historial de
   navegador, encabezado `Referer` y registros de proxy pueden exponerlo.
7. **Tokens en el log.** Comprobar qué queda en la consola/log al enviar un correo con
   `LogEmailSender`: escribe el cuerpo completo con `logger.LogInformation("Correo para {To} —
   {Subject}\n{Body}", …)` (`AccountSecurity.cs:56`), es decir, el enlace con el token de
   restablecimiento o invitación.
8. **Datos personales en claro.** Revisar el modelo: `Person` guarda documento, teléfono y correo
   sin cifrado en reposo; no hay `ValueConverter` criptográfico en `ApplicationDbContext`
   (solo `UtcDateTimeConverter`).
9. **Credenciales versionadas.** Contrastar con `DataSeeder.cs:59-101` (hashes y comentarios con
   la contraseña en claro) y `launchSettings.json:37`.

## Fundamento en el API

### Lo que sí cumple con la épica

- Contraseñas con BCrypt (salt integrado, verificación constante en tiempo comparable).
- JWT con firma HMAC-SHA256 y validación de emisor, audiencia, firma y vida útil
  (`Program.cs:54-62`); clave mínima de 32 bytes exigida al arrancar (`JwtSettings.MinKeyBytes`).
- Tokens de un solo uso generados con `RandomNumberGenerator` de 32 bytes y guardados solo como
  hash SHA-256, con expiración de 60 minutos (recuperación) y 72 horas (invitación)
  (`AccountSecurity.cs:74-75,98-107`).
- La bitácora no guarda credenciales ni sellos: exclusión `[NotAudited]` + prueba automatizada
  `La_bitacora_nunca_contiene_contrasenas_sellos_ni_tokens` (`AuditTests.cs:107`).
- Secretos de configuración fuera de `appsettings.json` y validación al arrancar.

### Lo que no cumple o falta verificar

- Token de sesión en `localStorage` (no `HttpOnly`, no `Secure`, no `SameSite`): cualquier XSS
  roba la sesión completa; pendiente declarado en HU-113.
- Enlace con token en query string (URL compartida, historial, `Referer`).
- `LogEmailSender` escribe los enlaces con token en el log a nivel Information: en entornos donde
  el log persista hay tokens recuperables (se cruza con los criterios 5 y 7 de la épica).
- Sin cifrado en reposo de datos personales: documento, teléfono, correo y placas en claro.
- Credenciales versionadas: contraseñas sembradas con su texto en comentarios
  (`DataSeeder.cs:59-101`) y `Password=test` en `launchSettings.json:37`.
- Sin rotación documentada de la clave JWT y con sesión de 240 minutos sin renovación (HU-113
  pendiente).
- HSTS ausente (ver HU-135): la transmisión depende de que el proxy/red imponga HTTPS.

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A02-1 | Cuentas sembradas con contraseñas conocidas en código | `DataSeeder.cs:59-101` | Acceso directo a superadministrador | Crítico | No sembrar credenciales en producto; forzar rotación al primer ingreso | P0 |
| A02-2 | Token en `localStorage` | `auth.service.ts:33,52` | Robo de sesión ante cualquier XSS | Alto | Token de acceso corto en memoria + refresh en cookie `HttpOnly; Secure; SameSite=Strict` (HU-113) | P1 |
| A02-3 | Tokens de recuperación en el log | `AccountSecurity.cs:56` | Cualquiera con acceso al log restablece contraseñas | Alto | En despliegue usar SMTP real; en desarrollo registrar solo el hash/traza, nunca el enlace | P0 |
| A02-4 | Token en query string del enlace | `AccountSecurity.cs:134` | Filtración por historial, `Referer` y logs de proxy | Medio | Usar ruta corta con token en cuerpo o enlace de un solo uso con intercambio por POST | P1 |
| A02-5 | Sin cifrado en reposo de PII | `ApplicationDbContext` | Un volcado de BD expone documentos, teléfonos y placas | Medio | Cifrado a nivel de columna/volumen o cifrado del disco con gestión de claves; evaluar según HU-131 | P2 |
| A02-6 | Contraseña de BD versionada en `launchSettings.json` | `Properties/launchSettings.json:37` | Fuga de credencial | Medio | Mover a user-secrets y limpiar el repositorio (también A05-2) | P1 |
| A02-7 | Sin rotación de clave JWT ni renovación de token | `JwtSettings.cs`, HU-113 | Sesión larga no renovable; rotación implica caída global | Bajo | Definir rotación de clave y refresh token (HU-113) | P2 |

## Criterios de aceptación

- [ ] Documentada la verificación de almacenamiento (hashes, sellos, exclusiones de auditoría)
      con evidencia de base de datos o de código según corresponda (criterios 1 y 5).
- [ ] Documentada la verificación de transmisión (HTTPS, HSTS, manejo del token en el navegador
      y en los enlaces) (criterio 5).
- [ ] Cada hallazgo registrado con componente, impacto, riesgo, recomendación y prioridad
      (criterios 2 y 8).
- [ ] Dejado explícito qué datos personales viven en claro en la base de datos y bajo qué
      control de acceso se exponen.

## Verificación

Evidencia: captura del JWT decodificado (con la firma ocultada), consulta SQL de
`UserTokens` mostrando solo `TokenHash`, contenido del log tras disparar
`forgot-password`, y cabeceras HTTPS de la respuesta. Todo guardado en
`documentation/owasp-top10/evidencias/A02-<n>.txt`.
