# HU-141 · Fallas de integridad de software y datos (Software or Data Integrity Failures)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Software or Data Integrity Failures (A08 OWASP)
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 7, 8

## Historia

Como evaluador de seguridad, quiero comprobar que las actualizaciones, los datos y los procesos
de la aplicación no puedan alterarse de forma indebida —desde la deserialización hasta la
bitácora y las copias de seguridad—, para registrar los riesgos de integridad con evidencia.

## Qué se prueba

**A. Verificación de sesión y firmas**

1. Token con firma alterada, `alg: none`, emisor o audiencia falsos → 401
   (`Program.cs:54-62` valida firma, emisor, audiencia y vida útil).
2. Token con `stamp` que ya no coincide con el sello del usuario → 401 "La sesión ya no es
   válida" (`CurrentUserMiddleware.cs:30-38`).
3. Confirmar que no hay campos de permisos ni de rol dentro del token: los permisos se resuelven
   en el servidor (`CurrentUserMiddleware.cs:60`, `PermissionService`).

**B. Escrituras y transacciones**

1. Una operación que falla no debe dejar bitácora: probar un guardado que reviente a mitad de
   camino (prueba existente `Una_operacion_que_falla_no_deja_entrada`; auditoría escrita en la
   misma transacción en `ApplicationDbContext.Audit.cs`).
2. Duplicados rechazados por la base: correo de usuario único, código de unidad único por
   conjunto, `(ComplexId, Catalog, Code)`, `(ComplexId, DocumentNumber)`, `UserToken.TokenHash`
   único → 409 en lugar de silencio (`Correo_duplicado_responde_409…`).
3. Estado calculado, no editable: la vigencia de unidades, ocupaciones y autorizaciones sale de
   fechas y eventos; verificar que ningún endpoint permite escribir "estado" a mano donde debía
   calcularse (revisar `PUT api/units/{id}`, `POST api/occupancies/{id}/decide`).

**C. Bitácora y no repudio**

1. `GET api/audit` es de solo lectura: `DELETE/POST/PUT api/audit` → 405 (prueba
   `La_bitacora_es_de_solo_consulta_exige_permiso_y_respeta_el_conjunto`).
2. Los eventos de acceso no se borran: `DELETE api/visits/{id}` no existe y un evento se anula
   con motivo y responsable (`AccessController.Void`, permiso `access.void`).
3. La bitácora registra quién, qué, antes y después, IP y conjunto, y nunca secretos (pruebas
   `Un_cambio_deja_quien_que_y_cuando…` y `La_bitacora_nunca_contiene_contrasenas_sellos_ni_tokens`).
4. **Nivel de datos:** verificar que la tabla `AuditLogs` no tenga protección a nivel de base de
   datos (trigger de solo inserción o permisos de solo lectura): un administrador de BD podría
   editarla. Documentar el alcance real del control.

**D. Migraciones, semilla y despliegue**

1. Migraciones de EF versionadas y sin cambios pendientes (prueba
   `El_modelo_no_tiene_cambios_pendientes_de_migracion`).
2. La semilla usa valores fijos (`DataSeeder.cs:10-11`): verificar que no se regeneren hashes ni
   GUIDs al compilar (defecto histórico D8).
3. El CI construye desde `master`/`develop` y corre la suite; verificar que no haya otro mecanismo
   de publicación fuera de CI y qué tan reproducible es el artefacto (cruza con A06).

**E. Copias de seguridad**

1. Verificar si existe procedimiento de backup y prueba de restauración (HU-132) y dejar el
   resultado como riesgo pendiente si no existe.

**F. Deserialización**

1. Confirmar que no hay Newtonsoft.Json ni `TypeNameHandling` ni `JsonSerializerSettings` con
   tipos arbitrarios; respuestas con System.Text.Json por defecto.
2. Confirmar que no hay carga de archivos (`IFormFile`) ni endpoints que reciban binarios.

## Fundamento en el API

### Lo que sí cumple con la épica

- Firma y validación del token en cada petición; los privilegios no viajan en el token.
- Auditoría atómica con el cambio y exclusión de secretos, con pruebas automatizadas.
- Integridad referencial y únicos en la base de datos (correo, código de unidad, hash de token).
- Inmutabilidad de eventos de acceso por diseño (anulación en vez de borrado) y bitácora sin
  operaciones de escritura en la API.
- Semilla determinista para que las migraciones sean reproducibles.
- Sin deserialización insegura ni subida de archivos.
- Suite de 87 pruebas corriendo en CI en cada PR.

### Lo que no cumple o falta verificar

- La bitácora solo está protegida a nivel de aplicación: no hay evidencia de protección a nivel
  de base de datos (permisos, trigger de append-only).
- No hay copias de seguridad ni prueba de restauración documentadas (HU-132 pendiente).
- Las cuentas sembradas con hashes y contraseñas fijas están en el repositorio (integridad del
  entorno inicial comprometida por diseño, cruza A02-1/A07-2).
- El CI no verifica integridad de dependencias ni publica artefactos firmados (cruza A06).
- No se valida que las migraciones aplicadas en un entorno coincidan con el código desplegado
  fuera de la prueba local de "pendientes".

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A08-1 | Bitácora editable a nivel de base de datos | `AuditLogs` (sin trigger/permisos) | Alteración del rastro de quién hizo qué | Alto | Permisos de solo inserción o trigger de append-only + revisión periódica | P1 |
| A08-2 | Sin copias de seguridad verificadas | infraestructura | Pérdida o cifrado de datos sin recuperación | Alto | Definir y probar backup + restauración (HU-132) | P1 |
| A08-3 | Credenciales fijas en la semilla | `DataSeeder.cs` | Estado inicial predecible en cualquier entorno | Medio | Condicionar a entorno de desarrollo y forzar rotación | P1 |
| A08-4 | Sin publicación de artefactos reproducibles/firmados desde CI | `.github/workflows/ci.yml` | Dificultad para demostrar qué código corre en producción | Medio | Publicar artefactos desde CI con hash y registro de versión | P2 |
| A08-5 | Sin validación de integridad de migraciones en el despliegue | `Program.cs:164-183` | Migraciones manuales fuera de sincronía | Bajo | Paso explícito de migración en el despliegue con verificación | P2 |

## Criterios de aceptación

- [ ] Documentadas las pruebas de firma, transacción, bitácora e inmutabilidad con evidencia
      (criterios 1 y 7).
- [ ] Registrados los riesgos de integridad a nivel de datos y de despliegue con componente,
      impacto, riesgo, recomendación y prioridad (criterios 2 y 8).
- [ ] Dejado por escrito el estado de las copias de seguridad y su prueba de restauración.
- [ ] Confirmada la ausencia de deserialización insegura y de carga de archivos.

## Verificación

Evidencia: salida de pruebas de integración (`dotnet test`) más capturas de respuestas 401/405/409
y consulta de la tabla `AuditLogs`. Todo en `documentation/owasp-top10/evidencias/A08-<n>.txt`.
