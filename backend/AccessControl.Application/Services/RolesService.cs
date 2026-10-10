using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Role;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class RolesService : IRolesService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IPermissionService _permissions;

        public RolesService(ApplicationDbContext db, ICurrentUser currentUser, IPermissionService permissions)
        {
            _db = db;
            _currentUser = currentUser;
            _permissions = permissions;
        }

        public List<PermissionDto> GetCatalog()
        {
            return Permissions.All
                .Select(p => new PermissionDto { Key = p.Key, Module = p.Module, Label = p.Label })
                .ToList();
        }

        public async Task<List<RoleDto>> GetAllAsync()
        {
            return await _db.Roles
                .OrderByDescending(r => r.SystemKey != null)
                .ThenBy(r => r.Name)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    SystemKey = r.SystemKey,
                    IsSystem = r.SystemKey != null,
                    IsActive = r.IsActive,
                    Permissions = r.Permissions.Select(p => p.Permission).OrderBy(p => p).ToList(),
                    UserCount = r.UserRoles.Count
                })
                .ToListAsync();
        }

        public async Task<RoleDto> GetByIdAsync(Guid id)
        {
            return (await GetAllAsync()).FirstOrDefault(r => r.Id == id)
                ?? throw new NotFoundException("El rol no existe.");
        }

        public async Task<RoleDto> CreateAsync(SaveRoleDto dto)
        {
            var permissions = ValidatePermissions(dto.Permissions, alreadyGranted: new HashSet<string>());
            await EnsureNameIsFreeAsync(dto.Name, null);

            var role = new Role
            {
                Name = dto.Name.Trim(),
                Description = Blank.ToNull(dto.Description),
                Permissions = permissions.Select(k => new RolePermission { Permission = k }).ToList()
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            _permissions.Invalidate();

            return await GetByIdAsync(role.Id);
        }

        public async Task<RoleDto> UpdateAsync(Guid id, SaveRoleDto dto)
        {
            var role = await FindAsync(id);
            var current = role.Permissions.Select(p => p.Permission).ToHashSet();
            var requested = ValidatePermissions(dto.Permissions, alreadyGranted: current);

            if (role.SystemKey == null)
            {
                await EnsureNameIsFreeAsync(dto.Name, id);
                role.Name = dto.Name.Trim();
            }
            // el nombre de un rol de sistema no cambia: otras partes del sistema lo identifican por él
            role.Description = Blank.ToNull(dto.Description);

            foreach (var removed in role.Permissions.Where(p => !requested.Contains(p.Permission)).ToList())
            {
                role.Permissions.Remove(removed);
            }
            foreach (var added in requested.Where(k => !current.Contains(k)))
            {
                role.Permissions.Add(new RolePermission { Permission = added });
            }

            if (current.Contains(Permissions.RolesManage) && !requested.Contains(Permissions.RolesManage))
            {
                await EnsureSomeoneElseManagesRolesAsync(role.Id);
            }

            await _db.SaveChangesAsync();
            _permissions.Invalidate();

            return await GetByIdAsync(id);
        }

        public async Task<RoleDto> SetActiveAsync(Guid id, bool isActive)
        {
            var role = await FindAsync(id);

            if (!isActive && role.Permissions.Any(p => p.Permission == Permissions.RolesManage))
            {
                await EnsureSomeoneElseManagesRolesAsync(role.Id);
            }

            role.IsActive = isActive;
            await _db.SaveChangesAsync();
            _permissions.Invalidate();

            return await GetByIdAsync(id);
        }

        public async Task DeleteAsync(Guid id)
        {
            var role = await FindAsync(id);

            if (role.SystemKey != null)
            {
                throw new ConflictException("Los roles de sistema no se pueden eliminar; se pueden desactivar.");
            }
            if (await _db.UserRoles.AnyAsync(ur => ur.RoleId == id))
            {
                throw new ConflictException("El rol tiene usuarios asignados. Quíteselo a los usuarios o desactívelo.");
            }

            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
            _permissions.Invalidate();
        }

        private async Task<Role> FindAsync(Guid id)
        {
            return await _db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new NotFoundException("El rol no existe.");
        }

        /// <summary>
        /// Solo claves del catálogo, y nadie concede un permiso que no tiene. Los que el rol ya
        /// tenía se pueden conservar aunque quien edita no los tenga.
        /// </summary>
        private HashSet<string> ValidatePermissions(IEnumerable<string> requested, HashSet<string> alreadyGranted)
        {
            var permissions = requested.Select(p => p.Trim()).Where(p => p.Length > 0).ToHashSet();

            var unknown = permissions.Where(p => !Permissions.IsValid(p)).ToList();
            if (unknown.Count > 0)
            {
                throw new BusinessRuleException($"Permisos no reconocidos: {string.Join(", ", unknown)}.");
            }

            var notOwned = permissions
                .Where(p => !alreadyGranted.Contains(p) && !_currentUser.HasPermission(p))
                .ToList();
            if (notOwned.Count > 0)
            {
                throw new ForbiddenException($"No puede conceder permisos que usted no tiene: {string.Join(", ", notOwned)}.");
            }

            return permissions;
        }

        private async Task EnsureNameIsFreeAsync(string name, Guid? currentId)
        {
            var trimmed = name.Trim();
            if (await _db.Roles.AnyAsync(r => r.Id != currentId && r.Name.ToLower() == trimmed.ToLower()))
            {
                throw new ConflictException("Ya existe un rol con ese nombre.");
            }
        }

        /// <summary>El conjunto no puede quedarse sin nadie capaz de administrar roles.</summary>
        private async Task EnsureSomeoneElseManagesRolesAsync(Guid roleId)
        {
            var someoneElse = await _db.Roles
                .Where(r => r.Id != roleId && r.IsActive && r.Permissions.Any(p => p.Permission == Permissions.RolesManage))
                .AnyAsync(r => r.UserRoles.Any(ur => ur.User.IsActive));

            if (!someoneElse)
            {
                throw new ConflictException(
                    "Este es el único rol activo con usuarios que puede administrar roles; el conjunto quedaría sin quién lo haga.");
            }
        }
    }
}
