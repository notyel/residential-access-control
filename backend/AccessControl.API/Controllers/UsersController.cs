using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
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
            try
            {
                var paged = await _usersService.GetUsersAsync(role, pageNumber, pageSize);
                return Ok(new ResponseModel<PaginatedResultDto<UserDto>>(true, "Users retrieved successfully.", paged));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseModel<string>(false, ex.Message, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while retrieving users.", null, new() { ex.Message }));
            }
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            try
            {
                var dto = await _usersService.GetUserByIdAsync(id);
                if (dto == null) return NotFound(new ResponseModel<string>(false, "User not found.", null));

                return Ok(new ResponseModel<UserDto>(true, "User retrieved successfully.", dto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while retrieving the user.", null, new() { ex.Message }));
            }
        }
    }
}
