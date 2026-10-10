# HU-107 · Base de pruebas automatizadas

**Épica:** E7 · **Etapa:** 0 Estabilizar · **Estado:** Hecha con pendientes · **Depende de:** —
**Requerimiento:** §8 ("cuando se pueda demostrar que…")

## Historia

Como equipo, queremos poder probar las reglas de negocio de forma automática, para demostrar los
criterios de aceptación y no romper lo anterior al avanzar por las etapas.

## Situación actual

No hay proyecto de pruebas en `backend/` ni archivos `*.spec.ts` en `front/`. La verificación del
rediseño se hizo con datos simulados en el navegador, sin pasar por el API.

## Criterios de aceptación

- [x] Proyecto `AccessControl.Tests` (xUnit) en la solución; `dotnet test` corre desde `backend/`.
- [x] Pruebas de integración contra PostgreSQL real con `WebApplicationFactory`: levantan el API, aplican migraciones y llaman endpoints con un token de prueba por rol. Se usa `MysticMind.PostgresEmbed` (binarios embebidos) en lugar de Testcontainers porque la máquina de desarrollo no tiene Docker; funciona igual en el CI.
- [x] Casos iniciales, uno por flujo existente: login correcto e incorrecto; registrar visita; salida y doble salida; permisos (Guard no lista usuarios, Owner no ve visitas de otra residencia).
- [x] Pruebas unitarias de `DateTimeService` con `FakeTimeProvider` (cambio de mes en la zona horaria del negocio).
- [x] `ng test` corre en modo headless con al menos `roleGuard` y el interceptor de sesión cubiertos.
- [ ] Un flujo de CI (GitHub Actions) ejecuta `dotnet build`, `dotnet test`, `ng build` y `ng test` en cada PR. Escrito en `.github/workflows/ci.yml`; **pendiente verlo correr** en el primer PR.
- [x] `backend/README.md` y `front/README.md` explican cómo correr las pruebas.

## Diseño técnico

- Base de datos real y no el proveedor en memoria: las reglas de las etapas siguientes (unicidad por conjunto, no solapamiento de periodos) dependen de restricciones de PostgreSQL.
- Un ayudante `TestAuth.TokenFor(role)` que firma con la clave de prueba; desde HU-109, `TokenFor(permissions…)`.

## Verificación

`dotnet test` y `ng test --watch=false` en verde en local y en el CI de un PR de prueba.

## Resultado

29 pruebas de backend (26 de integración, 3 unitarias) y 11 de frontend. Se añadió además un entorno local sin Docker para recorrer la aplicación contra el API real: proyecto `AccessControl.DevDb`, perfil `local-db` y `npm run start:local` (ver `backend/README.md`).
