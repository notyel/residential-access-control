# HU-01 · Tokens de diseño y tema oscuro completo

**Épica:** E1 Fundamentos · **Estado:** Hecha · **Depende de:** —

## Historia

Como usuario, quiero que todos los elementos de la aplicación respeten el modo claro u oscuro
que elegí, para trabajar cómodamente en la portería de día y de noche.

## Criterios de aceptación

- [x] Existe un único archivo de tokens semánticos con valores para claro y oscuro (superficies, bordes, texto, primario, estados, sombras, scrim, scrollbar).
- [x] Los componentes de Angular Material (botones, form-field, select, datepicker, tabla, paginador, card, menú, diálogo) cambian con `data-theme="dark"`.
- [x] No queda ningún color literal en `.scss` de componentes del módulo `access-control` ni en `shared`.
- [x] La preferencia se persiste y, si no hay preferencia, se respeta `prefers-color-scheme`.
- [x] No hay parpadeo de tema claro al recargar en oscuro.
- [x] Existe un solo `ThemeToggleComponent`.

## Tokens (valores de referencia del diseño)

| Token | Claro | Oscuro |
| --- | --- | --- |
| `--app-bg` | `#f5f6f8` | `#0e1014` |
| `--app-surface` | `#ffffff` | `#16191e` |
| `--app-surface-2` | `#f0f2f5` | `#1d2127` |
| `--app-border` | `#e2e5ea` | `#2b3038` |
| `--app-text` | `#1a2230` | `#e9ebef` |
| `--app-text-muted` | `#5a6473` | `#9ca4b1` |
| `--app-primary` | `#0066ff` | `#0066ff` |
| `--app-primary-text` | `#0052cc` | `#7fb0ff` |
| `--app-primary-soft` | `#e8f0ff` | `#14294d` |
| `--app-success-bg` / `-text` | `#e3f4e8` / `#17693a` | `#12331f` / `#7ad69a` |
| `--app-neutral-bg` / `-text` | `#eceef1` / `#4a5361` | `#262b33` / `#b3bac5` |
| `--app-scrollbar` | `#c5cad3` | `#3a414c` |

Además: escala de espaciado (4/8/12/16/20/24), radios (8 controles, 12 tarjetas, 14 popups),
altura de topbar (56px), ancho de sidebar (232px), ancho de panel lateral (420px).

## Tareas técnicas

1. Crear `core/theme/_tokens.scss` con `:root, [data-theme="light"]` y `[data-theme="dark"]`. Mantener temporalmente alias de las variables antiguas (`--background-color`, `--card-background-color`, …) apuntando a los tokens nuevos para no romper vistas aún no migradas.
2. `_material-theme.scss`: definir tema claro y oscuro con `mat.define-theme`; aplicar el oscuro con `mat.all-component-colors` bajo `[data-theme="dark"]`. Mover los overrides `--mat-*`/`--mdc-*` para que usen los tokens en lugar de hex.
3. `ThemeService`: exponer el tema como signal, leer `localStorage` → `prefers-color-scheme`, escribir `data-theme` y `color-scheme` en `<html>`.
4. Script en línea en `index.html` que aplique `data-theme` antes del arranque de Angular.
5. Eliminar `modules/access-control/components/layout/theme-toggle`; conservar el de `shared`.
6. Reemplazar literales (`#ef4444`, `#22c55e`, `rgba(0,0,0,…)`, etc.) en `_base.scss`, `_badges.scss`, `_buttons.scss`.

## Archivos

`front/src/app/core/theme/*`, `front/src/app/core/services/theme.service.ts`, `front/src/index.html`,
`front/src/app/shared/components/theme-toggle/*`.

## Verificación

Recorrer Dashboard, Visitas, Registrar visita, Residentes y Nuevo residente en ambos temas;
abrir un datepicker y un select en oscuro.
