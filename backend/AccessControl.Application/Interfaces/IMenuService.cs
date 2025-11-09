using AccessControl.Domain.Entities;
using AccessControl.Shared.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IMenuService
    {
        Task<IEnumerable<Menu>> GetMenusByRoleAsync(Role role);
    }
}
