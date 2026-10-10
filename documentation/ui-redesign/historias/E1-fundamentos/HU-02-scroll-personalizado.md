# HU-02 · Scroll interno personalizado

**Épica:** E1 Fundamentos · **Estado:** Hecha con pendientes · **Depende de:** HU-01

## Historia

Como usuario, quiero que el desplazamiento ocurra dentro de cada región (menú, tabla, formulario,
panel) con barras discretas y coherentes con el tema, para no perder de vista el navbar ni las acciones.

## Criterios de aceptación

- [x] `html` y `body` no hacen scroll (`overflow: hidden`, alto `100dvh`) dentro del layout autenticado.
- [x] Existe una utilidad `.app-scroll` (y mixin `app-scroll()`) que aplica barras finas con el color del token `--app-scrollbar`, pista transparente y radio completo.
- [x] Funciona en Chromium/Edge, Firefox y Safari (`scrollbar-width`/`scrollbar-color` + `::-webkit-scrollbar`).
- [ ] Las regiones con scroll conservan `overscroll-behavior: contain` (hecho). Pendiente: `tabindex="0"` explícito en regiones sin elementos enfocables (hoy se depende de que el navegador las haga enfocables).
- [x] Los overlays de Material/CDK (select, datepicker, menú) usan el mismo estilo de barra.

## Tareas técnicas

1. Crear `core/theme/_scrollbar.scss` con el mixin y la clase utilitaria; importarlo en `styles.scss`.
2. Aplicar el mixin a `.cdk-overlay-pane`, `.mat-mdc-select-panel`, `.mat-mdc-autocomplete-panel`.
3. Documentar el patrón de contenedor: padre `display:flex; flex-direction:column; min-height:0`, hijo con scroll `flex:1; min-height:0; overflow:auto`.

## Archivos

`front/src/app/core/theme/_scrollbar.scss`, `front/src/app/core/theme/styles.scss`, `front/src/app/core/theme/_reset.scss`.

## Verificación

Con una lista larga en Visitas, la barra del navegador no aparece y la barra interna cambia de
color al alternar el tema.
