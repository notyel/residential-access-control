using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Menu;
using AccessControl.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMenu()
        {
            try
            {
                var roleStr = User.FindFirstValue(ClaimTypes.Role);
                if (string.IsNullOrEmpty(roleStr) || !Enum.TryParse<Role>(roleStr, out var role))
                {
                    return Unauthorized(new ResponseModel<string>(false, "Invalid role.", null));
                }

                var menus = await _menuService.GetMenusByRoleAsync(role);
                return Ok(new ResponseModel<IEnumerable<MenuDto>>(true, "Menu retrieved successfully.", menus));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while retrieving the menu.", null, new() { ex.Message }));
            }
        }
    }
}
