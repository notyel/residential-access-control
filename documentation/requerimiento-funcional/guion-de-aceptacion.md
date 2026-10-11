# Guion manual de aceptación

Complementa a las pruebas automatizadas de aceptación
(`backend/AccessControl.Tests/Integration/AcceptanceTests.cs`):

```bash
dotnet test backend/AccessControl.Tests -c Release --filter Category=Aceptacion
```

El guion se ejecuta sobre conjuntos recién creados, en el entorno local (`devdb` + `api-local` +
`front-local`) o en uno de pruebas. Anote al final quién lo ejecutó y cuándo.

## Preparación

1. Entre como **superadministrador** → *Conjuntos* → cree dos conjuntos, cada uno con su
   administrador:
   - **Casas del Lago**: tipo conjunto. En *Políticas*: visitas sin autorización "A criterio de
     portería", documento del visitante no obligatorio.
   - **Torres del Parque**: tipo edificio. En *Políticas*: visitas sin autorización "No se
     permiten".
2. En cada conjunto, como administrador, cree un usuario con rol **Portería** y otro con rol
   **Auditor** (*Usuarios* → *Nuevo usuario*). Acepte las invitaciones (en local, el enlace sale en
   el log de la API).

## Criterios

| # | Pasos | Resultado esperado |
| --- | --- | --- |
| 8.1 | *Unidades* → nueva unidad `A-101`. *Propietarios y residentes* → nueva persona. En la ficha de la unidad → *Agregar propietario*. | La ficha muestra al propietario con su fecha de inicio. |
| 8.2 | En la misma ficha → *Agregar ocupante* con relación **Arrendatario** (otra persona). | Propietario y arrendatario aparecen cada uno con su relación; la unidad figura "Arrendada". |
| 8.3 | Dé acceso al arrendatario (*Dar acceso a la plataforma*, rol Residente). Como residente: *Autorizar visitantes* → autorice a alguien para hoy. Como portería: *Validar visitantes* → busque el nombre → *Registrar ingreso* → luego *Salida*. | En *Visitantes* → *Historial* → detalle: autorización previa, quién autorizó, quién registró la entrada y la salida, y las dos horas. |
| 8.4 | *Vehículos* → registre uno del propietario y uno del residente. Como portería registre una visita con placa. *Verificar vehículos* → escriba cada placa (pruebe con guiones y minúsculas). | Verde "Autorizado" con unidad y responsable para los dos primeros; "Vehículo de una visita" para el tercero; "No registrado" para una placa inventada. |
| 8.5 | *Parqueaderos* → *Crear en lote*: 2 privados, 3 comunitarios, 5 de visitantes. Asocie un privado a una unidad; asigne temporalmente un comunitario; ponga uno de visitantes fuera de servicio; registre una visita con vehículo eligiendo un parqueadero. | Las tarjetas por tipo muestran exactamente lo registrado y la suma de estados coincide con la capacidad. Al registrar la salida de la visita, el espacio vuelve a disponible sin otra acción. |
| 8.6 | Entre con cada rol e intente abrir por URL una pantalla que no está en su menú (p. ej. portería → `/access-control/users`; residente → `/access-control/units`). | "Sin acceso". Con el residente, *Visitantes* e *Historial de accesos* solo muestran su unidad, con los documentos enmascarados. |
| 8.7 | Compare el menú de cada rol con la lista de abajo. | Coinciden. |
| 8.8 | Cambie el propietario de `A-101` (*Transferir*), finalice el arrendamiento, revoque una autorización de visita y reasigne el parqueadero privado a otra unidad. | La ficha de la unidad muestra al propietario y al arrendatario anteriores en *Historial* con sus fechas; la autorización aparece en "Revocadas" con su motivo; el parqueadero muestra las dos asociaciones en su historial. |
| 8.9 | *Historial de accesos*: filtre por placa y por fechas → *Exportar CSV*. *Visitantes* → *Presentes*. *Reportes* → "Ocupación de parqueaderos por tipo" → *Exportar CSV*. | Las consultas muestran solo lo filtrado; los CSV se descargan y abren con acentos correctos; en *Bitácora* quedan las dos exportaciones con sus filtros. |
| 8.10 | En **Casas del Lago**, portería registra una visita sin autorización y sin documento. En **Torres del Parque** (unidad dentro de una estructura "Torre A"), intente lo mismo. | En Casas entra ("Aprobó portería"). En Torres el sistema lo impide y solo ofrece rechazar o, con permiso, acceso excepcional; con autorización previa sí entra. Ningún conjunto ve datos del otro. |

## Menú por rol de sistema (§8.7)

| Rol | Opciones |
| --- | --- |
| Administrador | Panel, Unidades, Propietarios y residentes, Validar visitantes, Visitantes, Autorizaciones, Verificar vehículos, Vehículos, Parqueaderos, Usuarios, Roles, Novedades, Historial de accesos, Reportes, Bitácora, Políticas |
| Portería | Panel, Unidades, Propietarios y residentes, Validar visitantes, Visitantes, Autorizaciones, Verificar vehículos, Vehículos, Parqueaderos, Novedades, Historial de accesos |
| Propietario | Panel, Visitantes, Mis propiedades, Mi residencia, Autorizar visitantes, Mis vehículos, Mis parqueaderos, Historial de accesos |
| Residente | Panel, Visitantes, Mi residencia, Autorizar visitantes, Mis vehículos, Mis parqueaderos, Historial de accesos |
| Auditor | Panel, Unidades, Propietarios y residentes, Visitantes, Autorizaciones, Vehículos, Parqueaderos, Usuarios, Roles, Novedades, Historial de accesos, Reportes, Bitácora, Políticas (sin ningún botón de crear o editar) |

## Registro de ejecuciones

| Fecha | Entorno | Ejecutó | Resultado | Observaciones |
| --- | --- | --- | --- | --- |
| *Pendiente* | | | | |
