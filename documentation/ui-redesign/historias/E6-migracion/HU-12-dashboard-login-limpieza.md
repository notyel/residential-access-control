# HU-12 · Migrar Dashboard, Login y limpieza final

**Épica:** E6 Migración · **Estado:** Hecha con pendientes · **Depende de:** HU-09, HU-10, HU-11

## Historia

Como equipo, queremos que las vistas restantes usen los nuevos fundamentos y que no quede código
del diseño anterior, para mantener una sola forma de construir pantallas.

## Criterios de aceptación

- [x] Dashboard: título en el navbar; tarjetas de indicadores en grilla fluida a todo el ancho; "Visitas recientes" con el estilo de tabla de HU-07; scroll interno.
- [ ] Login migrado a los tokens (compila; falta revisarlo visualmente en oscuro). `modules/visitors` no se tocó: es código sin ruta ni uso.
- [x] Se eliminan: `app-page-header`, `.page-layout`, `.page-container`, `.page-header`, `.page-title`, `.filter-*`, `.data-table`, `.recent-visits*`, `.visit-*` de `_base.scss`, y los alias de variables antiguas de HU-01.
- [x] No quedan referencias a `_variables.scss` antiguas ni componentes duplicados.
- [ ] `ng build` de producción compila con dos advertencias de presupuesto: bundle inicial 660 kB (límite 500 kB) y `login.component.scss` 4,2 kB (límite 2 kB).

## Tareas técnicas

1. Migrar `dashboard.component.*` y `stats-card`.
2. Migrar `login` y `visitors`.
3. Búsqueda global de clases y variables obsoletas y eliminación.
4. Actualizar este plan marcando todas las historias como hechas.

## Archivos

`front/src/app/modules/access-control/dashboard/**`, `front/src/app/modules/login/**`,
`front/src/app/modules/visitors/**`, `front/src/app/core/theme/*`,
`front/src/app/modules/access-control/components/page-header/*` (eliminar).

## Verificación

Recorrido completo de la aplicación en ambos temas y en los tres anchos de referencia.
