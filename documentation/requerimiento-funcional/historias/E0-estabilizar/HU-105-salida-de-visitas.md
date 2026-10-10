# HU-105 · Salida de visitas sin perder historial

**Épica:** E7 · **Etapa:** 0 Estabilizar · **Estado:** Hecha · **Depende de:** HU-102
**Requerimiento:** §5.4, §5.6, §4.7, §8.3, §8.8 · **Defecto:** D5

## Historia

Como guarda, quiero registrar la salida de un visitante desde la lista de visitas, para saber quién
sigue dentro del conjunto, sin que nadie pueda alterar o borrar lo ya registrado.

## Situación actual

- `POST visits/{id}/checkout` existe pero ninguna pantalla lo llama; exige rol Guard (el Admin no puede).
- Llamarlo dos veces sobrescribe la hora de salida.
- `PUT visits/{id}` permite cambiar la persona de una visita ya registrada.
- `DELETE visits/{id}` elimina la fila.
- No queda registrado quién marcó la salida.

## Criterios de aceptación

- [x] La lista de Visitas tiene la acción "Registrar salida" en las visitas sin salida, visible para Admin y Guard; pide confirmación.
- [x] Tras registrar la salida, la fila cambia de estado sin recargar la página.
- [x] Registrar salida sobre una visita que ya la tiene responde 409 y no cambia la hora.
- [x] Se guarda quién registró la salida (`CheckedOutById`).
- [x] La lista se puede filtrar por estado: dentro / con salida.
- [x] `PUT visits/{id}` solo permite corregir la placa mientras la visita no tenga salida; no cambia persona ni residencia.
- [x] `DELETE visits/{id}` se retira del API.

## Diseño técnico

- `VisitFilterDto.Status` (`Inside | CheckedOut`); filtro en `VisitService.GetVisitsAsync`.
- Con esta historia la tabla de Visitas pasa a tener acciones por fila (pendiente anotado en HU-07 del rediseño).
- El modelo de eventos separados de ingreso y salida llega en HU-120; aquí solo se protege el dato actual.

## Archivos

`backend/AccessControl.API/Controllers/VisitsController.cs`,
`backend/AccessControl.Application/Services/VisitService.cs`, `Visit.cs` + migración,
`front/.../visits/pages/visits/*`, `visit-filters/*`, `visits.service.ts`.

## Verificación

Registrar una visita, marcar salida, intentar marcarla otra vez por API (409), filtrar por "dentro"
y confirmar que ya no aparece.
