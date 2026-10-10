using System.ComponentModel.DataAnnotations;

namespace AccessControl.Shared.Dtos.Auth
{
    public record LoginDto(
        [Required(ErrorMessage = "El correo es obligatorio.")] string Email,
        [Required(ErrorMessage = "La contraseña es obligatoria.")] string Password);
}
