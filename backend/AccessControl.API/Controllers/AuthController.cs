using AccessControl.Application.Interfaces;
using AccessControl.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AccessControl.Common.DTOs;
using System;
using AccessControl.Common.DTOs.Auth;
using AccessControl.Common.DTOs.User;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("register")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var user = await _auth.CreateUserAsync(dto.Email, dto.FirstName, dto.LastName, dto.ApartmentNumber, dto.Password, dto.Role);
                return Ok(new ResponseModel<UserDto>(true, "User created successfully.", user));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseModel<string>(false, ex.Message, null, new() { ex.Message }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred while creating the user.", null, new() { ex.Message }));
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var loginResponse = await _auth.AuthenticateAsync(dto.Email, dto.Password);
                if (loginResponse == null) return Unauthorized(new ResponseModel<string>(false, "Invalid credentials.", null));

                return Ok(new ResponseModel<LoginResponseDto>(true, "Login successful.", loginResponse));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseModel<string>(false, "An error occurred during login.", null, new() { ex.Message }));
            }
        }
    }
}
