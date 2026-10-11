# HU-121 · Autorizaciones de visita

**Épica:** E7 · **Etapa:** 3 Visitantes y accesos · **Estado:** Hecha · **Depende de:** HU-120, HU-119
**Requerimiento:** §1.3, §5.1, §5.2, §3 (Autorizar visitantes, Solicitar visitas, Mis autorizaciones), §8.3

## Historia

Como propietario o residente, quiero autorizar por adelantado a mis visitantes, una vez, por un
periodo o de forma recurrente, para que portería los deje pasar sin llamarme.

## Criterios de aceptación

- [x] `VisitAuthorization`: visitante (persona existente o nombre + documento opcional), unidad de destino, persona que autoriza, motivo, vigencia y estado. Las tres primeras y la vigencia son obligatorias (§5.1).
- [x] Tres modalidades: **única** (un ingreso en una fecha, con ventana horaria opcional), **por periodo** (cualquier número de ingresos entre dos fechas), **recurrente** (días de la semana y franja horaria, entre dos fechas).
- [x] La vigencia no puede superar la política "Duración máxima de una autorización" ni la fecha de fin de la ocupación de quien autoriza.
- [x] Estados guardados: vigente, revocada, consumida (solo la única, al usarse). "Vencida" se calcula por fecha.
- [x] Quien autoriza solo puede elegir unidades donde tiene vínculo vigente (`IUnitScope` de HU-119). El residente puede autorizar solo si la política lo permite.
- [x] Quien la creó, otro ocupante autorizado de la misma unidad o el administrador pueden revocarla; se guarda quién, cuándo y por qué.
- [x] Una autorización no se edita: se revoca y se crea otra. Así el historial conserva lo que estaba autorizado en cada momento (§8.8).
- [x] "Autorizar visitantes" / "Solicitar visitas": formulario y lista de mis autorizaciones con filtros vigentes / vencidas / revocadas.
- [x] El administrador puede crear autorizaciones en nombre de una unidad indicando quién de la unidad la solicitó.
- [x] Función única `IsValidAt(autorización, instante)` que responde sí/no y el motivo (fuera de fecha, fuera de franja, revocada, ya usada, vínculo de quien autoriza terminado).
- [x] Permisos `authorizations.create_own`, `authorizations.manage`.

## Diseño técnico

- Recurrencia guardada como días de la semana + hora de inicio y fin; se evalúa en la zona horaria del conjunto.
- `IsValidAt` es función pura con pruebas unitarias exhaustivas: límites de fecha, franja que cruza medianoche, cambio de día por zona horaria.
- Al finalizar una ocupación (HU-117) se revocan las autorizaciones emitidas por esa persona para esa unidad.

## Verificación

Crear una de cada modalidad. Pruebas unitarias de `IsValidAt`. Finalizar la ocupación del residente
que autorizó: sus autorizaciones quedan revocadas con motivo "vinculación finalizada".

## Cómo quedó

- `VisitAuthorization` con tres modalidades (única, por periodo, recurrente con días de la semana y franja horaria, que puede cruzar la medianoche).
- `AuthorizationRules.IsValidAt` es la única función que decide si una autorización permite ingresar en un instante, y por qué no; tiene pruebas unitarias propias (`AuthorizationRulesTests`).
- Reglas aplicadas en el servidor: unidad con vínculo vigente de quien autoriza, duración máxima de la política, fin de la ocupación de quien autoriza, y la política "El residente puede autorizar visitas".
- No se editan: se revocan (quién, cuándo, por qué) y se crea otra. Al terminar la vinculación de quien autorizó, sus autorizaciones se revocan solas.
- Portal: "Autorizar visitantes" (`/access-control/my/authorizations`). Administración: "Autorizaciones", donde se crean en nombre de alguien de la unidad.
- Los permisos quedaron como `authorizations.read`, `authorizations.write` y `authorizations.write_own` (en lugar de `manage` / `create_own`) para seguir la regla general "X implica X_own".
