using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Menu;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class MenuService : IMenuService
    {
        private readonly IRepository<RoleMenu> _roleMenuRepository;

        public MenuService(IRepository<RoleMenu> roleMenuRepository)
        {
            _roleMenuRepository = roleMenuRepository;
        }

        public async Task<IEnumerable<MenuDto>> GetMenusByRoleAsync(Role role)
        {
            return await _roleMenuRepository.GetQueryable()
                .Where(rm => rm.Role == role)
                .Select(rm => new MenuDto
                {
                    Id = rm.Menu.Id,
                    Name = rm.Menu.Name,
                    Path = rm.Menu.Path,
                    Icon = rm.Menu.Icon
                })
                .ToListAsync();
        }
    }
}
