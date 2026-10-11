# HU-119 · Portal de propietario y residente

**Épica:** E7 · **Etapa:** 2 Unidades y personas · **Estado:** Hecha · **Depende de:** HU-117, HU-111
**Requerimiento:** §1.2, §2 criterios 3 y 4, §3 (menús Propietario y Residente)

## Historia

Como propietario, quiero ver mis propiedades y gestionar a quienes viven en ellas; como residente,
quiero consultar mi vinculación; en ambos casos sin poder ver ni tocar unidades ajenas.

## Criterios de aceptación

- [x] "Mis propiedades" (propietario): unidades con titularidad vigente, con su estado de ocupación.
- [x] "Mis residentes" (propietario): ocupantes de sus unidades; puede registrar un ocupante o arrendatario, actualizar sus datos de contacto y solicitar la finalización.
- [x] Según la política "El propietario gestiona sus ocupantes" (HU-114): *no* → solo consulta; *con aprobación* → lo que registra queda pendiente hasta que el administrador lo aprueba o rechaza con motivo; *directo* → queda vigente.
- [x] El administrador tiene una bandeja "Solicitudes pendientes" con aprobar / rechazar.
- [x] "Mi residencia" (residente o arrendatario): su unidad, relación, vigencia y convivientes (solo nombres).
- [x] El alcance se aplica en el servidor: cada consulta y cada escritura de estos endpoints se limita a las unidades donde el usuario tiene titularidad u ocupación **vigente**. Pedir o modificar otra unidad responde 404.
- [x] Un propietario con el permiso adicional `units.manage_any` puede actuar sobre otras unidades (§2 criterio 4).
- [x] Al terminar la titularidad u ocupación, la unidad desaparece del portal en la siguiente recarga; el usuario conserva su cuenta.
- [x] Un usuario que es propietario de una unidad y arrendatario de otra ve ambas, cada una con las acciones que le corresponden.
- [x] El propietario no residente no ve los accesos personales de sus arrendatarios.

## Diseño técnico

- `IUnitScope.GetAccessibleUnitIds(userId, mode)` como único punto que decide el alcance; lo usan también HU-121, HU-124 y HU-129.
- Endpoints bajo `api/my/...`, separados de los administrativos, para que el filtro de alcance no sea opcional.
- Reemplaza a `OwnersController`.

## Verificación

Prueba de integración: propietario A pide por id la unidad de B (404) e intenta registrar un
ocupante en ella (404). Con política "con aprobación": el ocupante no aparece como vigente hasta
aprobarse.

## Cómo quedó

- Endpoints `api/my/...` aparte de los administrativos; todo pasa por `IUnitScope`, que solo devuelve unidades con titularidad u ocupación vigente. Una unidad ajena responde 404.
- "Mis propiedades" (propietario) y "Mi residencia" (residente) en el menú; solo las ve quien tiene ese permiso propio, no quien administra el conjunto.
- La política "El propietario gestiona sus ocupantes" decide si registra directo, con aprobación o no puede. La bandeja de solicitudes está en "Propietarios y residentes" y en la ficha de la unidad.
- Pendiente menor: el motivo del rechazo se pide con un cuadro del navegador, no con un panel propio.
