# HU-118 · Ficha de unidad e historial

**Épica:** E7 · **Etapa:** 2 Unidades y personas · **Estado:** Pendiente · **Depende de:** HU-117
**Requerimiento:** §1.1 (consultar propietarios, ocupantes, vehículos y parqueaderos; historial), §8.8

## Historia

Como administrador o guarda, quiero abrir una unidad y ver de un vistazo quién es el dueño, quién
vive allí y qué tiene asociado, para responder consultas sin recorrer varias pantallas.

## Criterios de aceptación

- [ ] Ficha de unidad con: datos de la unidad y estado de ocupación calculado; propietarios vigentes; ocupantes vigentes con su relación y vigencia.
- [ ] Sección "Historial": línea de tiempo con titularidades y ocupaciones anteriores (quién, desde, hasta, quién lo registró).
- [ ] Secciones "Vehículos", "Parqueaderos" y "Visitas recientes": se añaden cuando existan HU-124, HU-126 y HU-123; hasta entonces no se muestran.
- [ ] Acciones en la ficha según permiso: agregar o cambiar propietario, agregar ocupante, finalizar ocupación.
- [ ] Vista para portería (`units.read` sin `people.read`): solo nombres y relación de los ocupantes vigentes, sin documento, teléfono ni historial.
- [ ] `GET units/{id}/summary` entrega todo en una sola llamada.
- [ ] La ficha de persona muestra el inverso: todas sus unidades y la relación con cada una, vigentes y pasadas.
- [ ] Desde la lista de unidades y desde el buscador por comando (`Ctrl K`) se llega a la ficha escribiendo el código.

## Diseño técnico

- Ruta `units/:id`; secciones como componentes independientes que cada historia posterior agrega.
- Proveedor de búsqueda de unidades y de personas para la paleta de comandos, en servidor (cierra el pendiente de HU-05 del rediseño).

## Verificación

Tras un cambio de propietario y un arrendamiento finalizado, la ficha muestra el estado actual
arriba y ambos hechos en el historial. Con rol Portería no aparecen documentos ni teléfonos.
