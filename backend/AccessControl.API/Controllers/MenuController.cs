using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            var menus = await _menuService.GetMenuAsync();
            return Ok(new ResponseModel<IEnumerable<MenuDto>>(true, "Menu retrieved successfully.", menus));
        }
    }
}
