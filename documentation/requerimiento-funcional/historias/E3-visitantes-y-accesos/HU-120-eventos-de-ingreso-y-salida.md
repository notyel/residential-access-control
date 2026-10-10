# HU-120 · Eventos de ingreso y salida

**Épica:** E7 · **Etapa:** 3 Visitantes y accesos · **Estado:** Pendiente · **Depende de:** HU-115, HU-116
**Requerimiento:** §1.6, §5.4, §4.7, §8.3, §8.8

## Historia

Como guarda, quiero que cada ingreso y cada salida quede como un registro propio que nadie pueda
modificar, para tener una bitácora de accesos confiable de visitantes y residentes.

## Situación actual

Una visita es una fila `Visit`: la entrada es su `CreatedAt` y la salida un campo `CheckOut` sobre
la misma fila. No se registran accesos de residentes.

## Criterios de aceptación

- [ ] `AccessEvent`, de solo inserción: tipo (entrada, salida, rechazo), fecha y hora, persona, clase de persona (visitante, residente, propietario), unidad, vehículo (opcional), operador que lo registró, punto de acceso (portería), observación.
- [ ] El API no tiene operaciones para editar ni borrar un evento. Un error se corrige con un evento de anulación que referencia al original y exige motivo; el original sigue visible, marcado como anulado.
- [ ] `Visit` pasa a ser la estancia que agrupa: persona, unidad, motivo, evento de entrada, evento de salida (opcional), autorización (HU-121) y vehículo.
- [ ] Registrar visita crea la `Visit` y su evento de entrada en una transacción; registrar salida crea el evento de salida y lo enlaza. `Visit.CheckOut` y `CheckedOutById` (HU-105) desaparecen.
- [ ] La visita tiene motivo (catálogo configurable: visita social, domicilio, servicio técnico, otro + texto).
- [ ] Portería puede registrar la entrada y salida de un residente o propietario buscándolo por nombre, documento o unidad; solo se permite si tiene vínculo vigente.
- [ ] Los catálogos de puntos de acceso (porterías) son configurables por conjunto; con una sola portería no se pregunta.
- [ ] Migración: cada `Visit` existente genera su evento de entrada y, si tenía `CheckOut`, el de salida, conservando fechas y operador.
- [ ] Permisos `access.register` y `access.void`.

## Diseño técnico

- Sin `UpdatedAt` ni `IsActive` en `AccessEvent`; a nivel de base de datos, un trigger rechaza `UPDATE` y `DELETE`.
- Los eventos guardan una copia del nombre y documento de la persona en el momento del acceso, para que el historial no cambie si luego se corrigen sus datos.
- "Está dentro" = visita con entrada y sin salida; no hay campo de estado.

## Verificación

Registrar entrada y salida de un visitante y de un residente; en la base hay cuatro eventos y
ninguno fue actualizado. Anular una entrada equivocada: quedan el original y la anulación. Las
visitas anteriores a la migración muestran sus horas originales.
