# HU-115 · Estructura del conjunto y unidades

**Épica:** E7 · **Etapa:** 2 Unidades y personas · **Estado:** Hecha con pendientes · **Depende de:** HU-108
**Requerimiento:** §1.1, §8.1, §8.10

## Historia

Como administrador del conjunto, quiero registrar la estructura física (torres, bloques, etapas) y
las unidades (casas, apartamentos, locales), para que todo lo demás se asocie a una unidad real.

## Criterios de aceptación

- [x] Entidad `Structure` anidable y opcional (torre, bloque, etapa, piso…): un conjunto de casas puede no tener ninguna; un edificio puede tener torre → piso.
- [x] Los tipos de estructura y de unidad son catálogos configurables por conjunto, con valores iniciales (torre, bloque; casa, apartamento, local).
- [x] Entidad `Unit`: código, tipo, estructura (opcional), área y notas opcionales, activa.
- [x] El código de la unidad es único dentro del conjunto (restricción en base de datos); el duplicado responde 409 con mensaje claro.
- [x] Se genera una etiqueta legible a partir de la estructura y el código (p. ej. "Torre A · 102").
- [x] Pantalla "Unidades inmobiliarias": lista paginada con búsqueda por código y filtros por estructura, tipo y estado; alta y edición.
- [x] Creación en lote: de un rango (torre, pisos, unidades por piso) con vista previa antes de guardar.
- [x] Una unidad no se borra; se desactiva. Una unidad inactiva no admite nuevos propietarios, ocupantes ni visitas, y conserva su historial.
- [x] Permisos `units.read` y `units.write`; opción de menú "Unidades inmobiliarias".
- [x] Migración: cada `Residence` existente se convierte en una `Unit` con el mismo id e identificador; `Visit.ResidenceId` pasa a `UnitId`. `Residence` se elimina.
- [x] El selector de residencia de Registrar visita (HU-102) pasa a buscar unidades.

## Diseño técnico

- Índice único `(ComplexId, Code)`; la unicidad no depende de la estructura para que el código sirva como identificador en portería.
- `UnitsController`, `StructuresController`; frontend en `modules/access-control/units`.
- El estado de ocupación (propietario / arrendada / desocupada) no es un campo de `Unit`: se calcula en HU-117.

## Verificación

Crear una torre con 3 pisos × 4 apartamentos en lote, intentar repetir un código (409), desactivar
una unidad y comprobar que no aparece en el selector de Registrar visita. Las visitas anteriores a
la migración conservan su unidad.

## Cómo quedó

- `Structure` (anidable y opcional) y `Unit` con código único por conjunto y etiqueta calculada ("Torre A · Piso 1 · 101") que se actualiza al renombrar la estructura.
- Catálogos configurables por conjunto (`CatalogItem`): rigen los valores iniciales del código hasta que el conjunto guarda los suyos. Hay ocho: tipos de estructura, de unidad y de documento, motivos de visita, tipos de vehículo, motivos de rechazo, puntos de acceso y categorías de novedad.
- La migración `UnitsAndPeople` convierte cada residencia en una unidad con el mismo id (las visitas conservan su destino) y elimina `Residence`.
- Creación en lote por rango de pisos con vista previa; las unidades no se borran, se desactivan.
- **Pendiente:** no hay pantalla para editar los catálogos ni para renombrar o eliminar estructuras (el API sí lo permite: `PUT api/catalogs/{nombre}`, `PUT/DELETE api/structures/{id}`). Las estructuras se crean desde el formulario de unidad.
