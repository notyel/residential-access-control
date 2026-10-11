# HU-108 · Conjuntos y separación de datos

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Hecha · **Depende de:** HU-107
**Requerimiento:** §1.1, §2 (Superadministrador), §7.4, §8.10

## Historia

Como superadministrador, quiero registrar conjuntos residenciales y que los datos de cada uno queden
aislados, para operar varios clientes en la misma plataforma sin que uno vea información de otro.

## Criterios de aceptación

- [x] Entidad `Complex` (nombre, tipo: conjunto / edificio / propiedad con portería, dirección, zona horaria, activo).
- [x] Todas las entidades de negocio existentes (`User`, `Residence`, `Person`, `Visit`) llevan `ComplexId` obligatorio; la migración crea un conjunto inicial y le asigna los datos actuales.
- [x] El conjunto del usuario viaja en el token; toda consulta se filtra por él de forma automática, sin depender de que cada servicio lo recuerde.
- [x] Pedir por id un registro de otro conjunto responde 404 (no 403, para no revelar que existe).
- [x] Al guardar, `ComplexId` se toma del token; un valor distinto enviado por el cliente se ignora.
- [x] Las restricciones de unicidad pasan a ser por conjunto: documento de persona e identificador de residencia. **El correo de usuario se mantuvo único en toda la plataforma**: es lo que identifica la cuenta al iniciar sesión, y hacerlo único por conjunto obligaría a elegir conjunto en el login.
- [x] El superadministrador no pertenece a un conjunto: puede listar, crear, editar y desactivar conjuntos, y elegir en cuál trabajar.
- [x] Un conjunto desactivado no permite iniciar sesión a sus usuarios. Hecho para el inicio de sesión; **pendiente**: los cerrado en HU-110: el servidor valida en cada petición que el conjunto siga activo.
- [x] Pantalla "Conjuntos" (lista, alta, edición), visible solo para el superadministrador.

## Diseño técnico

- Interfaz `IComplexScoped { Guid ComplexId }` + `HasQueryFilter` global en `ApplicationDbContext`, alimentado por un `ICurrentUser` (id, conjunto, permisos) que lee los claims.
- `SaveChangesAsync` asigna `ComplexId` en las entidades agregadas.
- La zona horaria del negocio pasa de `appsettings` (`DateTimeSettings`) a `Complex.TimeZoneId`; `IDateTimeService` la resuelve por conjunto.
- Mientras no exista HU-109, el superadministrador es un valor nuevo del enum `Role`.

## Verificación

Prueba de integración con dos conjuntos y un usuario en cada uno: ninguno lista ni abre por id las
visitas, personas o usuarios del otro; dos personas con el mismo documento pueden existir en
conjuntos distintos.

## Cómo quedó

- `IComplexScoped` + filtro global en `ApplicationDbContext`; sin conjunto en la sesión las consultas no devuelven nada (seguro por defecto). `SaveChangesAsync` asigna el conjunto y rechaza cambiarlo.
- La sesión es `CurrentUser` (una por petición), llenada por `CurrentUserMiddleware` desde los claims `complex`, `tz` y `super`.
- El superadministrador es una marca del usuario (`IsSuperAdmin`), no un rol: no pertenece a un conjunto y elige en cuál trabajar con el encabezado `X-Complex-Id`, que el servidor ignora para cualquier otro usuario.
- Crear un conjunto crea también su primer administrador.
- Se sembró `superadmin@accesscontrol.com` con la misma contraseña inicial del administrador sembrado: **hay que cambiarla** en cualquier base real.
- La migración `Complexes` asigna todos los datos existentes al conjunto inicial. Crea un índice único `(ComplexId, Identifier)` en residencias: si la base real tuviera identificadores repetidos, la migración fallaría y habría que depurarlos antes.
- Verificado con 6 pruebas de integración (`ComplexTests`) y recorrido en navegador.
