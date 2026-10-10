using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AccessControl.Application.Security
{
    public interface IPermissionService
    {
        /// <summary>Unión de los permisos de los roles activos del usuario.</summary>
        Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId);

        /// <summary>Descarta lo que esté en memoria: se llama al cambiar roles, permisos o asignaciones.</summary>
        void Invalidate();
    }

    /// <summary>
    /// Los permisos se resuelven en el servidor en cada petición (no viajan en el token), así un
    /// cambio hecho por el administrador rige desde la siguiente petición del usuario afectado.
    /// </summary>
    public class PermissionService : IPermissionService
    {
        // Sube con cada cambio; forma parte de la clave de caché, así que lo anterior deja de usarse
        private static long _version;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public PermissionService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId)
        {
            var key = $"permissions:{Interlocked.Read(ref _version)}:{userId}";
            if (_cache.TryGetValue(key, out IReadOnlySet<string>? cached) && cached != null)
            {
                return cached;
            }

            // Sin filtro de conjunto: se consulta por usuario, y un usuario solo tiene roles de su conjunto
            var permissions = await _db.Set<UserRole>()
                .IgnoreQueryFilters()
                .Where(ur => ur.UserId == userId && ur.Role.IsActive)
                .SelectMany(ur => ur.Role.Permissions.Select(p => p.Permission))
                .Distinct()
                .ToListAsync();

            IReadOnlySet<string> result = permissions.Where(Permissions.IsValid).ToHashSet();
            _cache.Set(key, result, CacheDuration);
            return result;
        }

        public void Invalidate() => Interlocked.Increment(ref _version);
    }
}
