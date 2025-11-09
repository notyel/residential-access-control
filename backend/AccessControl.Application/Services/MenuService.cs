using AccessControl.Application.Interfaces;
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

        public async Task<IEnumerable<Menu>> GetMenusByRoleAsync(Role role)
        {
            return await _roleMenuRepository.GetQueryable()
                .Where(rm => rm.Role == role)
                .Select(rm => rm.Menu)
                .ToListAsync();
        }
    }
}
