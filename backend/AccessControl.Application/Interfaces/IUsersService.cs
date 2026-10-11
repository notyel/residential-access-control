using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.User;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IUsersService
    {
        Task<PaginatedResultDto<UserDto>> GetUsersAsync(UserFilterDto filter);
        Task<UserDto> GetUserByIdAsync(Guid id);

        /// <summary>Crea el usuario como invitado y le envía el enlace para definir su contraseña.</summary>
        Task<UserDto> CreateUserAsync(CreateUserDto dto);

        Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto dto);

        /// <summary>No puede iniciar sesión y sus sesiones abiertas dejan de servir. Conserva sus roles.</summary>
        Task<UserDto> DeactivateAsync(Guid id);

        Task<UserDto> ActivateAsync(Guid id);

        /// <summary>Cierra sus sesiones y le quita todos los roles, sin borrar el usuario.</summary>
        Task<UserDto> RevokeAsync(Guid id);

        Task ResendInvitationAsync(Guid id);
    }
}
