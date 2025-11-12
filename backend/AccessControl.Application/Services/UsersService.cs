using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.User;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using AccessControl.Common.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class UsersService : IUsersService
    {
        private readonly IRepository<User> _userRepository;

        public UsersService(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<PaginatedResultDto<UserDto>> GetUsersAsync(string? role, int pageNumber = 1, int pageSize = 10)
        {
            var query = _userRepository.GetQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                if (Enum.TryParse<Role>(role, true, out var parsedRole))
                {
                    query = query.Where(u => u.Role == parsedRole);
                }
                else
                {
                    // Try numeric parse
                    if (int.TryParse(role, out var roleInt) && Enum.IsDefined(typeof(Role), roleInt))
                    {
                        var parsed = (Role)roleInt;
                        query = query.Where(u => u.Role == parsed);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid role.");
                    }
                }
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    ApartmentNumber = u.ApartmentNumber,
                    Role = u.Role
                })
                .ToListAsync();

            return new PaginatedResultDto<UserDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ApartmentNumber = user.ApartmentNumber,
                Role = user.Role
            };
        }
    }
}
