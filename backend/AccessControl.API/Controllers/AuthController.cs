using AccessControl.API.Dtos;
using AccessControl.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("register")]
        [Authorize(Policy = "AdminOnly")] // solo admin puede crear admins o propietarios; opcionalmente permitir self-register
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = await _auth.CreateUserAsync(dto.Email, dto.FullName, dto.Password, dto.Role);
            return Ok(new { user.Id, user.Email, user.FullName, user.Role });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _auth.AuthenticateAsync(dto.Email, dto.Password);
            if (user == null) return Unauthorized(new { message = "Credenciales inválidas" });

            var token = _auth.GenerateJwtToken(user);
            return Ok(new { token, role = user.Role.ToString(), userId = user.Id });
        }
    }
}
