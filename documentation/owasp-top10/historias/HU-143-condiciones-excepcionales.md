# HU-143 · Manejo inadecuado de condiciones excepcionales (Mishandling of Exceptional Conditions)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Mishandling of Exceptional Conditions
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 6, 8

## Historia

Como evaluador de seguridad, quiero probar cómo responde el API ante errores, excepciones y
situaciones inesperadas, para comprobar que no se filtra información interna ni se dejan estados
inseguros, y para cerrar el criterio 6 de la épica sobre gestión de errores.

## Qué se prueba

**A. Errores controlados**

1. No encontrado → 404 con mensaje de negocio: `GET api/units/{id}` con GUID inexistente
   (`La unidad no existe.`, `UnitsService.cs:241`), `GET api/visits/{id}`
   (`La visita no existe.`, `AccessService.cs:566`).
2. Conflicto → 409: correo duplicado (`Correo_duplicado_responde_409_y_datos_invalidos_400`),
   documento duplicado, código de unidad repetido.
3. Regla de negocio → 400: `POST api/access/check-in` sin residencia/unidad válida
   (`Residencia_inexistente_responde_400_y_no_500`, `Sin_residencia_responde_400_con_el_error_del_campo`),
   políticas inválidas, rol sin permisos.
4. Prohibido → 403 con el mensaje de la regla (p. ej. `No puede conceder permisos que usted no
   tiene…`) y 401 de sesión inválida (`La sesión ya no es válida. Inicie sesión de nuevo.`).
5. Validación de modelo → 400 con `Hay datos que no son válidos.` y errores por campo
   (`Program.cs:77-85`).

**B. Excepciones no controladas**

1. Provocar un 500 (p. ej. pausar la base de datos o enviar un GUID malformado en una ruta donde
   se espere `Guid`) y verificar que la respuesta es exactamente
   `{"ok":false,"message":"Ocurrió un error inesperado."}` **sin** stack trace, sin tipo de
   excepción, sin mensaje de Npgsql (`GlobalExceptionHandler.cs:21`).
2. Verificar que el detalle completo solo aparece en el log del servidor
   (`logger.LogError(exception, …)`, `GlobalExceptionHandler.cs:26`).
3. Verificar que `Include Error Detail=true` de la cadena de conexión de desarrollo no se
   filtra a las respuestas.

**C. Estados inseguros y respuestas inconsistentes**

1. Operación que falla a mitad: debe ser atómica y no dejar bitácora
   (`Una_operacion_que_falla_no_deja_entrada`).
2. Revisar si los mensajes de negocio devueltos en 400/403 filtran detalles internos: el
   manejador copia `e.Message` **y** lo repite en `errors[]`
   (`GlobalExceptionHandler.cs:30`); probar con mensajes que incluyan claves de permiso o
   identificadores internos.
3. Verificar que `ResponseModel` no es ProblemDetails aunque `AddProblemDetails()` está
   registrado sin usarse (`Program.cs:101`) → comprobar si algún consumidor espera RFC 7807.
4. Entradas malformadas: cuerpo vacío en `PUT api/policies`, JSON inválido, método no permitido
   (`DELETE api/audit` → 405), ruta inexistente (`GET api/xyz` → 404 sin detalle), encabezado
   `Authorization: Bearer <basura>` → 401.
5. Tamaño de cuerpo excesivo y timeouts: verificar que no queda un 500 con stack ni una
   conexión colgada (no hay límite de cuerpo configurado, ver A05-6).
6. Concurrencia: `POST api/access/visits/{id}/check-out` dos veces → la segunda debe responder
   regla de negocio, no 500 (prueba `La_salida_se_registra_una_sola_vez…` / equivalente de
   `AccessTests`).

## Fundamento en el API

### Lo que sí cumple con la épica

- Manejador global único (`GlobalExceptionHandler : IExceptionHandler`) registrado con
  `UseExceptionHandler()`; mapea `NotFoundException→404`, `ConflictException→409`,
  `BusinessRuleException→400`, `ForbiddenException→403` y todo lo demás a **500 genérico**
  (`Infrastructure/GlobalExceptionHandler.cs:15-22`).
- Los controladores no tienen `try/catch` (regla del `backend/README.md`): la decisión de error
  es central.
- El stack trace y el mensaje interno solo van al log; el cliente nunca los recibe.
- Respuesta 400 de validación uniforme y errores por campo (`Program.cs:77-85`).
- Falta de configuración crítica detiene el arranque con mensaje accionable en lugar de operar
  a medias (`Program.cs:25-36`).
- Pruebas existentes de códigos: 400 sin residencia, 409 duplicado, 404/405 en bitácora,
  403 por permiso, 401 sin token/sesión inválida.

### Lo que no cumple o falta verificar

- El mensaje de negocio se devuelve tal cual **y** se duplica en `errors[]`, lo que puede exponer
  detalles internos (claves de permiso, identificadores) en 400/403/404/409.
- `AddProblemDetails()` está registrado pero no se usa: las respuestas no siguen RFC 7807 y el
  manejo queda mezclado entre `ResponseModel` y ProblemDetails.
- No hay manejo explícito de cancelación/timeout ni política de comandos EF: una BD caída produce
  500 genérico (correcto) pero sin distinguir el caso ni reintentar.
- No hay límite de tamaño de cuerpo: verificar el comportamiento ante payloads gigantes.
- No se verifica en pruebas que un 500 sea genérico (no existe un caso que provoque una
  excepción no controlada en la suite).

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A10-1 | Mensajes de negocio reflejados en `errors[]` sin filtrar | `GlobalExceptionHandler.cs:30` | Fuga de detalles internos al cliente | Medio | Devolver solo `message` y un `errors[]` de validación; sanitizar mensajes 403/409 | P1 |
| A10-2 | `AddProblemDetails()` sin usar y respuestas fuera de RFC 7807 | `Program.cs:101` | Contrato de error inconsistente para consumidores | Bajo | Elegir `ProblemDetails` o `ResponseModel` y documentarlo (limpiar el registro muerto) | P2 |
| A10-3 | Sin prueba de que el 500 es genérico | `AccessControl.Tests` | Una regresión podría exponer stack traces sin detectarse | Medio | Test que fuerce una excepción no controlada y afirme el cuerpo exacto | P1 |
| A10-4 | Sin límite de cuerpo ni manejo de timeout | `Program.cs` | 500 no controlados o DoS con payloads grandes | Medio | Fijar límite de cuerpo y política de tiempo/agotamiento de BD | P2 |
| A10-5 | 404 de ruta inexistente sin cuerpo controlado | pipeline | Posible fuga de detalle del framework | Bajo | Verificar `UseStatusCodePages`/middleware 404 y respuesta uniforme | P3 |

## Criterios de aceptación

- [ ] Documentadas las respuestas ante cada clase de error (404, 409, 400, 403, 401, 405, 429,
      500) con código y cuerpo exactos (criterios 1 y 6).
- [ ] Probado que un error no controlado devuelve mensaje genérico y que el detalle solo vive en
      el log (criterio 6).
- [ ] Registrados los hallazgos de fuga de información con componente, impacto, riesgo,
      recomendación y prioridad (criterios 2 y 8).
- [ ] Verificada la atomicidad de las operaciones ante fallo (no quedan estados a medias ni
      entradas de bitácora huérfanas).

## Verificación

Evidencia: capturas de cuerpo y código de cada respuesta en
`documentation/owasp-top10/evidencias/A10-<n>-(peticion|respuesta).txt`, más la salida del log
del servidor para el caso 500 (mostrando que el detalle no viaja en la respuesta). Ejecutar en
entorno de pruebas con datos ficticios.
