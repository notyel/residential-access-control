using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.User;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.UsersRead, Permissions.UsersManage)]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        // GET: api/users?search=ana&role=owner&status=Active&pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter)
        {
            var paged = await _usersService.GetUsersAsync(filter);
            return Ok(new ResponseModel<PaginatedResultDto<UserDto>>(true, "Usuarios consultados.", paged));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            return Ok(new ResponseModel<UserDto>(true, "Usuario consultado.", await _usersService.GetUserByIdAsync(id)));
        }

        [HttpPost]
        [HasPermission(Permissions.UsersManage)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            return Ok(new ResponseModel<UserDto>(true, "Usuario creado. Se le envió la invitación.", await _usersService.CreateUserAsync(dto)));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.UsersManage)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            return Ok(new ResponseModel<UserDto>(true, "Usuario actualizado.", await _usersService.UpdateUserAsync(id, dto)));
        }

        [HttpPost("{id}/deactivate")]
        [HasPermission(Permissions.UsersManage)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            return Ok(new ResponseModel<UserDto>(true, "Usuario desactivado.", await _usersService.DeactivateAsync(id)));
        }

        [HttpPost("{id}/activate")]
        [HasPermission(Permissions.UsersManage)]
        public async Task<IActionResult> Activate(Guid id)
        {
            return Ok(new ResponseModel<UserDto>(true, "Usuario activado.", await _usersService.ActivateAsync(id)));
        }

        [HttpPost("{id}/revoke")]
        [HasPermission(Permissions.UsersManage)]
        public async Task<IActionResult> Revoke(Guid id)
        {
            return Ok(new ResponseModel<UserDto>(true, "Accesos revocados.", await _usersService.RevokeAsync(id)));
        }

        [HttpPost("{id}/resend-invitation")]
        [HasPermission(Permissions.UsersManage)]
        public async Task<IActionResult> ResendInvitation(Guid id)
        {
            await _usersService.ResendInvitationAsync(id);
            return Ok(new ResponseModel<string>(true, "Invitación enviada.", null));
        }
    }
}
