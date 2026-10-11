# HU-131 · Protección de datos personales

**Épica:** E7 · **Etapa:** 6 Consultas y operación · **Estado:** Hecha con pendientes · **Depende de:** HU-109, HU-112
**Requerimiento:** §7.3, §2 criterio 3, §8.6

## Historia

Como administrador del conjunto, quiero que los documentos de identidad, teléfonos y datos de
vehículos solo los vea quien los necesita, para cumplir con la protección de datos personales.

## Situación actual

Todos los DTO devuelven la persona completa. Un propietario recibe documento, teléfono y correo de
cada visitante de su residencia (`OwnersService`).

## Criterios de aceptación

- [x] Los datos se clasifican: **básicos** (nombre, unidad, relación), **de contacto** (teléfono, correo) y **de identidad** (tipo y número de documento).
- [x] Permisos separados `people.view_contact` y `people.view_identity`. Sin ellos, el servidor no envía el dato o lo envía enmascarado (documento `••••4321`); el enmascarado no se hace en el navegador.
- [x] Portería ve el documento completo solo en el momento de validar un ingreso; en listas e historiales, enmascarado.
- [x] Propietarios y residentes ven de sus visitantes el nombre y el documento enmascarado; no ven datos de contacto de otros ocupantes salvo los que ellos mismos registraron.
- [x] Ver el documento completo desde una ficha ("Mostrar") queda en la bitácora de auditoría.
- [x] Las exportaciones respetan el mismo enmascarado según los permisos de quien exporta.
- [ ] Los logs de la aplicación no contienen documentos, teléfonos, correos, contraseñas ni tokens. **Sin verificar:** no se registran datos personales a propósito, pero falta una revisión sistemática del log tras un flujo completo (en desarrollo, EF Core puede escribir parámetros si se activa el registro sensible).
- [ ] Texto de tratamiento de datos configurable por conjunto, mostrado al registrar un visitante y aceptado por los usuarios en su primer ingreso (se guarda fecha y versión). **Pendiente:** no hay texto configurable ni aceptación con fecha y versión.
- [ ] Periodo de retención configurable para datos de visitantes: pasado el plazo, la persona visitante se anonimiza (nombre y documento reemplazados) y los eventos de acceso se conservan. **Parcial:** la persona visitante se anonimiza; los eventos de acceso conservan el nombre y el documento del momento, porque son inmutables. Decidir con el asesor legal si deben anonimizarse también.
- [ ] La revisión cubre todos los endpoints existentes; ninguno devuelve la entidad de base de datos directamente. **Parcial:** todos los endpoints devuelven DTO, no entidades; el enmascarado cubre personas, visitas, historial y exportaciones. Los nombres en fichas de unidad, autorizaciones y vehículos son datos básicos y salen completos.

## Diseño técnico

- El enmascarado se aplica en un único punto al proyectar a DTO (`IPersonalDataPolicy.Apply(dto, currentUser)`), no en cada servicio.
- La retención se ejecuta en el mismo proceso periódico de HU-117.
- Qué norma aplica y qué plazos de retención usar es una decisión del conjunto y de su asesor legal; el sistema solo lo hace configurable.

## Verificación

Prueba de integración por endpoint que devuelve personas: sin `people.view_identity` ninguna
respuesta contiene un documento completo. Revisar el log tras un flujo completo de visita.

## Cómo quedó

- `IPersonalDataPolicy` es el único punto que enmascara: sin `people.view_identity` el documento sale como `••••4321`; sin `people.view_contact` el teléfono y el correo no salen. Se aplica en el servidor a personas, visitas, historial de accesos y exportaciones.
- Portería ve el documento completo solo al validar un ingreso (búsqueda de "Validar visitantes"); en listas e historiales, enmascarado. Propietarios, residentes y auditor, siempre enmascarado.
- Abrir la ficha de una persona con el documento completo queda en la bitácora (`person.identity_viewed`). Quien solo lo ve enmascarado no puede cambiarlo al editar.
- Retención: política "Retención de datos de visitantes (días)"; pasado el plazo desde su último acceso, el proceso periódico anonimiza a quien solo fue visitante.
