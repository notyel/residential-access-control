using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Person;
using AccessControl.Common.DTOs.Visit;
using AccessControl.Domain.Entities;
using AccessControl.Persistence.Generics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class OwnersService : IOwnersService
    {
        private readonly IRepository<Visit> _visitRepository;

        public OwnersService(IRepository<Visit> visitRepository)
        {
            _visitRepository = visitRepository;
        }

        public async Task<IEnumerable<VisitDto>> GetMyVisitsAsync(Guid userId)
        {
            return await _visitRepository.GetQueryable()
                .Include(v => v.Person)
                .Where(v => v.Residence.OwnerId == userId)
                .OrderByDescending(v => v.CreatedAt)
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
        }
    }
}
