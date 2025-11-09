using AccessControl.Domain.Entities;
using AccessControl.Shared.Enums;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> CreateUserAsync(string email, string firstName, string lastName, string apartmentNumber, string password, Role role);
        string GenerateJwtToken(User user);
    }
}
