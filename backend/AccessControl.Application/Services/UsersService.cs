using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Role;
using AccessControl.Common.DTOs.User;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
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
        private readonly IUserTokenService _tokens;
        private readonly IEmailSender _email;
        private readonly IAuditService _audit;

        public UsersService(
            ApplicationDbContext db,
            ICurrentUser currentUser,
            IPermissionService permissions,
            IUserTokenService tokens,
            IEmailSender email,
            IAuditService audit)
        {
            _audit = audit;
            _db = db;
            _currentUser = currentUser;
            _permissions = permissions;
            _tokens = tokens;
            _email = email;
        }

        public async Task<PaginatedResultDto<UserDto>> GetUsersAsync(UserFilterDto filter)
        {
            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = $"%{filter.Search.Trim()}%";
                query = query.Where(u =>
                    EF.Functions.ILike(u.FirstName + " " + u.LastName, pattern) ||
                    EF.Functions.ILike(u.Email, pattern));
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                // acepta el id del rol o la clave de un rol de sistema ("owner")
                var key = filter.Role.Trim().ToLower();
                query = Guid.TryParse(key, out var roleId)
                    ? query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId))
                    : query.Where(u => u.UserRoles.Any(ur => ur.Role.SystemKey == key));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (!Enum.TryParse<UserStatus>(filter.Status, true, out var status) || !Enum.IsDefined(status))
                {
                    throw new BusinessRuleException("El estado indicado no es válido.");
                }
                query = query.Where(u => u.Status == status);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(ToDtoExpression)
                .ToListAsync();

            return new PaginatedResultDto<UserDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
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
                PasswordHash = string.Empty,
                Status = UserStatus.Invited,
                UserRoles = roles.Select(r => new UserRole { RoleId = r.Id }).ToList()
            };
            _db.Users.Add(user);

            var token = _tokens.Create(user, UserTokenPurpose.Invitation);
            await _db.SaveChangesAsync();
            _permissions.Invalidate();
            await SendInvitationAsync(user, token);

            return await GetUserByIdAsync(user.Id);
        }

        public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto dto)
        {
            var user = await FindAsync(id);

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.ApartmentNumber = Blank.ToNull(dto.ApartmentNumber) ?? string.Empty;

            if (dto.RoleIds != null)
            {
                var roles = await ResolveRolesAsync(dto.RoleIds, null);
                var wanted = roles.Select(r => r.Id).ToHashSet();
                var removing = user.UserRoles.Where(ur => !wanted.Contains(ur.RoleId)).ToList();

                if (removing.Count > 0)
                {
                    await EnsureSomeoneElseManagesUsersAsync(user, wanted);
                }
                foreach (var removed in removing)
                {
                    user.UserRoles.Remove(removed);
                }
                foreach (var roleId in wanted.Where(roleId => user.UserRoles.All(ur => ur.RoleId != roleId)))
                {
                    user.UserRoles.Add(new UserRole { RoleId = roleId });
                }
            }

            await _db.SaveChangesAsync();
            _permissions.Invalidate();

            return await GetUserByIdAsync(id);
        }

        public async Task<UserDto> DeactivateAsync(Guid id)
        {
            var user = await FindAsync(id);
            EnsureNotSelf(user, "desactivarse");
            await EnsureSomeoneElseManagesUsersAsync(user, new HashSet<Guid>());

            user.Status = UserStatus.Disabled;
            user.IsActive = false;
            // sus sesiones abiertas dejan de servir en la siguiente petición
            user.SecurityStamp = Guid.NewGuid();

            await _db.SaveChangesAsync();
            _permissions.Invalidate();
            return await GetUserByIdAsync(id);
        }

        public async Task<UserDto> ActivateAsync(Guid id)
        {
            var user = await FindAsync(id);

            // vuelve al estado que tenía: invitado si nunca definió su contraseña
            user.Status = string.IsNullOrEmpty(user.PasswordHash) ? UserStatus.Invited : UserStatus.Active;
            user.IsActive = true;

            await _db.SaveChangesAsync();
            _permissions.Invalidate();
            return await GetUserByIdAsync(id);
        }

        public async Task<UserDto> RevokeAsync(Guid id)
        {
            var user = await FindAsync(id);
            EnsureNotSelf(user, "revocar su propio acceso");
            await EnsureSomeoneElseManagesUsersAsync(user, new HashSet<Guid>());

            // cierra sus sesiones y le quita todo lo que podía hacer; el usuario y su historial quedan
            user.UserRoles.Clear();
            user.SecurityStamp = Guid.NewGuid();
            _audit.Record(AuditActions.AccessRevoked, entityType: nameof(User), entityId: user.Id.ToString());

            await _db.SaveChangesAsync();
            _permissions.Invalidate();
            return await GetUserByIdAsync(id);
        }

        public async Task ResendInvitationAsync(Guid id)
        {
            var user = await FindAsync(id);
            if (user.Status != UserStatus.Invited)
            {
                throw new ConflictException("El usuario ya definió su contraseña.");
            }

            var token = _tokens.Create(user, UserTokenPurpose.Invitation);
            _audit.Record(AuditActions.InvitationSent, entityType: nameof(User), entityId: user.Id.ToString());
            await _db.SaveChangesAsync();
            await SendInvitationAsync(user, token);
        }

        private Task SendInvitationAsync(User user, string token)
        {
            var link = _tokens.BuildLink("invitation", token);
            return _email.SendAsync(
                user.Email,
                "Su acceso a Zentryal",
                $"Hola {user.FirstName}:\n\nSe creó una cuenta para usted. Defina su contraseña en este enlace " +
                $"(vence en {UserTokenService.InvitationLifetime.TotalHours:0} horas):\n{link}");
        }

        private async Task<User> FindAsync(Guid id)
        {
            return await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new NotFoundException("El usuario no existe.");
        }

        private void EnsureNotSelf(User user, string action)
        {
            if (user.Id == _currentUser.UserId)
            {
                throw new ConflictException($"No puede {action}.");
            }
        }

        /// <summary>
        /// El conjunto siempre conserva al menos un usuario activo capaz de administrar usuarios.
        /// </summary>
        /// <param name="remainingRoleIds">Roles con los que quedaría el usuario tras el cambio.</param>
        private async Task EnsureSomeoneElseManagesUsersAsync(User user, HashSet<Guid> remainingRoleIds)
        {
            var managerRoleIds = await _db.Roles
                .Where(r => r.IsActive && r.Permissions.Any(p => p.Permission == Permissions.UsersManage))
                .Select(r => r.Id)
                .ToListAsync();

            var isManagerNow = user.Status == UserStatus.Active && user.UserRoles.Any(ur => managerRoleIds.Contains(ur.RoleId));
            if (!isManagerNow || remainingRoleIds.Any(managerRoleIds.Contains))
            {
                return;
            }

            var someoneElse = await _db.Users.AnyAsync(u =>
                u.Id != user.Id && u.Status == UserStatus.Active && u.UserRoles.Any(ur => managerRoleIds.Contains(ur.RoleId)));
            if (!someoneElse)
            {
                throw new ConflictException("Es el único usuario activo que puede administrar usuarios; el conjunto quedaría sin administrador.");
            }
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
            IsActive = u.Status != UserStatus.Disabled,
            Status = u.Status.ToString(),
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
