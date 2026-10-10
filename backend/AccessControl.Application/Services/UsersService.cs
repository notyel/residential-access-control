using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Role;
using AccessControl.Common.DTOs.User;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class UsersService : IUsersService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IPermissionService _permissions;

        public UsersService(ApplicationDbContext db, ICurrentUser currentUser, IPermissionService permissions)
        {
            _db = db;
            _currentUser = currentUser;
            _permissions = permissions;
        }

        public async Task<PaginatedResultDto<UserDto>> GetUsersAsync(string? role, int pageNumber = 1, int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                // acepta el id del rol o la clave de un rol de sistema ("owner")
                var key = role.Trim().ToLower();
                query = Guid.TryParse(key, out var roleId)
                    ? query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
                    : query.Where(u => u.UserRoles.Any(ur => ur.Role.SystemKey == key));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(ToDtoExpression)
                .ToListAsync();

            return new PaginatedResultDto<UserDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<UserDto> GetUserByIdAsync(Guid id)
        {
            return await _db.Users.Where(u => u.Id == id).Select(ToDtoExpression).FirstOrDefaultAsync()
                ?? throw new NotFoundException("El usuario no existe.");
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var complexId = _currentUser.ComplexId
                ?? throw new BusinessRuleException("Seleccione un conjunto antes de crear usuarios.");

            var email = dto.Email.Trim();
            // el correo es único en toda la plataforma: se busca sin el filtro de conjunto
            if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            {
                throw new ConflictException("Ya existe un usuario con ese correo.");
            }

            var roles = await ResolveRolesAsync(dto.RoleIds, dto.RoleKeys);

            var user = new User
            {
                ComplexId = complexId,
                Email = email,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                ApartmentNumber = Blank.ToNull(dto.ApartmentNumber) ?? string.Empty,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                UserRoles = roles.Select(r => new UserRole { RoleId = r.Id }).ToList()
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            _permissions.Invalidate();

            return await GetUserByIdAsync(user.Id);
        }

        public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new NotFoundException("El usuario no existe.");

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.ApartmentNumber = Blank.ToNull(dto.ApartmentNumber) ?? string.Empty;
            await _db.SaveChangesAsync();

            return await GetUserByIdAsync(id);
        }

        /// <summary>
        /// Roles activos del conjunto, por id o por clave de sistema. Nadie asigna un rol con
        /// permisos que él mismo no tiene: sería una forma de concederse más de lo que le dieron.
        /// </summary>
        internal async Task<List<Role>> ResolveRolesAsync(IEnumerable<Guid>? roleIds, IEnumerable<string>? roleKeys)
        {
            var ids = (roleIds ?? Enumerable.Empty<Guid>()).Distinct().ToList();
            var keys = (roleKeys ?? Enumerable.Empty<string>()).Select(k => k.Trim().ToLower()).Distinct().ToList();

            if (ids.Count == 0 && keys.Count == 0)
            {
                throw new BusinessRuleException("El usuario debe tener al menos un rol.");
            }

            var roles = await _db.Roles
                .Include(r => r.Permissions)
                .Where(r => ids.Contains(r.Id) || (r.SystemKey != null && keys.Contains(r.SystemKey)))
                .ToListAsync();

            if (roles.Count != ids.Count + keys.Count)
            {
                throw new BusinessRuleException("Alguno de los roles indicados no existe en este conjunto.");
            }
            if (roles.Any(r => !r.IsActive))
            {
                throw new BusinessRuleException("No se puede asignar un rol inactivo.");
            }

            var notOwned = roles
                .Where(r => r.Permissions.Any(p => !_currentUser.HasPermission(p.Permission)))
                .Select(r => r.Name)
                .ToList();
            if (notOwned.Count > 0)
            {
                throw new ForbiddenException($"No puede asignar roles con permisos que usted no tiene: {string.Join(", ", notOwned)}.");
            }

            return roles;
        }

        internal static readonly Expression<Func<User, UserDto>> ToDtoExpression = u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            ApartmentNumber = u.ApartmentNumber,
            IsActive = u.IsActive,
            IsSuperAdmin = u.IsSuperAdmin,
            ComplexId = u.ComplexId,
            ComplexName = u.Complex == null ? null : u.Complex.Name,
            Roles = u.UserRoles
                .OrderBy(ur => ur.Role.Name)
                .Select(ur => new RoleRefDto { Id = ur.Role.Id, Name = ur.Role.Name, SystemKey = ur.Role.SystemKey })
                .ToList()
        };
    }
}
