# HU-122 · Validación en portería, rechazos y accesos excepcionales

**Épica:** E7 · **Etapa:** 3 Visitantes y accesos · **Estado:** Hecha con pendientes · **Depende de:** HU-121, HU-114
**Requerimiento:** §1.3, §1.6, §5.2, §5.3, §5.7, §7.9, §3 (Validar visitantes, Registrar ingreso y salida)

## Historia

Como guarda, quiero que al buscar a un visitante el sistema me diga si puede entrar y por qué, y
dejar constancia cuando lo rechazo o cuando entra por excepción, para decidir rápido y con respaldo.

## Criterios de aceptación

- [x] Pantalla "Validar visitantes": un solo campo que busca por documento, nombre o unidad y muestra las autorizaciones que coinciden con su resultado: **puede ingresar** o **no puede** con el motivo (vencida, revocada, fuera de franja, ya usada).
- [x] Con autorización vigente: "Registrar ingreso" crea la visita y el evento de entrada enlazados a la autorización; una autorización única pasa a consumida.
- [x] El servidor vuelve a evaluar la vigencia al registrar; una autorización que no es válida en ese instante no produce un ingreso normal, aunque la pantalla estuviera abierta desde antes (§5.2).
- [x] Sin autorización previa, según la política del conjunto (§5.3): *no permitidas* → solo queda rechazar o acceso excepcional; *con confirmación del residente* → el guarda registra qué ocupante de la unidad confirmó y por qué medio (citófono, teléfono, presencial); *a criterio de portería* → el guarda aprueba y queda como aprobador.
- [x] La persona que confirma debe ser un ocupante o propietario vigente de la unidad de destino.
- [x] Toda visita guarda el resultado de la verificación: con autorización previa, confirmada por residente, aprobada por portería o excepcional.
- [x] "Rechazar ingreso": crea un evento de rechazo con persona (o nombre libre), unidad, motivo (catálogo + texto) y operador. No crea visita.
- [x] "Acceso excepcional": permite el ingreso sin autorización válida solo con el permiso `access.exception`; exige motivo escrito (mínimo 10 caracteres) y guarda quién lo autorizó. Si quien autoriza no es el operador (p. ej. el administrador por teléfono), se identifica a ambos.
- [x] El documento del visitante es obligatorio u opcional según la política.
- [ ] Rapidez en portería: el flujo con autorización vigente se completa con teclado (buscar → Enter → Enter); el foco inicial está en el buscador; funciona a 390 px. **Parcial:** el buscador del panel de portería recibe el foco y lleva a la validación con Enter, y la pantalla funciona en un teléfono; falta completar el ingreso solo con teclado (hoy hay que pulsar "Registrar ingreso").
- [x] La pantalla "Registrar visita" actual se integra aquí como el camino "sin autorización previa".

## Diseño técnico

- `POST access/check-in` recibe `authorizationId` o los datos de verificación; la regla vive en `IAccessDecisionService`, que usa `IsValidAt` (HU-121) y las políticas (HU-114).
- Rechazos y excepcionales son `AccessEvent` (HU-120) con campos `Reason`, `ApprovedById`, `ConfirmedByPersonId`, `ConfirmationChannel`.

## Verificación

Pruebas de integración por cada rama: autorización vigente, vencida, revocada, sin autorización bajo
cada valor de la política, excepcional sin permiso (403) y sin motivo (400). Recorrido manual con
rol Portería en móvil.

## Cómo quedó

- "Validar visitantes" (`/access-control/gate`): un solo campo busca por documento, nombre o unidad y muestra autorizaciones (con el veredicto del servidor: puede ingresar o por qué no), residentes y personas ya registradas. Sin búsqueda muestra los esperados de hoy y quién está dentro.
- El ingreso se registra en un panel lateral; el servidor vuelve a evaluar la autorización al guardar (`CheckIn`), así que una vencida o revocada responde 409 aunque la pantalla estuviera abierta.
- Sin autorización previa manda la política del conjunto: no permitidas (solo rechazo o excepción), con confirmación de un ocupante vigente de la unidad (se guarda quién y por qué medio) o a criterio de portería.
- Rechazo: evento propio con motivo de catálogo, sin crear visita. Acceso excepcional: exige `access.exception` y motivo de 10 caracteres o más; queda el responsable.
- El documento del visitante es obligatorio u opcional según la política; sin documento se asigna un identificador interno.
- La antigua pantalla "Registrar visita" se eliminó: es el botón "Visitante sin autorización" de esta pantalla.
