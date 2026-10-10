# Requerimientos funcionales: Sistema de control de acceso residencial

Texto fuente del requerimiento. Las historias lo citan por sección (p. ej. "§6" = reglas de
parqueaderos). No se edita al implementar; los cambios de alcance se anotan en el
[README de la épica](README.md).

El sistema debe permitir administrar de forma centralizada el acceso de propietarios, residentes,
arrendatarios, visitantes y vehículos en conjuntos residenciales, edificios y propiedades con
portería. También debe gestionar las unidades inmobiliarias, los parqueaderos, los permisos de
acceso y las funciones disponibles para cada rol.

El diseño debe ser modular, de manera que cada conjunto pueda configurar sus unidades, zonas
comunes, parqueaderos y políticas de acceso según sus necesidades.

## 1. Módulos funcionales del sistema

### 1.1 Administración de propiedades y unidades

- Registrar conjuntos, edificios, torres, bloques, casas, apartamentos y locales.
- Asignar un identificador único a cada unidad inmobiliaria.
- Consultar propietarios, ocupantes actuales, vehículos y parqueaderos asociados a cada unidad.
- Mantener el historial de ocupación, cambios de propietario y arrendamientos.
- Permitir que una persona sea propietaria de varias unidades.

### 1.2 Propietarios y residentes

- Registrar los datos de los propietarios y sus unidades.
- Diferenciar entre propietario residente, propietario no residente, arrendatario y otros ocupantes autorizados.
- Permitir que un propietario tenga acceso a su cuenta personal.
- Registrar los residentes autorizados por unidad y su relación con el inmueble.
- Gestionar estados de vinculación, fechas de inicio y finalización de residencia y permisos de acceso.
- Permitir que el propietario autorice o actualice los datos de sus ocupantes, según las políticas de la administración.

### 1.3 Control de visitantes

- Registrar visitantes desde la portería o mediante una autorización previa del propietario o residente.
- Asociar cada visitante con la unidad que visita y con la persona que autoriza su ingreso.
- Registrar nombre, documento cuando corresponda, motivo de visita, fecha y hora de ingreso y salida.
- Gestionar autorizaciones únicas, por periodos definidos o recurrentes.
- Consultar visitantes esperados, visitantes presentes y visitas anteriores.
- Registrar el resultado de la verificación en portería y los ingresos rechazados.

### 1.4 Inventario de vehículos

- Registrar automóviles, motocicletas y otros tipos de vehículos autorizados.
- Asociar cada vehículo con su propietario, residente o unidad inmobiliaria.
- Registrar placa, tipo, marca, color y demás datos requeridos.
- Diferenciar vehículos permanentes de vehículos visitantes.
- Gestionar autorizaciones, estados y novedades de ingreso y salida.
- Consultar los vehículos registrados por unidad y detectar placas sin autorización vigente.

### 1.5 Administración de parqueaderos

- Registrar los espacios de parqueo con código, ubicación, tipo y estado.
- Diferenciar parqueaderos privados, comunitarios y destinados a visitantes.
- Asociar parqueaderos privados con sus propietarios o unidades.
- Controlar la disponibilidad de parqueaderos comunitarios y de visitantes.
- Registrar ocupación, reservas cuando aplique, asignaciones temporales y novedades.
- Consultar capacidad total, espacios ocupados, disponibles y fuera de servicio.
- Evitar asignaciones incompatibles o la ocupación simultánea de un mismo espacio.

### 1.6 Seguridad y registro de accesos

- Registrar cada ingreso y salida de residentes, visitantes y vehículos.
- Validar que las autorizaciones estén vigentes antes de permitir el acceso.
- Mantener una bitácora consultable de eventos y novedades.
- Registrar intentos rechazados, accesos excepcionales y su justificación.
- Permitir consultas por fecha, persona, unidad, placa y tipo de acceso.
- Restringir la consulta de información según el rol del usuario.

## 2. Gestión de roles y permisos

El sistema debe contar con un esquema de roles que determine qué módulos, opciones del menú y
operaciones puede ejecutar cada usuario.

| Rol | Responsabilidades principales |
| --- | --- |
| Superadministrador | Configuración global de la plataforma y administración de conjuntos registrados. |
| Administrador del conjunto | Gestionar unidades, propietarios, residentes, parqueaderos, visitantes, usuarios y políticas de acceso. |
| Personal de portería o vigilancia | Validar autorizaciones, registrar ingresos y salidas, consultar información necesaria y reportar novedades. |
| Propietario | Consultar sus propiedades, registrar o autorizar residentes, autorizar visitantes y administrar sus vehículos y parqueaderos asociados, según permisos. |
| Residente o arrendatario | Consultar su vinculación residencial, registrar solicitudes de visita y administrar sus vehículos autorizados según las políticas establecidas. |
| Auditor o consulta | Consultar reportes y registros autorizados sin modificar información operativa. |

Los roles deben ser configurables y no depender exclusivamente de una lista fija. La administración
podrá habilitar o restringir permisos individuales según las necesidades del conjunto.

### Criterios de aceptación (§2)

1. Cada usuario debe tener uno o varios roles autorizados.
2. El sistema debe validar los permisos tanto en la interfaz como en las operaciones del servidor.
3. Un usuario solo debe consultar las propiedades y los datos para los que tenga autorización.
4. Un propietario no debe poder modificar la información de otras unidades sin un permiso adicional.
5. El sistema debe permitir activar, desactivar y revocar accesos de usuarios.
6. Las acciones administrativas sensibles deben quedar registradas en una bitácora.

## 3. Menú dinámico según el rol

La navegación debe generarse a partir de los permisos efectivos del usuario autenticado. No basta
con ocultar opciones visualmente: las operaciones restringidas también deben estar protegidas en el
servidor.

### Ejemplo de estructura de navegación

La siguiente estructura es una propuesta funcional, no una interfaz definitiva.

- **Administrador:** Panel principal · Unidades inmobiliarias · Propietarios y residentes · Visitantes · Vehículos · Parqueaderos · Usuarios y roles · Reportes · Configuración
- **Portería y vigilancia:** Panel de portería · Validar visitantes · Registrar ingreso y salida · Consultar residentes · Verificar vehículos · Disponibilidad de parqueaderos · Novedades · Historial de accesos
- **Propietario:** Mis propiedades · Mis residentes · Autorizar visitantes · Mis vehículos · Mis parqueaderos · Historial de visitas
- **Residente o arrendatario:** Mi residencia · Solicitar visitas · Mis vehículos · Mis autorizaciones · Historial de accesos permitido

### Criterios de aceptación del menú (§3)

1. Mostrar únicamente los módulos y las opciones permitidos para el usuario autenticado.
2. Ocultar las acciones para las que no tenga permisos de creación, edición, eliminación o consulta.
3. Actualizar las opciones disponibles cuando cambien sus permisos o su vinculación residencial.
4. Impedir el acceso a rutas restringidas incluso si el usuario introduce directamente la URL.
5. Mantener una navegación consistente en dispositivos de escritorio, tabletas y móviles.

## 4. Reglas de negocio para propietarios y ocupantes

Esta distinción es fundamental para evitar inconsistencias en la información residencial.

1. Una unidad puede tener uno o varios propietarios registrados, según la configuración del conjunto.
2. El propietario puede residir en su inmueble o ser propietario no residente.
3. Una unidad puede estar ocupada por el propietario, arrendada o temporalmente desocupada.
4. Un arrendatario debe estar vinculado a una unidad y contar con un periodo de ocupación definido cuando corresponda.
5. Una persona puede estar vinculada a más de una unidad y tener una relación diferente con cada una.
6. Al finalizar un arrendamiento, se debe actualizar la vigencia de la ocupación y revocar los permisos asociados que ya no correspondan.
7. La finalización de la residencia no debe eliminar el historial de accesos ni los registros históricos.
8. Los permisos de acceso de propietarios y residentes deben gestionarse independientemente de sus credenciales para iniciar sesión en la plataforma.

## 5. Reglas de negocio para visitantes y accesos

1. Toda autorización debe identificar a la persona que autoriza, la unidad de destino y el periodo de vigencia.
2. Una autorización vencida, revocada o no válida no debe permitir el ingreso.
3. El sistema debe permitir registrar visitas sin autorización previa cuando la política del conjunto lo admita, dejando constancia de la verificación y aprobación correspondiente.
4. Debe registrarse el ingreso y la salida por separado, sin sobrescribir el evento original.
5. Si un visitante ingresa con vehículo, el vehículo debe quedar relacionado con la visita y su registro de acceso.
6. El sistema debe permitir consultar qué visitantes permanecen dentro del conjunto, según los registros disponibles.
7. Los accesos excepcionales deben incluir la identidad del usuario que los autorizó y el motivo.

## 6. Reglas de negocio para parqueaderos

Se deben manejar dos conceptos independientes: el tipo de parqueadero y su estado operativo.

| Tipo de parqueadero | Comportamiento esperado |
| --- | --- |
| Privado | Pertenece o está asignado a un propietario o unidad. No debe tratarse como espacio público disponible para visitantes. |
| Comunitario | Su uso depende de las reglas de asignación del conjunto; puede admitir turnos o asignaciones temporales. |
| Visitantes | Está destinado a visitas y debe permitir controlar su disponibilidad y ocupación. |

Estados recomendados: disponible, ocupado, reservado —cuando aplique— y fuera de servicio.

### Criterios de aceptación (§6)

1. Cada parqueadero debe contar con un código único dentro del conjunto.
2. Un parqueadero privado debe poder asociarse con una unidad y registrar su titular o asignatario.
3. La disponibilidad de parqueaderos comunitarios y de visitantes debe calcularse con base en sus asignaciones y ocupaciones vigentes.
4. El sistema debe impedir dos asignaciones incompatibles sobre el mismo espacio y periodo.
5. Debe ser posible consultar la cantidad total, ocupada, disponible y fuera de servicio por tipo.
6. Una reserva o asignación temporal debe tener reglas de vigencia y liberación definidas.
7. La disponibilidad debe reflejar los cambios operativos sin depender de actualizaciones manuales de contadores.

## 7. Requerimientos generales de seguridad y operación

1. Autenticación segura para usuarios administrativos y residenciales.
2. Recuperación de contraseña y administración de sesiones.
3. Protección de datos personales y acceso restringido a documentos de identidad, registros de visitas e información de vehículos.
4. Separación de datos entre conjuntos residenciales cuando la plataforma gestione múltiples clientes.
5. Registro de cambios importantes: quién realizó la acción, qué cambió y cuándo.
6. Validación de datos obligatorios, duplicados y relaciones entre propietarios, unidades, vehículos y parqueaderos.
7. Consultas y reportes filtrables por conjunto, fecha, unidad, persona y tipo de evento.
8. Copias de seguridad y mecanismos de recuperación de información.
9. Diseño adaptable a escritorio, tableta y móvil, priorizando la rapidez de operación en portería.

## 8. Criterios de aceptación transversales

El sistema se considerará funcionalmente aceptable cuando se pueda demostrar que:

1. Se registra una unidad inmobiliaria y se vincula correctamente con su propietario.
2. Se registra un arrendatario sin confundirlo con el propietario del inmueble.
3. Se autoriza un visitante y se registra su ingreso y salida con trazabilidad.
4. Se registra un vehículo y se valida su asociación con un propietario, residente o visitante.
5. Se registran parqueaderos privados, comunitarios y de visitantes, con sus estados y asignaciones.
6. Se evita que un usuario sin permisos consulte o modifique información restringida.
7. Cada rol visualiza exclusivamente las opciones de menú que tiene autorizadas.
8. Se conservan los registros históricos cuando cambia el propietario, el residente, una autorización o la asignación de un parqueadero.
9. Se pueden generar consultas de accesos, visitantes presentes y disponibilidad de parqueaderos.
10. Se puede configurar el sistema para distintos tipos de propiedades residenciales sin asumir que todos los conjuntos tienen la misma estructura o las mismas reglas.

## 9. Funcionalidades recomendadas para una segunda fase

Una vez cubiertos los requisitos principales, se pueden evaluar:

- Códigos QR o códigos temporales para autorizar visitantes.
- Notificaciones al propietario cuando llega un visitante.
- Registro fotográfico de novedades, cuando esté autorizado.
- Integración con barreras vehiculares, lectores de placas o dispositivos de control físico.
- Reservas de zonas comunes.
- Gestión de personal de servicio, contratistas y proveedores.
- Reportes de incidentes y novedades de vigilancia.
- Alertas por autorizaciones vencidas, vehículos no registrados o parqueaderos ocupados indebidamente.

Estas funcionalidades deben considerarse ampliaciones y no requisitos obligatorios para la primera
versión, salvo que el conjunto las necesite expresamente.

Recomendación: comenzar con los módulos de unidades, propietarios y residentes, visitantes,
vehículos, parqueaderos, usuarios y roles. La estructura de permisos y las relaciones entre estos
módulos deben definirse desde el principio, ya que condicionan todo el funcionamiento posterior del
sistema.
