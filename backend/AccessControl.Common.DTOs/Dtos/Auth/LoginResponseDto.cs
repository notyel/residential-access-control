using AccessControl.Common.DTOs.User;

namespace AccessControl.Common.DTOs.Auth
{
    public class LoginResponseDto
    {
        public UserDto? User { get; set; }
        public string? Token { get; set; }
    }
}
