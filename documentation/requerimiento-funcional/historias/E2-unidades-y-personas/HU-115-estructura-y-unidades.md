# HU-115 · Estructura del conjunto y unidades

**Épica:** E7 · **Etapa:** 2 Unidades y personas · **Estado:** Pendiente · **Depende de:** HU-108
**Requerimiento:** §1.1, §8.1, §8.10

## Historia

Como administrador del conjunto, quiero registrar la estructura física (torres, bloques, etapas) y
las unidades (casas, apartamentos, locales), para que todo lo demás se asocie a una unidad real.

## Criterios de aceptación

- [ ] Entidad `Structure` anidable y opcional (torre, bloque, etapa, piso…): un conjunto de casas puede no tener ninguna; un edificio puede tener torre → piso.
- [ ] Los tipos de estructura y de unidad son catálogos configurables por conjunto, con valores iniciales (torre, bloque; casa, apartamento, local).
- [ ] Entidad `Unit`: código, tipo, estructura (opcional), área y notas opcionales, activa.
- [ ] El código de la unidad es único dentro del conjunto (restricción en base de datos); el duplicado responde 409 con mensaje claro.
- [ ] Se genera una etiqueta legible a partir de la estructura y el código (p. ej. "Torre A · 102").
- [ ] Pantalla "Unidades inmobiliarias": lista paginada con búsqueda por código y filtros por estructura, tipo y estado; alta y edición.
- [ ] Creación en lote: de un rango (torre, pisos, unidades por piso) con vista previa antes de guardar.
- [ ] Una unidad no se borra; se desactiva. Una unidad inactiva no admite nuevos propietarios, ocupantes ni visitas, y conserva su historial.
- [ ] Permisos `units.read` y `units.write`; opción de menú "Unidades inmobiliarias".
- [ ] Migración: cada `Residence` existente se convierte en una `Unit` con el mismo id e identificador; `Visit.ResidenceId` pasa a `UnitId`. `Residence` se elimina.
- [ ] El selector de residencia de Registrar visita (HU-102) pasa a buscar unidades.

## Diseño técnico

- Índice único `(ComplexId, Code)`; la unicidad no depende de la estructura para que el código sirva como identificador en portería.
- `UnitsController`, `StructuresController`; frontend en `modules/access-control/units`.
- El estado de ocupación (propietario / arrendada / desocupada) no es un campo de `Unit`: se calcula en HU-117.

## Verificación

Crear una torre con 3 pisos × 4 apartamentos en lote, intentar repetir un código (409), desactivar
una unidad y comprobar que no aparece en el selector de Registrar visita. Las visitas anteriores a
la migración conservan su unidad.
