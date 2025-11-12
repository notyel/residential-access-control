using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.User;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IUsersService
    {
        Task<PaginatedResultDto<UserDto>> GetUsersAsync(string? role, int pageNumber = 1, int pageSize = 10);
        Task<UserDto?> GetUserByIdAsync(Guid id);
    }
}
