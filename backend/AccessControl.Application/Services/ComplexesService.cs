using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Complex;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using AccessControl.Application.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class ComplexesService : IComplexesService
    {
        private readonly IRepository<Complex> _complexRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRoleProvisioner _roleProvisioner;

        public ComplexesService(IRepository<Complex> complexRepository, IRepository<User> userRepository, IRoleProvisioner roleProvisioner)
        {
            _roleProvisioner = roleProvisioner;
            _complexRepository = complexRepository;
            _userRepository = userRepository;
        }

        public async Task<List<ComplexDto>> GetAllAsync()
        {
            var complexes = await _complexRepository.GetQueryable().OrderBy(c => c.Name).ToListAsync();
            return complexes.Select(ToDto).ToList();
        }

        public async Task<ComplexDto> GetByIdAsync(Guid id)
        {
            return ToDto(await FindAsync(id));
        }

        public async Task<ComplexDto> CreateAsync(CreateComplexDto dto)
        {
            await ValidateAsync(dto, null);

            var adminEmail = dto.AdminEmail.Trim();
            // el correo es único en toda la plataforma: se busca sin el filtro de conjunto
            var emailTaken = await _userRepository.GetQueryable().IgnoreQueryFilters()
                .AnyAsync(u => u.Email.ToLower() == adminEmail.ToLower());
            if (emailTaken)
            {
                throw new ConflictException("Ya existe un usuario con el correo indicado para el administrador.");
            }

            var complex = new Complex
            {
                Name = dto.Name.Trim(),
                Type = dto.Type,
                Address = Blank.ToNull(dto.Address),
                TimeZoneId = dto.TimeZoneId.Trim()
            };
            await _complexRepository.AddAsync(complex);

            var adminRole = _roleProvisioner.AddSystemRoles(complex.Id).First(r => r.SystemKey == SystemRoles.Admin);

            await _userRepository.AddAsync(new User
            {
                ComplexId = complex.Id,
                Email = adminEmail,
                FirstName = dto.AdminFirstName.Trim(),
                LastName = dto.AdminLastName.Trim(),
                ApartmentNumber = string.Empty,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.AdminPassword),
                UserRoles = new List<UserRole> { new() { Role = adminRole } }
            });

            // conjunto, roles de sistema y administrador se guardan en la misma transacción
            await _complexRepository.SaveChangesAsync();

            return ToDto(complex);
        }

        public async Task<ComplexDto> UpdateAsync(Guid id, SaveComplexDto dto)
        {
            var complex = await FindAsync(id);
            await ValidateAsync(dto, id);

            complex.Name = dto.Name.Trim();
            complex.Type = dto.Type;
            complex.Address = Blank.ToNull(dto.Address);
            complex.TimeZoneId = dto.TimeZoneId.Trim();
            await _complexRepository.SaveChangesAsync();

            return ToDto(complex);
        }

        public async Task<ComplexDto> SetActiveAsync(Guid id, bool isActive)
        {
            var complex = await FindAsync(id);
            complex.IsActive = isActive;
            await _complexRepository.SaveChangesAsync();

            return ToDto(complex);
        }

        private async Task<Complex> FindAsync(Guid id)
        {
            return await _complexRepository.GetByIdAsync(id) ?? throw new NotFoundException("El conjunto no existe.");
        }

        private async Task ValidateAsync(SaveComplexDto dto, Guid? currentId)
        {
            if (!TimeZoneInfo.TryFindSystemTimeZoneById(dto.TimeZoneId.Trim(), out _))
            {
                throw new BusinessRuleException("La zona horaria indicada no es válida.");
            }

            var name = dto.Name.Trim();
            var duplicated = await _complexRepository.GetQueryable()
                .AnyAsync(c => c.Id != currentId && c.Name.ToLower() == name.ToLower());
            if (duplicated)
            {
                throw new ConflictException("Ya existe un conjunto con ese nombre.");
            }
        }

        private static ComplexDto ToDto(Complex complex) => new()
        {
            Id = complex.Id,
            Name = complex.Name,
            Type = complex.Type,
            Address = complex.Address,
            TimeZoneId = complex.TimeZoneId,
            IsActive = complex.IsActive,
            CreatedAt = complex.CreatedAt
        };
    }
}
