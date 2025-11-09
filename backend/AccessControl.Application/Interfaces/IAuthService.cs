using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> CreateUserAsync(string email, string fullName, string password, Role role);
        string GenerateJwtToken(User user);
    }
}
