# HU-10 · Migrar Registrar visita

**Épica:** E6 Migración · **Estado:** Hecha con pendientes · **Depende de:** HU-04, HU-06, HU-08

## Historia

Como guarda, quiero registrar una visita en un formulario de dos secciones con resumen, para
completar el ingreso rápido y verificar los datos antes de guardar.

## Criterios de aceptación

- [x] Navbar: volver a Visitas, miga "Visitas /", título "Registrar visita", acciones Cancelar y Guardar (deshabilitado mientras el formulario sea inválido o `canRegister` sea falso).
- [x] Sección 1 "Visitante": documento + "Buscar persona"; aviso de persona encontrada con "Limpiar"; campos de persona en grilla de 3 columnas (solo editables cuando la persona no existe).
- [x] Sección 2 "Detalle de la visita": residencia mediante selector en panel lateral (búsqueda por torre, apartamento o residente) y placa del vehículo.
- [x] Columna "Resumen" a la derecha que refleja los valores del formulario en vivo.
- [ ] Tipo de documento y tipo de persona como `mat-select`. Pendiente: el backend no define el catálogo (`DocumentType` es texto libre y `PersonType` un entero sin enum); se mantienen como campos libres hasta acordar los valores.
- [x] `Enter` en el campo documento ejecuta la búsqueda; los botones que no envían tienen `type="button"`.
- [x] Sin `mat-icon`; se unifica el contenedor (desaparece `.page-content`).

## Tareas técnicas

1. Crear `visits/components/residence-picker` (contenido del panel lateral, devuelve la residencia elegida).
2. Reescribir `register-visit.component.*` con `app-form-section` y la grilla de HU-08.
3. Resumen con `computed()` sobre `toSignal(form.valueChanges)`.

## Archivos

`front/src/app/modules/access-control/visits/pages/register-visit/*`,
`front/src/app/modules/access-control/visits/components/residence-picker/*`.

## Verificación

Flujo completo con persona existente y con persona nueva; validar a 390px y en oscuro.
