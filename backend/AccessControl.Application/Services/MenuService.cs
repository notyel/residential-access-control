using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Menu;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class MenuService : IMenuService
    {
        private static readonly Guid ComplexesMenuId = new("c0f1e2d3-4b5a-4697-8a9b-0c1d2e3f4a5b");

        private readonly IRepository<Menu> _menuRepository;
        private readonly ICurrentUser _currentUser;

        public MenuService(IRepository<Menu> menuRepository, ICurrentUser currentUser)
        {
            _menuRepository = menuRepository;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<MenuDto>> GetMenuAsync()
        {
            var menus = new List<MenuDto>();
            if (_currentUser.IsSuperAdmin)
            {
                menus.Add(new MenuDto { Id = ComplexesMenuId, Name = "Conjuntos", Path = "/access-control/complexes", Icon = "Building2", Order = 0 });
                if (_currentUser.ComplexId == null)
                {
                    return menus;
                }
            }

            var all = await _menuRepository.GetQueryable()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Order)
                .ToListAsync();

            menus.AddRange(all
                .Where(m => m.Permission.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(_currentUser.HasPermission))
                .Select(m => new MenuDto { Id = m.Id, Name = m.Name, Path = m.Path, Icon = m.Icon, Order = m.Order, Group = m.Group }));

            return menus;
        }
    }
}
