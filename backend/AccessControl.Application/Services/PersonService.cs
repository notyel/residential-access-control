using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Person;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class PersonService : IPersonService
    {
        private const int MaxSearchResults = 50;

        private readonly IRepository<Person> _personRepository;

        public PersonService(IRepository<Person> personRepository)
        {
            _personRepository = personRepository;
        }

        public async Task<PersonDto> CreatePersonAsync(CreatePersonDto dto)
        {
            var documentNumber = dto.DocumentNumber.Trim();
            var duplicated = await _personRepository.GetQueryable()
                .AnyAsync(p => p.DocumentNumber == documentNumber);

            if (duplicated)
            {
                throw new ConflictException("Ya existe una persona con ese número de documento.");
            }

            var person = new Person
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                DocumentType = dto.DocumentType.Trim(),
                DocumentNumber = documentNumber,
                Phone = Blank.ToNull(dto.Phone),
                Email = Blank.ToNull(dto.Email),
                PersonType = dto.PersonType
            };

            await _personRepository.AddAsync(person);
            await _personRepository.SaveChangesAsync();

            return MapPersonToDto(person);
        }

        public async Task<List<PersonDto>> SearchPersonsAsync(string? documentNumber)
        {
            // Sin criterio no se devuelve el directorio completo de personas
            if (string.IsNullOrWhiteSpace(documentNumber))
            {
                return new List<PersonDto>();
            }

            var value = documentNumber.Trim();
            var persons = await _personRepository.GetQueryable()
                .Where(p => p.DocumentNumber == value)
                .Take(MaxSearchResults)
                .ToListAsync();

            return persons.Select(MapPersonToDto).ToList();
        }

        public async Task<PersonDto> UpdatePersonAsync(Guid id, UpdatePersonDto dto)
        {
            var person = await _personRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("La persona no existe.");

            person.FirstName = dto.FirstName.Trim();
            person.LastName = dto.LastName.Trim();
            person.DocumentType = dto.DocumentType.Trim();
            person.Phone = Blank.ToNull(dto.Phone);
            person.Email = Blank.ToNull(dto.Email);
            person.PersonType = dto.PersonType;

            await _personRepository.SaveChangesAsync();

            return MapPersonToDto(person);
        }

        private static PersonDto MapPersonToDto(Person person)
        {
            return new PersonDto
            {
                Id = person.Id,
                FirstName = person.FirstName,
                LastName = person.LastName,
                DocumentType = person.DocumentType,
                DocumentNumber = person.DocumentNumber,
                Phone = person.Phone,
                Email = person.Email,
                PersonType = person.PersonType
            };
        }
    }
}
