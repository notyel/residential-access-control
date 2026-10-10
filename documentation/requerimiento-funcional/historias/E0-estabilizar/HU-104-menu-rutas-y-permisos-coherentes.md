# HU-104 · Menú, rutas y permisos coherentes para los roles actuales

**Épica:** E7 · **Etapa:** 0 Estabilizar · **Estado:** Hecha · **Depende de:** —
**Requerimiento:** §2 criterio 2, §3 criterios 1, 2 y 4, §8.6, §8.7 · **Defectos:** D3, D4, D8

## Historia

Como usuario con rol Admin, Guard u Owner, quiero que cada opción que veo funcione y que no pueda
abrir lo que no me corresponde, para no encontrarme pantallas con error.

## Situación actual

| Pantalla | Menú sembrado | Guarda de ruta | API |
| --- | --- | --- | --- |
| Panel | Admin, Guard, Owner | ninguna | solo Admin → 403 para Guard y Owner |
| Visitas | Admin, Guard, Owner | ninguna | Admin y Guard; Owner usa `owners/visits`, que devuelve lista y el frontend espera paginado |
| Residentes | Admin (como "Users") | ninguna | solo Admin |
| Registrar visita | — | `roleGuard` Admin, Guard | Admin, Guard |

- Rutas sembradas: `/dashboard`, `/visits`, `/users`. Rutas reales: `/access-control/dashboard`,
  `/access-control/visits`, `/access-control/residents`. Por confirmar qué hay en la base de datos.
- Si `GET menu` falla, `MenuService` muestra un menú de respaldo idéntico para todos los roles.
- `DataSeeder` genera ids y hashes nuevos en cada compilación.

## Criterios de aceptación

- [x] El Panel responde a los tres roles con los datos que les corresponden: Admin y Guard, todo el conjunto; Owner, solo sus residencias.
- [x] La lista de Visitas funciona con rol Owner, paginada y con filtros. Se resolvió distinto a lo planeado: `GET visits` limita en el servidor el resultado a las residencias del propietario y `owners/visits` se eliminó, para no depender de que el frontend elija el endpoint correcto.
- [x] Las rutas del menú coinciden con las del frontend y con sus nombres en español; se corrige con una migración.
- [x] Todas las rutas bajo `/access-control` declaran `requiredRoles` y pasan por `roleGuard`; escribir la URL de Residentes con rol Guard u Owner redirige al Panel.
- [x] "Nuevo residente" y "Editar" solo se muestran a Admin.
- [x] Si `GET menu` falla se muestra un error con reintento; se elimina el menú de respaldo.
- [x] El sembrado usa ids fijos y hashes precalculados: generar una migración sin cambios de modelo produce una migración vacía.
- [x] Se elimina `core/interceptors/AuthInterceptor.ts` (duplicado sin uso) y el módulo vacío `modules/visitors`. También se retiró el resto de código sin referencias: `main-layout`, `navigation`, `visitor.service`, `visitor.model`, `auth.models`.

## Diseño técnico

- `DashboardController`: `[Authorize]` y filtrar en el servicio según rol e id de usuario.
- Mover `roleGuard` a `canActivateChild` en `access-control.routes.ts` leyendo `data.requiredRoles` de cada ruta hija.
- Esta historia deja consistente el esquema de tres roles fijos; HU-109 y HU-111 lo reemplazan por permisos.

## Verificación

Iniciar sesión con cada uno de los tres usuarios sembrados contra el API real: abrir cada opción del
menú (ninguna da 403 ni queda vacía por error) y probar por URL directa las rutas que no están en
su menú.

## Hallazgos al implementar

- Confirmado D8: con el sembrado no determinista **ninguna migración se podía aplicar** (EF Core rechaza un modelo que cambia en cada compilación). El sembrado quedó con los ids y hashes del último snapshot.
- Una migración antigua (`AddOrderToMenu`) dejó una fila huérfana de menú "Residentes"; la migración `DeterministicSeedAndMenuPaths` la elimina antes de renombrar.
- Los iconos de menú sembrados (`LayoutDashboard`, `CarFront`) no estaban registrados en `IconService` y salían como un círculo; se registraron.
- El Panel mostraba cifras fijas de relleno ("Visitas hoy: 2", "Tiempo promedio: 30m") y todas las visitas como "Activo". Ahora muestra visitantes dentro, visitas de hoy y del mes calculados; la tarjeta de tiempo promedio se retiró hasta HU-130.
