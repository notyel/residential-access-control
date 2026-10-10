using AccessControl.Common.DTOs.Role;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IRolesService
    {
        /// <summary>Catálogo de permisos que se pueden asignar a un rol.</summary>
        List<PermissionDto> GetCatalog();

        Task<List<RoleDto>> GetAllAsync();
        Task<RoleDto> GetByIdAsync(Guid id);
        Task<RoleDto> CreateAsync(SaveRoleDto dto);
        Task<RoleDto> UpdateAsync(Guid id, SaveRoleDto dto);
        Task<RoleDto> SetActiveAsync(Guid id, bool isActive);
        Task DeleteAsync(Guid id);
    }
}
