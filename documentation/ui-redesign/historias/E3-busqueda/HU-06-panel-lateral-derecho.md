# HU-06 · Panel lateral derecho estándar

**Épica:** E3 Búsqueda · **Estado:** Hecha · **Depende de:** HU-03

## Historia

Como usuario, quiero que los filtros y las búsquedas de apoyo de cualquier formulario se abran
siempre en un panel desde la derecha, para tener un patrón único y no perder el contexto de la vista.

## Criterios de aceptación

- [x] Existe un contenedor reutilizable de panel: cabecera (título + cerrar), cuerpo con scroll interno y pie de acciones fijo.
- [x] Entra desde la derecha (420px; 100% del ancho en móvil) sobre un scrim; animación de 200ms, sin animación con `prefers-reduced-motion`.
- [x] Se cierra con `Esc`, el botón cerrar o el scrim; el foco queda atrapado y vuelve al disparador.
- [x] Cualquier componente puede abrirse dentro del panel y devolver un resultado al cerrarse (p. ej. filtros aplicados, residencia seleccionada).
- [x] Dos usos de referencia quedan definidos: **Filtros** (formulario + Limpiar/Aplicar) y **Selector** (campo de búsqueda + lista + Cancelar/Seleccionar).
- [x] Funciona en claro y oscuro.

## Diseño técnico

- `SidePanelService.open<T, R>(component, { title, data }): SidePanelRef<R>` sobre `Dialog` del CDK con `positionStrategy` global a la derecha y `panelClass: 'app-side-panel'`.
- `SidePanelComponent` (`shared/components/side-panel`) con proyección: `<ng-content>` para el cuerpo y `[sidePanelFooter]` para el pie.
- `SidePanelRef.close(result)` y `afterClosed()` como observable.

```ts
const ref = this.sidePanel.open(VisitFiltersComponent, { title: 'Filtros', data: this.filters() });
ref.afterClosed().subscribe((filters) => filters && this.applyFilters(filters));
```

## Archivos

`front/src/app/shared/components/side-panel/*`, `front/src/app/core/services/side-panel.service.ts`,
`front/src/app/core/theme/_overlays.scss`.

## Verificación

Abrir un panel de prueba desde el topbar, recorrerlo con `Tab`, cerrar con `Esc` y comprobar que
el foco vuelve al botón.
