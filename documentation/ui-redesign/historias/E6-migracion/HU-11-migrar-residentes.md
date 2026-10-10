# HU-11 · Migrar Residentes

**Épica:** E6 Migración · **Estado:** Hecha · **Depende de:** HU-04, HU-07, HU-08

## Historia

Como administrador, quiero la gestión de residentes con la misma tabla y los mismos formularios
que el resto del sistema, para trabajar con una interfaz consistente.

## Criterios de aceptación

- [x] Listado: título "Residentes" y acción "Nuevo residente" en el navbar; los residentes se muestran en `app-data-table` (Residente, Apartamento, Correo, Acciones). Las tarjetas actuales (`app-resident-card`) quedan como la presentación móvil de la fila o se eliminan.
- [x] Nuevo y Editar residente comparten un único componente de formulario (`resident-form`) con la estructura de HU-08: campos en grilla (Nombre, Apellido, Apartamento, Correo, Contraseña solo al crear).
- [x] Navbar de Nuevo/Editar: volver, miga "Residentes /", Cancelar y Guardar.
- [x] Estados de carga, vacío ("No hay residentes registrados" + acción) y error.

## Tareas técnicas

1. Extraer `residents/components/resident-form` y usarlo desde `new-resident` y `edit-resident`.
2. Reescribir `residents.component.*` con la tabla.
3. Actualizar `residents.routes.ts` con `title` y `data`.

## Archivos

`front/src/app/modules/access-control/residents/**`.

## Verificación

Crear y editar un residente; listado en 1440/390px, claro y oscuro.
