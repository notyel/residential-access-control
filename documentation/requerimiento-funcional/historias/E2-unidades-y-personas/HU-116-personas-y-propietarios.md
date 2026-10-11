# HU-116 · Personas y propietarios de unidades

**Épica:** E7 · **Etapa:** 2 Unidades y personas · **Estado:** Hecha con pendientes · **Depende de:** HU-115
**Requerimiento:** §1.1, §1.2, §4.1, §4.2, §4.5, §4.7, §4.8, §8.1, §8.8

## Historia

Como administrador del conjunto, quiero registrar a los propietarios y vincularlos con sus unidades,
para saber de quién es cada inmueble hoy y de quién fue antes.

## Criterios de aceptación

- [x] `Person` es el registro único de cualquier persona (propietario, ocupante, visitante): nombres, tipo y número de documento, teléfono, correo. Documento único por conjunto.
- [x] Tipo de documento como catálogo (CC, CE, TI, pasaporte, NIT…); `Person.PersonType` desaparece: el papel de una persona sale de sus vínculos, no de un campo. (Cierra el pendiente de HU-10 del rediseño.)
- [x] `User` enlaza opcionalmente a una `Person`. Una persona puede existir sin cuenta, y tener cuenta no le da por sí mismo ningún vínculo con una unidad (§4.8).
- [x] `UnitOwnership`: persona, unidad, fecha de inicio, fecha de fin (vacía = vigente), porcentaje opcional.
- [x] Una unidad admite varios propietarios vigentes si la política lo permite (HU-114); una persona puede ser propietaria de varias unidades.
- [x] Cambio de propietario: cierra la titularidad anterior con fecha de fin y crea la nueva; nunca se edita la persona de una titularidad existente.
- [x] No se permiten dos titularidades vigentes de la misma persona sobre la misma unidad, ni fecha de fin anterior a la de inicio.
- [x] Pantalla "Propietarios y residentes": directorio de personas con sus unidades y relación; alta de propietario buscando primero por documento para no duplicar.
- [x] Desde el propietario se puede "Dar acceso a la plataforma": crea el usuario invitado con rol Propietario (HU-110).
- [x] Permisos `people.read`, `people.write`, `ownership.write`.
- [ ] Migración: cada usuario con rol Propietario genera una `Person`, y una `UnitOwnership` vigente por cada residencia que tenía. `User.ApartmentNumber`, `User.FirstName` y `User.LastName` se eliminan (el nombre sale de la persona). La pantalla "Residentes" se reemplaza por esta. Hecho salvo la eliminación de columnas: `User` conserva `FirstName`, `LastName` y `ApartmentNumber` (ver abajo).

## Diseño técnico

- Restricción de exclusión en PostgreSQL sobre `(UnitId, PersonId, rango de fechas)` para impedir solapamientos aunque haya peticiones simultáneas.
- "Propietario residente / no residente" no se guarda: es residente si además tiene una ocupación vigente en la unidad (HU-117).
- Frontend: `modules/access-control/people`; el formulario reutiliza la búsqueda de persona de Registrar visita.

## Verificación

Registrar una unidad con dos propietarios; transferirla a un tercero y ver en el historial las tres
titularidades con fechas. Una misma persona figura como propietaria de dos unidades. Criterio
transversal §8.1 demostrable.

## Cómo quedó

- `Person` es el registro único; `PersonType` se eliminó y el tipo de documento sale del catálogo. `User.PersonId` enlaza la cuenta con la persona; son independientes.
- `UnitOwnership` con fechas; "Cambiar propietario" cierra las vigentes el día anterior y abre las nuevas. Una restricción de exclusión en PostgreSQL impide dos titularidades de la misma persona que se crucen en la misma unidad, también ante peticiones simultáneas.
- Pantalla "Propietarios y residentes" (directorio, ficha de persona, "Dar acceso a la plataforma"). La pantalla "Residentes" se eliminó.
- **Migración de datos:** cada usuario que era dueño de una residencia quedó con una persona y una titularidad vigente. Como no se conocía su documento, la persona tiene uno provisional (tipo NA, número `USR-…`); su ficha avisa que hay que completarlo.
- **No se hizo:** quitar `FirstName`, `LastName` y `ApartmentNumber` de `User`. Hay cuentas que no corresponden a una persona del conjunto (administrador, portería, superadministrador) y necesitan nombre propio; `ApartmentNumber` ya no se usa en ninguna pantalla y se puede retirar más adelante.
