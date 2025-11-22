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
        private readonly IRepository<Person> _personRepository;

        public PersonService(IRepository<Person> personRepository)
        {
            _personRepository = personRepository;
        }

        public async Task<PersonDto> CreatePersonAsync(CreatePersonDto dto)
        {
            var existingPerson = await _personRepository.GetQueryable()
                .FirstOrDefaultAsync(p => p.DocumentNumber == dto.DocumentNumber);

            if (existingPerson != null)
            {
                throw new InvalidOperationException("A person with this document number already exists.");
            }

            var person = new Person
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DocumentType = dto.DocumentType,
                DocumentNumber = dto.DocumentNumber,
                Phone = dto.Phone,
                Email = dto.Email,
                PersonType = dto.PersonType
            };

            await _personRepository.AddAsync(person);
            await _personRepository.SaveChangesAsync();

            return MapPersonToDto(person);
        }

        public async Task<List<PersonDto>> SearchPersonsAsync(string documentNumber)
        {
            var query = _personRepository.GetQueryable();

            if (!string.IsNullOrEmpty(documentNumber))
            {
                query = query.Where(p => p.DocumentNumber == documentNumber);
            }

            return await query.Select(p => MapPersonToDto(p)).ToListAsync();
        }

        public async Task<PersonDto> UpdatePersonAsync(Guid id, UpdatePersonDto dto)
        {
            var person = await _personRepository.GetByIdAsync(id);

            if (person == null)
            {
                return null;
            }

            person.FirstName = dto.FirstName;
            person.LastName = dto.LastName;
            person.DocumentType = dto.DocumentType;
            person.Phone = dto.Phone;
            person.Email = dto.Email;
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
