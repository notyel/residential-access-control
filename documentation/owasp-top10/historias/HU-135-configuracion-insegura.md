# HU-135 · Configuración insegura (Security Misconfiguration)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Security Misconfiguration (A05 OWASP)
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 5, 8

## Historia

Como evaluador de seguridad, quiero revisar la configuración del API, del despliegue y de los
mecanismos de protección (CORS, HTTPS, cabeceras, límites, secretos, cuentas sembradas), para
documentar qué protecciones existen hoy y cuáles faltan antes de considerar el sistema apto para
producción.

## Qué se prueba

1. **Secretos fuera del repositorio.** Verificar que `appsettings.json` tenga
   `ConnectionStrings:DefaultConnection` y `Jwt:Key` vacíos y que la API se niegue a arrancar sin
   ellos (`Program.cs:24-36`). Verificar que no existan secretos en `appsettings.Development.json`
   ni en el historial de git.
2. **Credenciales versionadas.** Confirmar los hallazgos actuales: cadena de conexión con
   `Password=test` en `Properties/launchSettings.json:37` (incluye `Include Error Detail=true`)
   y cuentas sembradas con contraseñas conocidas en `Persistence/DataSeeder.cs:59-101`
   (`superadmin@`, `admin@`, `owner@`, `guard@accesscontrol.com` con los comentarios
   `// AdminPass123!`, `// OwnerPass123!`, `// GuardPass123!`).
3. **CORS.** `OPTIONS` desde un origen no listado en `Cors:AllowedOrigins` → sin cabeceras
   CORS; desde `http://localhost:4400` → permitido. Confirmar que no se usa `AllowCredentials`
   (`Program.cs:149-159`).
4. **HTTPS y HSTS.** Responderá la API por HTTP sin redirigir si el entorno no expone HTTPS;
   comprobar con `curl -I` si el encabezado `Strict-Transport-Security` existe (hoy no: no hay
   `UseHsts()` en `Program.cs`).
5. **Cabeceras de seguridad.** Verificar ausencia de `X-Content-Type-Options`,
   `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`, `Permissions-Policy` — no hay
   middleware que las agregue (`Program.cs:185-197`).
6. **Host header.** `GET /api/auth/me` con `Host: ejemplo.com` → comprobar el comportamiento con
   `AllowedHosts: "*"` (`appsettings.json:26`).
7. **Límite de peticiones.** Confirmar que solo los 4 endpoints anónimos llevan
   `[EnableRateLimiting(RateLimits.Auth)]` (`AuthController.cs:22,36,45,55`); `logout` y `me` y
   todos los endpoints de negocio no tienen límite. Verificar la partición por IP:
   `Program.cs:92-94` usa `Connection.RemoteIpAddress` y **no** hay `UseForwardedHeaders`, así
   que detrás de un reverse proxy todas las peticiones comparten la IP del proxy.
8. **Exposición de documentación.** Confirmar que no hay Swagger/OpenAPI publicado (no hay
   paquete Swashbuckle ni `UseSwagger`).
9. **Configuración por entorno.** Verificar que `RateLimits`, `Database:MigrateOnStartup` y
   `Maintenance:Enabled` no están en `appsettings.json` y corren con valores por defecto
   (`RateLimits.cs`, `Program.cs:135,164`), y que existe instrucción de despliegue para
   sobre-escribirlos por variables de entorno.
10. **Tamaño de cuerpo.** Enviar un cuerpo grande a `POST api/units/bulk` y comprobar el
    comportamiento (no hay límite de `MaxRequestBodySize` configurado).

## Fundamento en el API

### Lo que sí cumple con la épica

- Secretos obligatorios fuera de appsettings y validación al arrancar (`Program.cs:23-36`,
  `UserSecretsId accesscontrol-api-7b1d2c64` en el `.csproj`).
- CORS con lista explícita de orígenes, sin credenciales (`Program.cs:149-159`).
- `RequireHttpsMetadata = true` y `UseHttpsRedirection()` (`Program.cs:52,187`).
- Sin Swagger publicado: la superficie de API no se documenta automáticamente en el sitio.
- Sin credenciales versionadas en `appsettings.json` ni en `appsettings.Development.json`
  (ambos solo tienen Logging).

### Lo que no cumple o falta verificar

- No hay `UseHsts()` en el pipeline (`Program.cs:185-197`).
- No hay cabeceras de seguridad (CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy).
- `AllowedHosts: "*"` (`appsettings.json:26`).
- `launchSettings.json:37` conserva `Password=test` en el repositorio.
- `DataSeeder.cs` siembra cuatro cuentas con contraseñas fijas y sus comentarios en claro.
- Rate limit solo en 4 rutas y particionado por IP sin `UseForwardedHeaders`.
- Sin límite de tamaño de cuerpo ni configuración de Kestrel.
- No existe archivo de configuración para producción (`appsettings.Production.json`).

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A05-1 | Contraseñas de cuentas sembradas en claro en el código | `DataSeeder.cs:59-101` | Acceso directo a superadministrador en cualquier entorno donde corra el seed | Crítico | Sembrar solo en desarrollo, forzar cambio de contraseña al primer ingreso o eliminar el seed de producto | P0 |
| A05-2 | Cadena de conexión con contraseña versionada | `Properties/launchSettings.json:37` | Fuga de credencial de base de datos del entorno de desarrollo | Medio | Moverla a user-secrets / variables de entorno y quitarla del repositorio | P1 |
| A05-3 | Sin HSTS ni cabeceras de seguridad | `Program.cs:185-197` | Fuerza a HTTP, clickjacking, MIME-sniffing, fugas por Referer | Medio | `UseHsts()` + middleware de cabeceras (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, CSP, `Referrer-Policy`) | P1 |
| A05-4 | `AllowedHosts: "*"` | `appsettings.json:26` | Ataques de encabezado Host | Medio | Fijar los hostes reales por entorno | P2 |
| A05-5 | Rate limit solo en autenticación y por IP sin `ForwardedHeaders` | `Program.cs:88-98` | Lentitud en mitigación de fuerza bruta detrás de proxy; endpoints de negocio sin límite | Medio | `UseForwardedHeaders` con lista de proxies de confianza + política global por usuario/ruta | P1 |
| A05-6 | Sin límite de tamaño de cuerpo | `Program.cs` (sin `Kestrel`/`MaxRequestBodySize`) | Negación de servicio con payloads grandes | Bajo | Fijar límite explícito (p. ej. 1 MB) y `MultipartBodyLengthLimit` | P2 |
| A05-7 | Configuración por defecto no declarada (`RateLimits`, `Maintenance`, `Database`) | `Program.cs:135,164` · `RateLimits.cs` | Un despliegue puede operar con valores de desarrollo sin darse cuenta | Bajo | Publicar la sección con valores de producción y documentarlas en el README de despliegue | P2 |

## Criterios de aceptación

- [ ] Documentadas las pruebas de configuración con la petición usada y la respuesta o cabecera
      observada (criterio 1).
- [ ] Inventario de configuraciones inseguras con evidencia, impacto, riesgo, recomendación y
      prioridad (criterios 2 y 8).
- [ ] Verificado que ningún archivo versionado contiene credenciales de base de datos, clave JWT
      ni contraseñas de cuentas reales, salvo el seed declarado y su remediación (criterio 5).
- [ ] Confirmado el comportamiento de CORS, HTTPS/HSTS y cabeceras con pruebas reproducibles.

## Verificación

Pruebas con `curl` contra el entorno de pruebas: cabeceras de respuesta (`-D`), origen cruzado,
cuerpo excesivo y encabezado Host. El inventario final se contrasta contra `git grep` de
`Password`, `Jwt:Key` y `$2a$` para demostrar qué secretos siguen versionados.
