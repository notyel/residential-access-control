using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Auth;
using AccessControl.Common.DTOs.User;
using AccessControl.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        [EnableRateLimiting(RateLimits.Auth)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var loginResponse = await _auth.AuthenticateAsync(dto.Email, dto.Password);
            if (loginResponse == null)
            {
                return Unauthorized(new ResponseModel<string>(false, "Correo o contraseña incorrectos.", null));
            }

            return Ok(new ResponseModel<LoginResponseDto>(true, "Login successful.", loginResponse));
        }

        [HttpPost("accept-invitation")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimits.Auth)]
        public async Task<IActionResult> AcceptInvitation([FromBody] SetPasswordDto dto)
        {
            await _auth.AcceptInvitationAsync(dto.Token, dto.Password);
            return Ok(new ResponseModel<string>(true, "Contraseña definida. Ya puede iniciar sesión.", null));
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimits.Auth)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _auth.RequestPasswordResetAsync(dto.Email);
            // misma respuesta exista o no la cuenta
            return Ok(new ResponseModel<string>(true, "Si el correo está registrado, recibirá un enlace para definir una contraseña nueva.", null));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimits.Auth)]
        public async Task<IActionResult> ResetPassword([FromBody] SetPasswordDto dto)
        {
            await _auth.ResetPasswordAsync(dto.Token, dto.Password);
            return Ok(new ResponseModel<string>(true, "Contraseña actualizada. Ya puede iniciar sesión.", null));
        }

        // Invalida en el servidor todos los tokens del usuario; el frontend lo llama al cerrar sesión
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _auth.LogoutAllAsync(User.GetUserId());
            return Ok(new ResponseModel<string>(true, "Sesión cerrada.", null));
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
