# HU-139 · Diseño inseguro (Insecure Design)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Insecure Design (A04 OWASP)
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 3, 8

## Historia

Como evaluador de seguridad, quiero evaluar las reglas de negocio y los controles que previenen
suplantaciones, escaladas de privilegio y operaciones inconsistentes en el flujo de control de
accesos (autorizaciones, ingresos, salidas, rechazos, ocupaciones), para documentar qué está
diseñado de forma segura y qué huecos de diseño tienen el sistema.

## Qué se prueba

**A. Reglas de negocio del dominio de acceso**

1. Ingreso sin autorización: la política del conjunto decide (`visits.without_authorization`,
   `AccessTests.Sin_autorizacion_previa_la_politica_del_conjunto_decide`).
2. Autorización revocada, vencida o para otro día → rechazo (`AuthorizationRules`, prueba
   `Una_autorizacion_revocada_o_vencida_no_permite_el_ingreso`).
3. Acceso excepcional exige permiso `access.exception` y motivo, y deja responsable
   (`El_acceso_excepcional_exige_permiso_y_motivo_y_deja_al_responsable`).
4. Los eventos de acceso no se editan ni se borran: se anulan con motivo
   (`AccessController.Void`, permiso `access.void`; pruebas `Los_eventos_no_se_modifican…`,
   `Las_visitas_no_se_pueden_borrar`).
5. Al terminar una ocupación se revocan las autorizaciones de quien se va
   (`AuthorizationService` como `IOccupancyEndHandler`).
6. Salida registrada una sola vez y anulación de una salida devuelve la visita a "presente"
   (`Anular_una_salida_devuelve_la_visita_a_presente…`).

**B. Reglas de privilegio**

1. Nadie concede ni se asigna permisos que no tiene
   (`RolesService.ValidatePermissions`, `UsersService.ResolveRolesAsync`; prueba
   `Nadie_concede_ni_asigna_permisos_que_no_tiene`).
2. El conjunto no se queda sin administrador de roles ni sin administrador de usuarios
   (`El_conjunto_no_puede_quedarse_sin_quien_administre_roles`,
   `Nadie_se_desactiva_a_si_mismo…`).
3. Roles de sistema no se borran; un rol inactivo deja de dar permisos
   (`Los_roles_de_sistema_no_se_borran…`).
4. Auditor solo consulta (`El_auditor_consulta_pero_no_modifica`).
5. El superadministrador no puede ser suplantado con `X-Complex-Id` por usuarios comunes
   (`El_encabezado_de_conjunto_se_ignora_si_no_es_superadministrador`).

**C. Reglas de diseño declaradas pero no aplicadas**

1. Política `session.max_hours` ("Duración máxima de sesión (horas)"): existe en
   `ComplexPolicies.cs:48,111` pero **no se usa en ningún punto del código** → la duración real
   es fija en 240 minutos (`Jwt:ExpireMinutes`).
2. Renovación de sesión con token corto y refresh rotativo: pendiente declarado en HU-113.
3. Rate limit por IP sin `UseForwardedHeaders`: detrás de proxy la política no separa
   atacantes (diseño del control, cruza con A05/A07).
4. Equipo compartido de portería: no existe diseño de sesión por estación ni cierre automático
   por inactividad.
5. `logout` autenticado sin límite de peticiones: un script autenticado puede provocar la
   invalidación masiva de su propia sesión de forma repetida (baja impacto, pero no está
   acotado).

## Fundamento en el API

### Lo que sí cumple con la épica

- Autorización por permiso `modulo.accion` en el servidor, no por nombre de rol
  (`Permissions.cs` con 26 claves, `HasPermissionAttribute.cs`).
- Diseño anti-escalada: catálogo cerrado de permisos, validación de otorgamiento, protección del
  último administrador, roles de sistema con `IsSystem`.
- Auditoría de solo lectura: `AuditController` solo expone `[HttpGet]`; no hay endpoints de
  escritura o borrado de bitácora.
- Inmutabilidad de eventos de acceso: se registran y se anulan, no se editan; una operación
  fallida no deja entrada (la auditoría se escribe en la misma transacción).
- Estados calculados (unidad ocupada/vigente) en lugar de campos manuales.
- 87 pruebas automatizadas, de las cuales las de `AccessTests`, `RoleTests`, `PolicyTests` y
  `AuditTests` cubren directamente estas reglas.

### Lo que no cumple o falta verificar

- `session.max_hours` definida y sin implementar: el control de duración de sesión es una
  regla de diseño anunciada que no existe en ejecución.
- Sin diseño de rotación/renovación de tokens (HU-113 pendiente) y con sesión fija de 4 horas.
- Rate limiting diseñado por IP sin cabeceras de proxy confiables.
- Falta diseño de operación en equipo compartido (portería) y de cierre por inactividad.
- No está definido el flujo de "denegación de autorización" con motivo y responsable más allá
  del rechazo de portería (`reject`): verificar si la épica espera también rechazo desde el
  portal.
- Suplantación por enlace de invitación/recuperación: el enlace va en el query string y es
  compartible (cruza con A02-4).

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A04-1 | Política `session.max_hours` sin implementar | `ComplexPolicies.cs:48,111` | El administrador cree que controla la duración de sesión y no la controla | Medio | Aplicarla en `CurrentUserMiddleware`/emisión del JWT o retirarla del catálogo | P1 |
| A04-2 | Sin renovación de token ni sesión por estación | HU-113 pendiente | Sesiones largas compartidas y caídas abruptas | Medio | Definir refresh rotativo y cierre por inactividad para portería | P2 |
| A04-3 | Rate limit sin `ForwardedHeaders` | `Program.cs:92-94` | El control no cumple su propósito tras un proxy | Medio | Configurar proxy de confianza y partición por usuario/IP real | P1 |
| A04-4 | Enlace de cuenta en URL compartible | `AccountSecurity.cs:134` | Suplantación si el enlace se filtra | Medio | Vía de canje corta y de un solo uso (cruza A02-4) | P1 |
| A04-5 | Sin política de inactividad ni límites por cuenta en autenticación | `AuthService.cs`, `RateLimits.cs` | Fuerza bruta distribuida sin tasa por cuenta | Bajo | Sumar límite por usuario además del por IP | P2 |

## Criterios de aceptación

- [ ] Documentadas las pruebas de reglas de negocio de acceso y de privilegio, con el escenario,
      la entrada y el resultado (criterios 1 y 3).
- [ ] Registradas las reglas de diseño anunciadas que no se aplican, con componente, impacto,
      riesgo, recomendación y prioridad (criterios 2 y 8).
- [ ] Dejado por escrito qué suplantaciones quedan cubiertas (roles, conjunto, unidad, autorización)
      y cuáles quedan abiertas.
- [ ] Ninguna regla crítica de esta categoría depende solo de la interfaz: verificada en el API.

## Verificación

Las reglas ya tienen pruebas de integración en `AccessTests.cs`, `RoleTests.cs` y
`PolicyTests.cs`: se re-ejecutan y se adjunta la salida. Las reglas no aplicadas se demuestran
con una prueba negativa (p. ej.: cambiar `session.max_hours` a 1 y comprobar que la sesión sigue
viva a las 2 horas) guardada en `documentation/owasp-top10/evidencias/A04-<n>.txt`.
