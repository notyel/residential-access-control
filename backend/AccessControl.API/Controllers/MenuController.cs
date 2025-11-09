using AccessControl.Application.Interfaces;
using AccessControl.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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
            var roleStr = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrEmpty(roleStr) || !Enum.TryParse<Role>(roleStr, out var role))
            {
                return Unauthorized();
            }

            var menus = await _menuService.GetMenusByRoleAsync(role);
            return Ok(menus);
        }
    }
}
