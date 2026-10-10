# HU-05 · Búsqueda por comando

**Épica:** E3 Búsqueda · **Estado:** Hecha con pendientes · **Depende de:** HU-03

## Historia

Como guarda o administrador, quiero abrir una búsqueda global con un atajo desde cualquier vista,
para encontrar una visita, un residente o una acción sin recorrer el menú.

## Criterios de aceptación

- [x] El `<input>` del header se reemplaza por un botón "Buscar…" con la pista `Ctrl K` (solo icono en móvil).
- [x] `Ctrl K` / `⌘ K` y `/` (fuera de campos de texto) abren el popup; `Esc` y clic en el scrim lo cierran y devuelven el foco al botón.
- [x] El popup es un diálogo centrado arriba (640px máx.), con campo de búsqueda, resultados agrupados (Personas, Residentes, Acciones, Ir a) y pie con ayuda de teclado.
- [x] `↑`/`↓` mueven la selección, `Enter` ejecuta; patrón ARIA combobox + listbox (`aria-activedescendant`).
- [x] La consulta se aplica con debounce (250ms), cancela peticiones anteriores y resalta la coincidencia.
- [ ] Estados: inicial (acciones), cargando y sin resultados (hecho). Pendiente: búsquedas recientes y mensaje de error (hoy un proveedor que falla simplemente no aporta resultados).
- [x] Las acciones respetan el rol del usuario.
- [x] La lista de resultados hace scroll interno con `.app-scroll`.

## Diseño técnico

- `CommandPaletteService`: `open()`, `close()`, `toggle()`. Los proveedores se registran en `core/config/app.config.ts` con el token `SEARCH_PROVIDERS`.
- `SearchProvider { search(term): Observable<SearchResult[]> }`; proveedores: personas por documento exacto (`PersonService`), residentes por nombre, apartamento o correo (filtrado en cliente, solo Admin) y acciones/navegación (estático, por rol).
- `CommandPaletteComponent` abierto con `Dialog` del CDK (`@angular/cdk/dialog`), que ya aporta scrim, trampa de foco y restauración.
- Atajo global registrado una vez en el layout con `@HostListener('document:keydown')`.

## Dependencia de backend

Confirmado al implementar: `GET /api/persons` solo busca por número de documento exacto y
`GET /api/visits` solo filtra por fechas. Por eso la búsqueda encuentra personas escribiendo el
documento completo y no busca visitas. Para buscar por nombre o encontrar visitas hace falta una
historia de backend que añada un parámetro de texto a esos endpoints; después basta con registrar
un proveedor nuevo en `SEARCH_PROVIDERS`.

## Archivos

`front/src/app/shared/components/command-palette/*`, `front/src/app/core/services/command-palette.service.ts`,
`front/src/app/shared/components/topbar/*`.

## Verificación

Abrir con teclado, buscar "mart", navegar con flechas, `Enter` navega al resultado; repetir en
oscuro y a 390px.
