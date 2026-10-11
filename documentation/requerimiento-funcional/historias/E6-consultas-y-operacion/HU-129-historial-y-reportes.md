# HU-129 · Historial de accesos y reportes

**Épica:** E7 · **Etapa:** 6 Consultas y operación · **Estado:** Hecha con pendientes · **Depende de:** HU-122, HU-125
**Requerimiento:** §1.6, §2 (Auditor), §3 (Reportes, Historial de accesos, Novedades), §7.7, §8.9

## Historia

Como administrador o auditor, quiero consultar y exportar los accesos con filtros combinables; como
guarda, quiero anotar y consultar las novedades del turno.

## Criterios de aceptación

- [x] "Historial de accesos": todos los `AccessEvent` (entradas, salidas, rechazos, excepcionales, anulaciones) de visitantes, residentes y vehículos.
- [ ] Filtros combinables: rango de fechas, persona (nombre o documento), unidad, placa, tipo de acceso, clase de persona, operador y punto de acceso (§1.6); el superadministrador puede filtrar además por conjunto (§7.7). **Parcial:** en la pantalla: fechas, persona, placa, tipo de acceso y clase de persona. Unidad, operador y punto de acceso están en el API. El superadministrador elige el conjunto con el selector general, no con un filtro propio.
- [x] Paginado y ordenado en servidor; el rango de fechas es obligatorio y tiene un máximo configurable para proteger el rendimiento.
- [x] La consulta respeta el rol (§1.6): administrador y auditor, todo el conjunto; portería, los últimos días que defina su permiso; propietario y residente, solo sus unidades y solo durante su vínculo ("Historial de accesos permitido").
- [x] Sección "Reportes" con, al menos: accesos por periodo, visitas por unidad, rechazos y accesos excepcionales con su justificación, ocupación de parqueaderos por tipo, vehículos sin autorización vigente detectados, vinculaciones que vencen en los próximos 30 días.
- [x] Cada reporte se exporta a CSV con los filtros aplicados; la exportación exige `reports.export` y queda en la bitácora de auditoría con los filtros usados.
- [x] El rol Auditor puede consultar y exportar, y ninguna pantalla le muestra acciones de escritura.
- [ ] "Novedades": el guarda registra una novedad del turno (texto, categoría, unidad, persona o vehículo opcionales); son de solo inserción y se consultan por fecha y categoría. **Parcial:** texto, categoría y placa desde la pantalla; unidad y persona solo por el API; el filtro por fecha también.
- [x] Permisos `access.read`, `access.read_own`, `reports.read`, `reports.export`, `incidents.write`.

## Diseño técnico

- Las consultas de reporte son proyecciones de solo lectura (`AsNoTracking`) sobre eventos y asignaciones; sin tablas de resumen.
- Exportación en streaming para no cargar el resultado completo en memoria.
- "Novedades" cubre lo mínimo que pide §1.6; el módulo completo de incidentes es de segunda fase (§9).

## Verificación

Consulta combinando unidad + placa + rango de fechas devuelve solo lo esperado. Con rol Auditor:
exporta y no ve ningún botón de crear o editar; intentar un `POST` responde 403. Con rol Residente
solo aparecen accesos de su unidad.

## Cómo quedó

- "Historial de accesos" lista todos los eventos (entradas, salidas, rechazos, excepcionales y anulaciones) con rango de fechas obligatorio y un máximo configurable (`AccessHistory:MaxRangeDays`, 92 días).
- Alcance por rol, resuelto en el servidor: con `access.read` y `reports.read`, todo; con `access.read` solo (portería), los últimos días de la política "Días de historial de accesos para portería"; con `access.read_own`, solo sus unidades y solo desde que empezó su vínculo.
- "Reportes" con los seis pedidos; son consultas de solo lectura calculadas en el momento. Cada uno y el historial se exportan a CSV con `reports.export`; la exportación queda en la bitácora (`report.exported`) con los filtros.
- "Novedades": solo se agregan y se consultan por categoría.
- El auditor consulta y exporta; la matriz de permisos de HU-133 comprueba que no escribe.
