using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Application.Settings;
using AccessControl.Common.DTOs.Auth;
using AccessControl.Common.DTOs.User;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
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

        public AuthService(ApplicationDbContext db, IPermissionService permissions, IOptions<JwtSettings> jwt)
        {
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

            // Usuario inexistente, inactivo, de un conjunto inactivo o contraseña errada responden igual
            if (user == null || !user.IsActive) return null;
            if (user.Complex is { IsActive: false }) return null;
            if (!user.IsSuperAdmin && user.Complex == null) return null;
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

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
                .Where(u => u.Id == userId && u.IsActive)
                .Select(UsersService.ToDtoExpression)
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException("El usuario no existe.");

            // El superadministrador tiene todos los permisos del catálogo
            user.Permissions = user.IsSuperAdmin
                ? Permissions.AllKeys.OrderBy(k => k).ToList()
                : (await _permissions.GetEffectivePermissionsAsync(userId)).OrderBy(k => k).ToList();

            return user;
        }

        // Los roles y permisos no van en el token: se resuelven en el servidor en cada petición
        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>{
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email)
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
