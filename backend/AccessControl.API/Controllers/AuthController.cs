using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Auth;
using AccessControl.Common.DTOs.User;
using AccessControl.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var loginResponse = await _auth.AuthenticateAsync(dto.Email, dto.Password);
            if (loginResponse == null)
            {
                return Unauthorized(new ResponseModel<string>(false, "Correo o contraseña incorrectos.", null));
            }

            return Ok(new ResponseModel<LoginResponseDto>(true, "Login successful.", loginResponse));
        }

        // Usuario de la sesión con roles y permisos al día; el frontend lo consulta para refrescar lo que muestra
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var user = await _auth.GetCurrentUserAsync(User.GetUserId());
            return Ok(new ResponseModel<UserDto>(true, "Usuario consultado.", user));
        }
    }
}
