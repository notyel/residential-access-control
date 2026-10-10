using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.User;
using Microsoft.AspNetCore.Authorization;
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

        // GET: api/users?role=Owner&pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? role, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var paged = await _usersService.GetUsersAsync(role, pageNumber, pageSize);
            return Ok(new ResponseModel<PaginatedResultDto<UserDto>>(true, "Users retrieved successfully.", paged));
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var dto = await _usersService.GetUserByIdAsync(id);
            return Ok(new ResponseModel<UserDto>(true, "User retrieved successfully.", dto));
        }

        [HttpPost]
        [HasPermission(Permissions.UsersManage)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var user = await _usersService.CreateUserAsync(dto);
            return Ok(new ResponseModel<UserDto>(true, "Usuario creado.", user));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.UsersManage)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            var user = await _usersService.UpdateUserAsync(id, dto);
            return Ok(new ResponseModel<UserDto>(true, "Usuario actualizado.", user));
        }
    }
}
