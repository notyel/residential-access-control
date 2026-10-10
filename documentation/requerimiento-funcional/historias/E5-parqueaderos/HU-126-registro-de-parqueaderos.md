# HU-126 · Registro de parqueaderos

**Épica:** E7 · **Etapa:** 5 Parqueaderos · **Estado:** Pendiente · **Depende de:** HU-115, HU-116
**Requerimiento:** §1.5, §6 (tipos, estados, criterios 1 y 2), §8.5

## Historia

Como administrador, quiero registrar cada espacio de parqueo con su tipo y a quién pertenece, para
tener el inventario sobre el que se controlará la ocupación.

## Criterios de aceptación

- [ ] `ParkingSpace`: código, ubicación (zona, sótano o nivel — texto o estructura de HU-115), **tipo** (privado, comunitario, visitantes), tipo de vehículo admitido (carro, moto, cualquiera), notas.
- [ ] El código es único dentro del conjunto (restricción en base de datos).
- [ ] Tipo y estado son conceptos separados (§6): el tipo es un dato del espacio; el estado operativo (disponible, ocupado, reservado) **no se guarda**, se calcula en HU-128.
- [ ] El único estado que se registra a mano es **fuera de servicio**, como un periodo con fecha de inicio, fin opcional y motivo; al terminar, el espacio vuelve solo a su estado calculado.
- [ ] Un espacio privado se asocia a una unidad y registra su titular o asignatario (persona), con fecha de inicio y fin; puede existir privado sin asignar.
- [ ] Reasignar un privado cierra la asociación anterior y crea otra; el historial se conserva (§8.8).
- [ ] Un espacio privado nunca aparece en las listas de disponibles para visitantes.
- [ ] Cambiar el tipo de un espacio exige que no tenga asignaciones ni ocupaciones vigentes.
- [ ] Pantalla "Parqueaderos": lista con filtros por tipo, ubicación y estado; alta individual y en lote por rango de códigos (p. ej. V-01 a V-20).
- [ ] Un espacio no se borra; se da de baja y conserva su historial.
- [ ] La ficha de unidad (HU-118) muestra la sección "Parqueaderos" con sus privados.
- [ ] "Mis parqueaderos" (propietario): los privados de sus unidades.
- [ ] Permisos `parking.read`, `parking.write`.

## Diseño técnico

- Índice único `(ComplexId, Code)`.
- La asociación de un privado con su unidad es una `ParkingAssignment` de clase *titular* (misma tabla que usa HU-127), así toda relación espacio–beneficiario–periodo vive en un solo lugar.
- `ParkingOutOfService (SpaceId, From, To, Reason)`.

## Verificación

Crear 2 privados, 3 comunitarios y 5 de visitantes; asociar un privado a una unidad, reasignarlo y
ver ambas asociaciones en su historial. Código repetido → 409.
