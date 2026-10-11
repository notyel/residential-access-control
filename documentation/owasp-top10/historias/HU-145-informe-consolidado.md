# HU-145 · Informe consolidado y matriz de vulnerabilidades

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Entregables de la evaluación
**Estado:** Pendiente · **Depende de:** HU-134, HU-135, HU-136, HU-137, HU-138, HU-139, HU-140,
HU-141, HU-142, HU-143, HU-144 · **Criterios de la épica:** 1, 10 (y cierre de 2, 8, 9)

## Historia

Como responsable de la evaluación, quiero consolidar cobertura, hallazgos, riesgos pendientes y
recomendaciones en un informe único con su matriz de vulnerabilidades y el plan de remediación
priorizado, para entregar la evaluación OWASP Top 10 completa y trazable.

## Qué se entrega

1. **Informe de evaluación** con, por cada una de las diez categorías: alcance probado, endpoints y
   roles cubiertos, pruebas ejecutadas, resultado (cumple / parcial / no cumple) y hallazgos.
2. **Matriz de vulnerabilidades** con columnas: id (`A01-1`…), categoría, componente afectado
   (archivo/ruta del endpoint), evidencia reproducible (referencia al archivo en
   `evidencias/`), impacto, riesgo (crítico/alto/medio/bajo), recomendación, prioridad (P0–P3),
   estado (abierto / corregido / aceptado) y resultado de la regresión de HU-144.
3. **Registro de pruebas ejecutadas** (HU-134 a HU-143 + suite de regresión) con fecha, entorno y
   resultado.
4. **Plan de remediación priorizado** derivado de la matriz (P0 → P3), con responsable y
   verificación prevista.
5. **Evidencias de validación posterior a las correcciones** (salida de HU-144).

## Fundamento en el API (qué sustenta cada bloque del informe)

| Bloque del informe | Historia que lo alimenta | Evidencia base ya existente |
| --- | --- | --- |
| Control de acceso y separación por conjunto/unidad | HU-134 | `PermissionTests.cs`, `ComplexTests.cs`, `RoleTests.cs` |
| Configuración, cabeceras y secretos | HU-135 | `Program.cs`, `appsettings.json`, `launchSettings.json`, `DataSeeder.cs` |
| Dependencias y cadena de suministro | HU-136 | `.csproj`, `package.json`, `.github/workflows/ci.yml` |
| Credenciales, tokens y PII | HU-137 | `AccountSecurity.cs`, `auth.service.ts`, `User.cs` |
| Validación de entradas e inyección | HU-138 | servicios de búsqueda con `EF.Functions.ILike`, DTO con `DataAnnotations` |
| Reglas de negocio y diseño | HU-139 | `AccessTests.cs`, `RoleTests.cs`, `PolicyTests.cs`, `ComplexPolicies.cs` |
| Autenticación y sesiones | HU-140 | `AuthTests.cs`, `UserTests.cs`, `AuthService.cs`, `RateLimits.cs` |
| Integridad | HU-141 | `ApplicationDbContext.Audit.cs`, `AuditTests.cs`, migraciones |
| Registro y alertas | HU-142 | `AuditTests.cs`, `IAuditService`, `LogEmailSender` |
| Manejo de errores | HU-143 | `GlobalExceptionHandler.cs`, pruebas 400/409/403/401 |
| Regresión | HU-144 | `AccessControl.Tests` + CI |

## Criterios de aceptación

- [ ] El informe cubre las diez categorías con su prueba, resultado y hallazgos (criterio 1).
- [ ] La matriz incluye, para cada vulnerabilidad: evidencia reproducible, componente afectado,
      impacto, nivel de riesgo, recomendación y prioridad (criterios 2 y 8).
- [ ] Queda registrado el cumplimiento verificado de los criterios 3 a 7 con su prueba asociada
      (usuarios solo operan lo autorizado; no se alcanza información de otro conjunto o unidad
      manipulando identificadores; protección de credenciales/tokens/PII; autenticación,
      autorización, validación y errores; registro de eventos sin secretos).
- [ ] Incluye el estado de la regresión por hallazgo (criterio 9) y la lista de riesgos pendientes
      con responsable.
- [ ] Se lista la cobertura real: endpoints probados sobre el total de las 18 clases de
      controlador, y roles usados.

## Verificación

Revisión cruzada: cada fila de la matriz debe apuntar a un archivo de `evidencias/` y a una historia
de HU-134 a HU-144; cada categoría del informe debe tener al menos un resultado de prueba. El
informe se considera entregable cuando no quedan hallazgos P0 sin estado ni sin decisión de
riesgo aceptado por escrito.
