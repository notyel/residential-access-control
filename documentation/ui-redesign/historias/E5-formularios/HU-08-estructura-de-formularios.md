# HU-08 · Estructura estándar de formularios

**Épica:** E5 Formularios · **Estado:** Hecha · **Depende de:** HU-01

## Historia

Como usuario, quiero formularios organizados en secciones y columnas que aprovechen el ancho de
la pantalla, para completar un registro sin desplazarme de más.

## Criterios de aceptación

- [x] Todos los formularios usan la misma estructura: grilla de página (contenido + columna lateral opcional de 340px) y secciones en tarjetas con título numerado.
- [x] Los campos se distribuyen en grilla de 3 columnas (≥ 1100px), 2 (≥ 821px) y 1 (móvil); un campo puede ocupar 2 o 3 columnas.
- [x] Un único `appearance` de `mat-form-field` (`outline`) configurado globalmente; altura de control 44px.
- [x] Guardar/Cancelar se declaran en el navbar (HU-04); en móvil quedan accesibles sin hacer scroll.
- [x] Errores de validación junto al campo (`mat-error`) y un resumen de error de servidor en la parte superior de la sección.
- [x] Solo iconos de `lucide-angular` (se eliminan los `mat-icon` con ligaduras).
- [x] Los selectores de entidades relacionadas (p. ej. residencia) abren el panel lateral (HU-06) en lugar de un `mat-select` con todos los registros.
- [x] El contenido del formulario hace scroll interno; la columna lateral (resumen) queda fija en escritorio.

## Diseño técnico

- `core/theme/_forms.scss`: clases `.app-form-page`, `.app-form-grid`, modificadores `.span-2`/`.span-3`, con container queries.
- `FormSectionComponent` (`shared/components/form-section`): entradas `step`, `title`; proyecta el cuerpo.
- `MAT_FORM_FIELD_DEFAULT_OPTIONS` en `core/config/app.config.ts` con `{ appearance: 'outline', subscriptSizing: 'dynamic' }`.
- Mensajes de validación centralizados en `shared/utils/validators.ts`.

## Archivos

`front/src/app/core/theme/_forms.scss`, `front/src/app/shared/components/form-section/*`,
`front/src/app/core/config/app.config.ts`, `front/src/app/shared/utils/validators.ts`.

## Verificación

Maqueta de formulario con 6 campos a 1440/1024/390px en ambos temas.
