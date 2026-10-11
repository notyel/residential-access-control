# HU-113 · Recuperación de contraseña y sesiones

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Hecha con pendientes · **Depende de:** HU-110
**Requerimiento:** §7.1, §7.2

## Historia

Como usuario, quiero recuperar mi contraseña sin ayuda y ver y cerrar mis sesiones abiertas, para
mantener el control de mi cuenta.

## Criterios de aceptación

- [x] "¿Olvidó su contraseña?" en el login: pide el correo y responde siempre lo mismo, exista o no la cuenta.
- [x] El enlace enviado sirve una sola vez y vence a los 60 minutos; el mismo mecanismo se usa para la invitación de HU-110 (vence a las 72 horas).
- [x] Política de contraseña: mínimo 10 caracteres y rechazo de las más comunes; se valida en servidor.
- [x] Cambiar o restablecer la contraseña cierra todas las demás sesiones del usuario.
- [ ] Token de acceso corto (15 min) más token de renovación rotativo; el frontend renueva sin interrumpir al usuario. Pendiente.
- [ ] Reutilizar un token de renovación ya usado cierra toda esa familia de sesiones. Pendiente.
- [ ] El token de renovación viaja en cookie `HttpOnly`, `Secure`, `SameSite=Strict`; el de acceso solo vive en memoria. Nada de sesión queda en `localStorage`. Pendiente: el token sigue en localStorage.
- [ ] "Mi cuenta → Sesiones": lista de sesiones (dispositivo, IP, último uso) con "Cerrar" y "Cerrar todas las demás". Pendiente.
- [x] "Cerrar sesión" invalida la sesión en el servidor.
- [x] Bloqueo temporal tras 5 intentos fallidos seguidos (15 min) y límite de peticiones en login y recuperación.
- [x] Todos estos eventos van a la bitácora (HU-112).

## Diseño técnico

- Tablas `UserSession` (hash del token de renovación, familia, expiración, revocada) y `UserToken` (hash, propósito, expiración, usado).
- `IEmailSender` con implementación SMTP configurable; en desarrollo escribe el enlace en el log.
- Se guarda solo el hash de cada token.
- El panel de portería necesita sesiones largas en un equipo compartido: la duración máxima de sesión es configurable y pasa a ser política del conjunto en HU-114.

## Verificación

Flujo completo de recuperación en local leyendo el enlace del log; usar el enlace dos veces (la
segunda falla). Dejar pasar la expiración del token de acceso y seguir navegando sin volver al login.

## Cómo quedó

- Recuperación de contraseña e invitación con enlaces de un solo uso (60 minutos y 72 horas); política de 10 caracteres con lista de contraseñas comunes; cambiar la contraseña cierra las demás sesiones.
- Bloqueo de 15 minutos tras 5 intentos fallidos seguidos y límite de peticiones por dirección en los endpoints de autenticación (`RateLimits:AuthPerMinute`, 20 por defecto).
- `POST auth/logout` invalida en el servidor todos los tokens del usuario; el frontend lo llama al cerrar sesión. En el navegador solo queda el token; usuario y permisos viven en memoria.
- **No se hizo** la renovación con token corto y cookie `HttpOnly`, ni la lista de sesiones por dispositivo. Hoy la sesión es un token único de 240 minutos que el servidor puede invalidar en cualquier momento (sello de seguridad), y "cerrar sesión" las cierra todas. Queda como mejora: exige decidir cómo se despliegan frontend y API (mismo sitio para la cookie).
- El correo sale por `IEmailSender`, que por defecto solo escribe en el log: **falta configurar un envío real (SMTP)** antes de producción.
