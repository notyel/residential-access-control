# HU-124 · Inventario de vehículos

**Épica:** E7 · **Etapa:** 4 Vehículos · **Estado:** Hecha con pendientes · **Depende de:** HU-116, HU-117
**Requerimiento:** §1.4, §3 (Vehículos, Mis vehículos), §7.6, §8.4

## Historia

Como administrador, quiero un inventario de los vehículos autorizados y a quién pertenecen; como
propietario o residente, quiero registrar los míos.

## Criterios de aceptación

- [x] `Vehicle`: placa, tipo (automóvil, motocicleta, bicicleta, otro — catálogo configurable), marca, color, modelo y notas opcionales, clase (**permanente** o **visitante**).
- [x] La placa se guarda normalizada (mayúsculas, sin espacios ni guiones) y es única por conjunto; los tipos sin placa (bicicleta) usan un identificador interno.
- [x] Un vehículo permanente se asocia a una unidad y a una persona responsable con vínculo vigente en ella, mediante `VehicleAuthorization` con fechas de inicio y fin y estado (pendiente, vigente, suspendida, revocada).
- [x] Cambiar un vehículo de unidad o de responsable cierra la autorización anterior y abre otra; el historial se conserva.
- [x] No puede haber dos autorizaciones vigentes del mismo vehículo; se respeta la política "Vehículos máximos por unidad".
- [x] Un vehículo visitante no tiene autorización permanente: se crea al registrar una visita y queda ligado a ella (HU-125).
- [ ] Pantalla "Vehículos": lista con búsqueda por placa y filtros por unidad, tipo, clase y estado; alta, edición, suspender y revocar con motivo. **Parcial:** búsqueda y filtros por tipo, clase y estado; el filtro por unidad existe en el API pero no en la pantalla.
- [x] "Mis vehículos" (propietario y residente): ver y registrar vehículos de sus unidades, según la política *no / con aprobación / directo*; las solicitudes van a la bandeja de pendientes de HU-119.
- [x] Al finalizar la ocupación del responsable (HU-117) se revocan sus autorizaciones de vehículo en esa unidad.
- [x] La ficha de unidad (HU-118) muestra la sección "Vehículos".
- [x] `GET vehicles/by-plate/{placa}` responde: registrado o no, unidad, responsable y si su autorización está vigente.
- [x] Permisos `vehicles.read`, `vehicles.write`, `vehicles.manage_own`.

## Diseño técnico

- Columna `PlateNormalized` con índice único `(ComplexId, PlateNormalized)`.
- Novedades del vehículo (suspensión, cambio de color, observaciones) como entradas con fecha y autor, no como texto que se sobrescribe.

## Verificación

Registrar un carro para un residente y una moto para un propietario. Registrar la misma placa con
otro formato ("abc-123" / "ABC 123") da duplicado. Finalizar la ocupación del residente: su
vehículo queda revocado y la consulta por placa lo indica.

## Cómo quedó

- `Vehicle` con placa normalizada (mayúsculas, sin espacios ni guiones) e índice único por conjunto; una bicicleta puede no tener placa y recibe un identificador interno.
- `VehicleAuthorization` une el vehículo con una unidad y un responsable con vínculo vigente. Un índice único parcial en la base impide dos autorizaciones en curso del mismo vehículo. Reasignar cierra una y abre otra; suspender y revocar exigen motivo y dejan una novedad con fecha y autor.
- Se respeta "Vehículos máximos por unidad". Al terminar la vinculación del responsable, sus vehículos en esa unidad quedan revocados.
- Pantallas: "Vehículos" (inventario con filtros, alta, edición, reasignación, bandeja de solicitudes), "Mis vehículos" (portal, según la política no / con aprobación / directo) y sección "Vehículos" en la ficha de unidad.
- `GET vehicles/by-plate/{placa}` responde si está registrado, de qué unidad, quién responde y si puede entrar hoy.
- El permiso del portal se llama `vehicles.write_own` (no `manage_own`), por la regla "X implica X_own".
- Pruebas: `VehicleTests`.
