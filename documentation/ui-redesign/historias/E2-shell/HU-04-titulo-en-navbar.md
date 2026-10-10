# HU-04 · Título y acciones de la vista en el navbar

**Épica:** E2 Shell · **Estado:** Hecha · **Depende de:** HU-03

## Historia

Como usuario, quiero ver el título de la vista y sus acciones principales en la barra superior,
para que el contenido (tablas y formularios) gane el alto que hoy ocupa el encabezado de página.

## Criterios de aceptación

- [x] El topbar muestra: botón de menú, (opcional) botón volver, (opcional) miga de pan del padre, título (`<h1>`), subtítulo en gris solo si hay espacio, y a la derecha las acciones de la vista, tema y usuario.
- [x] El título proviene de la ruta (`title`/`data` del `Route`) y también actualiza `document.title`.
- [x] Una página puede declarar acciones (p. ej. "Registrar visita", "Guardar", "Cancelar") que se pintan en el topbar y respetan roles (`appIfRole`).
- [x] Al salir de la página, sus acciones desaparecen del topbar.
- [x] En móvil el subtítulo y la miga se ocultan y las acciones secundarias muestran solo icono (con `aria-label`).
- [x] `app-page-header` deja de usarse y se elimina al terminar la migración (HU-12).

## Diseño técnico

- `PageHeaderService` (`core/services`): signals `title`, `subtitle`, `backUrl`, `parent`, y `actions: TemplateRef | null`.
- Título/subtítulo/volver: se leen del `data` de la ruta activa al evento `NavigationEnd` (una `TitleStrategy` propia mantiene `document.title`).
- Acciones: directiva estructural `*appPageActions` que registra su `TemplateRef` en el servicio en `ngOnInit` y lo limpia en `ngOnDestroy`; el topbar lo pinta con `ngTemplateOutlet`. Así la página conserva sus handlers y formularios sin acoplarse al topbar.

```html
<!-- en una página -->
<ng-template appPageActions>
  <a mat-flat-button routerLink="register" *appIfRole="['Admin', 'Guard']">Registrar visita</a>
</ng-template>
```

## Tareas técnicas

1. Crear `PageHeaderService`, `AppTitleStrategy` y la directiva `PageActionsDirective`.
2. Añadir `title` y `data: { subtitle, backUrl, parent }` en `visits.routes.ts`, `residents.routes.ts`, `dashboard.routes.ts`.
3. Actualizar el topbar para consumir el servicio.

## Archivos

`front/src/app/core/services/page-header.service.ts`, `front/src/app/shared/directives/page-actions.directive.ts`,
`front/src/app/shared/components/topbar/*`, `front/src/app/modules/access-control/**/**.routes.ts`, `front/src/app/core/config/app.config.ts`.

## Verificación

Navegar Visitas → Registrar visita → volver: título, botón volver y acciones cambian sin residuos.
