# HU-127 · Asignaciones, ocupación y reservas

**Épica:** E7 · **Etapa:** 5 Parqueaderos · **Estado:** Pendiente · **Depende de:** HU-126, HU-125
**Requerimiento:** §1.5, §6 criterios 3, 4, 6 y 7, §8.5, §8.8

## Historia

Como guarda, quiero asignar un parqueadero de visitantes al registrar un ingreso; como
administrador, quiero asignar temporalmente los comunitarios; en ambos casos sin que un espacio
quede con dos usos a la vez.

## Criterios de aceptación

- [ ] `ParkingAssignment`: espacio, clase (titular, asignación temporal, reserva, ocupación), beneficiario (unidad, persona, vehículo y/o visita), inicio, fin, estado (vigente, liberada, cancelada), quién la registró.
- [ ] **Ocupación**: al registrar el ingreso de una visita con vehículo, el guarda elige un espacio de visitantes de la lista de disponibles; se crea una ocupación ligada a la visita y al vehículo.
- [ ] **Liberación**: la salida del vehículo cierra la ocupación en la misma operación. No hay que "desocupar" el espacio a mano (§6 criterio 7).
- [ ] **Asignación temporal** de comunitarios a una unidad o vehículo: inicio y fin obligatorios; se libera sola al llegar la fecha de fin.
- [ ] **Reserva** (si la política lo permite): franja futura para una unidad; se convierte en ocupación cuando el vehículo ingresa; se libera sola si no se usa dentro de un margen configurable tras su inicio.
- [ ] El sistema impide dos asignaciones incompatibles sobre el mismo espacio con periodos que se crucen; la validación se hace en la base de datos, no solo en la pantalla, y dos peticiones simultáneas no pueden ganar ambas (§6 criterio 4).
- [ ] No se puede asignar un espacio fuera de servicio, ni uno cuyo tipo de vehículo no coincida.
- [ ] Un privado solo admite ocupaciones de vehículos de su unidad titular; cualquier otro uso requiere acceso excepcional con motivo.
- [ ] Un vehículo no puede ocupar dos espacios a la vez.
- [ ] Las ocupaciones de visitantes que superan la política de tiempo máximo se marcan "excedida" (calculado); no se liberan solas.
- [ ] Novedades de un espacio (mal estacionado, daño) como notas con fecha y autor.
- [ ] Historial por espacio: todas sus asignaciones y ocupaciones pasadas.
- [ ] Permisos `parking.assign`, `parking.reserve`.

## Diseño técnico

- Restricción de exclusión de PostgreSQL (`EXCLUDE USING gist`) sobre `(SpaceId, tstzrange(inicio, fin))` para las clases incompatibles entre sí; requiere la extensión `btree_gist`. Una ocupación abierta usa rango sin fin.
- La clase *titular* no excluye a la *ocupación* del vehículo de la misma unidad; sí excluye asignaciones temporales y reservas.
- La violación de la restricción se traduce a 409 "El espacio ya está asignado en ese periodo".
- Proceso periódico para liberar reservas no usadas; las asignaciones temporales vencidas no necesitan proceso (dejan de contar por fecha).

## Verificación

Prueba de integración con dos peticiones concurrentes sobre el mismo espacio: una responde 200 y la
otra 409. Ingreso de visita con vehículo ocupa V-03; su salida lo deja disponible sin otra acción.
Reserva no usada se libera con reloj simulado.
