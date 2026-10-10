using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Role;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.RolesRead, Permissions.RolesManage)]
    public class RolesController : ControllerBase
    {
        private readonly IRolesService _rolesService;

        public RolesController(IRolesService rolesService)
        {
            _rolesService = rolesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(new ResponseModel<List<RoleDto>>(true, "Roles consultados.", await _rolesService.GetAllAsync()));
        }

        [HttpGet("permissions")]
        public IActionResult GetCatalog()
        {
            return Ok(new ResponseModel<List<PermissionDto>>(true, "Permisos consultados.", _rolesService.GetCatalog()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok(new ResponseModel<RoleDto>(true, "Rol consultado.", await _rolesService.GetByIdAsync(id)));
        }

        [HttpPost]
        [HasPermission(Permissions.RolesManage)]
        public async Task<IActionResult> Create([FromBody] SaveRoleDto dto)
        {
            return Ok(new ResponseModel<RoleDto>(true, "Rol creado.", await _rolesService.CreateAsync(dto)));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.RolesManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveRoleDto dto)
        {
            return Ok(new ResponseModel<RoleDto>(true, "Rol actualizado.", await _rolesService.UpdateAsync(id, dto)));
        }

        [HttpPost("{id}/activate")]
        [HasPermission(Permissions.RolesManage)]
        public async Task<IActionResult> Activate(Guid id)
        {
            return Ok(new ResponseModel<RoleDto>(true, "Rol activado.", await _rolesService.SetActiveAsync(id, true)));
        }

        [HttpPost("{id}/deactivate")]
        [HasPermission(Permissions.RolesManage)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            return Ok(new ResponseModel<RoleDto>(true, "Rol desactivado.", await _rolesService.SetActiveAsync(id, false)));
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.RolesManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _rolesService.DeleteAsync(id);
            return Ok(new ResponseModel<string>(true, "Rol eliminado.", null));
        }
    }
}
