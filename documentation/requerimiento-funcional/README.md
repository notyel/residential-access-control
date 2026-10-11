# Épica E7 · Cumplimiento del requerimiento funcional

Valida lo construido hasta hoy contra el [requerimiento funcional](requerimiento.md) y convierte
lo que falta en historias de usuario que se construyen una por una, en el orden de la tabla
[Orden de ejecución](#orden-de-ejecución). Cada historia vive en su archivo dentro de `historias/`
y es entregable por separado (un commit o PR por historia).

## Cómo se hizo la validación

- Fecha: 2026-10-10, rama `develop`, con las 33 historias de la épica ya trabajadas.
- Método: pruebas automatizadas contra el API real con PostgreSQL (129 pruebas de backend, entre
  ellas las de aceptación de §8: `dotnet test --filter Category=Aceptacion`), 27 pruebas del
  frontend, y recorrido de las pantallas nuevas en el entorno local (`devdb` + `api-local` +
  `front-local`). No se ha desplegado ni probado en la base de datos compartida.
- Escala: **Cumple** · **Parcial** (funciona, con un pendiente anotado) · **No cumple**.
- La validación inicial (antes de construir nada) quedó en el historial de git, commit `21be9a1`.

## Resultado en una línea

Los 10 criterios transversales (§8) se demuestran con pruebas automatizadas de extremo a extremo.
De las 33 historias, 12 están hechas y 21 hechas con pendientes
anotados; ninguna está sin empezar. Lo que sigue abierto es sobre todo operación (copias de
seguridad sin ejecutar, rotación de credenciales), sesiones con token de renovación y detalles de
pantalla. Está reunido en [Pendientes abiertos](#pendientes-abiertos).

## Qué existe hoy

| Capa | Construido |
| --- | --- |
| Datos | Conjuntos (multiinquilino con filtro global por `ComplexId`), usuarios con estado e invitación, roles y permisos configurables, políticas y catálogos por conjunto, estructuras y unidades, personas, titularidades y ocupaciones con vigencia, autorizaciones de visita, visitas y eventos de acceso inmutables, vehículos y sus autorizaciones, parqueaderos con asignaciones, novedades y bitácora de auditoría |
| API | Autenticación con bloqueo e invitaciones; administración de conjuntos, usuarios, roles, políticas y catálogos; unidades y personas; portal (`api/my/...`); portería (validación, ingreso, salida, rechazo, excepción, anulación); vehículos; parqueaderos y disponibilidad; historial de accesos, reportes con exportación CSV, novedades; panel por bloques; `GET api/health` |
| Seguridad | Permisos `modulo.accion` exigidos en cada endpoint con `[HasPermission]`, datos aislados por conjunto y por unidad, datos personales enmascarados en el servidor, bitácora de cambios y de exportaciones, límite de intentos |
| Interfaz | Panel por rol, Unidades, Propietarios y residentes, Validar visitantes, Visitantes, Autorizaciones, Verificar vehículos, Vehículos, Parqueaderos, Novedades, Historial de accesos, Reportes, Usuarios, Roles, Bitácora, Políticas, y el portal (Mis propiedades, Mi residencia, Autorizar visitantes, Mis vehículos, Mis parqueaderos) |
| Pruebas | 129 de backend (integración contra PostgreSQL embebido y unitarias) y 27 de frontend; flujo de CI escrito |

## Validación por sección del requerimiento

### §1.1 Propiedades y unidades — Cumple

Estructuras (torre, bloque, etapa…) opcionales, unidades con código único por conjunto, alta
individual y en lote, estado de ocupación calculado. Historias HU-115 y HU-118.
Pendiente: no hay pantalla para administrar catálogos ni estructuras (se hace por el API).

### §1.2 y §4 Propietarios, residentes y ocupantes — Cumple

Persona separada de la cuenta de usuario; titularidades y ocupaciones con fecha de inicio y fin;
la base impide periodos cruzados; propietario no residente y arrendatario conviven con su
relación; el historial se conserva. Portal del propietario y del residente. Historias HU-116 a
HU-119. Pendiente: `User` conserva nombre y apellido propios (HU-116).

### §1.3 y §5 Visitantes y accesos — Cumple

Autorización previa (única, por periodo, recurrente), validación en portería con el veredicto del
servidor, ingreso sin autorización según la política del conjunto, rechazo, acceso excepcional con
permiso y motivo, salida, anulación en lugar de edición, y trazabilidad completa en el detalle.
Historias HU-120 a HU-123. Pendientes menores de pantalla en HU-122 y HU-123.

### §1.4 Vehículos — Cumple

Inventario con placa normalizada y única, autorización por unidad y responsable con historial,
registro desde el portal según la política, vehículo de visitante enlazado a su visita y
verificación por placa con cuatro resultados. Historias HU-124 y HU-125.

### §1.5 y §6 Parqueaderos — Cumple

Privados, comunitarios y de visitantes; titular, asignación temporal, reserva y ocupación en una
sola tabla con restricciones de exclusión en la base; el estado y la disponibilidad se calculan en
cada consulta; la salida del vehículo libera el espacio. Historias HU-126 a HU-128.

### §1.6 Seguridad y registro de accesos — Cumple

Todo evento de acceso queda registrado y no se puede modificar ni borrar (lo impide también la
base). Historial filtrable según el rol, reportes, exportación con registro en bitácora y
novedades del turno. Historia HU-129.

### §2 Roles y permisos — Cumple

Cinco roles de sistema ajustables permiso por permiso, roles propios por conjunto, y una matriz
automatizada que comprueba que cada rol solo entra donde tiene permiso. Historias HU-109 y HU-110.

### §3 Menú dinámico — Parcial

El menú sale del servidor según los permisos efectivos y las rutas tienen guarda. Las opciones
del portal dependen del rol, no del vínculo: si la vinculación termina siguen visibles, vacías
(HU-111).

### §7 Seguridad y operación — Parcial

Cumple: secretos fuera del repositorio, contraseñas con política y bloqueo, invitación y
recuperación por token de un solo uso, cierre de sesión al cambiar permisos, bitácora, datos
personales enmascarados, separación por conjunto, endpoint de salud.
No cumple todavía: token de renovación en cookie y lista de sesiones (HU-113); copias de seguridad
ejecutadas y verificadas (HU-132, solo hay scripts y procedimiento); rotación de la contraseña de
la base y de la clave JWT que estuvieron versionadas (HU-101, la hace el dueño de la cuenta); texto
de tratamiento de datos con aceptación (HU-131); el flujo de CI no se ha visto correr (HU-107).

### §8 Criterios transversales

Cada uno tiene su prueba en `AcceptanceTests` y su paso en el
[guion manual](guion-de-aceptacion.md).

| # | Criterio | Estado | Prueba |
| --- | --- | --- | --- |
| 1 | Unidad vinculada con su propietario | Cumple | `C8_1_y_8_2…` |
| 2 | Arrendatario distinto del propietario | Cumple | `C8_1_y_8_2…` |
| 3 | Visitante autorizado, ingreso y salida trazables | Cumple | `C8_3…` |
| 4 | Vehículo asociado a propietario, residente o visitante | Cumple | `C8_4…` |
| 5 | Parqueaderos de los tres tipos con estados y asignaciones | Cumple | `C8_5_y_8_9…` |
| 6 | Usuario sin permisos no consulta ni modifica | Cumple | `C8_6…` (matriz de consultas por rol; las escrituras, en las pruebas de cada módulo) |
| 7 | Cada rol ve solo su menú | Cumple | `C8_7…`; "Sin acceso" por URL, en el guion manual |
| 8 | Historial conservado ante cambios | Cumple | `C8_8…` |
| 9 | Consultas de accesos, presentes y disponibilidad | Cumple | `C8_5_y_8_9…` |
| 10 | Configurable para distintos tipos de conjunto | Cumple | `C8_10…` |

El guion manual aún no tiene una ejecución firmada.

## Defectos en lo construido

Problemas que tenían las funciones de la interfaz al empezar. **Los ocho quedaron corregidos en la
etapa 0** (D6 con un pendiente: rotar las credenciales que estuvieron versionadas). Las rutas de la
columna "Dónde" son las de entonces; varios de esos archivos ya no existen.

| # | Defecto | Dónde | Historia |
| --- | --- | --- | --- |
| D1 | Registrar visita envía el id del **usuario** elegido como `residenceId`; la FK apunta a `Residences`, así que el guardado falla. Además el selector carga `GET users`, que es solo para Admin: el guarda no puede elegir residencia | `register-visit.component.ts:206`, `ResidentsService`, `UsersController` | HU-102 |
| D2 | Alta y edición de residentes llaman `POST users` y `PUT users/{id}`, que no existen (el API solo tiene `GET`) | `residents.service.ts`, `UsersController.cs` | HU-103 |
| D3 | Panel: el API es solo Admin, pero el menú se lo da a Guard y Owner y es la ruta por defecto → 403 al entrar. Residentes: sin guarda de ruta y API solo Admin | `DashboardController.cs`, `DataSeeder.cs`, `access-control.routes.ts` | HU-104 |
| D4 | Visitas del propietario: el frontend espera un resultado paginado y `GET owners/visits` devuelve una lista | `visits.service.ts`, `OwnersController.cs` | HU-104 |
| D5 | La salida de una visita no se puede registrar desde la interfaz; el endpoint es solo para Guard, se puede repetir y sobrescribe la hora. `DELETE visits/{id}` borra el registro | `VisitsController.cs`, `VisitService.cs` | HU-105 |
| D6 | Contraseña de la base de datos y clave de firma JWT versionadas | `backend/AccessControl.API/appsettings.json` | HU-101 |
| D7 | Errores de negocio (documento duplicado, persona inexistente) responden 500 con el mensaje interno; el frontend no trata el 401 de sesión vencida | Controladores, `auth.interceptor.ts` | HU-106 |
| D8 | El sembrado usa `Guid.NewGuid()` y un hash BCrypt nuevo en cada compilación: cada migración regenera usuarios, menús y permisos | `DataSeeder.cs` | HU-104 |

## Orden de ejecución

Se resolvió de arriba abajo. El estado de cada historia y sus pendientes están en su archivo.

### Etapa 0 · Estabilizar lo construido

| # | Historia | Depende de | Estado |
| --- | --- | --- | --- |
| HU-101 | [Secretos fuera del repositorio](historias/E0-estabilizar/HU-101-secretos-fuera-del-repositorio.md) | — | Hecha con pendientes |
| HU-102 | [Registrar visita contra una residencia real](historias/E0-estabilizar/HU-102-registrar-visita-residencia-real.md) | — | Hecha |
| HU-103 | [Alta y edición de residentes contra el API](historias/E0-estabilizar/HU-103-alta-y-edicion-de-residentes.md) | — | Hecha |
| HU-104 | [Menú, rutas y permisos coherentes para los roles actuales](historias/E0-estabilizar/HU-104-menu-rutas-y-permisos-coherentes.md) | — | Hecha |
| HU-105 | [Salida de visitas sin perder historial](historias/E0-estabilizar/HU-105-salida-de-visitas.md) | HU-102 | Hecha |
| HU-106 | [Sesión vencida y errores del API](historias/E0-estabilizar/HU-106-sesion-vencida-y-errores.md) | — | Hecha |
| HU-107 | [Base de pruebas automatizadas](historias/E0-estabilizar/HU-107-base-de-pruebas.md) | — | Hecha con pendientes |

### Etapa 1 · Fundamentos: conjunto, permisos y bitácora

| # | Historia | Depende de | Estado |
| --- | --- | --- | --- |
| HU-108 | [Conjuntos y separación de datos](historias/E1-fundamentos/HU-108-conjuntos-y-separacion-de-datos.md) | HU-107 | Hecha |
| HU-109 | [Roles y permisos configurables](historias/E1-fundamentos/HU-109-roles-y-permisos-configurables.md) | HU-108 | Hecha |
| HU-110 | [Usuarios: alta, activación y revocación](historias/E1-fundamentos/HU-110-gestion-de-usuarios.md) | HU-109 | Hecha con pendientes |
| HU-111 | [Menú dinámico y rutas por permiso](historias/E1-fundamentos/HU-111-menu-dinamico-por-permiso.md) | HU-109 | Hecha con pendientes |
| HU-112 | [Bitácora de auditoría](historias/E1-fundamentos/HU-112-bitacora-de-auditoria.md) | HU-108 | Hecha |
| HU-113 | [Recuperación de contraseña y sesiones](historias/E1-fundamentos/HU-113-recuperacion-y-sesiones.md) | HU-110 | Hecha con pendientes |
| HU-114 | [Políticas del conjunto](historias/E1-fundamentos/HU-114-politicas-del-conjunto.md) | HU-108, HU-109 | Hecha |

### Etapa 2 · Unidades y personas

| # | Historia | Depende de | Estado |
| --- | --- | --- | --- |
| HU-115 | [Estructura del conjunto y unidades](historias/E2-unidades-y-personas/HU-115-estructura-y-unidades.md) | HU-108 | Hecha con pendientes |
| HU-116 | [Personas y propietarios de unidades](historias/E2-unidades-y-personas/HU-116-personas-y-propietarios.md) | HU-115 | Hecha con pendientes |
| HU-117 | [Ocupantes y arrendamientos con vigencia](historias/E2-unidades-y-personas/HU-117-ocupantes-y-arrendamientos.md) | HU-116, HU-114 | Hecha |
| HU-118 | [Ficha de unidad e historial](historias/E2-unidades-y-personas/HU-118-ficha-de-unidad.md) | HU-117 | Hecha con pendientes |
| HU-119 | [Portal de propietario y residente](historias/E2-unidades-y-personas/HU-119-portal-propietario-y-residente.md) | HU-117, HU-111 | Hecha |

### Etapa 3 · Visitantes y accesos

| # | Historia | Depende de | Estado |
| --- | --- | --- | --- |
| HU-120 | [Eventos de ingreso y salida](historias/E3-visitantes-y-accesos/HU-120-eventos-de-ingreso-y-salida.md) | HU-115, HU-116 | Hecha con pendientes |
| HU-121 | [Autorizaciones de visita](historias/E3-visitantes-y-accesos/HU-121-autorizaciones-de-visita.md) | HU-120, HU-119 | Hecha |
| HU-122 | [Validación en portería, rechazos y accesos excepcionales](historias/E3-visitantes-y-accesos/HU-122-validacion-en-porteria.md) | HU-121, HU-114 | Hecha con pendientes |
| HU-123 | [Visitantes esperados, presentes e historial](historias/E3-visitantes-y-accesos/HU-123-esperados-presentes-historial.md) | HU-122 | Hecha con pendientes |

### Etapa 4 · Vehículos

| # | Historia | Depende de | Estado |
| --- | --- | --- | --- |
| HU-124 | [Inventario de vehículos](historias/E4-vehiculos/HU-124-inventario-de-vehiculos.md) | HU-116, HU-117 | Hecha con pendientes |
| HU-125 | [Vehículos en el control de acceso](historias/E4-vehiculos/HU-125-vehiculos-en-el-acceso.md) | HU-124, HU-122 | Hecha con pendientes |

### Etapa 5 · Parqueaderos

| # | Historia | Depende de | Estado |
| --- | --- | --- | --- |
| HU-126 | [Registro de parqueaderos](historias/E5-parqueaderos/HU-126-registro-de-parqueaderos.md) | HU-115, HU-116 | Hecha con pendientes |
| HU-127 | [Asignaciones, ocupación y reservas](historias/E5-parqueaderos/HU-127-asignaciones-ocupacion-reservas.md) | HU-126, HU-125 | Hecha con pendientes |
| HU-128 | [Disponibilidad de parqueaderos](historias/E5-parqueaderos/HU-128-disponibilidad.md) | HU-127 | Hecha con pendientes |

### Etapa 6 · Consultas y operación

| # | Historia | Depende de | Estado |
| --- | --- | --- | --- |
| HU-129 | [Historial de accesos y reportes](historias/E6-consultas-y-operacion/HU-129-historial-y-reportes.md) | HU-122, HU-125 | Hecha con pendientes |
| HU-130 | [Paneles por rol](historias/E6-consultas-y-operacion/HU-130-paneles-por-rol.md) | HU-123, HU-128 | Hecha con pendientes |
| HU-131 | [Protección de datos personales](historias/E6-consultas-y-operacion/HU-131-proteccion-de-datos-personales.md) | HU-109, HU-112 | Hecha con pendientes |
| HU-132 | [Copias de seguridad y recuperación](historias/E6-consultas-y-operacion/HU-132-copias-de-seguridad.md) | HU-101, HU-108 | Hecha con pendientes |
| HU-133 | [Validación de aceptación final](historias/E6-consultas-y-operacion/HU-133-validacion-de-aceptacion.md) | Todas | Hecha con pendientes |

## Pendientes abiertos

Criterios sin cerrar de cada historia, tal como están anotados en su archivo. Decidir cuáles se
hacen ahora y cuáles pasan a una segunda fase le corresponde al responsable del producto.

**[HU-101](historias/E0-estabilizar/HU-101-secretos-fuera-del-repositorio.md) · Secretos fuera del repositorio**

- **La contraseña de la base de datos y la clave JWT expuestas se rotan.** Sacarlas del archivo no las invalida: siguen en el historial de git. La rotación la hace el dueño de la cuenta de la base de datos.

**[HU-107](historias/E0-estabilizar/HU-107-base-de-pruebas.md) · Base de pruebas automatizadas**

- Un flujo de CI (GitHub Actions) ejecuta `dotnet build`, `dotnet test`, `ng build` y `ng test` en cada PR. Escrito en `.github/workflows/ci.yml`; **pendiente verlo correr** en el primer PR.

**[HU-111](historias/E1-fundamentos/HU-111-menu-dinamico-por-permiso.md) · Menú dinámico y rutas por permiso**

- Si la vinculación residencial del usuario termina (HU-117), sus opciones "Mi residencia / Mis propiedades" desaparecen en la siguiente recarga. **Parcial:** las opciones dependen del rol, no del vínculo: al terminar la vinculación siguen en el menú, pero ya no muestran ninguna unidad (el servidor solo entrega las de vínculo vigente).

**[HU-113](historias/E1-fundamentos/HU-113-recuperacion-y-sesiones.md) · Recuperación de contraseña y sesiones**

- Token de acceso corto (15 min) más token de renovación rotativo; el frontend renueva sin interrumpir al usuario. Pendiente.
- Reutilizar un token de renovación ya usado cierra toda esa familia de sesiones. Pendiente.
- El token de renovación viaja en cookie `HttpOnly`, `Secure`, `SameSite=Strict`; el de acceso solo vive en memoria. Nada de sesión queda en `localStorage`. Pendiente: el token sigue en localStorage.
- "Mi cuenta → Sesiones": lista de sesiones (dispositivo, IP, último uso) con "Cerrar" y "Cerrar todas las demás". Pendiente.

**[HU-116](historias/E2-unidades-y-personas/HU-116-personas-y-propietarios.md) · Personas y propietarios de unidades**

- Migración: cada usuario con rol Propietario genera una `Person`, y una `UnitOwnership` vigente por cada residencia que tenía. `User.ApartmentNumber`, `User.FirstName` y `User.LastName` se eliminan (el nombre sale de la persona). La pantalla "Residentes" se reemplaza por esta. Hecho salvo la eliminación de columnas: `User` conserva `FirstName`, `LastName` y `ApartmentNumber` (ver abajo).

**[HU-118](historias/E2-unidades-y-personas/HU-118-ficha-de-unidad.md) · Ficha de unidad e historial**

- Secciones "Vehículos", "Parqueaderos" y "Visitas recientes". **Parcial:** "Vehículos" y "Parqueaderos" ya están en la ficha; "Visitas recientes" no (se consultan en "Visitantes" filtrando por la unidad desde el API).

**[HU-120](historias/E3-visitantes-y-accesos/HU-120-eventos-de-ingreso-y-salida.md) · Eventos de ingreso y salida**

- Los catálogos de puntos de acceso (porterías) son configurables por conjunto; con una sola portería no se pregunta. **Pendiente:** el catálogo `access-point` existe y el API lo valida, pero la pantalla de portería no pregunta el punto de acceso ni hay pantalla para administrar catálogos (ver HU-115).

**[HU-122](historias/E3-visitantes-y-accesos/HU-122-validacion-en-porteria.md) · Validación en portería, rechazos y accesos excepcionales**

- Rapidez en portería: el flujo con autorización vigente se completa con teclado (buscar → Enter → Enter); el foco inicial está en el buscador; funciona a 390 px. **Parcial:** el buscador del panel de portería recibe el foco y lleva a la validación con Enter, y la pantalla funciona en un teléfono; falta completar el ingreso solo con teclado (hoy hay que pulsar "Registrar ingreso").

**[HU-123](historias/E3-visitantes-y-accesos/HU-123-esperados-presentes-historial.md) · Visitantes esperados, presentes e historial**

- Esperados: autorizaciones válidas para hoy (únicas del día, por periodo y recurrentes que aplican al día de la semana) que aún no han ingresado; con acción "Registrar ingreso". **Parcial:** la vista existe; la acción "Registrar ingreso" está en "Validar visitantes", no en esta lista.
- Presentes: visitas con entrada y sin salida, con el tiempo transcurrido y acción "Registrar salida". Las que superan la política "horas para marcar por revisar" se destacan. **Parcial:** muestra la hora de ingreso y destaca las "por revisar"; no muestra el tiempo transcurrido, y la salida se registra desde el detalle.
- "Historial de visitas" (propietario y residente): las mismas vistas limitadas a sus unidades con vínculo vigente; el arrendatario solo ve visitas ocurridas durante su ocupación. **Parcial:** limitado a sus unidades; el corte "solo durante su ocupación" se aplica en "Historial de accesos" (HU-129), no en esta lista de visitas.

**[HU-124](historias/E4-vehiculos/HU-124-inventario-de-vehiculos.md) · Inventario de vehículos**

- Pantalla "Vehículos": lista con búsqueda por placa y filtros por unidad, tipo, clase y estado; alta, edición, suspender y revocar con motivo. **Parcial:** búsqueda y filtros por tipo, clase y estado; el filtro por unidad existe en el API pero no en la pantalla.

**[HU-125](historias/E4-vehiculos/HU-125-vehiculos-en-el-acceso.md) · Vehículos en el control de acceso**

- Novedades de ingreso o salida (daño visible, placa no coincide, etc.) como observación del evento. **Parcial:** el ingreso de visitas tiene "Observaciones" y los vehículos tienen novedades; la entrada o salida de un vehículo de residente desde "Verificar vehículos" no pide observación.

**[HU-126](historias/E5-parqueaderos/HU-126-registro-de-parqueaderos.md) · Registro de parqueaderos**

- Pantalla "Parqueaderos": lista con filtros por tipo, ubicación y estado; alta individual y en lote por rango de códigos (p. ej. V-01 a V-20). **Parcial:** la edición de un espacio (código, ubicación, tipo) está en el API (`PUT parking/spaces/{id}`) pero no en la pantalla; el resto sí.

**[HU-127](historias/E5-parqueaderos/HU-127-asignaciones-ocupacion-reservas.md) · Asignaciones, ocupación y reservas**

- **Asignación temporal** de comunitarios a una unidad o vehículo: inicio y fin obligatorios; se libera sola al llegar la fecha de fin. **Parcial:** a una unidad desde la pantalla; a un vehículo concreto solo por el API.

**[HU-128](historias/E5-parqueaderos/HU-128-disponibilidad.md) · Disponibilidad de parqueaderos**

- Legible y operable a 390 px. **Sin verificar:** la cuadrícula y las tarjetas son adaptables, pero no se revisó en un teléfono de 390 px.

**[HU-129](historias/E6-consultas-y-operacion/HU-129-historial-y-reportes.md) · Historial de accesos y reportes**

- Filtros combinables: rango de fechas, persona (nombre o documento), unidad, placa, tipo de acceso, clase de persona, operador y punto de acceso (§1.6); el superadministrador puede filtrar además por conjunto (§7.7). **Parcial:** en la pantalla: fechas, persona, placa, tipo de acceso y clase de persona. Unidad, operador y punto de acceso están en el API. El superadministrador elige el conjunto con el selector general, no con un filtro propio.
- "Novedades": el guarda registra una novedad del turno (texto, categoría, unidad, persona o vehículo opcionales); son de solo inserción y se consultan por fecha y categoría. **Parcial:** texto, categoría y placa desde la pantalla; unidad y persona solo por el API; el filtro por fecha también.

**[HU-130](historias/E6-consultas-y-operacion/HU-130-paneles-por-rol.md) · Paneles por rol**

- Cada indicador enlaza a la lista filtrada correspondiente. **Parcial:** enlaza a la pantalla correspondiente, sin el filtro ya aplicado.
- El panel de portería se actualiza solo cada 30 segundos y es usable con una mano a 390 px. **Parcial:** se actualiza solo; no se verificó el uso con una mano a 390 px.

**[HU-131](historias/E6-consultas-y-operacion/HU-131-proteccion-de-datos-personales.md) · Protección de datos personales**

- Los logs de la aplicación no contienen documentos, teléfonos, correos, contraseñas ni tokens. **Sin verificar:** no se registran datos personales a propósito, pero falta una revisión sistemática del log tras un flujo completo (en desarrollo, EF Core puede escribir parámetros si se activa el registro sensible).
- Texto de tratamiento de datos configurable por conjunto, mostrado al registrar un visitante y aceptado por los usuarios en su primer ingreso (se guarda fecha y versión). **Pendiente:** no hay texto configurable ni aceptación con fecha y versión.
- Periodo de retención configurable para datos de visitantes: pasado el plazo, la persona visitante se anonimiza (nombre y documento reemplazados) y los eventos de acceso se conservan. **Parcial:** la persona visitante se anonimiza; los eventos de acceso conservan el nombre y el documento del momento, porque son inmutables. Decidir con el asesor legal si deben anonimizarse también.
- La revisión cubre todos los endpoints existentes; ninguno devuelve la entidad de base de datos directamente. **Parcial:** todos los endpoints devuelven DTO, no entidades; el enmascarado cubre personas, visitas, historial y exportaciones. Los nombres en fichas de unidad, autorizaciones y vehículos son datos básicos y salen completos.

**[HU-132](historias/E6-consultas-y-operacion/HU-132-copias-de-seguridad.md) · Copias de seguridad y recuperación**

- Objetivos escritos y acordados: pérdida máxima de datos (RPO) y tiempo máximo de recuperación (RTO). **Pendiente:** están propuestos (RPO 24 h, RTO 4 h); falta acordarlos.
- Copia automática diaria completa con retención mínima de 30 días, más recuperación a un punto en el tiempo si el proveedor la ofrece; se confirma y documenta lo que da el plan actual. **Pendiente:** confirmar y anotar lo que ofrece el plan del proveedor.
- Una copia adicional fuera del proveedor de la base de datos (`pg_dump` programado a un almacenamiento distinto, cifrado). **Pendiente:** el script existe; falta el almacenamiento de destino y programarlo.
- Procedimiento de restauración paso a paso, ejecutado al menos una vez contra una base vacía; se anota la fecha y el tiempo que tomó. **Pendiente:** escrito, sin ejecutar; no hay fecha ni tiempo que anotar.
- Verificación automática: una tarea periódica restaura la última copia en una base temporal y comprueba que las migraciones coinciden y que hay datos recientes. **Pendiente:** `verify.sh` existe; falta la tarea periódica que restaure y lo ejecute.
- Alerta cuando una copia falla o tiene más de 26 horas. **Pendiente:** depende de dónde se programe la tarea.
- Procedimiento para restaurar la información de **un solo conjunto** sin afectar a los demás. **Parcial:** documentado, sin ensayar.

**[HU-133](historias/E6-consultas-y-operacion/HU-133-validacion-de-aceptacion.md) · Validación de aceptación final**

- **§8.6** Para cada rol de sistema, una matriz de pruebas llama a todos los endpoints: responde 200 solo donde tiene permiso y 403/404 en el resto; ningún usuario obtiene datos de otro conjunto ni de otra unidad. **Parcial:** la matriz llama a todas las consultas (GET sin parámetros de ruta) y verifica que todo endpoint declara su protección; las operaciones de escritura se prueban por rol en las pruebas de cada módulo, no en la matriz.
- **§8.7** El menú de cada rol de sistema coincide con el definido en §3; las rutas fuera del menú muestran "Sin acceso". **Parcial:** el menú se verifica con prueba automatizada; "Sin acceso" en rutas fuera del menú se cubre con el guion manual.
- Se revisan los pendientes abiertos de las historias `Hecha con pendientes` y se decide cuáles pasan a segunda fase junto con §9. **Pendiente:** están reunidos en el README de la épica; decidir cuáles pasan a segunda fase le corresponde al responsable del producto.

## Modelo de datos objetivo

Referencia para que las historias usen los mismos nombres. Cada historia crea solo lo que necesita.

```
Complex (conjunto)            ← todo lo demás lleva ComplexId
  ComplexPolicy               políticas configurables (HU-114)
  Structure                   torre / bloque / etapa, anidable (HU-115)
  Unit                        casa, apartamento, local; código único en el conjunto (HU-115)
Person                        cualquier persona: propietario, ocupante, visitante (HU-116)
  User                        credenciales; opcional, enlaza a Person (§4.8)
  UserRole → Role → RolePermission → Permission   (HU-109)
UnitOwnership                 Person + Unit + desde/hasta (HU-116)
Occupancy                     Person + Unit + relación + desde/hasta + estado (HU-117)
VisitAuthorization            visitante, unidad, quién autoriza, vigencia, recurrencia (HU-121)
Visit                         una estancia: liga ingreso, salida, autorización, vehículo
AccessEvent                   inmutable: entrada | salida | rechazo, persona, vehículo, operador (HU-120)
Vehicle                       placa, tipo, marca, color, permanente | visitante (HU-124)
ParkingSpace                  código, ubicación, tipo, fuera de servicio (HU-126)
ParkingAssignment             espacio + unidad/persona/vehículo/visita + desde/hasta + clase (HU-127)
AuditLog                      quién, qué, antes/después, cuándo (HU-112)
```

Dos reglas que atraviesan todas las historias:

- **Nada con historia se borra ni se sobrescribe.** Los vínculos se cierran con una fecha de fin;
  los eventos de acceso solo se agregan.
- **Los estados se calculan.** "Ocupado", "presente" o "vigente" salen de fechas y eventos, no de
  un campo que alguien actualiza a mano (§6 criterio 7).

## Decisiones que hay que tomar antes de la etapa 1

| Decisión | Propuesta por defecto | Afecta a |
| --- | --- | --- |
| Separación entre conjuntos | Una sola base de datos, columna `ComplexId` y filtro global de EF Core | HU-108 |
| Qué pasa con los usuarios `Owner` actuales | Migrarlos a `Person` + `User` + `UnitOwnership` usando `ApartmentNumber` como código de unidad | HU-116 |
| Qué pasa con las `Visit` existentes | Generar un evento de entrada (y de salida si hay `CheckOut`) por cada una | HU-120 |
| Envío de correo para recuperar contraseña | SMTP configurable; en desarrollo, registrar el enlace en el log | HU-113 |

## Fuera de alcance

Lo listado en §9 del requerimiento (QR, notificaciones, fotos, barreras y lectores de placas,
zonas comunes, contratistas, incidentes, alertas). Se revisa al cerrar HU-133.

## Definición de terminado (aplica a todas)

- Criterios de aceptación marcados `[x]` en el archivo de la historia y `Estado: Hecha` (o
  `Hecha con pendientes`, con el pendiente escrito).
- Cada regla de negocio nueva tiene una prueba automatizada en el backend (desde HU-107).
- Toda operación nueva del API valida el permiso en el servidor; la interfaz oculta lo que el
  usuario no puede hacer.
- `dotnet build` y `ng build` sin errores ni advertencias nuevas; migración de EF incluida.
- Recorrido manual contra el API real (no datos simulados) con al menos dos roles distintos.
- Lo de la [definición de terminado del rediseño](../ui-redesign/README.md#definición-de-terminado-aplica-a-todas)
  para cualquier pantalla nueva: claro/oscuro, 1440/1024/390px, teclado, tokens.
