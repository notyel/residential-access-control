# HU-128 · Disponibilidad de parqueaderos

**Épica:** E7 · **Etapa:** 5 Parqueaderos · **Estado:** Pendiente · **Depende de:** HU-127
**Requerimiento:** §1.5, §6 criterios 3, 5 y 7, §3 (Disponibilidad de parqueaderos), §8.9

## Historia

Como guarda, quiero ver cuántos parqueaderos hay libres y cuáles son, para dirigir a un visitante
sin recorrer el parqueadero.

## Criterios de aceptación

- [ ] El estado de cada espacio se calcula en el momento de la consulta con esta precedencia: fuera de servicio → ocupado (ocupación abierta) → reservado (reserva o asignación temporal vigente) → disponible.
- [ ] No existe ningún contador ni campo de estado almacenado; registrar un ingreso, una salida, una asignación o un periodo fuera de servicio cambia el resultado de inmediato (§6 criterio 7).
- [ ] `GET parking/availability` devuelve, por tipo (privado, comunitario, visitantes) y en total: capacidad, ocupados, reservados, disponibles y fuera de servicio (§6 criterio 5). La suma de los cuatro estados es igual a la capacidad.
- [ ] Para los privados, "disponible" significa "sin vehículo dentro"; no se ofrecen para asignar a visitantes.
- [ ] Pantalla "Disponibilidad de parqueaderos": tarjetas de resumen por tipo y lista o cuadrícula de espacios con su estado (color + texto + icono); filtros por tipo, ubicación y tipo de vehículo.
- [ ] Al tocar un espacio: quién lo ocupa (placa, unidad, visita), desde cuándo y hasta cuándo.
- [ ] Consulta de disponibilidad para un periodo futuro (para crear reservas y asignaciones temporales).
- [ ] La pantalla se actualiza sola cada 30 segundos y al volver a la pestaña.
- [ ] El selector de espacio del ingreso de visitas (HU-127) usa esta misma consulta.
- [ ] Legible y operable a 390 px.

## Diseño técnico

- Una sola consulta SQL agregada sobre `ParkingSpace` + `ParkingAssignment` vigentes + `ParkingOutOfService`; sin tabla de resumen.
- El estado es una función pura `ComputeState(espacio, asignaciones, instante)` con pruebas unitarias de la precedencia.

## Verificación

Con 5 espacios de visitantes: ocupar 2, reservar 1, sacar 1 de servicio → el resumen muestra
2 / 1 / 1 / 1 sin ninguna acción adicional; registrar una salida lo cambia a 1 / 1 / 2 / 1.
Criterio §8.9 (disponibilidad) demostrable.
