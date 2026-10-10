# HU-125 · Vehículos en el control de acceso

**Épica:** E7 · **Etapa:** 4 Vehículos · **Estado:** Pendiente · **Depende de:** HU-124, HU-122
**Requerimiento:** §1.4, §1.6, §5.5, §3 (Verificar vehículos), §8.4

## Historia

Como guarda, quiero verificar una placa al instante y registrar la entrada y salida de vehículos,
para detectar los que no tienen autorización vigente.

## Criterios de aceptación

- [ ] Pantalla "Verificar vehículos": al escribir una placa responde con uno de cuatro resultados diferenciados por color y texto: autorizado (unidad y responsable), autorización vencida o revocada, vehículo de una visita en curso, no registrado.
- [ ] Registrar entrada o salida de un vehículo permanente crea un `AccessEvent` con el vehículo y su conductor (persona con vínculo vigente en la unidad).
- [ ] Un vehículo sin autorización vigente no registra un ingreso normal: solo rechazo o acceso excepcional (HU-122).
- [ ] Al registrar una visita con vehículo se piden placa, tipo y, opcionalmente, marca y color; el vehículo queda creado o reutilizado como clase *visitante* y enlazado a la visita y a su evento de entrada (§5.5). `Visit.VehiclePlate` (texto) desaparece.
- [ ] La salida de la visita registra también la salida del vehículo; si el visitante sale sin él, el vehículo sigue "dentro" y aparece en la lista de vehículos presentes.
- [ ] Lista "Vehículos dentro del conjunto", calculada de los eventos, separando permanentes y visitantes.
- [ ] Si una placa de visita coincide con un vehículo permanente del conjunto, se avisa al guarda antes de guardar.
- [ ] Novedades de ingreso o salida (daño visible, placa no coincide, etc.) como observación del evento.
- [ ] El historial de accesos (HU-123) filtra por placa usando la forma normalizada.
- [ ] Migración: las placas de visitas anteriores se convierten en vehículos visitantes enlazados a su visita.

## Diseño técnico

- `AccessEvent.VehicleId`; "dentro" = último evento del vehículo es una entrada.
- La verificación reutiliza `GET vehicles/by-plate` (HU-124) y añade el estado de presencia.
- El campo de placa aplica la normalización mientras se escribe.

## Verificación

Placa autorizada, placa revocada y placa desconocida muestran tres resultados distintos. Visita con
vehículo: el detalle de la visita y el del evento muestran el vehículo; tras la salida no aparece en
"dentro". Criterio §8.4 demostrable para propietario, residente y visitante.
