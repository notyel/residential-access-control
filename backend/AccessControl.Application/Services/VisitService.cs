using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Person;
using AccessControl.Common.DTOs.Visit;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using AccessControl.Shared.Dtos.Visit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class VisitService : IVisitService
    {
        private readonly IRepository<Visit> _visitRepository;
        private readonly IRepository<Person> _personRepository;

        public VisitService(IRepository<Visit> visitRepository, IRepository<Person> personRepository)
        {
            _visitRepository = visitRepository;
            _personRepository = personRepository;
        }

        public async Task<VisitDto> RegisterVisitAsync(CreateVisitDto dto, Guid userId)
        {
            Person person;

            if (dto.PersonId.HasValue)
            {
                person = await _personRepository.GetByIdAsync(dto.PersonId.Value);
                if (person == null)
                {
                    throw new ArgumentException("Invalid PersonId.");
                }
            }
            else if (dto.NewPerson != null)
            {
                var existingPerson = await _personRepository.GetQueryable()
                    .FirstOrDefaultAsync(p => p.DocumentNumber == dto.NewPerson.DocumentNumber);

                if (existingPerson != null)
                {
                    throw new InvalidOperationException("A person with this document number already exists.");
                }

                person = new Person
                {
                    FirstName = dto.NewPerson.FirstName,
                    LastName = dto.NewPerson.LastName,
                    DocumentType = dto.NewPerson.DocumentType,
                    DocumentNumber = dto.NewPerson.DocumentNumber,
                    Phone = dto.NewPerson.Phone,
                    Email = dto.NewPerson.Email,
                    PersonType = dto.NewPerson.PersonType
                };
                await _personRepository.AddAsync(person);
            }
            else
            {
                throw new ArgumentException("Either PersonId or NewPerson must be provided.");
            }

            var visit = new Visit
            {
                Person = person,
                VehiclePlate = dto.VehiclePlate,
                ResidenceId = dto.ResidenceId,
                RegisteredById = userId
            };

            await _visitRepository.AddAsync(visit);
            await _visitRepository.SaveChangesAsync();

            return MapVisitToDto(visit);
        }

        public async Task<VisitDto?> GetVisitByIdAsync(Guid visitId)
        {
            var visit = await _visitRepository.GetQueryable()
                .Include(v => v.Person)
                .Include(v => v.Residence)
                .Include(v => v.RegisteredBy)
                .FirstOrDefaultAsync(v => v.Id == visitId);
            return visit != null ? MapVisitToDto(visit) : null;
        }

        public async Task<VisitDto?> UpdateVisitAsync(Guid visitId, UpdateVisitDto dto)
        {
            var visit = await _visitRepository.GetQueryable()
                .Include(v => v.Person)
                .FirstOrDefaultAsync(v => v.Id == visitId);
            if (visit != null)
            {
                visit.PersonId = dto.PersonId;
                visit.VehiclePlate = dto.VehiclePlate;
                await _visitRepository.SaveChangesAsync();
            }
            return visit != null ? MapVisitToDto(visit) : null;
        }

        public async Task<bool> DeleteVisitAsync(Guid visitId)
        {
            var visit = await _visitRepository.GetByIdAsync(visitId);
            if (visit != null)
            {
                _visitRepository.Remove(visit);
                await _visitRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<VisitDto?> CheckoutAsync(Guid visitId)
        {
            var visit = await _visitRepository.GetQueryable()
                .Include(v => v.Person)
                .FirstOrDefaultAsync(v => v.Id == visitId);
            if (visit != null)
            {
                visit.CheckOut = DateTime.UtcNow;
                await _visitRepository.SaveChangesAsync();
            }
            return visit != null ? MapVisitToDto(visit) : null;
        }

        public async Task<PaginatedResultDto<VisitDto>> GetVisitsAsync(VisitFilterDto filter)
        {
            var query = _visitRepository.GetQueryable();

            if (filter.StartDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(v => v.CreatedAt <= filter.EndDate.Value);
            }

            var totalCount = await query.CountAsync();

            var pagedVisits = await query
                .Include(v => v.Person)
                .OrderByDescending(v => v.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(v => new VisitDto
                {
                    Id = v.Id,
                    Person = new PersonDto
                    {
                        Id = v.Person.Id,
                        FirstName = v.Person.FirstName,
                        LastName = v.Person.LastName,
                        DocumentType = v.Person.DocumentType,
                        DocumentNumber = v.Person.DocumentNumber,
                        Phone = v.Person.Phone,
                        Email = v.Person.Email,
                        PersonType = v.Person.PersonType
                    },
                    VehiclePlate = v.VehiclePlate,
                    CheckOut = v.CheckOut,
                    ResidenceId = v.ResidenceId,
                    ResidenceIdentifier = v.Residence.Identifier,
                    RegisteredById = v.RegisteredById,
                    RegisteredByFullName = $"{v.RegisteredBy.FirstName} {v.RegisteredBy.LastName}",
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();

            return new PaginatedResultDto<VisitDto>
            {
                Items = pagedVisits,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        private static VisitDto MapVisitToDto(Visit visit)
        {
            return new VisitDto
            {
                Id = visit.Id,
                Person = new PersonDto
                {
                    Id = visit.Person.Id,
                    FirstName = visit.Person.FirstName,
                    LastName = visit.Person.LastName,
                    DocumentType = visit.Person.DocumentType,
                    DocumentNumber = visit.Person.DocumentNumber,
                    Phone = visit.Person.Phone,
                    Email = visit.Person.Email,
                    PersonType = visit.Person.PersonType
                },
                VehiclePlate = visit.VehiclePlate,
                CheckOut = visit.CheckOut,
                ResidenceId = visit.ResidenceId,
                ResidenceIdentifier = visit.Residence?.Identifier,
                RegisteredById = visit.RegisteredById,
                RegisteredByFullName = visit.RegisteredBy != null ? $"{visit.RegisteredBy.FirstName} {visit.RegisteredBy.LastName}" : null,
                CreatedAt = visit.CreatedAt
            };
        }
    }
}
