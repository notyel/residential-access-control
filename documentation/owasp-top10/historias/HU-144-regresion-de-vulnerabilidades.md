# HU-144 · Pruebas de regresión de las vulnerabilidades corregidas

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Cierre de correcciones
**Estado:** Pendiente · **Depende de:** HU-134, HU-135, HU-136, HU-137, HU-138, HU-139, HU-140,
HU-141, HU-142, HU-143 · **Criterios de la épica:** 9

## Historia

Como responsable de la evaluación, quiero ejecutar de nuevo las pruebas que detectaron cada
vulnerabilidad corregida y documentar el resultado, para demostrar que la corrección cerró el
hallazgo y no rompió el comportamiento anterior.

## Qué se prueba

1. **Trazabilidad hallazgo → prueba → corrección.** Para cada hallazgo de las diez categorías con
   prioridad P0/P1/P2, verificar que existe al menos una prueba automatizada o un caso manual
   documentado que:
   - fallaba antes de la corrección (resultado registrado),
   - pasa después de la corrección (resultado registrado),
   - cubre el caso vecino para no regresionar (p. ej.: corregir `units/{id}` exige probar
     también `/summary` y `activate/deactivate`).
2. **Ejecución de la suite completa.** `dotnet build`, `dotnet test` (hoy 87 pruebas: 11 unitarias
   en `DateTimeServiceTests` y `AuthorizationRulesTests`, 76 de integración en `AccessTests`,
   `AuditTests`, `AuthTests`, `ComplexTests`, `PermissionTests`, `PolicyTests`, `RoleTests`,
   `UnitTests`, `UserTests`) y `ng build` + `npm run test:ci`, sin errores ni advertencias nuevas.
3. **CI verde.** Confirmar que `.github/workflows/ci.yml` pasa en el PR de la corrección.
4. **Pruebas de los controles que hoy no se ejercitan.** Ejecutar aparte, fuera de la fixture que
   los desactiva, los controles de HU-135 (cabeceras), HU-140 (rate limit,
   `ApiFixture.cs:61` los sube a 1000000) y HU-142 (limpieza del log), para que queden cubiertos.
5. **Reversión de hallazgos.** Verificar que ningún hallazgo corregido reaparece: volver a
   ejecutar la evidencia original (mismos payloads) y comparar código de respuesta esperado vs
   obtenido.

## Fundamento en el API

### Situación actual

- La base de pruebas ya existe y corre en CI: `AccessControl.Tests` con fixture de PostgreSQL
  embebido (`ApiFixture.cs`), reemplazo de `IEmailSender` por `CapturingEmailSender` y datos de
  prueba en `TestData.cs`.
- Ya hay cobertura de regresión para varios controles que la épica exige: aislamiento por
  conjunto, permisos por rol, bloqueo por intentos, cierre de sesión server-side, bitácora sin
  secretos y de solo lectura, autorizaciones y eventos de acceso.
- **No están cubiertos hoy:** rate limiting real, cabeceras de seguridad/HSTS, IDOR por
  identificador manipulado en unidades/ocupaciones/autorizaciones, curingas de LIKE, contenido
  del log de aplicación y respuesta 500 genérica.

### Lo que falta para cerrar el criterio 9

- Convertir en pruebas los casos manuales documentados en HU-134 a HU-143.
- Guardar el resultado de cada corrida (antes/después) como evidencia.
- Corregir los hallazgos (o aceptarlos formalmente con riesgo) antes de reclamar regresión verde.

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| R-1 | Rate limit y cabeceras sin prueba automatizable en la fixture actual | `ApiFixture.cs:61`, pipeline | Los controles pueden romperse sin que CI lo note | Medio | Políticas de límite configurables por prueba y aserción de cabeceras en una prueba dedicada | P1 |
| R-2 | IDOR y curingas sin pruebas | `AccessControl.Tests` | Regresión de los hallazgos principales de A01/A03 | Alto | Agregar las pruebas listadas en HU-134 y HU-138 | P1 |
| R-3 | Resultados de regresión no documentados | documentación | No se puede demostrar el criterio 9 | Medio | Plantilla `evidencias/regresion-<hallazgo>.txt` con antes/después | P2 |

## Criterios de aceptación

- [ ] Cada hallazgo P0/P1/P2 tiene una prueba que lo reproduce y una que valida la corrección
      (criterio 9).
- [ ] La suite completa y el CI quedan verdes en el PR de corrección, con la salida adjunta
      (criterio 9).
- [ ] Los controles antes no ejercitados (rate limit, cabeceras, log, IDOR) quedan cubiertos o
      quedan como riesgo aceptado por escrito (criterio 9).
- [ ] Documentados los resultados antes/después por hallazgo (criterio 9).

## Verificación

Evidencia: salida de `dotnet test` y de `npm run test:ci` por corrida, guardada como
`documentation/owasp-top10/evidencias/regresion-<HU/categoría>-<fecha>.txt`, más la tabla
hallazgo → prueba → resultado. Si un hallazgo no se corrige, se marca como "riesgo pendiente" y
pasa al informe de HU-145.
