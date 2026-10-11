# HU-128 · Disponibilidad de parqueaderos

**Épica:** E7 · **Etapa:** 5 Parqueaderos · **Estado:** Hecha con pendientes · **Depende de:** HU-127
**Requerimiento:** §1.5, §6 criterios 3, 5 y 7, §3 (Disponibilidad de parqueaderos), §8.9

## Historia

Como guarda, quiero ver cuántos parqueaderos hay libres y cuáles son, para dirigir a un visitante
sin recorrer el parqueadero.

## Criterios de aceptación

- [x] El estado de cada espacio se calcula en el momento de la consulta con esta precedencia: fuera de servicio → ocupado (ocupación abierta) → reservado (reserva o asignación temporal vigente) → disponible.
- [x] No existe ningún contador ni campo de estado almacenado; registrar un ingreso, una salida, una asignación o un periodo fuera de servicio cambia el resultado de inmediato (§6 criterio 7).
- [x] `GET parking/availability` devuelve, por tipo (privado, comunitario, visitantes) y en total: capacidad, ocupados, reservados, disponibles y fuera de servicio (§6 criterio 5). La suma de los cuatro estados es igual a la capacidad.
- [x] Para los privados, "disponible" significa "sin vehículo dentro"; no se ofrecen para asignar a visitantes.
- [x] Pantalla "Disponibilidad de parqueaderos": tarjetas de resumen por tipo y lista o cuadrícula de espacios con su estado (color + texto + icono); filtros por tipo, ubicación y tipo de vehículo.
- [x] Al tocar un espacio: quién lo ocupa (placa, unidad, visita), desde cuándo y hasta cuándo.
- [x] Consulta de disponibilidad para un periodo futuro (para crear reservas y asignaciones temporales).
- [x] La pantalla se actualiza sola cada 30 segundos y al volver a la pestaña.
- [x] El selector de espacio del ingreso de visitas (HU-127) usa esta misma consulta.
- [ ] Legible y operable a 390 px. **Sin verificar:** la cuadrícula y las tarjetas son adaptables, pero no se revisó en un teléfono de 390 px.

## Diseño técnico

- Una sola consulta SQL agregada sobre `ParkingSpace` + `ParkingAssignment` vigentes + `ParkingOutOfService`; sin tabla de resumen.
- El estado es una función pura `ComputeState(espacio, asignaciones, instante)` con pruebas unitarias de la precedencia.

## Verificación

Con 5 espacios de visitantes: ocupar 2, reservar 1, sacar 1 de servicio → el resumen muestra
2 / 1 / 1 / 1 sin ninguna acción adicional; registrar una salida lo cambia a 1 / 1 / 2 / 1.
Criterio §8.9 (disponibilidad) demostrable.

## Cómo quedó

- El estado es la función pura `ParkingRules.ComputeState` (fuera de servicio → ocupado → reservado → disponible), con pruebas unitarias de la precedencia. No hay contadores ni campo de estado.
- `GET parking/availability` devuelve por tipo y en total capacidad, ocupados, reservados, disponibles y fuera de servicio; la suma siempre es la capacidad. Acepta un periodo futuro.
- La pantalla "Parqueaderos" muestra las tarjetas por tipo y la cuadrícula de espacios con color, icono y texto; al abrir un espacio se ve quién lo ocupa y desde cuándo. Se actualiza sola cada 30 segundos y al volver a la pestaña.
- El selector de espacio del ingreso de visitas usa `GET parking/visitor-spaces`, que sale de la misma consulta.
- Se calcula con tres consultas y se combina en memoria, no con una sola consulta SQL agregada como proponía el diseño; con cientos de espacios no hay diferencia apreciable.
