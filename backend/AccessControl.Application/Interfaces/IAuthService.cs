using AccessControl.Domain.Entities;
using AccessControl.Domain.Models;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> RevokeTokenAsync(string username);
        Task<User> GetUserByUsernameAsync(string username);
    }
}