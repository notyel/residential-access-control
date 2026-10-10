using AccessControl.Common.DTOs.Auth;
using AccessControl.Common.DTOs.User;
using AccessControl.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> AuthenticateAsync(string email, string password);

        /// <summary>Usuario de la sesión con sus roles y permisos efectivos al día.</summary>
        Task<UserDto> GetCurrentUserAsync(Guid userId);

        string GenerateJwtToken(User user);
    }
}
