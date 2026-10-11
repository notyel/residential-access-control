using System.ComponentModel.DataAnnotations;

namespace AccessControl.Shared.Dtos.Auth
{
    public record LoginDto(
        [Required(ErrorMessage = "El correo es obligatorio.")] string Email,
        [Required(ErrorMessage = "La contraseña es obligatoria.")] string Password);

    public record ForgotPasswordDto(
        [Required(ErrorMessage = "El correo es obligatorio.")] string Email);

    /// <summary>Definir la contraseña con un enlace de invitación o de recuperación.</summary>
    public record SetPasswordDto(
        [Required(ErrorMessage = "El enlace no es válido.")] string Token,
        [Required(ErrorMessage = "La contraseña es obligatoria.")] string Password);
}
