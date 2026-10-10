# Épica E7 · Cumplimiento del requerimiento funcional

Valida lo construido hasta hoy contra el [requerimiento funcional](requerimiento.md) y convierte
lo que falta en historias de usuario que se construyen una por una, en el orden de la tabla
[Orden de ejecución](#orden-de-ejecución). Cada historia vive en su archivo dentro de `historias/`
y es entregable por separado (un commit o PR por historia).

## Cómo se hizo la validación

- Fecha: 2026-10-10, rama `develop`, commit `dc43fc6`.
- Método: lectura del código de `backend/` (API .NET, EF Core, PostgreSQL) y `front/` (Angular).
  **No se ejecutó la aplicación ni se consultó la base de datos**; lo que dependa de datos reales
  (por ejemplo, las rutas del menú guardadas en la BD) está marcado como "por confirmar".
- Escala: **Cumple** · **Parcial** (existe algo, no alcanza el criterio) · **No cumple** (no existe).

## Resultado en una línea

De los 10 criterios transversales (§8) hoy no se puede demostrar ninguno completo: 3 están
parciales (visitas, restricción por rol, menú por rol) y 7 no tienen implementación. Lo construido
es una base de autenticación, registro de visitas en portería y una interfaz ya rediseñada; faltan
el modelo de unidades, ocupantes, vehículos, parqueaderos, permisos configurables y bitácora.
Además, tres flujos que la interfaz ya ofrece fallan contra el API real (ver
[Defectos](#defectos-en-lo-construido)).

## Qué existe hoy

| Capa | Construido |
| --- | --- |
| Datos | `User` (un `Role` de enum: Admin, Guard, Owner; `ApartmentNumber` como texto), `Residence` (identificador + un dueño), `Person`, `Visit` (persona, placa como texto, residencia, quién registró, `CheckOut`), `Menu`, `RoleMenu` |
| API | `POST auth/login`, `POST auth/register` (Admin), `GET users` y `GET users/{id}` (Admin), `GET/POST/PUT persons` (Admin, Guard), `visits`: listar, ver, crear, editar, borrar, `checkout`; `GET owners/visits`; `GET menu`; `GET dashboard` (Admin) |
| Seguridad | JWT con expiración de 240 min, contraseñas con BCrypt, cuatro políticas fijas por rol |
| Interfaz | Login, Panel, Visitas (lista con filtro por fechas), Registrar visita, Residentes (lista, alta, edición), menú lateral cargado del API, búsqueda por comando, tema claro/oscuro, layout responsive |
| Pruebas | Ninguna (no hay proyectos de test en backend ni `*.spec.ts` en frontend) |

## Validación por sección del requerimiento

### §1.1 Propiedades y unidades — No cumple

| Requisito | Estado | Evidencia |
| --- | --- | --- |
| Registrar conjuntos, torres, bloques, casas, apartamentos, locales | No cumple | Solo existe `Residence` con un texto `Identifier`; no hay API ni pantalla para crearla (solo el dato sembrado `RES-001`) |
| Identificador único por unidad | Parcial | `Residence.Identifier` tiene índice pero no es único (`ApplicationDbContext.cs`) |
| Consultar propietarios, ocupantes, vehículos y parqueaderos de la unidad | No cumple | No hay endpoint de residencias |
| Historial de ocupación y cambios de propietario | No cumple | `Residence.OwnerId` se sobrescribe; no hay vigencias |
| Una persona propietaria de varias unidades | Parcial | El modelo lo permite (`User.Residences`), sin forma de registrarlo |

### §1.2 y §4 Propietarios, residentes y ocupantes — No cumple

| Requisito | Estado | Evidencia |
| --- | --- | --- |
| Registrar propietarios y sus unidades | Parcial | "Residentes" lista usuarios con rol Owner; el alta y la edición fallan (defecto D2) y no vinculan una `Residence` |
| Diferenciar propietario residente/no residente, arrendatario, otros | No cumple | No existe el concepto; "residente" = usuario con rol Owner |
| Cuenta personal del propietario | Parcial | El Owner inicia sesión; su única vista (visitas) falla (defecto D4) |
| Residentes por unidad, relación, estados y fechas | No cumple | — |
| El propietario gestiona sus ocupantes | No cumple | — |
| Varios propietarios por unidad (§4.1) | No cumple | `Residence` admite un solo `OwnerId` |
| Fin de arrendamiento revoca permisos y conserva historial (§4.6, §4.7) | No cumple | — |
| Permisos de acceso independientes del inicio de sesión (§4.8) | No cumple | La persona con acceso físico y la cuenta son la misma fila `User` |

### §1.3 y §5 Visitantes y accesos — Parcial

| Requisito | Estado | Evidencia |
| --- | --- | --- |
| Registrar visitantes desde portería | Parcial | Existe el flujo, pero envía el id del usuario como `residenceId` (defecto D1) |
| Autorización previa del propietario o residente | No cumple | No hay entidad de autorización |
| Asociar unidad y persona que autoriza | Parcial | Guarda la residencia y quién registró (el guarda), no quién autoriza |
| Nombre, documento, motivo, ingreso y salida | Parcial | Falta motivo; el documento es obligatorio siempre |
| Autorizaciones únicas, por periodo, recurrentes | No cumple | — |
| Esperados, presentes y anteriores | Parcial | Hay lista con filtro de fechas; no hay filtro "presentes" ni esperados |
| Resultado de la verificación y rechazos | No cumple | — |
| Ingreso y salida por separado sin sobrescribir (§5.4) | Parcial | La salida es el campo `Visit.CheckOut`; un segundo `checkout` lo sobrescribe, `PUT visits/{id}` permite cambiar la persona y `DELETE` borra físicamente. Además la salida no tiene botón en la interfaz |
| Vehículo relacionado con la visita (§5.5) | Parcial | Solo un texto `VehiclePlate` |
| Accesos excepcionales con autor y motivo (§5.7) | No cumple | — |

### §1.4 Vehículos — No cumple

No hay entidad, API ni pantalla. Lo único relacionado es el texto `Visit.VehiclePlate`.

### §1.5 y §6 Parqueaderos — No cumple

No hay entidad, API ni pantalla. Ninguno de los 7 criterios de §6 se puede evaluar.

### §1.6 Seguridad y registro de accesos — No cumple

| Requisito | Estado | Evidencia |
| --- | --- | --- |
| Ingreso y salida de residentes, visitantes y vehículos | Parcial | Solo visitantes |
| Validar vigencia de autorizaciones | No cumple | — |
| Bitácora de eventos y novedades | No cumple | — |
| Rechazos y accesos excepcionales | No cumple | — |
| Consultas por fecha, persona, unidad, placa y tipo | Parcial | `GET visits` filtra solo por fechas |
| Restringir consultas por rol | Parcial | Políticas por rol en los controladores; ver §2 |

### §2 Roles y permisos — Parcial

| # | Criterio | Estado | Evidencia |
| --- | --- | --- | --- |
| — | Roles configurables, permisos individuales | No cumple | `Role` es un enum de tres valores; faltan Superadministrador, Residente y Auditor |
| 1 | Uno o varios roles por usuario | No cumple | `User.Role` es un único valor |
| 2 | Validación en interfaz y servidor | Parcial | El servidor valida con `[Authorize(Policy)]`; la interfaz usa `*appIfRole` y un `roleGuard` aplicado a una sola ruta. No coinciden entre sí (defecto D3) |
| 3 | Solo datos autorizados | Parcial | `GET visits/{id}` comprueba que el Owner sea dueño; `GET owners/visits` filtra por dueño. No hay separación por conjunto |
| 4 | Propietario no modifica otras unidades | Cumple por omisión | El Owner no tiene ninguna operación de escritura |
| 5 | Activar, desactivar y revocar usuarios | No cumple | `IsActive` existe pero el login no lo consulta y no hay endpoint para cambiarlo |
| 6 | Bitácora de acciones administrativas | No cumple | Solo `CreatedAt`/`UpdatedAt`, sin autor |

### §3 Menú dinámico — Parcial

| # | Criterio | Estado | Evidencia |
| --- | --- | --- | --- |
| 1 | Solo módulos permitidos | Parcial | `GET menu` devuelve las opciones del rol y el sidebar las pinta. Las rutas sembradas (`/dashboard`, `/visits`, `/users`) no existen en el frontend (`/access-control/...`); por confirmar qué hay en la BD. Si el API falla, se muestra un menú de respaldo igual para todos |
| 2 | Ocultar acciones sin permiso | Parcial | `*appIfRole` en dos botones; "Nuevo residente" y "Editar" no se ocultan |
| 3 | Actualizar al cambiar permisos | No cumple | El rol queda en el token y en `localStorage` hasta que expira |
| 4 | Bloquear rutas por URL directa | Parcial | Solo `visits/register` tiene `roleGuard`; `residents/*` y `dashboard` no |
| 5 | Navegación consistente en escritorio, tableta y móvil | Cumple | Shell responsive de la épica de rediseño (HU-03), verificado a 1440/1280/390px con datos simulados |

### §7 Seguridad y operación — Parcial

| # | Requisito | Estado | Evidencia |
| --- | --- | --- | --- |
| 1 | Autenticación segura | Parcial | JWT + BCrypt. Pero la cadena de conexión y la clave JWT están versionadas en `appsettings.json` (defecto D6) y el token vive en `localStorage` |
| 2 | Recuperación de contraseña y sesiones | No cumple | No hay refresh token, cierre remoto ni manejo de 401 en el frontend |
| 3 | Protección de datos personales | Parcial | Personas solo para Admin/Guard; el Owner recibe documento, teléfono y correo completos de sus visitantes |
| 4 | Separación entre conjuntos | No cumple | No existe la entidad conjunto |
| 5 | Quién, qué y cuándo | No cumple | — |
| 6 | Validación de obligatorios, duplicados y relaciones | Parcial | Únicos: correo y documento. Los DTO no tienen validaciones; los errores de negocio salen como 500 |
| 7 | Reportes filtrables | Parcial | Solo fechas en visitas |
| 8 | Copias de seguridad | No cumple | Sin procedimiento documentado |
| 9 | Diseño adaptable, rápido en portería | Cumple | Épica de rediseño de interfaz |

### §8 Criterios transversales

| # | Criterio | Estado | Se cierra con |
| --- | --- | --- | --- |
| 1 | Unidad vinculada con su propietario | No cumple | HU-115, HU-116 |
| 2 | Arrendatario distinto del propietario | No cumple | HU-117 |
| 3 | Visitante autorizado, ingreso y salida trazables | Parcial | HU-102, HU-105, HU-120 a HU-122 |
| 4 | Vehículo asociado a propietario, residente o visitante | No cumple | HU-124, HU-125 |
| 5 | Parqueaderos de los tres tipos con estados y asignaciones | No cumple | HU-126, HU-127 |
| 6 | Usuario sin permisos no consulta ni modifica | Parcial | HU-104, HU-109, HU-111 |
| 7 | Cada rol ve solo su menú | Parcial | HU-104, HU-111 |
| 8 | Historial conservado ante cambios | No cumple | HU-105, HU-116, HU-117, HU-127 |
| 9 | Consultas de accesos, presentes y disponibilidad | No cumple | HU-123, HU-128, HU-129 |
| 10 | Configurable para distintos tipos de conjunto | No cumple | HU-108, HU-114, HU-115 |

## Defectos en lo construido

Problemas en funciones que ya están en la interfaz. Se corrigen en la etapa 0, antes de construir
nada nuevo.

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

Se resuelve de arriba abajo. Una historia no empieza hasta que sus dependencias estén en
`Estado: Hecha`.

### Etapa 0 · Estabilizar lo construido

| # | Historia | Depende de |
| --- | --- | --- |
| HU-101 | [Secretos fuera del repositorio](historias/E0-estabilizar/HU-101-secretos-fuera-del-repositorio.md) | — |
| HU-102 | [Registrar visita contra una residencia real](historias/E0-estabilizar/HU-102-registrar-visita-residencia-real.md) | — |
| HU-103 | [Alta y edición de residentes contra el API](historias/E0-estabilizar/HU-103-alta-y-edicion-de-residentes.md) | — |
| HU-104 | [Menú, rutas y permisos coherentes para los roles actuales](historias/E0-estabilizar/HU-104-menu-rutas-y-permisos-coherentes.md) | — |
| HU-105 | [Salida de visitas sin perder historial](historias/E0-estabilizar/HU-105-salida-de-visitas.md) | HU-102 |
| HU-106 | [Sesión vencida y errores del API](historias/E0-estabilizar/HU-106-sesion-vencida-y-errores.md) | — |
| HU-107 | [Base de pruebas automatizadas](historias/E0-estabilizar/HU-107-base-de-pruebas.md) | — |

### Etapa 1 · Fundamentos: conjunto, permisos y bitácora

| # | Historia | Depende de |
| --- | --- | --- |
| HU-108 | [Conjuntos y separación de datos](historias/E1-fundamentos/HU-108-conjuntos-y-separacion-de-datos.md) | HU-107 |
| HU-109 | [Roles y permisos configurables](historias/E1-fundamentos/HU-109-roles-y-permisos-configurables.md) | HU-108 |
| HU-110 | [Usuarios: alta, activación y revocación](historias/E1-fundamentos/HU-110-gestion-de-usuarios.md) | HU-109 |
| HU-111 | [Menú dinámico y rutas por permiso](historias/E1-fundamentos/HU-111-menu-dinamico-por-permiso.md) | HU-109 |
| HU-112 | [Bitácora de auditoría](historias/E1-fundamentos/HU-112-bitacora-de-auditoria.md) | HU-108 |
| HU-113 | [Recuperación de contraseña y sesiones](historias/E1-fundamentos/HU-113-recuperacion-y-sesiones.md) | HU-110 |
| HU-114 | [Políticas del conjunto](historias/E1-fundamentos/HU-114-politicas-del-conjunto.md) | HU-108, HU-109 |

### Etapa 2 · Unidades y personas

| # | Historia | Depende de |
| --- | --- | --- |
| HU-115 | [Estructura del conjunto y unidades](historias/E2-unidades-y-personas/HU-115-estructura-y-unidades.md) | HU-108 |
| HU-116 | [Personas y propietarios de unidades](historias/E2-unidades-y-personas/HU-116-personas-y-propietarios.md) | HU-115 |
| HU-117 | [Ocupantes y arrendamientos con vigencia](historias/E2-unidades-y-personas/HU-117-ocupantes-y-arrendamientos.md) | HU-116, HU-114 |
| HU-118 | [Ficha de unidad e historial](historias/E2-unidades-y-personas/HU-118-ficha-de-unidad.md) | HU-117 |
| HU-119 | [Portal de propietario y residente](historias/E2-unidades-y-personas/HU-119-portal-propietario-y-residente.md) | HU-117, HU-111 |

### Etapa 3 · Visitantes y accesos

| # | Historia | Depende de |
| --- | --- | --- |
| HU-120 | [Eventos de ingreso y salida](historias/E3-visitantes-y-accesos/HU-120-eventos-de-ingreso-y-salida.md) | HU-115, HU-116 |
| HU-121 | [Autorizaciones de visita](historias/E3-visitantes-y-accesos/HU-121-autorizaciones-de-visita.md) | HU-120, HU-119 |
| HU-122 | [Validación en portería, rechazos y accesos excepcionales](historias/E3-visitantes-y-accesos/HU-122-validacion-en-porteria.md) | HU-121, HU-114 |
| HU-123 | [Visitantes esperados, presentes e historial](historias/E3-visitantes-y-accesos/HU-123-esperados-presentes-historial.md) | HU-122 |

### Etapa 4 · Vehículos

| # | Historia | Depende de |
| --- | --- | --- |
| HU-124 | [Inventario de vehículos](historias/E4-vehiculos/HU-124-inventario-de-vehiculos.md) | HU-116, HU-117 |
| HU-125 | [Vehículos en el control de acceso](historias/E4-vehiculos/HU-125-vehiculos-en-el-acceso.md) | HU-124, HU-122 |

### Etapa 5 · Parqueaderos

| # | Historia | Depende de |
| --- | --- | --- |
| HU-126 | [Registro de parqueaderos](historias/E5-parqueaderos/HU-126-registro-de-parqueaderos.md) | HU-115, HU-116 |
| HU-127 | [Asignaciones, ocupación y reservas](historias/E5-parqueaderos/HU-127-asignaciones-ocupacion-reservas.md) | HU-126, HU-125 |
| HU-128 | [Disponibilidad de parqueaderos](historias/E5-parqueaderos/HU-128-disponibilidad.md) | HU-127 |

### Etapa 6 · Consultas y operación

| # | Historia | Depende de |
| --- | --- | --- |
| HU-129 | [Historial de accesos y reportes](historias/E6-consultas-y-operacion/HU-129-historial-y-reportes.md) | HU-122, HU-125 |
| HU-130 | [Paneles por rol](historias/E6-consultas-y-operacion/HU-130-paneles-por-rol.md) | HU-123, HU-128 |
| HU-131 | [Protección de datos personales](historias/E6-consultas-y-operacion/HU-131-proteccion-de-datos-personales.md) | HU-109, HU-112 |
| HU-132 | [Copias de seguridad y recuperación](historias/E6-consultas-y-operacion/HU-132-copias-de-seguridad.md) | HU-101, HU-108 |
| HU-133 | [Validación de aceptación final](historias/E6-consultas-y-operacion/HU-133-validacion-de-aceptacion.md) | Todas |

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
