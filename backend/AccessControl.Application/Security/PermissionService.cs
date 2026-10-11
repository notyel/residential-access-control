using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AccessControl.Application.Security
{
    /// <summary>Lo que el servidor sabe hoy de un usuario, sin depender de lo que diga su token.</summary>
    /// <param name="CanSignIn">Usuario activo y, si pertenece a un conjunto, conjunto activo.</param>
    /// <param name="SecurityStamp">Sello vigente; un token con otro sello ya no sirve.</param>
    public sealed record UserAccess(bool CanSignIn, Guid SecurityStamp, IReadOnlySet<string> Permissions);

    public interface IPermissionService
    {
        /// <summary>Estado, sello y permisos efectivos del usuario; null si no existe.</summary>
        Task<UserAccess?> GetAccessAsync(Guid userId);

        /// <summary>Unión de los permisos de los roles activos del usuario.</summary>
        Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId);

        /// <summary>Descarta lo que esté en memoria: se llama al cambiar usuarios, roles, permisos o conjuntos.</summary>
        void Invalidate();
    }

    /// <summary>
    /// El acceso se resuelve en el servidor en cada petición (no viaja en el token), así un cambio
    /// hecho por el administrador rige desde la siguiente petición del usuario afectado.
    /// </summary>
    public class PermissionService : IPermissionService
    {
        // Sube con cada cambio; forma parte de la clave de caché, así que lo anterior deja de usarse
        private static long _version;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private static readonly IReadOnlySet<string> None = new HashSet<string>();

        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public PermissionService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<UserAccess?> GetAccessAsync(Guid userId)
        {
            var key = $"access:{Interlocked.Read(ref _version)}:{userId}";
            if (_cache.TryGetValue(key, out UserAccess? cached) && cached != null)
            {
                return cached;
            }

            // Sin filtro de conjunto: se consulta por usuario, y un usuario solo tiene roles de su conjunto
            var user = await _db.Users.IgnoreQueryFilters()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Status,
                    u.SecurityStamp,
                    u.IsSuperAdmin,
                    ComplexActive = u.Complex == null ? (bool?)null : u.Complex.IsActive,
                    Permissions = u.UserRoles
                        .Where(ur => ur.Role.IsActive)
                        .SelectMany(ur => ur.Role.Permissions.Select(p => p.Permission))
                        .Distinct()
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return null;
            }

            var canSignIn = user.Status == UserStatus.Active
                && (user.IsSuperAdmin ? user.ComplexActive != false : user.ComplexActive == true);

            var access = new UserAccess(
                canSignIn,
                user.SecurityStamp,
                user.Permissions.Where(Permissions.IsValid).ToHashSet());

            _cache.Set(key, access, CacheDuration);
            return access;
        }

        public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId)
        {
            return (await GetAccessAsync(userId))?.Permissions ?? None;
        }

        public void Invalidate() => Interlocked.Increment(ref _version);
    }
}
