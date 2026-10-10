# HU-117 · Ocupantes y arrendamientos con vigencia

**Épica:** E7 · **Etapa:** 2 Unidades y personas · **Estado:** Pendiente · **Depende de:** HU-116, HU-114
**Requerimiento:** §1.2, §4.2 a §4.7, §8.2, §8.8

## Historia

Como administrador del conjunto, quiero registrar quién vive en cada unidad, con qué relación y
durante qué periodo, para distinguir al arrendatario del propietario y retirar el acceso cuando la
residencia termina.

## Criterios de aceptación

- [ ] `Occupancy`: persona, unidad, relación, fecha de inicio, fecha de fin (opcional), estado.
- [ ] Relaciones: propietario residente, arrendatario, familiar u otro ocupante autorizado. "Propietario residente" exige una titularidad vigente de esa persona sobre esa unidad.
- [ ] Un arrendatario exige fecha de inicio; la fecha de fin es obligatoria si el contrato es a término fijo.
- [ ] Estados: pendiente de aprobación, vigente, finalizada, rechazada. "Vencida" no se guarda: una ocupación con fecha de fin pasada deja de contar como vigente sin que nadie la toque.
- [ ] Una persona puede tener ocupaciones en varias unidades con relaciones distintas (propietaria en una, arrendataria en otra).
- [ ] El estado de la unidad se calcula: ocupada por propietario, arrendada, desocupada.
- [ ] Finalizar una ocupación: fija la fecha de fin y, en la misma operación, revoca lo que dependía de ella: autorizaciones de visita que esa persona emitió para la unidad, sus vehículos permanentes en la unidad y, si no le queda ningún otro vínculo vigente en el conjunto, su rol de Residente.
- [ ] Finalizar el arrendamiento principal ofrece finalizar también a los ocupantes que dependían de él.
- [ ] Nada se borra: la ocupación, los accesos y las visitas anteriores siguen consultables.
- [ ] Un proceso diario aplica las mismas revocaciones a las ocupaciones que vencieron por fecha.
- [ ] Se puede dar acceso a la plataforma a un ocupante (rol Residente o arrendatario).
- [ ] Permiso `occupancy.write`; todo cambio queda en la bitácora.

## Diseño técnico

- La "vigencia" es siempre una consulta: `Status = Vigente` y hoy dentro de `[inicio, fin]`, con "hoy" en la zona horaria del conjunto.
- `IOccupancyService.EndAsync` concentra las revocaciones para que el cierre manual y el automático (`BackgroundService`) hagan lo mismo. Las revocaciones de autorizaciones y vehículos se conectan cuando existan HU-121 y HU-124.
- Restricción de exclusión para que una persona no tenga dos ocupaciones solapadas en la misma unidad.

## Verificación

Unidad con propietario no residente y arrendatario con contrato a 6 meses: la ficha muestra a cada
uno con su relación (§8.2). Finalizar el arrendamiento: el arrendatario pierde el acceso a "Mi
residencia" y la unidad pasa a desocupada; el historial sigue visible. Prueba con reloj simulado
para el vencimiento automático.
