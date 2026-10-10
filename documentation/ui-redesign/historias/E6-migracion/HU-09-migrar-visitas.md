# HU-09 · Migrar Visitas

**Épica:** E6 Migración · **Estado:** Hecha · **Depende de:** HU-04, HU-06, HU-07

## Historia

Como guarda, quiero el listado de visitas en el nuevo formato, para ver más registros por pantalla
y filtrar sin que los filtros ocupen espacio permanente.

## Criterios de aceptación

- [x] Sin `app-page-header`: título "Visitas" y acción "Registrar visita" (roles Admin/Guard) en el navbar.
- [x] El botón de filtros del navbar muestra el número de filtros activos y abre el panel derecho con fecha de inicio, fecha de fin y demás filtros disponibles.
- [x] Los filtros activos se muestran como chips removibles sobre la tabla; "Aplicar" recarga la página 1.
- [x] La tabla usa `app-data-table` con las columnas existentes (Visitante, Documento, Fecha, Acciones) y las que el API ya entregue (residencia, placa, estado).
- [x] Carga, vacío y error implementados.
- [x] La paginación del servidor sigue funcionando (`pageIndex`, `pageSize`, `totalCount`).

## Tareas técnicas

1. Crear `visits/components/visit-filters` (formulario reactivo dentro del panel lateral).
2. Reescribir `visits.component.html/.scss/.ts`: signals para filtros y página, `OnPush`.
3. Eliminar `.filter-form`/`.filter-grid` de `_base.scss` si ya no se usan.

## Archivos

`front/src/app/modules/access-control/visits/pages/visits/*`,
`front/src/app/modules/access-control/visits/components/visit-filters/*`,
`front/src/app/modules/access-control/visits/visits.routes.ts`.

## Verificación

Filtrar por rango de fechas, cambiar de página, quitar un chip; comparar contra el diseño de referencia.
