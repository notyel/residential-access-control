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

/// <summary>Define la contraseña de un usuario invitado y lo deja activo.</summary>
        Task AcceptInvitationAsync(string token, string password);

        /// <summary>Envía el enlace de recuperación si la cuenta existe; nunca revela si existe.</summary>
        Task RequestPasswordResetAsync(string email);

        /// <summary>Cambia la contraseña con un enlace de recuperación y cierra las demás sesiones.</summary>
        Task ResetPasswordAsync(string token, string password);

/// <summary>Cierra todas las sesiones del usuario, en todos los dispositivos.</summary>
        Task LogoutAllAsync(Guid userId);

        string GenerateJwtToken(User user);
    }
}
