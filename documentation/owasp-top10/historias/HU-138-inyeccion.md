# HU-138 · Inyección (Injection)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Injection (A03 OWASP)
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 6, 8

## Historia

Como evaluador de seguridad, quiero probar que ninguna entrada del usuario (búsqueda, filtro,
identificador, cuerpo JSON, catálogo, política) pueda alterar la ejecución de consultas u
operaciones, para cerrar el criterio 6 de la épica sobre validación de entradas.

## Qué se prueba

**A. Inyección SQL**

1. Endpoints de búsqueda con patrón: `GET api/units?search=`, `GET api/persons/search?search=`,
   `GET api/users?search=`, `GET api/access/lookup?term=`, `GET api/visits?search=`,
   `GET api/authorizations?search=`. Enviar payloads clásicos y variantes:
   - `' OR '1'='1`
   - `'; DROP TABLE "Users"; --`
   - `%'; DELETE FROM "Visits"; --`
   - `%" UNION SELECT "Email","PasswordHash" FROM "Users" --`
   - `\'; SELECT pg_sleep(5); --`
   Resultado esperado: 400 o 200 con 0 resultados; nunca 500 ni error de base de datos en la
   respuesta; verificar además que el tiempo de respuesta no indique consulta añadida.
2. Confirmar por inspección que no hay SQL crudo: grep de `FromSqlRaw`, `ExecuteSqlRaw`,
   `SqlQuery`, `FromSqlInterpolated` en `backend/` → sin coincidencias; todo pasa por LINQ/EF
   Core (`Persistence/Generics/Repository.cs`).
3. Encabezado `Host`, `X-Complex-Id` y ruta con caracteres especiales (`/api/units/../../etc`)
   para descartar manipulación de ruta.

**B. Comodines de LIKE sin escapar**

Los patrones se construyen como `$"%{filter.Search.Trim()}%"` y se pasan a
`EF.Functions.ILike` en:

- `UnitsService.cs:189-192`
- `PeopleService.cs:73-74`
- `UsersService.cs:53-54`
- `AccessService.cs:88-109` y `:521-522`
- `AuthorizationService.cs:195-197`

Pruebas: `search=%` debe traer todo el conjunto (hoy sí: el `%` del usuario actúa como curinga);
`search=_` debe coincidir con cualquier carácter. El resultado esperado de la remediación es que
`%` y `_` escritos por el usuario se busquen literalmente. Registrar el número de filas y el
tiempo en cada caso.

**C. Validación de entradas y operaciones**

1. Cuerpos con tipos incorrectos y campos faltantes en `POST api/users`, `POST api/units`,
   `POST api/access/check-in`, `PUT api/policies` → 400 con errores por campo
   (`InvalidModelStateResponseFactory`, `Program.cs:77-85`; prueba existente:
   `Sin_residencia_responde_400_con_el_error_del_campo`).
2. `PUT api/policies` con claves inventadas o valores fuera de catálogo → 400
   (`PolicyService.UpdateAsync`, prueba `Valores_y_claves_invalidos_se_rechazan`).
3. `PUT api/catalogs/{catalog}` con catálogo desconocido o códigos duplicados → 400/409
   (`CatalogService`).
4. `POST api/roles` con permisos fuera del catálogo (`Permissions.IsValid`) → 400.
5. Cadenas largas y con HTML/JS en nombres, correo y documento → verificar que los DTO tienen
   `[MaxLength]`/`[EmailAddress]` y que la respuesta no refleja el script sin codificar
   (verificar también el `Content-Type: application/json` de las respuestas).
6. Cuerpo no JSON o con formato binario en `POST api/auth/login` → respuesta controlada.

## Fundamento en el API

### Lo que sí cumple con la épica

- Sin SQL concatenado: grep sin coincidencias de `FromSqlRaw`/`ExecuteSqlRaw`/`SqlQuery`; todas
  las consultas son LINQ parametrizado por EF Core, incluido `EF.Functions.ILike`.
- Validación declarativa en DTO (`DataAnnotations`) + respuesta 400 uniforme con errores por
  campo (`Program.cs:77-85`); criterio 6 cubierto para obligatorios, tipos y longitudes.
- Listas de catálogo cerradas: políticas (`ComplexPolicies.Normalize`), catálogos
  (`CatalogService`), permisos (`Permissions.IsValid`), roles de sistema.
- Pruebas existentes de entradas inválidas: `UserTests`, `VisitTests`/`AccessTests`,
  `PolicyTests`, `UnitTests`.

### Lo que no cumple o falta verificar

- `%` y `_` del usuario sin escapar en seis servicios de búsqueda: manipulación del patrón de
  búsqueda y barrido de tablas (no es inyección SQL, pero es entrada manipulada que altera el
  resultado de la consulta).
- `AllowedHosts: "*"` amplía el riesgo del encabezado Host (cruza con HU-135).
- Los campos de búsqueda (`term`, `search`, `placa`) no tienen `[MaxLength]` en todos los DTO:
  verificar cuáles aceptan cadenas de miles de caracteres.
- No hay pruebas automatizadas de fuzzing/inyección en la suite (87 `[Fact]`, ninguno con
  payloads de inyección).
- Verificar la respuesta ante SQL inválido provocado por ordenamiento/filtros si algún endpoint
  acepta `orderBy` libre (revisar DTO de filtros: `VisitFilterDto`, `AuthorizationFilterDto`).

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A03-1 | Curingas `%`/`_` sin escapar en búsquedas | `UnitsService.cs:189`, `PeopleService.cs:73`, `UsersService.cs:53`, `AccessService.cs:88,521`, `AuthorizationService.cs:195` | Manipulación de resultados y barridos lentos por tablas grandes | Medio | Escapar `%`, `_` y `\` del usuario antes de `ILike` (o usar `EF.Functions.Like` con escape) + prueba con `search=%` | P1 |
| A03-2 | Sin pruebas de inyección en la suite | `AccessControl.Tests` | Regresiones no detectadas | Medio | Agregar la batería de la sección A como pruebas automatizadas | P1 |
| A03-3 | `AllowedHosts: "*"` | `appsettings.json:26` | Host header injection | Medio | Fijar hostes (cruza A05-4) | P2 |
| A03-4 | Sin límite de longitud en campos de búsqueda | DTOs de filtro | Entradas excesivas que degradan la consulta | Bajo | `[MaxLength(100)]` en los DTO de filtro | P2 |

## Criterios de aceptación

- [ ] Documentada la batería de payloads de inyección con endpoint, payload, código y cuerpo de
      respuesta (criterios 1 y 6).
- [ ] Verificado por inspección que no existe SQL crudo y documentado ese resultado (criterio 6).
- [ ] Medido el efecto de las curingas de LIKE con evidencia antes/después (criterio 6).
- [ ] Cada hallazgo con componente, impacto, riesgo, recomendación y prioridad (criterios 2 y 8).

## Verificación

Las pruebas se ejecutan contra el entorno de pruebas con datos ficticios y se guardan como
`documentation/owasp-top10/evidencias/A03-<n>-(peticion|respuesta).txt`. Para descartar tiempo de
ejecución de sentencias, comparar el tiempo de respuesta de un payload neutro frente al payload
malicioso en el mismo endpoint.
