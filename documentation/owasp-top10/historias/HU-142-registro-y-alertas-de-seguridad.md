# HU-142 · Registro y alertas de seguridad (Security Logging and Alerting Failures)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Security Logging and Alerting Failures (A09 OWASP)
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 7, 8

## Historia

Como evaluador de seguridad, quiero comprobar que los eventos de seguridad relevantes quedan
registrados de forma que puedan investigarse —y que los registros no contengan credenciales, tokens
ni otros datos sensibles—, para cerrar el criterio 7 de la épica.

## Qué se prueba

**A. Cobertura de eventos**

1. Bitácora de autenticación: generar login correcto, login fallido, bloqueo, cierre de sesión,
   solicitud y ejecución de recuperación, invitación aceptada; verificar que cada uno deja fila en
   `AuditLogs` con IP, usuario y fecha (pruebas existentes `Los_inicios_de_sesion_correctos_y_fallidos_quedan_registrados`).
2. Bitácora de cambios: modificar un rol, desactivar un usuario y cambiar una política; verificar
   fila con valor anterior y nuevo (pruebas `Un_cambio_deja_quien_que_y_cuando…`,
   `Los_cambios_de_permisos_y_de_estado_de_usuarios_quedan_registrados`,
   `Cambiar_una_politica_persiste_solo_en_ese_conjunto_y_queda_en_la_bitacora`).
3. **Eventos faltantes a documentar:** denegaciones de autorización (403), accesos rechazados en
   portería (sí existen como `AccessEvent`, verificar que son consultables), límite de peticiones
   excedido (429) y manipulación de identificadores (404 inesperados). Hoy el `GlobalExceptionHandler`
   solo registra los 500.
4. Investigabilidad: con IP + fecha + usuario + acción, verificar que se puede reconstruir un
   incidente de intentos de login usando `GET api/audit` con filtros (`AuditController`,
   `AuditService.SearchAsync`).

**B. Ausencia de secretos en registros**

1. Bitácora de base de datos: ejecutar la prueba existente
   `La_bitacora_nunca_contiene_contrasenas_sellos_ni_tokens` (`AuditTests.cs:107`) que busca
   `PasswordHash`, `SecurityStamp`, `TokenHash`, `$2a$` y la contraseña intentada.
2. **Log de la aplicación (consola):** enviar `POST api/auth/forgot-password` y revisar la salida
   del proceso; `LogEmailSender` escribe `Correo para {To} — {Subject}` **con el cuerpo completo
   del enlace y el token** (`AccountSecurity.cs:56`). Este es el hallazgo central de esta
   categoría.
3. Revisar el resto de `Log*` del código: `GlobalExceptionHandler` (excepción completa, incluye
   ruta y método), `Program.cs:177` (migraciones pendientes), `DailyMaintenanceService`. Ninguno
   debe registrar credenciales.
4. Verificar que los mensajes de negocio que vuelven al cliente no incluyan datos sensibles
   (cruza con A10: `GlobalExceptionHandler.cs:30` copia el mensaje al cuerpo).

**C. Retención, confiabilidad y alertas**

1. ¿Dónde quedan los logs? Hoy solo consola (`ILogger`, `Logging:LogLevel` en `appsettings.json`);
   no hay Serilog ni sink a archivo/centro. Comprobar si el proceso de despliegue persiste la
   consola y por cuánto tiempo.
2. No hay alertas: no existe ninguna notificación por eventos como `auth.login_locked` repetidos,
   ni integración de monitoreo.
3. No hay definición de retención/rotación ni de sincronización de hora documentada.

## Fundamento en el API

### Lo que sí cumple con la épica

- Tabla `AuditLogs` con acción, entidad, usuario, IP, cambios en JSON, fecha y conjunto
  (`Domain/Entities/AuditLog.cs`, `ApplicationDbContext.Audit.cs`), escrita en la misma
  transacción que el cambio.
- Eventos de autenticación completos (`IAuditService.AuditActions`, 9 acciones) + acción
  `visit.checkout` y eventos de acceso del dominio (`AccessEvent` con anulación y motivo).
- Bitácora de solo lectura, con permiso `audit.read`, aislada por conjunto y sin secretos:
  6 pruebas en `AuditTests.cs` incluyen la de "nunca contiene contraseñas, sellos ni tokens".
- Exclusiones `[NotAudited]` sobre `PasswordHash` y `SecurityStamp`.
- Excepciones no controladas registradas con detalle completo en el log del servidor
  (`GlobalExceptionHandler.cs:26`) mientras el cliente solo ve un mensaje genérico.

### Lo que no cumple o falta verificar

- El log de la aplicación sí contiene tokens: `LogEmailSender` imprime el enlace de
  recuperación/invitación (criterio 7 incumplido mientras se use ese `IEmailSender`).
- Logging solo a consola: sin archivo, sin centro de logs, sin retención definida → no es
  posible investigar un incidente después de reiniciar el proceso.
- No hay alertas de ningún tipo; los eventos de seguridad se quedan en la bitácora sin
  notificación.
- No se registran denegaciones de autorización (403), rechazos por rate limit (429) ni intentos
  de acceso a identificadores ajenos.
- No existe prueba automatizada de que el **log de aplicación** (no la bitácora) esté limpio.

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A09-1 | Tokens de recuperación/invitación impresos en el log | `AccountSecurity.cs:56` | Quien lee el log toma cuentas; además incumple el criterio 7 | Crítico | Reemplazar `LogEmailSender` por SMTP en despliegue y dejar de registrar el cuerpo | P0 |
| A09-2 | Logs solo en consola, sin retención | `Program.cs`, `appsettings.json` | No hay investigación posible tras reinicio; sin correlación | Alto | Serilog (o equivalente) con sink a archivo/centro, rotación y retención definida | P1 |
| A09-3 | Sin alertas de seguridad | infraestructura | Bloqueos masivos, intentos de login o 403 se detectan tarde | Alto | Alertas sobre contadores de `auth.login_failed`, `auth.login_locked` y picos de 403/429 | P1 |
| A09-4 | No se registran denegaciones (403/429) ni accesos fallidos por IDOR | `GlobalExceptionHandler`, pipeline | No se puede investigar un intento de acceso indebido | Medio | Middleware de registro de respuestas 401/403/404 con IP, ruta y usuario | P1 |
| A09-5 | Sin prueba de que el log de aplicación esté limpio | `AccessControl.Tests` | Regresiones del mismo tipo sin cubrir | Medio | Test que dispare `forgot-password` sobre un logger capturado y afirme que no aparece el token | P1 |

## Criterios de aceptación

- [ ] Documentado qué eventos quedan registrados y cuáles faltan, con la prueba que lo demuestra
      (criterios 1 y 7).
- [ ] Ejecutada la verificación de secretos tanto en la bitácora como en el log de la
      aplicación, con evidencia (criterio 7).
- [ ] Cada hallazgo con componente, impacto, riesgo, recomendación y prioridad (criterios 2 y 8).
- [ ] Dejado por escrito dónde viven los logs en cada entorno, con cuánta retención y quién los
      revisa.

## Verificación

Evidencia: salida de `dotnet test --filter AuditTests`, transcripción del log del proceso tras
`forgot-password` (con el token enmascarado en el documento, pero mostrando que aparece) y
consulta de `AuditLogs`. Archivos en `documentation/owasp-top10/evidencias/A09-<n>.txt`.
