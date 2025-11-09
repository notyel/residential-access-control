using AccessControl.Domain.Enums;

namespace AccessControl.API.Dtos
{
    public record LoginDto(string Email, string Password);
    public record RegisterDto(string Email, string FullName, string Password, Role Role);
}
