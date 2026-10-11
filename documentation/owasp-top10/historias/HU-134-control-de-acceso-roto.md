# HU-134 · Control de acceso roto (Broken Access Control)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Broken Access Control (A01)
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 3, 4, 8

## Historia

Como evaluador de seguridad, quiero probar en el API —no solo en la interfaz— que cada endpoint
niega lo que el rol del usuario no puede hacer y que la manipulación de identificadores (unidad,
conjunto, visita, ocupación, autorización) no abre información ajena, para cerrar los criterios 3
y 4 de la épica con evidencia reproducible.

## Qué se prueba

**A. Superficie y autenticación**

1. De las 18 clases de controlador del API, solo `AuthController` declara `[AllowAnonymous]` en
   cuatro rutas (`login`, `accept-invitation`, `forgot-password`, `reset-password`).
   Probar que cualquier otra ruta sin token responde 401 (prueba existente:
   `Sin_token_no_se_accede_a_un_endpoint_protegido`, `AuthTests.cs`).
2. Revisar que **todo** endpoint tenga `[HasPermission]`, `[Authorize]` o `[Authorize(Policy)]`.
   Rutas que hoy solo exigen sesión (sin permiso) y deben quedar documentadas como decisión:
   `GET api/menu` (`MenuController.cs:12`) y `GET api/catalogs/{catalog}` (`UnitsController.cs:161`).

**B. Matriz rol × operación** (admin, guard, owner, resident, auditor, superadmin, sin rol)

| Petición | Permiso exigido | Caso a demostrar |
| --- | --- | --- |
| `GET /api/users` | `users.read` | portería, propietario y residente → 403 |
| `POST /api/units` | `units.write` | portería y auditor → 403 |
| `PUT /api/policies` | `settings.manage` | auditor → 403 (prueba: `El_auditor_consulta_las_politicas_pero_no_las_cambia…`) |
| `GET /api/complexes` | política `SuperAdminOnly` | administrador de conjunto → 403 (prueba: `Solo_el_superadministrador_gestiona_conjuntos`) |
| `POST /api/access/check-in` | `access.register` | propietario → 403 |
| `DELETE /api/audit` | — | 405: la bitácora no tiene operaciones de escritura (prueba: `La_bitacora_es_de_solo_consulta…`) |
| `GET /api/visits` | `visits.read` \| `visits.read_own` | propietario solo ve las visitas de sus unidades (prueba: `El_propietario_solo_ve_las_visitas_de_sus_residencias`) |

**C. Manipulación de identificadores (IDOR) y de ámbito**

1. `GET /api/units/{id}` con una unidad de **otro residente del mismo conjunto**, sesionando con
   un usuario que solo tiene `units.read_own`: `UnitsService.GetAsync(id)` no recibe ámbito
   (`UnitsService.cs:25` y `:238-241`) y el controlador solo exige
   `[HasPermission(UnitsRead, UnitsReadOwn)]` (`UnitsController.cs:14`). **Punto crítico a
   confirmar**: se espera 404/403 y hoy todo indica que responde 200 con la ficha.
2. `GET /api/units/{id}` con unidad de **otro conjunto** → 404 (filtro global de EF por
   `ComplexId`, `ApplicationDbContext.ApplyComplexScope`).
3. `GET /api/units/{id}/summary` con el mismo escenario: `GetSummaryAsync` tampoco recibe ámbito
   (`UnitsService.cs:361-364`); `includeHistory` sí queda detrás de `people.read`
   (`UnitsController.cs:44`).
4. `GET /api/visits/{id}` con visita de unidad ajena siendo `visits.read_own` → 404
   (`AccessService.cs:556-566` aplica `unitScope`).
5. `POST /api/my/occupants/{occupancyId}/end` y `PUT /api/my/occupants/{occupancyId}/contact`
   con una ocupación que no pertenezca a las unidades del usuario → debe fallar
   (`MyController`, `PeopleControllers.cs:111-153`).
6. `POST /api/authorizations/{id}/revoke` (rol administrador) con id de otro conjunto → 404 por
   filtro global; `POST /api/my/authorizations/{id}/revoke` con id de unidad ajena → 404/403
   (`AuthorizationService.RevokeAsync` acepta `unitScope`, `AuthorizationService.cs:27`).
7. Encabezado `X-Complex-Id` apuntando a otro conjunto con un usuario que **no** es
   superadministrador → se ignora (`CurrentUserMiddleware.cs:46-57`; prueba existente:
   `El_encabezado_de_conjunto_se_ignora_si_no_es_superadministrador`).
8. `GET /api/units/{id}` con el encabezado `X-Complex-Id` de otro conjunto **siendo
   superadministrador** → el superadministrador cambia de ámbito; verificar que la fila devuelta
   pertenece al conjunto elegido.

## Fundamento en el API

### Lo que sí cumple con la épica

- **Criterio 3 (solo operaciones autorizadas):** `[HasPermission]` deriva de `AuthorizeAttribute`
  y se aplica en el servidor en cada petición (`Infrastructure/HasPermissionAttribute.cs`,
  handler registrado en `Program.cs:70`); los permisos se resuelven en
  `CurrentUserMiddleware.cs:60` y nunca se leen del token.
- **Criterio 4 (separación por conjunto):** filtro global `HasQueryFilter` por `ComplexId` en
  `ApplicationDbContext` (entidades `IComplexScoped`, `User`, `UserRole`, `RolePermission`,
  `AuditLog`); escritura con `ComplexId` ajeno rechazada en `SaveChangesAsync`; 11 pruebas de
  `ComplexTests.cs` y `PermissionTests.cs` cubren aislamiento, encabezado y roles cruzados.
- **Criterio 4 (separación por unidad):** ámbito por vínculo vigente (`IUnitScope` /
  `PortalService.cs`) aplicado en `VisitsController.UnitScopeAsync()`, `DashboardController`,
  `MyController` y `MyAuthorizationsController`.
- **Criterio 3 (defensa en profundidad):** sin `FallbackPolicy` no hay política por defecto, pero
  hoy los 18 controladores cargan atributo (verificado por lectura de todos los controladores).

### Lo que no cumple o falta verificar

- `GET api/units/{id}` y `GET api/units/{id}/summary` no aplican el ámbito `units.read_own`:
  dentro del conjunto, cualquier usuario con ese permiso puede leer la ficha de una unidad ajena
  manipulando el GUID (arriba, prueba C.1/C.3).
- No existe política de respaldo (`FallbackPolicy`): un endpoint nuevo sin atributo quedaría
  anónimo (`Program.cs:65-69` solo registra `SuperAdminOnly`).
- No hay pruebas automatizadas de IDOR con identificador manipulado para unidades, ocupaciones y
  autorizaciones (las existentes cubren visitas y conjuntos).
- Los permisos se cachean 5 minutos (`PermissionService.CacheDuration`,
  `PermissionService.cs:40`): falta verificar que toda baja de rol/usuario dispare
  `Invalidate()` y que la ventana no deje accesos recientes.

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A01-1 | `GET api/units/{id}` y `/summary` sin ámbito `units.read_own` | `UnitsService.cs:238,361` · `UnitsController.cs:14` | Lectura de fichas de unidades ajenas dentro del conjunto (filtración de información de residentes) | Alto | Recibir el ámbito (como `VisitsController`) y devolver 404 cuando la unidad no esté en él | P1 |
| A01-2 | Sin `FallbackPolicy` de autorización | `Program.cs:65-69` | Un endpoint futuro sin atributo queda anónimo | Medio | `options.FallbackPolicy = requireAuthenticatedUser + permiso explícito` y prueba que falle cualquier ruta nueva sin atributo | P1 |
| A01-3 | IDOR sin cobertura de pruebas (ocupaciones, autorizaciones, unidades) | `AccessControl.Tests` | Regresiones silenciosas de control de acceso | Medio | Agregar pruebas por escenario de la columna C antes de cerrar la historia | P1 |
| A01-4 | Ventana de 5 min en la caché de permisos | `PermissionService.cs:40,99` | Permiso revocado sigue operativo hasta 5 min | Bajo | Confirmar invalidación en todas las mutaciones; si falla, reducir TTL o invalidar por usuario | P2 |

## Criterios de aceptación

- [ ] Documentadas las pruebas de esta categoría con método, ruta, encabezados, código y cuerpo de
      respuesta (criterio 1).
- [ ] Probado el acceso sin token y la matriz rol × operación sobre una muestra representativa de
      los 18 controladores; resultado esperado 401/403 en todos los casos no autorizados (criterio 3).
- [ ] Probados los 8 escenarios de manipulación de identificadores de la sección C, con resultado
      esperado registrado (criterio 4).
- [ ] Cada hallazgo de la tabla tiene evidencia reproducible, componente, impacto, riesgo,
      recomendación y prioridad (criterios 2 y 8).
- [ ] Se deja escrito qué endpoints exigen solo sesión (`api/menu`, `api/catalogs/{catalog}`) y por
      qué es aceptable.

## Verificación

Ejecutar en entorno de pruebas con datos ficticios, con al menos dos conjuntos y dos unidades por
conjunto. Cada prueba se guarda como par de archivos
`documentation/owasp-top10/evidencias/A01-<n>-peticion.txt` y `A01-<n>-respuesta.txt`
(método, ruta, encabezados, cuerpo y código HTTP). Repetir el bloque C con un usuario dueño de una
unidad y ajeno a la otra; el resultado esperado se marca antes de ejecutar, y lo observado se
contrasta con él.
