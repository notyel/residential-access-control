# HU-133 · Validación de aceptación final

**Épica:** E7 · **Etapa:** 6 Consultas y operación · **Estado:** Pendiente · **Depende de:** HU-101 a HU-132
**Requerimiento:** §8 completo

## Historia

Como dueño del producto, quiero una demostración repetible de los diez criterios transversales, para
declarar la primera versión funcionalmente aceptable.

## Criterios de aceptación

Cada criterio se demuestra con una prueba automatizada de extremo a extremo y con un guion manual
sobre un conjunto de demostración recién creado.

- [ ] **§8.1** Se registra una unidad y se vincula con su propietario; la ficha lo muestra.
- [ ] **§8.2** Se registra un arrendatario en una unidad con propietario no residente; cada uno aparece con su relación.
- [ ] **§8.3** Un residente autoriza un visitante; portería valida, registra ingreso y salida; el detalle muestra autorización, quién autorizó, operador y horas.
- [ ] **§8.4** Se registra un vehículo de propietario, uno de residente y uno de visitante; la verificación por placa devuelve la asociación correcta de cada uno.
- [ ] **§8.5** Existen parqueaderos privados, comunitarios y de visitantes con asignaciones; el resumen por tipo coincide con lo registrado.
- [ ] **§8.6** Para cada rol de sistema, una matriz de pruebas llama a todos los endpoints: responde 200 solo donde tiene permiso y 403/404 en el resto; ningún usuario obtiene datos de otro conjunto ni de otra unidad.
- [ ] **§8.7** El menú de cada rol de sistema coincide con el definido en §3; las rutas fuera del menú muestran "Sin acceso".
- [ ] **§8.8** Tras cambiar propietario, finalizar un arrendamiento, revocar una autorización y reasignar un parqueadero, los cuatro hechos anteriores siguen consultables con sus fechas.
- [ ] **§8.9** Se generan las consultas de accesos (filtradas), visitantes presentes y disponibilidad de parqueaderos, y se exportan.
- [ ] **§8.10** Se crean dos conjuntos distintos (uno de casas sin torres, con visitas a criterio de portería; otro de torres, con autorización previa obligatoria) y cada uno opera con sus reglas sin cambios de código.
- [ ] Se actualiza la sección "Validación por sección" del [README de la épica](../../README.md): todo en *Cumple*, o con la excepción anotada y aceptada.
- [ ] Se revisan los pendientes abiertos de las historias `Hecha con pendientes` y se decide cuáles pasan a segunda fase junto con §9.

## Diseño técnico

- Script de datos de demostración (`dotnet run -- seed-demo`) que crea los dos conjuntos, para que el guion manual sea repetible.
- La matriz de §8.6 se genera recorriendo los endpoints registrados y su atributo `[HasPermission]`, así un endpoint nuevo sin permiso declarado hace fallar la prueba.
- Guion manual en `documentation/requerimiento-funcional/guion-de-aceptacion.md`.

## Verificación

`dotnet test --filter Category=Aceptacion` en verde y guion manual ejecutado y firmado con fecha.
