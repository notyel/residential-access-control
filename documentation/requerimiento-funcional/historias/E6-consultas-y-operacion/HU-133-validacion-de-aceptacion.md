# HU-133 · Validación de aceptación final

**Épica:** E7 · **Etapa:** 6 Consultas y operación · **Estado:** Hecha con pendientes · **Depende de:** HU-101 a HU-132
**Requerimiento:** §8 completo

## Historia

Como dueño del producto, quiero una demostración repetible de los diez criterios transversales, para
declarar la primera versión funcionalmente aceptable.

## Criterios de aceptación

Cada criterio se demuestra con una prueba automatizada de extremo a extremo y con un guion manual
sobre un conjunto de demostración recién creado.

- [x] **§8.1** Se registra una unidad y se vincula con su propietario; la ficha lo muestra.
- [x] **§8.2** Se registra un arrendatario en una unidad con propietario no residente; cada uno aparece con su relación.
- [x] **§8.3** Un residente autoriza un visitante; portería valida, registra ingreso y salida; el detalle muestra autorización, quién autorizó, operador y horas.
- [x] **§8.4** Se registra un vehículo de propietario, uno de residente y uno de visitante; la verificación por placa devuelve la asociación correcta de cada uno.
- [x] **§8.5** Existen parqueaderos privados, comunitarios y de visitantes con asignaciones; el resumen por tipo coincide con lo registrado.
- [ ] **§8.6** Para cada rol de sistema, una matriz de pruebas llama a todos los endpoints: responde 200 solo donde tiene permiso y 403/404 en el resto; ningún usuario obtiene datos de otro conjunto ni de otra unidad. **Parcial:** la matriz llama a todas las consultas (GET sin parámetros de ruta) y verifica que todo endpoint declara su protección; las operaciones de escritura se prueban por rol en las pruebas de cada módulo, no en la matriz.
- [ ] **§8.7** El menú de cada rol de sistema coincide con el definido en §3; las rutas fuera del menú muestran "Sin acceso". **Parcial:** el menú se verifica con prueba automatizada; "Sin acceso" en rutas fuera del menú se cubre con el guion manual.
- [x] **§8.8** Tras cambiar propietario, finalizar un arrendamiento, revocar una autorización y reasignar un parqueadero, los cuatro hechos anteriores siguen consultables con sus fechas.
- [x] **§8.9** Se generan las consultas de accesos (filtradas), visitantes presentes y disponibilidad de parqueaderos, y se exportan.
- [x] **§8.10** Se crean dos conjuntos distintos (uno de casas sin torres, con visitas a criterio de portería; otro de torres, con autorización previa obligatoria) y cada uno opera con sus reglas sin cambios de código.
- [x] Se actualiza la sección "Validación por sección" del [README de la épica](../../README.md): todo en *Cumple*, o con la excepción anotada y aceptada.
- [ ] Se revisan los pendientes abiertos de las historias `Hecha con pendientes` y se decide cuáles pasan a segunda fase junto con §9. **Pendiente:** están reunidos en el README de la épica; decidir cuáles pasan a segunda fase le corresponde al responsable del producto.

## Diseño técnico

- Script de datos de demostración (`dotnet run -- seed-demo`) que crea los dos conjuntos, para que el guion manual sea repetible.
- La matriz de §8.6 se genera recorriendo los endpoints registrados y su atributo `[HasPermission]`, así un endpoint nuevo sin permiso declarado hace fallar la prueba.
- Guion manual en `documentation/requerimiento-funcional/guion-de-aceptacion.md`.

## Verificación

`dotnet test --filter Category=Aceptacion` en verde y guion manual ejecutado y firmado con fecha.

## Cómo quedó

- `AcceptanceTests` (`dotnet test --filter Category=Aceptacion`) demuestra por el API los criterios §8.1 a §8.10 sobre conjuntos recién creados.
- §8.6: la matriz recorre los endpoints registrados. Falla si alguno no declara cómo se protege, y comprueba para cada rol de sistema que entra a cada consulta solo si su rol tiene el permiso, que sin sesión no se entra a ninguna y que no se obtienen datos de otra unidad ni de otro conjunto.
- §8.7: el menú de cada rol de sistema se compara con la lista definida.
- Guion manual en [guion-de-aceptacion.md](../../guion-de-aceptacion.md).
- La sección de validación del README de la épica se actualizó con el estado real y las excepciones.
