using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Application.Services;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Person;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Common.DTOs.User;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Units
{
    public interface IPeopleService
    {
        /// <summary>Búsqueda exacta por documento, para portería.</summary>
        Task<List<PersonDto>> FindByDocumentAsync(string? documentNumber);

        /// <summary>Directorio: texto en nombre o documento; opcionalmente solo quienes tienen vínculo vigente.</summary>
        Task<PaginatedResultDto<PersonListItemDto>> SearchAsync(PersonFilterDto filter);

        Task<PersonDetailDto> GetAsync(Guid id);
        Task<PersonDto> CreateAsync(CreatePersonDto dto);
        Task<PersonDto> UpdateAsync(Guid id, UpdatePersonDto dto);

        /// <summary>Crea la cuenta (invitada) de una persona, con el rol de sistema indicado.</summary>
        Task<UserDto> GrantAccessAsync(Guid personId, GrantAccessDto dto);
    }

    public class PeopleService : IPeopleService
    {
        private const int MaxDocumentResults = 20;

        private readonly ApplicationDbContext _db;
        private readonly ICatalogService _catalogs;
        private readonly IDateTimeService _dateTime;
        private readonly IUsersService _users;

        public PeopleService(ApplicationDbContext db, ICatalogService catalogs, IDateTimeService dateTime, IUsersService users)
        {
            _db = db;
            _catalogs = catalogs;
            _dateTime = dateTime;
            _users = users;
        }

        public async Task<List<PersonDto>> FindByDocumentAsync(string? documentNumber)
        {
            // Sin criterio no se devuelve el directorio completo de personas
            if (string.IsNullOrWhiteSpace(documentNumber))
            {
                return new List<PersonDto>();
            }

            var value = documentNumber.Trim();
            var persons = await _db.Persons.Where(p => p.DocumentNumber == value).Take(MaxDocumentResults).ToListAsync();
            return persons.Select(ToDto).ToList();
        }

        public async Task<PaginatedResultDto<PersonListItemDto>> SearchAsync(PersonFilterDto filter)
        {
            var today = _dateTime.Today;
            var query = _db.Persons.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = $"%{filter.Search.Trim()}%";
                query = query.Where(p => EF.Functions.ILike(p.FirstName + " " + p.LastName, pattern)
                    || EF.Functions.ILike(p.DocumentNumber, pattern));
            }

            var ownerships = _db.UnitOwnerships.Where(o => o.StartDate <= today && (o.EndDate == null || o.EndDate >= today));
            var occupancies = _db.Occupancies.Where(o => o.Status == Shared.Enums.OccupancyStatus.Active
                && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today));

            if (filter.OnlyLinked)
            {
                // el directorio de "propietarios y residentes" deja fuera a quienes solo han sido visitantes
                query = query.Where(p => ownerships.Any(o => o.PersonId == p.Id) || occupancies.Any(o => o.PersonId == p.Id));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new PersonListItemDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    DocumentType = p.DocumentType,
                    DocumentNumber = p.DocumentNumber,
                    Phone = p.Phone,
                    Email = p.Email,
                    OwnedUnits = ownerships.Where(o => o.PersonId == p.Id).Select(o => o.Unit.Label).ToList(),
                    OccupiedUnits = occupancies.Where(o => o.PersonId == p.Id).Select(o => o.Unit.Label).ToList(),
                    HasUser = _db.Users.Any(u => u.PersonId == p.Id)
                })
                .ToListAsync();

            return new PaginatedResultDto<PersonListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<PersonDetailDto> GetAsync(Guid id)
        {
            var today = _dateTime.Today;
            var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new NotFoundException("La persona no existe.");

            return new PersonDetailDto
            {
                Person = ToDto(person),
                Ownerships = await _db.UnitOwnerships.Where(o => o.PersonId == id)
                    .OrderByDescending(o => o.StartDate).Select(OwnershipService.ToDto(today)).ToListAsync(),
                Occupancies = await _db.Occupancies.Where(o => o.PersonId == id)
                    .OrderByDescending(o => o.StartDate).Select(OccupancyService.ToDto(today)).ToListAsync(),
                User = await _db.Users.Where(u => u.PersonId == id).Select(UsersService.ToDtoExpression).FirstOrDefaultAsync()
            };
        }

        public async Task<PersonDto> CreateAsync(CreatePersonDto dto)
        {
            await _catalogs.EnsureValidAsync(Catalogs.DocumentType, dto.DocumentType, "Tipo de documento");

            var documentNumber = dto.DocumentNumber.Trim();
            if (await _db.Persons.AnyAsync(p => p.DocumentNumber == documentNumber))
            {
                throw new ConflictException("Ya existe una persona con ese número de documento.");
            }

            var person = new Person
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                DocumentType = dto.DocumentType.Trim().ToUpperInvariant(),
                DocumentNumber = documentNumber,
                Phone = Blank.ToNull(dto.Phone),
                Email = Blank.ToNull(dto.Email)
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            return ToDto(person);
        }

        public async Task<PersonDto> UpdateAsync(Guid id, UpdatePersonDto dto)
        {
            var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new NotFoundException("La persona no existe.");
            await _catalogs.EnsureValidAsync(Catalogs.DocumentType, dto.DocumentType, "Tipo de documento");

            if (!string.IsNullOrWhiteSpace(dto.DocumentNumber))
            {
                var documentNumber = dto.DocumentNumber.Trim();
                if (await _db.Persons.AnyAsync(p => p.Id != id && p.DocumentNumber == documentNumber))
                {
                    throw new ConflictException("Ya existe una persona con ese número de documento.");
                }
                person.DocumentNumber = documentNumber;
            }

            person.FirstName = dto.FirstName.Trim();
            person.LastName = dto.LastName.Trim();
            person.DocumentType = dto.DocumentType.Trim().ToUpperInvariant();
            person.Phone = Blank.ToNull(dto.Phone);
            person.Email = Blank.ToNull(dto.Email);
            await _db.SaveChangesAsync();

            return ToDto(person);
        }

        public async Task<UserDto> GrantAccessAsync(Guid personId, GrantAccessDto dto)
        {
            var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == personId)
                ?? throw new NotFoundException("La persona no existe.");
            if (await _db.Users.AnyAsync(u => u.PersonId == personId))
            {
                throw new ConflictException("Esa persona ya tiene una cuenta.");
            }
            if (dto.RoleKey is not (SystemRoles.Owner or SystemRoles.Resident))
            {
                throw new BusinessRuleException("Desde aquí solo se asignan los roles de propietario o residente.");
            }

            var user = await _users.CreateUserAsync(new CreateUserDto
            {
                Email = dto.Email,
                FirstName = person.FirstName,
                LastName = person.LastName,
                RoleKeys = new List<string> { dto.RoleKey }
            });

            // la cuenta queda enlazada a la persona: de ahí salen sus unidades en el portal
            await _db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.PersonId, personId));
            if (string.IsNullOrEmpty(person.Email))
            {
                person.Email = dto.Email.Trim();
                await _db.SaveChangesAsync();
            }

            return user;
        }

        internal static PersonDto ToDto(Person person) => new()
        {
            Id = person.Id,
            FirstName = person.FirstName,
            LastName = person.LastName,
            DocumentType = person.DocumentType,
            DocumentNumber = person.DocumentNumber,
            Phone = person.Phone,
            Email = person.Email
        };
    }
}
