# HU-130 · Paneles por rol

**Épica:** E7 · **Etapa:** 6 Consultas y operación · **Estado:** Pendiente · **Depende de:** HU-123, HU-128
**Requerimiento:** §3 (Panel principal, Panel de portería), §7.9

## Historia

Como usuario, quiero que la primera pantalla me muestre lo que necesito para mi trabajo: al guarda,
la operación del turno; al administrador, el estado del conjunto; al residente, lo suyo.

## Situación actual

Un único Panel con el total de visitas del mes y las últimas diez, pensado para el administrador.

## Criterios de aceptación

- [ ] El Panel se arma con bloques; cada bloque declara el permiso que lo habilita y el usuario ve solo los suyos.
- [ ] **Panel de portería**: buscador grande (persona, documento, unidad o placa) con el foco al entrar; accesos directos a registrar ingreso, registrar salida y verificar placa; visitantes esperados hoy; visitantes presentes; disponibilidad de parqueaderos de visitantes; últimas novedades.
- [ ] **Panel principal** (administrador): unidades por estado de ocupación; accesos de hoy y de la semana; presentes; rechazos y excepcionales recientes; parqueaderos por tipo; solicitudes pendientes de aprobación; vinculaciones por vencer.
- [ ] **Propietario / residente**: mis unidades, mis autorizaciones vigentes, visitas recientes a mi unidad, mis vehículos.
- [ ] **Auditor**: los indicadores del administrador, sin accesos directos de escritura.
- [ ] Cada indicador enlaza a la lista filtrada correspondiente.
- [ ] Todas las cifras se calculan en la consulta, en la zona horaria del conjunto.
- [ ] El panel de portería se actualiza solo cada 30 segundos y es usable con una mano a 390 px.
- [ ] Un usuario con varios roles ve la unión de bloques, sin duplicados.

## Diseño técnico

- `GET dashboard` devuelve solo los bloques permitidos; cada bloque es un componente independiente en `dashboard/components`.
- Reutiliza las consultas de HU-123, HU-128 y HU-129; no se crean consultas paralelas con otra lógica.

## Verificación

Entrar con cada rol de sistema y comparar los bloques visibles con esta historia. En el panel de
portería, registrar un ingreso desde el buscador sin usar el ratón.
