using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Security
{
    public interface IRoleProvisioner
    {
        /// <summary>
        /// Deja a todos los conjuntos con sus roles de sistema y reparte, una sola vez, los
        /// permisos del catálogo que aún no se habían repartido. Se puede ejecutar cuantas veces
        /// se quiera: no vuelve a conceder lo que un administrador quitó.
        /// </summary>
        Task SyncAsync();

        /// <summary>Agrega al contexto los roles de sistema de un conjunto nuevo (sin guardar).</summary>
        IReadOnlyList<Role> AddSystemRoles(Guid complexId);
    }

    public class RoleProvisioner : IRoleProvisioner
    {
        private readonly ApplicationDbContext _db;
        private readonly IPermissionService _permissions;

        public RoleProvisioner(ApplicationDbContext db, IPermissionService permissions)
        {
            _db = db;
            _permissions = permissions;
        }

        public IReadOnlyList<Role> AddSystemRoles(Guid complexId)
        {
            var roles = SystemRoles.All.Select(definition => NewRole(complexId, definition)).ToList();
            _db.Roles.AddRange(roles);
            return roles;
        }

        public async Task SyncAsync()
        {
            var complexIds = await _db.Complexes.Select(c => c.Id).ToListAsync();
            var roles = await _db.Roles.IgnoreQueryFilters()
                .Include(r => r.Permissions)
                .Where(r => r.SystemKey != null)
                .ToListAsync();

            var applied = (await _db.AppliedPermissionDefaults.Select(a => a.Key).ToListAsync()).ToHashSet();
            var pending = Permissions.AllKeys.Where(k => !applied.Contains(k)).ToList();

            foreach (var complexId in complexIds)
            {
                foreach (var definition in SystemRoles.All)
                {
                    var role = roles.FirstOrDefault(r => r.ComplexId == complexId && r.SystemKey == definition.Key);
                    if (role == null)
                    {
                        // conjunto sin este rol de sistema: nace con todos sus permisos por defecto
                        _db.Roles.Add(NewRole(complexId, definition));
                        continue;
                    }

                    foreach (var key in pending.Where(definition.Permissions.Contains))
                    {
                        if (role.Permissions.All(p => p.Permission != key))
                        {
                            role.Permissions.Add(new RolePermission { Permission = key });
                        }
                    }
                }
            }

            _db.AppliedPermissionDefaults.AddRange(pending.Select(k => new AppliedPermissionDefault { Key = k }));

            await _db.SaveChangesAsync();
            _permissions.Invalidate();
        }

        private static Role NewRole(Guid complexId, SystemRoleDefinition definition) => new()
        {
            ComplexId = complexId,
            Name = definition.Name,
            Description = definition.Description,
            SystemKey = definition.Key,
            Permissions = definition.Permissions.Select(k => new RolePermission { Permission = k }).ToList()
        };
    }
}
