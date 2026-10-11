# HU-125 · Vehículos en el control de acceso

**Épica:** E7 · **Etapa:** 4 Vehículos · **Estado:** Hecha con pendientes · **Depende de:** HU-124, HU-122
**Requerimiento:** §1.4, §1.6, §5.5, §3 (Verificar vehículos), §8.4

## Historia

Como guarda, quiero verificar una placa al instante y registrar la entrada y salida de vehículos,
para detectar los que no tienen autorización vigente.

## Criterios de aceptación

- [x] Pantalla "Verificar vehículos": al escribir una placa responde con uno de cuatro resultados diferenciados por color y texto: autorizado (unidad y responsable), autorización vencida o revocada, vehículo de una visita en curso, no registrado.
- [x] Registrar entrada o salida de un vehículo permanente crea un `AccessEvent` con el vehículo y su conductor (persona con vínculo vigente en la unidad).
- [x] Un vehículo sin autorización vigente no registra un ingreso normal: solo rechazo o acceso excepcional (HU-122).
- [x] Al registrar una visita con vehículo se piden placa, tipo y, opcionalmente, marca y color; el vehículo queda creado o reutilizado como clase *visitante* y enlazado a la visita y a su evento de entrada (§5.5). `Visit.VehiclePlate` (texto) desaparece.
- [x] La salida de la visita registra también la salida del vehículo; si el visitante sale sin él, el vehículo sigue "dentro" y aparece en la lista de vehículos presentes.
- [x] Lista "Vehículos dentro del conjunto", calculada de los eventos, separando permanentes y visitantes.
- [x] Si una placa de visita coincide con un vehículo permanente del conjunto, se avisa al guarda antes de guardar.
- [ ] Novedades de ingreso o salida (daño visible, placa no coincide, etc.) como observación del evento. **Parcial:** el ingreso de visitas tiene "Observaciones" y los vehículos tienen novedades; la entrada o salida de un vehículo de residente desde "Verificar vehículos" no pide observación.
- [x] El historial de accesos (HU-123) filtra por placa usando la forma normalizada.
- [x] Migración: las placas de visitas anteriores se convierten en vehículos visitantes enlazados a su visita.

## Diseño técnico

- `AccessEvent.VehicleId`; "dentro" = último evento del vehículo es una entrada.
- La verificación reutiliza `GET vehicles/by-plate` (HU-124) y añade el estado de presencia.
- El campo de placa aplica la normalización mientras se escribe.

## Verificación

Placa autorizada, placa revocada y placa desconocida muestran tres resultados distintos. Visita con
vehículo: el detalle de la visita y el del evento muestran el vehículo; tras la salida no aparece en
"dentro". Criterio §8.4 demostrable para propietario, residente y visitante.

## Cómo quedó

- "Verificar vehículos" (`/access-control/gate/vehicles`): al escribir la placa responde con uno de cuatro resultados diferenciados por color, icono y texto, y desde ahí se registra la entrada o la salida con quien conduce.
- Un vehículo permanente sin autorización vigente no entra de forma normal: 409, salvo acceso excepcional con permiso y motivo.
- El vehículo de una visita se crea (o se reutiliza) como vehículo visitante y queda enlazado a la visita y a su evento de entrada. `Visit.VehiclePlate` desapareció; la migración convirtió las placas anteriores en vehículos.
- "Dentro" se calcula de los eventos (entrada sin salida posterior ni anulación), separando residentes y visitantes. Si el visitante sale sin el vehículo, este sigue dentro y se le registra la salida aparte.
- Al registrar una visita, si la placa es de un vehículo registrado del conjunto o ya figura dentro, se avisa al guarda antes de guardar.
