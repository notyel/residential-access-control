# Rediseño de la interfaz — plan de trabajo

Plan para modernizar el frontend (`front/`, Angular 18 + Angular Material/CDK + lucide-angular).
Se resuelve historia por historia, en el orden indicado. Cada historia vive en su propio archivo
dentro de `historias/` y es entregable por separado (un commit o PR por historia).

Diseño de referencia: https://claude.ai/artifact/Dv1NU6dTWTaSn8tqyiwBg6
(Visitas claro/oscuro, móvil, búsqueda por comando, panel lateral, formulario de registro).

## Objetivos

1. **Aprovechar el espacio**: el contenido ocupa todo el ancho disponible (hoy `.page-layout` lo limita a 1200px).
2. **Título en el navbar**: el título, la navegación "volver" y las acciones de cada vista se muestran en la barra superior; desaparece el `app-page-header` dentro de la página.
3. **Búsqueda por comando**: la barra de búsqueda del header se reemplaza por un botón (`Ctrl K`) que abre un popup de búsqueda global.
4. **Panel lateral derecho estándar**: filtros y búsquedas de apoyo de cualquier formulario se abren en un panel que entra desde la derecha.
5. **Tablas modernas**: un único componente/estilo de tabla con encabezado fijo, scroll interno y paginación en el pie.
6. **Modo oscuro completo**: todo elemento (incluido Angular Material) responde a `data-theme`.
7. **Sin scroll del navegador**: la shell mide `100dvh`; solo hacen scroll las regiones internas, con barras personalizadas.
8. **Responsive**: el mismo layout funciona desde 360px (menú como overlay, tabla como tarjetas, formularios a una columna).

## Estado actual (hallazgos)

| Tema | Situación | Archivo |
| --- | --- | --- |
| Ancho | `.page-layout` y `.page-container` duplicados, `max-width: 1200px` | `core/theme/_base.scss` |
| Scroll | Layout con `min-height: 100vh`: hace scroll el `body` | `core/layouts/access-control-layout/*.scss` |
| Título | `app-page-header` repetido en cada página, consume ~90px de alto | `modules/access-control/components/page-header` |
| Búsqueda | `<input>` decorativo en el header, sin funcionalidad | `components/layout/header/header.component.html` |
| Tema oscuro | Variables propias con `[data-theme]`, pero el tema Material solo define `theme-type: light` | `core/theme/_material-theme.scss` |
| Tablas | `mat-table` con `mat-elevation-z8`, sin estilos propios; filtros en línea sobre la tabla | `visits/pages/visits` |
| Formularios | Una columna a ancho completo (`form-field-full`); contenedores distintos (`.page-layout` vs `.page-content`); mezcla de `mat-icon` y `lucide-icon` | `residents/pages/new-resident`, `visits/pages/register-visit` |
| Duplicados | Dos `ThemeToggleComponent` (shared y access-control) | `shared/components/theme-toggle`, `components/layout/theme-toggle` |

## Arquitectura de componentes objetivo

```
core/
  layouts/access-control-layout/   Shell: sidebar + topbar + <main> con scroll interno
  services/
    page-header.service.ts         Estado del topbar (título, subtítulo, volver, acciones) con signals
    command-palette.service.ts     Abre/cierra la paleta, registra proveedores de resultados
    side-panel.service.ts          Abre un componente en el panel derecho (CDK Overlay)
  theme/
    _tokens.scss                   Tokens semánticos claro/oscuro (reemplaza _variables.scss)
    _scrollbar.scss                Mixin + utilidad .app-scroll
    _table.scss                    Estilos de tabla
    _forms.scss                    Grillas y secciones de formulario
shared/components/                 Globales, sin lógica de negocio
  topbar/                          (mueve header) título, botón de búsqueda, acciones, tema, usuario
  sidebar/                         (mueve sidebar)
  command-palette/                 Popup de búsqueda
  side-panel/                      Contenedor del panel derecho (cabecera, cuerpo con scroll, pie)
  data-table/                      Contenedor de tabla (barra de filtros activos, cuerpo, paginador)
  form-section/                    Tarjeta de sección numerada de formulario
  status-badge/, theme-toggle/     (uno solo)
modules/<feature>/
  pages/                           Una carpeta por ruta; solo orquesta datos y compone globales
  components/                      Piezas propias de la feature (p. ej. visit-filters, resident-picker)
```

Reglas: componentes standalone con `ChangeDetectionStrategy.OnPush`, estado con signals, `inject()`
en vez de constructor, control flow nativo (`@if`, `@for`), sin colores literales en los `.scss`
de componentes (solo tokens).

## Orden de ejecución

| # | Historia | Épica | Depende de |
| --- | --- | --- | --- |
| HU-01 | [Tokens de diseño y tema oscuro completo](historias/E1-fundamentos/HU-01-tokens-y-tema-oscuro.md) | Fundamentos | — |
| HU-02 | [Scroll interno personalizado](historias/E1-fundamentos/HU-02-scroll-personalizado.md) | Fundamentos | HU-01 |
| HU-03 | [Shell de aplicación responsive](historias/E2-shell/HU-03-shell-responsive.md) | Shell | HU-01, HU-02 |
| HU-04 | [Título y acciones de la vista en el navbar](historias/E2-shell/HU-04-titulo-en-navbar.md) | Shell | HU-03 |
| HU-05 | [Búsqueda por comando](historias/E3-busqueda/HU-05-busqueda-por-comando.md) | Búsqueda | HU-03 |
| HU-06 | [Panel lateral derecho estándar](historias/E3-busqueda/HU-06-panel-lateral-derecho.md) | Búsqueda | HU-03 |
| HU-07 | [Tabla de datos moderna](historias/E4-tablas/HU-07-tabla-de-datos.md) | Tablas | HU-01, HU-02 |
| HU-08 | [Estructura estándar de formularios](historias/E5-formularios/HU-08-estructura-de-formularios.md) | Formularios | HU-01 |
| HU-09 | [Migrar Visitas](historias/E6-migracion/HU-09-migrar-visitas.md) | Migración | HU-04, HU-06, HU-07 |
| HU-10 | [Migrar Registrar visita](historias/E6-migracion/HU-10-migrar-registrar-visita.md) | Migración | HU-04, HU-06, HU-08 |
| HU-11 | [Migrar Residentes](historias/E6-migracion/HU-11-migrar-residentes.md) | Migración | HU-04, HU-07, HU-08 |
| HU-12 | [Migrar Dashboard, Login y limpieza](historias/E6-migracion/HU-12-dashboard-login-limpieza.md) | Migración | HU-09 a HU-11 |

## Estado de la implementación

Las doce historias están implementadas. Siete quedaron completas (HU-01, 03, 04, 06, 08, 09, 11)
y cinco con un pendiente menor anotado en su archivo como casilla sin marcar:

| Historia | Pendiente |
| --- | --- |
| HU-02 | `tabindex` explícito en regiones con scroll sin elementos enfocables |
| HU-05 | Búsquedas recientes y mensaje de error; buscar visitas o personas por nombre requiere backend |
| HU-07 | Menú de acciones por fila (hoy solo hay una acción: editar residente) |
| HU-10 | Catálogos de tipo de documento y tipo de persona (no existen en el backend) |
| HU-12 | Revisar el login en oscuro; presupuestos de `ng build` (bundle inicial y estilos del login) |

Cambios respecto al plan original:

- La tabla es un `<table class="app-table">` nativo con paginador propio, no `mat-table`/`MatPaginator`.
- Se retiró el tema precompilado `indigo-pink.css` de `angular.json`; el tema completo sale de `_material-theme.scss`.
- Se registró el locale `es-CO` y un `DateAdapter` que lee las fechas escritas como día/mes/año.
- Correcciones encontradas al migrar: `ResidentsService.getResident` no desenvolvía la respuesta
  del API (Editar residente abría vacío); la lista de residentes solo traía la primera página de 10;
  al registrar una persona nueva no se enviaba su número de documento.

Verificación hecha: `ng build` (desarrollo y producción) y recorrido en navegador de Visitas,
filtros, búsqueda por comando, Registrar visita, Residentes (lista, alta, edición) y Dashboard, en
claro y oscuro y a 1440/1280/390px. El recorrido usó datos simulados en el navegador porque el
navegador de pruebas no acepta el certificado local del API; falta una pasada contra el backend real.

## Definición de terminado (aplica a todas)

- `ng build` sin errores ni advertencias nuevas.
- Verificado en claro y oscuro, y en 1440px, 1024px y 390px de ancho.
- El `body` no muestra barra de scroll en ninguna vista del módulo.
- Navegable con teclado; foco visible; contraste de texto ≥ 4.5:1.
- Sin colores literales nuevos en `.scss` de componentes.
- La historia se marca como hecha en su archivo (`Estado: Hecha`) y en la tabla de arriba.
