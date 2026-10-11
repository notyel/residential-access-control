# HU-101 · Secretos fuera del repositorio

**Épica:** E7 · **Etapa:** 0 Estabilizar · **Estado:** Hecha con pendientes · **Depende de:** —
**Requerimiento:** §7.1 · **Defecto:** D6

## Historia

Como responsable de la plataforma, quiero que las credenciales no estén en el código fuente, para
que tener acceso al repositorio no equivalga a tener acceso a la base de datos ni a poder firmar
tokens.

## Situación actual

`backend/AccessControl.API/appsettings.json` contiene la cadena de conexión de PostgreSQL con
usuario y contraseña y la clave `Jwt:Key`, ambas versionadas. El origen CORS
(`http://localhost:4400`) está fijo en `Program.cs`.

## Criterios de aceptación

- [x] `appsettings.json` no contiene contraseña de base de datos ni clave JWT; quedan marcadores vacíos.
- [x] En desarrollo los valores se leen de `dotnet user-secrets`; en despliegue, de variables de entorno (`ConnectionStrings__DefaultConnection`, `Jwt__Key`).
- [x] La API no arranca si falta la cadena de conexión o si la clave JWT tiene menos de 32 bytes, con un mensaje que dice qué falta.
- [x] Los orígenes CORS permitidos se leen de configuración.
- [x] `backend/README.md` explica cómo configurar un entorno nuevo.
- [ ] **La contraseña de la base de datos y la clave JWT expuestas se rotan.** Sacarlas del archivo no las invalida: siguen en el historial de git. La rotación la hace el dueño de la cuenta de la base de datos.

## Diseño técnico

- `builder.Services.AddOptions<JwtSettings>().Bind(...).Validate(...).ValidateOnStart()`, igual que ya se hace con `DateTimeSettings`.
- `AuthService` recibe `IOptions<JwtSettings>` en lugar de `IConfiguration`.
- No reescribir el historial de git salvo decisión expresa; la rotación es lo que cierra el riesgo.

## Archivos

`backend/AccessControl.API/appsettings.json`, `Program.cs`,
`backend/AccessControl.Application/Services/AuthService.cs`, `backend/README.md`.

## Verificación

Clonar en limpio, arrancar sin secretos (debe fallar con mensaje claro), configurarlos y hacer
login. `git grep` de la contraseña anterior no devuelve resultados en el árbol de trabajo.
- **Ajuste posterior (decisión del dueño del producto):** la configuración de **desarrollo** volvió al repositorio, en `appsettings.Development.json`, para que el proyecto arranque sin pasos previos; se considera pública. Lo que se resguarda es producción: ese archivo no se lee fuera de desarrollo, los valores llegan por variables de entorno y la API no arranca en producción con la clave JWT de desarrollo. El criterio "en desarrollo se leen de `dotnet user-secrets`" queda como opción, no como requisito.
