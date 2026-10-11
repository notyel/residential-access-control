using AccessControl.Application.Exceptions;
using AccessControl.Application.Settings;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AccessControl.Application.Security
{
    public static class PasswordPolicy
    {
        public const int MinLength = 10;

        // Las más usadas que cumplen la longitud mínima; la lista completa no cabe aquí
        private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
        {
            "1234567890", "0123456789", "0987654321", "1111111111", "qwertyuiop", "password123",
            "password1234", "contraseña123", "contrasena123", "administrador", "qwerty123456",
            "123456789a", "abcdefghij", "iloveyou123", "bienvenido123"
        };

        public static void Validate(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            {
                throw new BusinessRuleException($"La contraseña debe tener al menos {MinLength} caracteres.");
            }
            if (Common.Contains(password))
            {
                throw new BusinessRuleException("Esa contraseña es demasiado común. Elija otra.");
            }
        }
    }

    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body);
    }

    /// <summary>
    /// Implementación por defecto: no envía nada, deja el mensaje en el log. Sirve en desarrollo
    /// para tomar los enlaces de invitación y recuperación; en despliegue se reemplaza por SMTP.
    /// </summary>
    public class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body)
        {
            logger.LogInformation("Correo para {To} — {Subject}\n{Body}", to, subject, body);
            return Task.CompletedTask;
        }
    }

    public interface IUserTokenService
    {
        /// <summary>Crea un enlace de un solo uso y anula los anteriores del mismo propósito. Devuelve el token en claro.</summary>
        string Create(User user, UserTokenPurpose purpose);

        /// <summary>Usuario dueño del token si es válido, no vencido y no usado; lo marca como usado.</summary>
        Task<User> ConsumeAsync(string token, UserTokenPurpose purpose);

        string BuildLink(string path, string token);
    }

    public class UserTokenService : IUserTokenService
    {
        public static readonly TimeSpan InvitationLifetime = TimeSpan.FromHours(72);
        public static readonly TimeSpan ResetLifetime = TimeSpan.FromMinutes(60);

        private readonly ApplicationDbContext _db;
        private readonly TimeProvider _time;
        private readonly AppSettings _settings;

        public UserTokenService(ApplicationDbContext db, TimeProvider time, IOptions<AppSettings> settings)
        {
            _db = db;
            _time = time;
            _settings = settings.Value;
        }

        public string Create(User user, UserTokenPurpose purpose)
        {
            var now = _time.GetUtcNow().UtcDateTime;

            // un enlace nuevo deja sin efecto los anteriores
            foreach (var previous in _db.UserTokens.Where(t => t.UserId == user.Id && t.Purpose == purpose && t.UsedAt == null))
            {
                previous.UsedAt = now;
            }

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            _db.UserTokens.Add(new UserToken
            {
                User = user,
                Purpose = purpose,
                TokenHash = Hash(token),
                CreatedAt = now,
                ExpiresAt = now + (purpose == UserTokenPurpose.Invitation ? InvitationLifetime : ResetLifetime)
            });

            return token;
        }

        public async Task<User> ConsumeAsync(string token, UserTokenPurpose purpose)
        {
            var now = _time.GetUtcNow().UtcDateTime;
            var hash = Hash(token ?? string.Empty);

            var entry = await _db.UserTokens
                .Include(t => t.User).ThenInclude(u => u.Complex)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.Purpose == purpose);

            if (entry == null || entry.UsedAt != null || entry.ExpiresAt <= now)
            {
                throw new BusinessRuleException("El enlace no es válido o ya venció. Solicite uno nuevo.");
            }

            entry.UsedAt = now;
            return entry.User;
        }

        public string BuildLink(string path, string token)
        {
            return $"{_settings.FrontendUrl.TrimEnd('/')}/{path.TrimStart('/')}?token={Uri.EscapeDataString(token)}";
        }

        private static string Hash(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
