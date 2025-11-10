using AccessControl.Common.DTOs.Menu;
using AccessControl.Shared.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IMenuService
    {
        Task<IEnumerable<MenuDto>> GetMenusByRoleAsync(Role role);
    }
}
