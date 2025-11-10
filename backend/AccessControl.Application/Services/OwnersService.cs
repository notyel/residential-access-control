using AccessControl.Application.Interfaces;
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
                .Where(v => v.Residence.OwnerId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new VisitDto
                {
                    Id = v.Id,
                    VisitorName = v.VisitorName,
                    VisitorId = v.VisitorId,
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
