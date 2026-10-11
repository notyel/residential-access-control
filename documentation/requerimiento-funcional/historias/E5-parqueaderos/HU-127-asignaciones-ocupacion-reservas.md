# HU-127 · Asignaciones, ocupación y reservas

**Épica:** E7 · **Etapa:** 5 Parqueaderos · **Estado:** Hecha con pendientes · **Depende de:** HU-126, HU-125
**Requerimiento:** §1.5, §6 criterios 3, 4, 6 y 7, §8.5, §8.8

## Historia

Como guarda, quiero asignar un parqueadero de visitantes al registrar un ingreso; como
administrador, quiero asignar temporalmente los comunitarios; en ambos casos sin que un espacio
quede con dos usos a la vez.

## Criterios de aceptación

- [x] `ParkingAssignment`: espacio, clase (titular, asignación temporal, reserva, ocupación), beneficiario (unidad, persona, vehículo y/o visita), inicio, fin, estado (vigente, liberada, cancelada), quién la registró.
- [x] **Ocupación**: al registrar el ingreso de una visita con vehículo, el guarda elige un espacio de visitantes de la lista de disponibles; se crea una ocupación ligada a la visita y al vehículo.
- [x] **Liberación**: la salida del vehículo cierra la ocupación en la misma operación. No hay que "desocupar" el espacio a mano (§6 criterio 7).
- [ ] **Asignación temporal** de comunitarios a una unidad o vehículo: inicio y fin obligatorios; se libera sola al llegar la fecha de fin. **Parcial:** a una unidad desde la pantalla; a un vehículo concreto solo por el API.
- [x] **Reserva** (si la política lo permite): franja futura para una unidad; se convierte en ocupación cuando el vehículo ingresa; se libera sola si no se usa dentro de un margen configurable tras su inicio.
- [x] El sistema impide dos asignaciones incompatibles sobre el mismo espacio con periodos que se crucen; la validación se hace en la base de datos, no solo en la pantalla, y dos peticiones simultáneas no pueden ganar ambas (§6 criterio 4).
- [x] No se puede asignar un espacio fuera de servicio, ni uno cuyo tipo de vehículo no coincida.
- [x] Un privado solo admite ocupaciones de vehículos de su unidad titular; cualquier otro uso requiere acceso excepcional con motivo.
- [x] Un vehículo no puede ocupar dos espacios a la vez.
- [x] Las ocupaciones de visitantes que superan la política de tiempo máximo se marcan "excedida" (calculado); no se liberan solas.
- [x] Novedades de un espacio (mal estacionado, daño) como notas con fecha y autor.
- [x] Historial por espacio: todas sus asignaciones y ocupaciones pasadas.
- [x] Permisos `parking.assign`, `parking.reserve`.

## Diseño técnico

- Restricción de exclusión de PostgreSQL (`EXCLUDE USING gist`) sobre `(SpaceId, tstzrange(inicio, fin))` para las clases incompatibles entre sí; requiere la extensión `btree_gist`. Una ocupación abierta usa rango sin fin.
- La clase *titular* no excluye a la *ocupación* del vehículo de la misma unidad; sí excluye asignaciones temporales y reservas.
- La violación de la restricción se traduce a 409 "El espacio ya está asignado en ese periodo".
- Proceso periódico para liberar reservas no usadas; las asignaciones temporales vencidas no necesitan proceso (dejan de contar por fecha).

## Verificación

Prueba de integración con dos peticiones concurrentes sobre el mismo espacio: una responde 200 y la
otra 409. Ingreso de visita con vehículo ocupa V-03; su salida lo deja disponible sin otra acción.
Reserva no usada se libera con reloj simulado.

## Cómo quedó

- `ParkingAssignment` cubre titular, asignación temporal, reserva y ocupación. Tres restricciones de exclusión de PostgreSQL impiden cruces: entre titular, temporal y reserva del mismo espacio; dos vehículos en el mismo espacio; y el mismo vehículo en dos espacios. Dos ingresos simultáneos al mismo espacio: uno responde 200 y el otro 409 (probado).
- Ocupación: al registrar una visita con vehículo, portería elige un espacio de visitantes libre. La salida del vehículo libera el espacio en la misma operación; anular la entrada también.
- Reservas solo si la política las habilita; se convierten en ocupación cuando entra un vehículo de la unidad y se liberan solas si no se usan dentro del margen (`Parking:ReservationGraceMinutes`, 60 por defecto).
- No se asigna un espacio fuera de servicio ni a un vehículo de otro tipo. Un privado solo recibe vehículos de su unidad titular; otro uso exige acceso excepcional con motivo.
- Las ocupaciones de visitantes que superan el máximo de la política se marcan "excedida".
- Limitación conocida: anular una **salida** devuelve el vehículo a "dentro", pero no reabre la ocupación del parqueadero; hay que ocuparlo de nuevo a mano.
