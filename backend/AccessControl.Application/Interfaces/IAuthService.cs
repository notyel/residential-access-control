using AccessControl.Application.DTOs;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<LoginResponseDto> RegisterAsync(string email, string password, string fullName);
    }
}
