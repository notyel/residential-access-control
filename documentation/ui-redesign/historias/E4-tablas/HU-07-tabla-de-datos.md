# HU-07 · Tabla de datos moderna

**Épica:** E4 Tablas · **Estado:** Hecha con pendientes · **Depende de:** HU-01, HU-02

## Historia

Como usuario, quiero tablas limpias que ocupen todo el alto disponible, con encabezado fijo y
paginación siempre visible, para leer muchos registros sin perder las columnas ni los controles.

## Criterios de aceptación

- [x] La tabla vive en una tarjeta que llena el alto de `<main>`: barra superior (filtros activos como chips + conteo), cuerpo con scroll interno y pie con paginación.
- [x] Encabezado fijo (`position: sticky`), 40px, texto en mayúsculas pequeñas sobre `--app-surface-2`.
- [x] Filas de 56px, separador de 1px, resaltado al pasar el cursor y foco visible; sin sombra `mat-elevation-z8`.
- [x] Celdas con tipografía monoespaciada y cifras tabulares para documentos, placas y horas.
- [x] Estados con `status-badge` (color + forma del indicador, no solo color).
- [ ] Columna de acciones: hecha como botón de 44px con `aria-label` (Residentes → editar). Pendiente: menú (`mat-menu`) cuando una fila tenga más de una acción; Visitas aún no tiene acciones por fila.
- [x] Estados de carga (filas esqueleto), vacío (mensaje + acción) y error (mensaje + reintentar).
- [x] Entre 821px y el ancho mínimo de la tabla hay scroll horizontal interno; a ≤ 820px cada fila se presenta como tarjeta de dos líneas y se ocultan las columnas secundarias.
- [x] El paginador muestra rango, filas por página y anterior/siguiente; en móvil solo rango y flechas.

## Diseño técnico

- Tabla nativa `<table class="app-table">` con `@for` (se retiró `mat-table`: no aportaba nada que la tabla no use y complicaba los estilos y la vista de tarjetas); estilos en `core/theme/_table.scss`. Paginador propio `app-paginator` (`shared/components/paginator`), en español.
- `DataTableComponent` (`shared/components/data-table`) como contenedor con proyección de contenido: `[tableToolbar]`, la tabla, `[tableEmptyAction]` y `[tableFooter]`; entradas `loading`, `empty`, `error`.
- `StatusBadgeComponent` (`shared/components/status-badge`) con variantes `success | neutral | warning | danger`.
- Vista de tarjetas en móvil con container queries sobre el contenedor de la tabla.

## Archivos

`front/src/app/core/theme/_table.scss`, `front/src/app/shared/components/data-table/*`,
`front/src/app/shared/components/status-badge/*`.

## Verificación

Con más de 30 filas: el encabezado y el paginador permanecen visibles al hacer scroll; revisar
claro/oscuro y 390px.
