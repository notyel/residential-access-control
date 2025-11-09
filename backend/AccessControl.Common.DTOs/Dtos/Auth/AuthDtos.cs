using AccessControl.Shared.Enums;

namespace AccessControl.Shared.Dtos.Auth
{
    public record LoginDto(string Email, string Password);
    public record RegisterDto(string Email, string FirstName, string LastName, string ApartmentNumber, string Password, Role Role);
}
