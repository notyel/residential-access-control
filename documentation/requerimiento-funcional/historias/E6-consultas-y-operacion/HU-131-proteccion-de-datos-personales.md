# HU-131 · Protección de datos personales

**Épica:** E7 · **Etapa:** 6 Consultas y operación · **Estado:** Pendiente · **Depende de:** HU-109, HU-112
**Requerimiento:** §7.3, §2 criterio 3, §8.6

## Historia

Como administrador del conjunto, quiero que los documentos de identidad, teléfonos y datos de
vehículos solo los vea quien los necesita, para cumplir con la protección de datos personales.

## Situación actual

Todos los DTO devuelven la persona completa. Un propietario recibe documento, teléfono y correo de
cada visitante de su residencia (`OwnersService`).

## Criterios de aceptación

- [ ] Los datos se clasifican: **básicos** (nombre, unidad, relación), **de contacto** (teléfono, correo) y **de identidad** (tipo y número de documento).
- [ ] Permisos separados `people.view_contact` y `people.view_identity`. Sin ellos, el servidor no envía el dato o lo envía enmascarado (documento `••••4321`); el enmascarado no se hace en el navegador.
- [ ] Portería ve el documento completo solo en el momento de validar un ingreso; en listas e historiales, enmascarado.
- [ ] Propietarios y residentes ven de sus visitantes el nombre y el documento enmascarado; no ven datos de contacto de otros ocupantes salvo los que ellos mismos registraron.
- [ ] Ver el documento completo desde una ficha ("Mostrar") queda en la bitácora de auditoría.
- [ ] Las exportaciones respetan el mismo enmascarado según los permisos de quien exporta.
- [ ] Los logs de la aplicación no contienen documentos, teléfonos, correos, contraseñas ni tokens.
- [ ] Texto de tratamiento de datos configurable por conjunto, mostrado al registrar un visitante y aceptado por los usuarios en su primer ingreso (se guarda fecha y versión).
- [ ] Periodo de retención configurable para datos de visitantes: pasado el plazo, la persona visitante se anonimiza (nombre y documento reemplazados) y los eventos de acceso se conservan.
- [ ] La revisión cubre todos los endpoints existentes; ninguno devuelve la entidad de base de datos directamente.

## Diseño técnico

- El enmascarado se aplica en un único punto al proyectar a DTO (`IPersonalDataPolicy.Apply(dto, currentUser)`), no en cada servicio.
- La retención se ejecuta en el mismo proceso periódico de HU-117.
- Qué norma aplica y qué plazos de retención usar es una decisión del conjunto y de su asesor legal; el sistema solo lo hace configurable.

## Verificación

Prueba de integración por endpoint que devuelve personas: sin `people.view_identity` ninguna
respuesta contiene un documento completo. Revisar el log tras un flujo completo de visita.
