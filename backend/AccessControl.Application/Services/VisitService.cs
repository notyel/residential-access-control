using AccessControl.Application.Exceptions;
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
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class VisitService : IVisitService
    {
        private readonly IRepository<Visit> _visitRepository;
        private readonly IRepository<Person> _personRepository;
        private readonly IRepository<Residence> _residenceRepository;
        private readonly IDateTimeService _dateTimeService;

        public VisitService(
            IRepository<Visit> visitRepository,
            IRepository<Person> personRepository,
            IRepository<Residence> residenceRepository,
            IDateTimeService dateTimeService)
        {
            _visitRepository = visitRepository;
            _personRepository = personRepository;
            _residenceRepository = residenceRepository;
            _dateTimeService = dateTimeService;
        }

        public async Task<VisitDto> RegisterVisitAsync(CreateVisitDto dto, Guid userId)
        {
            var residenceExists = await _residenceRepository.GetQueryable()
                .AnyAsync(r => r.Id == dto.ResidenceId && r.IsActive);
            if (!residenceExists)
            {
                throw new BusinessRuleException("La residencia indicada no existe.");
            }

            Person person;

            if (dto.PersonId.HasValue)
            {
                person = await _personRepository.GetByIdAsync(dto.PersonId.Value)
                    ?? throw new BusinessRuleException("La persona indicada no existe.");
            }
            else if (dto.NewPerson != null)
            {
                var documentNumber = dto.NewPerson.DocumentNumber.Trim();
                var duplicated = await _personRepository.GetQueryable()
                    .AnyAsync(p => p.DocumentNumber == documentNumber);
                if (duplicated)
                {
                    throw new ConflictException("Ya existe una persona con ese número de documento.");
                }

                person = new Person
                {
                    FirstName = dto.NewPerson.FirstName.Trim(),
                    LastName = dto.NewPerson.LastName.Trim(),
                    DocumentType = dto.NewPerson.DocumentType.Trim(),
                    DocumentNumber = documentNumber,
                    Phone = Blank.ToNull(dto.NewPerson.Phone),
                    Email = Blank.ToNull(dto.NewPerson.Email),
                    PersonType = dto.NewPerson.PersonType
                };
                await _personRepository.AddAsync(person);
            }
            else
            {
                throw new BusinessRuleException("Debe indicar una persona existente o los datos de una nueva.");
            }

            var visit = new Visit
            {
                Person = person,
                VehiclePlate = NormalizePlate(dto.VehiclePlate),
                ResidenceId = dto.ResidenceId!.Value,
                RegisteredById = userId
            };

            await _visitRepository.AddAsync(visit);
            await _visitRepository.SaveChangesAsync();

            return await GetVisitByIdAsync(visit.Id);
        }

        public async Task<VisitDto> GetVisitByIdAsync(Guid visitId, Guid? ownerId = null)
        {
            var query = _visitRepository.GetQueryable().Where(v => v.Id == visitId);
            if (ownerId.HasValue)
            {
                query = query.Where(v => v.Residence.OwnerId == ownerId.Value);
            }

            // Una visita ajena responde igual que una inexistente
            return await query.Select(ToDto).FirstOrDefaultAsync()
                ?? throw new NotFoundException("La visita no existe.");
        }

        public async Task<VisitDto> UpdateVisitAsync(Guid visitId, UpdateVisitDto dto)
        {
            var visit = await _visitRepository.GetByIdAsync(visitId)
                ?? throw new NotFoundException("La visita no existe.");

            if (visit.CheckOut.HasValue)
            {
                throw new ConflictException("La visita ya tiene salida registrada y no se puede modificar.");
            }

            visit.VehiclePlate = NormalizePlate(dto.VehiclePlate);
            await _visitRepository.SaveChangesAsync();

            return await GetVisitByIdAsync(visitId);
        }

        public async Task<VisitDto> CheckoutAsync(Guid visitId, Guid userId)
        {
            var now = _dateTimeService.UtcNow;

            // UPDATE condicional: si dos porteros marcan la salida a la vez, solo uno la registra
            var updated = await _visitRepository.GetQueryable()
                .Where(v => v.Id == visitId && v.CheckOut == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.CheckOut, now)
                    .SetProperty(v => v.CheckedOutById, userId)
                    .SetProperty(v => v.UpdatedAt, now));

            if (updated == 0)
            {
                var exists = await _visitRepository.GetQueryable().AnyAsync(v => v.Id == visitId);
                if (!exists) throw new NotFoundException("La visita no existe.");
                throw new ConflictException("La visita ya tiene salida registrada.");
            }

            return await GetVisitByIdAsync(visitId);
        }

        public async Task<PaginatedResultDto<VisitDto>> GetVisitsAsync(VisitFilterDto filter, Guid? ownerId = null)
        {
            var query = _visitRepository.GetQueryable();

            if (ownerId.HasValue)
            {
                query = query.Where(v => v.Residence.OwnerId == ownerId.Value);
            }

            if (filter.StartDate.HasValue)
            {
                var startUtc = _dateTimeService.ToUtc(filter.StartDate.Value);
                query = query.Where(v => v.CreatedAt >= startUtc);
            }

            if (filter.EndDate.HasValue)
            {
                var endUtc = _dateTimeService.ToUtc(filter.EndDate.Value);
                query = query.Where(v => v.CreatedAt <= endUtc);
            }

            if (filter.Status == VisitStatus.Inside)
            {
                query = query.Where(v => v.CheckOut == null);
            }
            else if (filter.Status == VisitStatus.CheckedOut)
            {
                query = query.Where(v => v.CheckOut != null);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(v => v.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(ToDto)
                .ToListAsync();

            return new PaginatedResultDto<VisitDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        private static string? NormalizePlate(string? plate)
        {
            return string.IsNullOrWhiteSpace(plate) ? null : plate.Trim().ToUpperInvariant();
        }

        private static readonly Expression<Func<Visit, VisitDto>> ToDto = v => new VisitDto
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
            CheckedOutById = v.CheckedOutById,
            CheckedOutByFullName = v.CheckedOutBy == null ? null : v.CheckedOutBy.FirstName + " " + v.CheckedOutBy.LastName,
            ResidenceId = v.ResidenceId,
            ResidenceIdentifier = v.Residence.Identifier,
            RegisteredById = v.RegisteredById,
            RegisteredByFullName = v.RegisteredBy.FirstName + " " + v.RegisteredBy.LastName,
            CreatedAt = v.CreatedAt
        };
    }
}
