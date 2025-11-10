using AccessControl.Application.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Shared.Enums;
using AccessControl.Persistence.Generics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AccessControl.Common.DTOs.Auth;
using AccessControl.Common.DTOs.User;

namespace AccessControl.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IConfiguration _config;

        public AuthService(IRepository<User> userRepository, IConfiguration config)
        {
            _userRepository = userRepository;
            _config = config;
        }

        public async Task<LoginResponseDto?> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.GetQueryable().FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null) return null;

            bool ok = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!ok) return null;

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ApartmentNumber = user.ApartmentNumber,
                Role = user.Role
            };

            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                User = userDto,
                Token = token
            };
        }

        public async Task<UserDto> CreateUserAsync(string email, string firstName, string lastName, string apartmentNumber, string password, Role role)
        {
            if (await _userRepository.GetQueryable().AnyAsync(u => u.Email == email)) throw new InvalidOperationException("Email already exists");
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User { Email = email, FirstName = firstName, LastName = lastName, ApartmentNumber = apartmentNumber, PasswordHash = hash, Role = role };
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ApartmentNumber = user.ApartmentNumber,
                Role = user.Role
            };
        }

        public string GenerateJwtToken(User user)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>{
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpireMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
