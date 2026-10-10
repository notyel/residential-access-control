# HU-03 · Shell de aplicación responsive

**Épica:** E2 Shell · **Estado:** Hecha · **Depende de:** HU-01, HU-02

## Historia

Como usuario, quiero que el menú, la barra superior y el contenido ocupen exactamente la pantalla
y se adapten a cualquier tamaño, para aprovechar todo el espacio disponible.

## Criterios de aceptación

- [x] El layout mide `100dvh`; sidebar (232px) + columna principal con topbar fijo de 56px y `<main>` que ocupa el resto.
- [x] `<main>` no impone `max-width`; el relleno es 20px (12px en móvil). Se eliminan `.page-layout`/`.page-container` de 1200px.
- [x] ≥ 1024px: sidebar visible y colapsable desde el botón del topbar; el estado se recuerda.
- [x] < 1024px: sidebar como overlay con scrim; se cierra al navegar, con `Esc` y al tocar fuera; el foco queda atrapado mientras está abierto.
- [x] El menú del sidebar hace scroll interno si no cabe; "Cerrar sesión" queda siempre visible abajo.
- [x] El breakpoint se detecta con `BreakpointObserver` del CDK (no con `window.innerWidth`).
- [x] Enlace "Saltar al contenido" como primer elemento enfocable.

## Tareas técnicas

1. Reescribir `access-control-layout` con CSS grid (`grid-template-columns: auto minmax(0,1fr)`, `grid-template-rows: 56px minmax(0,1fr)`), signals para `sidebarOpen`/`isCompact` y `OnPush`.
2. Mover `header` y `sidebar` de `modules/access-control/components/layout/` a `shared/components/topbar` y `shared/components/sidebar` (son globales, no de la feature).
3. Sidebar: ítem activo con fondo `--app-primary-soft`, objetivos táctiles de 44px, `aria-current="page"`.
4. Overlay móvil con `cdkTrapFocus` y animación `transform` de 200ms (respetar `prefers-reduced-motion`).
5. Quitar el `min-height: calc(100vh - 80px)` y demás cálculos de alto.

## Archivos

`front/src/app/core/layouts/access-control-layout/*`,
`front/src/app/modules/access-control/components/layout/{header,sidebar}/*` → `front/src/app/shared/components/{topbar,sidebar}/*`,
`front/src/app/core/theme/_base.scss`.

## Verificación

A 1440, 1024 y 390px: sin scroll en `body`, sidebar se comporta según el breakpoint y el contenido
llega de borde a borde.
