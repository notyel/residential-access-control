# HU-102 · Registrar visita contra una residencia real

**Épica:** E7 · **Etapa:** 0 Estabilizar · **Estado:** Hecha · **Depende de:** —
**Requerimiento:** §1.3, §8.3 · **Defecto:** D1

## Historia

Como guarda, quiero elegir la residencia de destino y guardar la visita sin errores, para registrar
el ingreso en portería.

## Situación actual

- El selector de residencia lista **usuarios** con rol Owner (`ResidentsService.getResidents()` →
  `GET users?role=Owner`) y guarda `resident.id` en `residenceId`
  (`register-visit.component.ts:206`). `Visit.ResidenceId` es FK a `Residences`: el guardado falla.
- `GET users` exige rol Admin, así que con rol Guard el selector queda vacío.
- No existe ningún endpoint de residencias.

## Criterios de aceptación

- [x] `GET api/residences?search=&pageNumber=&pageSize=` devuelve id, identificador y nombre del propietario; permitido a Admin y Guard.
- [x] El selector de Registrar visita usa ese endpoint, busca por identificador o nombre del propietario y guarda el id de la **residencia**.
- [x] Con rol Guard se puede completar el flujo: buscar persona, elegir residencia, guardar; la visita aparece en la lista con su residencia.
- [x] Si `residenceId` no existe, el API responde 400 con mensaje legible (no 500).
- [x] El resumen del formulario muestra el identificador de la residencia.

## Diseño técnico

- `ResidencesController` + `IResidencesService` con el patrón de los controladores existentes (`ResponseModel<PaginatedResultDto<ResidenceDto>>`).
- Frontend: `ResidencesService` en `visits/services`; `ResidencePickerComponent` recibe `Residence[]` en vez de `User[]`.
- Es un arreglo sobre el modelo actual; `Residence` se reemplaza por `Unit` en HU-115.

## Archivos

`backend/AccessControl.API/Controllers/ResidencesController.cs` (nuevo), servicio y DTO nuevos,
`front/.../visits/components/residence-picker/*`, `front/.../visits/pages/register-visit/*`.

## Verificación

Con el usuario guarda sembrado: registrar una visita con persona nueva y otra con persona existente
contra el API real; confirmar ambas en `GET visits`.
