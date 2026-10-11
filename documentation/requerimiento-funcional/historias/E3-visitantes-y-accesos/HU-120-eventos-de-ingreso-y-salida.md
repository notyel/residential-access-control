# HU-120 · Eventos de ingreso y salida

**Épica:** E7 · **Etapa:** 3 Visitantes y accesos · **Estado:** Hecha con pendientes · **Depende de:** HU-115, HU-116
**Requerimiento:** §1.6, §5.4, §4.7, §8.3, §8.8

## Historia

Como guarda, quiero que cada ingreso y cada salida quede como un registro propio que nadie pueda
modificar, para tener una bitácora de accesos confiable de visitantes y residentes.

## Situación actual

Una visita es una fila `Visit`: la entrada es su `CreatedAt` y la salida un campo `CheckOut` sobre
la misma fila. No se registran accesos de residentes.

## Criterios de aceptación

- [x] `AccessEvent`, de solo inserción: tipo (entrada, salida, rechazo), fecha y hora, persona, clase de persona (visitante, residente, propietario), unidad, vehículo (opcional), operador que lo registró, punto de acceso (portería), observación.
- [x] El API no tiene operaciones para editar ni borrar un evento. Un error se corrige con un evento de anulación que referencia al original y exige motivo; el original sigue visible, marcado como anulado.
- [x] `Visit` pasa a ser la estancia que agrupa: persona, unidad, motivo, evento de entrada, evento de salida (opcional), autorización (HU-121) y vehículo.
- [x] Registrar visita crea la `Visit` y su evento de entrada en una transacción; registrar salida crea el evento de salida y lo enlaza. `Visit.CheckOut` y `CheckedOutById` (HU-105) desaparecen.
- [x] La visita tiene motivo (catálogo configurable: visita social, domicilio, servicio técnico, otro + texto).
- [x] Portería puede registrar la entrada y salida de un residente o propietario buscándolo por nombre, documento o unidad; solo se permite si tiene vínculo vigente.
- [ ] Los catálogos de puntos de acceso (porterías) son configurables por conjunto; con una sola portería no se pregunta. **Pendiente:** el catálogo `access-point` existe y el API lo valida, pero la pantalla de portería no pregunta el punto de acceso ni hay pantalla para administrar catálogos (ver HU-115).
- [x] Migración: cada `Visit` existente genera su evento de entrada y, si tenía `CheckOut`, el de salida, conservando fechas y operador.
- [x] Permisos `access.register` y `access.void`.

## Diseño técnico

- Sin `UpdatedAt` ni `IsActive` en `AccessEvent`; a nivel de base de datos, un trigger rechaza `UPDATE` y `DELETE`.
- Los eventos guardan una copia del nombre y documento de la persona en el momento del acceso, para que el historial no cambie si luego se corrigen sus datos.
- "Está dentro" = visita con entrada y sin salida; no hay campo de estado.

## Verificación

Registrar entrada y salida de un visitante y de un residente; en la base hay cuatro eventos y
ninguno fue actualizado. Anular una entrada equivocada: quedan el original y la anulación. Las
visitas anteriores a la migración muestran sus horas originales.

## Cómo quedó

- `AccessEvent` es de solo inserción: el API no tiene operaciones para editarlo ni borrarlo y, además, un disparador de la base (`trg_access_events_immutable`) rechaza cualquier `UPDATE` o `DELETE`, venga de donde venga. Guarda el nombre y el documento tal como eran en el momento del acceso.
- Un error se corrige con `POST access/events/{id}/void` (permiso `access.void`, motivo obligatorio): agrega un evento de anulación que apunta al original. Anular una entrada saca la visita de "presentes"; anular una salida la devuelve.
- `Visit` es la estancia: persona, unidad, motivo, evento de entrada, evento de salida, autorización y vehículo. Visita y entrada se guardan en la misma operación. Dos salidas simultáneas: solo una se enlaza; la otra responde 409 y su evento queda anulado, no borrado.
- Entrada y salida de propietarios y residentes: `POST access/residents`, solo con vínculo vigente.
- La migración `AccessEvents` convierte cada visita anterior en sus eventos conservando fecha y operador (verificado sobre la base local de desarrollo).
- Pruebas: `AccessTests` (inmutabilidad a nivel de base, anulaciones, doble salida).
