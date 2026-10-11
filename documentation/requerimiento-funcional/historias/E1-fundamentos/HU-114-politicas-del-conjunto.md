# HU-114 · Políticas del conjunto

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Hecha · **Depende de:** HU-108, HU-109
**Requerimiento:** introducción ("cada conjunto pueda configurar…"), §1.2, §4.1, §5.3, §8.10

## Historia

Como administrador del conjunto, quiero configurar las reglas de operación de mi conjunto, para que
el sistema se adapte a nuestro reglamento sin cambios de programa.

## Criterios de aceptación

- [x] Pantalla "Configuración → Políticas", con `settings.manage`. Cada política tiene valor por defecto y una descripción de su efecto.
- [x] Políticas iniciales (cada una la consume la historia indicada):

| Política | Valores | Por defecto | La usa |
| --- | --- | --- | --- |
| Varios propietarios por unidad | sí / no | sí | HU-116 |
| El propietario gestiona sus ocupantes | no / con aprobación / directo | con aprobación | HU-117, HU-119 |
| El residente puede autorizar visitas | sí / no | sí | HU-121 |
| Visitas sin autorización previa | no permitidas / con confirmación del residente / a criterio de portería | con confirmación | HU-122 |
| Documento del visitante obligatorio | sí / no | sí | HU-122 |
| Duración máxima de una autorización | días | 30 | HU-121 |
| Horas tras las que una visita sin salida se marca "por revisar" | horas | 24 | HU-123 |
| El residente registra sus vehículos | no / con aprobación / directo | con aprobación | HU-124 |
| Vehículos máximos por unidad | número, 0 = sin límite | 0 | HU-124 |
| Tiempo máximo en parqueadero de visitantes | horas | 12 | HU-127 |
| Reservas de parqueadero comunitario | sí / no | no | HU-127 |
| Duración máxima de sesión | horas | 12 | HU-113 |

- [x] Los cambios quedan en la bitácora con valor anterior y nuevo.
- [x] Una política nueva añadida al catálogo toma su valor por defecto en los conjuntos existentes sin migración de datos.
- [x] El servidor es quien aplica cada política; la interfaz solo adapta lo que muestra.

## Diseño técnico

- Catálogo tipado en código (`ComplexPolicies`) y tabla `ComplexPolicy (ComplexId, Key, Value)` solo con los valores que difieren del defecto.
- `IPolicyService.Get<T>(key)` con caché por conjunto, invalidada al guardar.
- Esta historia entrega el mecanismo y la pantalla; cada política empieza a tener efecto cuando se construye la historia que la usa.

## Verificación

Cambiar una política, recargar y verla persistida; la bitácora muestra el cambio. Prueba unitaria:
conjunto sin valores guardados devuelve los valores por defecto.

## Cómo quedó

- Catálogo tipado en `ComplexPolicies` (12 políticas); la tabla `ComplexPolicies` solo guarda lo que un conjunto cambió. `IPolicyService` devuelve el valor vigente y valida tipo, rango y opciones.
- `GET/PUT api/policies` con permisos `settings.read` y `settings.manage`; pantalla "Configuración → Políticas". Los cambios quedan en la bitácora con valor anterior y nuevo.
- La lectura se guarda por petición, no por conjunto: una política cambiada rige desde la siguiente petición.
- Cada política empieza a tener efecto cuando se construye la historia que la consulta.
