using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Application.Settings;
using AccessControl.Common.DTOs.Auth;
using AccessControl.Common.DTOs.User;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPermissionService _permissions;
        private readonly JwtSettings _jwt;
        private readonly IUserTokenService _tokens;
        private readonly IEmailSender _email;
        public const int MaxFailedLogins = 5;
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IAuditService _audit;
        private readonly TimeProvider _time;

        public AuthService(
            ApplicationDbContext db,
            IPermissionService permissions,
            IOptions<JwtSettings> jwt,
            IUserTokenService tokens,
            IEmailSender email,
            IAuditService audit,
            TimeProvider time)
        {
            _time = time;
            _audit = audit;
            _tokens = tokens;
            _email = email;
            _db = db;
            _permissions = permissions;
            _jwt = jwt.Value;
        }

        public async Task<LoginResponseDto?> AuthenticateAsync(string email, string password)
        {
            // Antes de iniciar sesión no hay conjunto: se busca en toda la plataforma (el correo es único)
            var user = await _db.Users
                .IgnoreQueryFilters()
                .Include(u => u.Complex)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            // Usuario inexistente, invitado, desactivado, de un conjunto inactivo o contraseña errada responden igual
            var now = _time.GetUtcNow().UtcDateTime;
            var locked = user?.LockedUntil > now;

            var allowed = user != null
                && !locked
                && user.Status == UserStatus.Active
                && user.Complex is not { IsActive: false }
                && (user.IsSuperAdmin || user.Complex != null)
                && !string.IsNullOrEmpty(user.PasswordHash)
                && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            // El intento queda en la bitácora con el correo usado; nunca la contraseña
            _audit.Record(
                allowed ? AuditActions.Login : locked ? AuditActions.LoginLocked : AuditActions.LoginFailed,
                complexId: user?.ComplexId,
                userId: user?.Id,
                userName: user == null ? email : $"{user.FirstName} {user.LastName}",
                entityType: nameof(User),
                entityId: user?.Id.ToString(),
                details: new { email });

            if (user != null && !locked)
            {
                if (allowed)
                {
                    user.FailedLoginCount = 0;
                    user.LockedUntil = null;
                }
                else if (++user.FailedLoginCount >= MaxFailedLogins)
                {
                    // bloqueo temporal: frena el tanteo de contraseñas sin dejar la cuenta inservible
                    user.LockedUntil = now + LockoutDuration;
                    user.FailedLoginCount = 0;
                }
            }
            await _db.SaveChangesAsync();

            if (!allowed || user == null) return null;

            return new LoginResponseDto
            {
                User = await GetCurrentUserAsync(user.Id),
                Token = GenerateJwtToken(user)
            };
        }

        public async Task<UserDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _db.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == userId && u.Status == UserStatus.Active)
                .Select(UsersService.ToDtoExpression)
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException("El usuario no existe.");

            // El superadministrador tiene todos los permisos del catálogo
            user.Permissions = user.IsSuperAdmin
                ? Permissions.AllKeys.OrderBy(k => k).ToList()
                : (await _permissions.GetEffectivePermissionsAsync(userId)).OrderBy(k => k).ToList();

            return user;
        }

        public async Task LogoutAllAsync(Guid userId)
        {
            var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new NotFoundException("El usuario no existe.");

            // cambiar el sello invalida todos los tokens emitidos, en cualquier dispositivo
            user.SecurityStamp = Guid.NewGuid();
            RecordFor(user, AuditActions.LogoutAll);
            await _db.SaveChangesAsync();
            _permissions.Invalidate();
        }

        public async Task AcceptInvitationAsync(string token, string password)
        {
            PasswordPolicy.Validate(password);
            var user = await _tokens.ConsumeAsync(token, UserTokenPurpose.Invitation);

            if (user.Status == UserStatus.Disabled)
            {
                throw new BusinessRuleException("El enlace no es válido o ya venció. Solicite uno nuevo.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.Status = UserStatus.Active;
            user.SecurityStamp = Guid.NewGuid();
            RecordFor(user, AuditActions.InvitationAccepted);
            await _db.SaveChangesAsync();
            _permissions.Invalidate();
        }

        public async Task RequestPasswordResetAsync(string email)
        {
            var user = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());

            // No se revela si la cuenta existe: quien llama recibe siempre la misma respuesta
            if (user == null || user.Status != UserStatus.Active)
            {
                return;
            }

            var token = _tokens.Create(user, UserTokenPurpose.PasswordReset);
            RecordFor(user, AuditActions.PasswordResetRequested);
            await _db.SaveChangesAsync();

            await _email.SendAsync(
                user.Email,
                "Recuperar su contraseña de Zentryal",
                $"Hola {user.FirstName}:\n\nPara definir una contraseña nueva use este enlace " +
                $"(vence en {UserTokenService.ResetLifetime.TotalMinutes:0} minutos y sirve una sola vez):\n" +
                _tokens.BuildLink("reset-password", token) +
                "\n\nSi usted no lo pidió, ignore este mensaje.");
        }

        public async Task ResetPasswordAsync(string token, string password)
        {
            PasswordPolicy.Validate(password);
            var user = await _tokens.ConsumeAsync(token, UserTokenPurpose.PasswordReset);

            if (user.Status != UserStatus.Active)
            {
                throw new BusinessRuleException("El enlace no es válido o ya venció. Solicite uno nuevo.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            // cambiar la contraseña cierra todas las sesiones abiertas
            user.SecurityStamp = Guid.NewGuid();
            RecordFor(user, AuditActions.PasswordReset);
            await _db.SaveChangesAsync();
            _permissions.Invalidate();
        }

        // Hechos que ocurren sin sesión: el autor es el propio dueño de la cuenta
        private void RecordFor(User user, string action)
        {
            _audit.Record(action, complexId: user.ComplexId, userId: user.Id,
                userName: $"{user.FirstName} {user.LastName}", entityType: nameof(User), entityId: user.Id.ToString());
        }

        // Los roles y permisos no van en el token: se resuelven en el servidor en cada petición
        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>{
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(AppClaims.SecurityStamp, user.SecurityStamp.ToString())
            };

            if (user.IsSuperAdmin)
            {
                claims.Add(new Claim(AppClaims.SuperAdmin, "true"));
            }
            if (user.ComplexId.HasValue)
            {
                claims.Add(new Claim(AppClaims.ComplexId, user.ComplexId.Value.ToString()));
            }
            if (user.Complex != null)
            {
                claims.Add(new Claim(AppClaims.TimeZone, user.Complex.TimeZoneId));
            }

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.ExpireMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
