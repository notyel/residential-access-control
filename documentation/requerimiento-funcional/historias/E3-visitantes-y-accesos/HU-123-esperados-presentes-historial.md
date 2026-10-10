# HU-123 · Visitantes esperados, presentes e historial

**Épica:** E7 · **Etapa:** 3 Visitantes y accesos · **Estado:** Pendiente · **Depende de:** HU-122
**Requerimiento:** §1.3, §5.6, §3 (Historial de visitas), §8.9

## Historia

Como guarda o administrador, quiero ver quién se espera hoy, quién está dentro y quién vino antes;
como propietario o residente, quiero ver las visitas de mi unidad.

## Criterios de aceptación

- [ ] La pantalla "Visitantes" tiene tres vistas: **Esperados**, **Presentes** e **Historial**.
- [ ] Esperados: autorizaciones válidas para hoy (únicas del día, por periodo y recurrentes que aplican al día de la semana) que aún no han ingresado; con acción "Registrar ingreso".
- [ ] Presentes: visitas con entrada y sin salida, con el tiempo transcurrido y acción "Registrar salida". Las que superan la política "horas para marcar por revisar" se destacan.
- [ ] El conteo de presentes se calcula de los eventos; no hay contador almacenado.
- [ ] Una visita "por revisar" se puede cerrar con una salida administrativa que exige motivo y queda marcada como tal (no se inventa una hora de salida real).
- [ ] Historial: filtros por rango de fechas, unidad, persona (nombre o documento), placa, resultado de verificación y operador; paginado en servidor.
- [ ] Detalle de una visita en el panel lateral: autorización usada, quién autorizó o confirmó, eventos de entrada y salida con operador, vehículo.
- [ ] "Historial de visitas" (propietario y residente): las mismas vistas limitadas a sus unidades con vínculo vigente; el arrendatario solo ve visitas ocurridas durante su ocupación.
- [ ] Los rechazos aparecen en el historial como una fila diferenciada.
- [ ] Exportar el historial filtrado a CSV con `reports.export`.

## Diseño técnico

- Reutiliza `data-table`, el panel de filtros y `status-badge` del rediseño; las tres vistas son pestañas de la misma ruta con el estado en la URL.
- Índices sobre `AccessEvent (ComplexId, OccurredAt)`, `(UnitId, OccurredAt)` y `Visit` sin salida.
- Reemplaza la lista "Visitas" actual.

## Verificación

Con una autorización recurrente para hoy: aparece en Esperados, tras el ingreso pasa a Presentes y
tras la salida a Historial. Con rol Propietario solo se ven sus unidades (prueba de integración).
Criterio §8.9 (visitantes presentes) demostrable.
