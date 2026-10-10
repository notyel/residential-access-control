# HU-113 · Recuperación de contraseña y sesiones

**Épica:** E7 · **Etapa:** 1 Fundamentos · **Estado:** Pendiente · **Depende de:** HU-110
**Requerimiento:** §7.1, §7.2

## Historia

Como usuario, quiero recuperar mi contraseña sin ayuda y ver y cerrar mis sesiones abiertas, para
mantener el control de mi cuenta.

## Criterios de aceptación

- [ ] "¿Olvidó su contraseña?" en el login: pide el correo y responde siempre lo mismo, exista o no la cuenta.
- [ ] El enlace enviado sirve una sola vez y vence a los 60 minutos; el mismo mecanismo se usa para la invitación de HU-110 (vence a las 72 horas).
- [ ] Política de contraseña: mínimo 10 caracteres y rechazo de las más comunes; se valida en servidor.
- [ ] Cambiar o restablecer la contraseña cierra todas las demás sesiones del usuario.
- [ ] Token de acceso corto (15 min) más token de renovación rotativo; el frontend renueva sin interrumpir al usuario.
- [ ] Reutilizar un token de renovación ya usado cierra toda esa familia de sesiones.
- [ ] El token de renovación viaja en cookie `HttpOnly`, `Secure`, `SameSite=Strict`; el de acceso solo vive en memoria. Nada de sesión queda en `localStorage`.
- [ ] "Mi cuenta → Sesiones": lista de sesiones (dispositivo, IP, último uso) con "Cerrar" y "Cerrar todas las demás".
- [ ] "Cerrar sesión" invalida la sesión en el servidor.
- [ ] Bloqueo temporal tras 5 intentos fallidos seguidos (15 min) y límite de peticiones en login y recuperación.
- [ ] Todos estos eventos van a la bitácora (HU-112).

## Diseño técnico

- Tablas `UserSession` (hash del token de renovación, familia, expiración, revocada) y `UserToken` (hash, propósito, expiración, usado).
- `IEmailSender` con implementación SMTP configurable; en desarrollo escribe el enlace en el log.
- Se guarda solo el hash de cada token.
- El panel de portería necesita sesiones largas en un equipo compartido: la duración máxima de sesión es configurable y pasa a ser política del conjunto en HU-114.

## Verificación

Flujo completo de recuperación en local leyendo el enlace del log; usar el enlace dos veces (la
segunda falla). Dejar pasar la expiración del token de acceso y seguir navegando sin volver al login.
