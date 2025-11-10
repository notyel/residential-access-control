using AccessControl.Common.DTOs.Auth;
using AccessControl.Common.DTOs.User;
using AccessControl.Domain.Entities;
using AccessControl.Shared.Enums;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> AuthenticateAsync(string email, string password);
        Task<UserDto> CreateUserAsync(string email, string firstName, string lastName, string apartmentNumber, string password, Role role);
        string GenerateJwtToken(User user);
    }
}
