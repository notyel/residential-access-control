using AccessControl.Application.Interfaces;
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

        public VisitService(IRepository<Visit> visitRepository)
        {
            _visitRepository = visitRepository;
        }

        public async Task<Visit> RegisterVisitAsync(CreateVisitDto dto, Guid userId)
        {
            var visit = new Visit
            {
                VisitorName = dto.VisitorName,
                VisitorId = dto.VisitorId,
                VehiclePlate = dto.VehiclePlate,
                ResidenceId = dto.ResidenceId,
                RegisteredById = userId
            };

            await _visitRepository.AddAsync(visit);
            await _visitRepository.SaveChangesAsync();
            return visit;
        }

        public async Task<Visit?> GetVisitByIdAsync(Guid visitId)
        {
            return await _visitRepository.GetByIdAsync(visitId);
        }

        public async Task<Visit?> UpdateVisitAsync(Guid visitId, UpdateVisitDto dto)
        {
            var visit = await _visitRepository.GetByIdAsync(visitId);
            if (visit != null)
            {
                visit.VisitorName = dto.VisitorName;
                visit.VisitorId = dto.VisitorId;
                visit.VehiclePlate = dto.VehiclePlate;
                await _visitRepository.SaveChangesAsync();
            }
            return visit;
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

        public async Task<Visit?> CheckoutAsync(Guid visitId)
        {
            var visit = await _visitRepository.GetByIdAsync(visitId);
            if (visit != null)
            {
                visit.CheckOut = DateTime.UtcNow;
                await _visitRepository.SaveChangesAsync();
            }
            return visit;
        }

        public async Task<(IEnumerable<Visit> Visits, int TotalCount)> GetVisitsAsync(VisitFilterDto filter)
        {
            var query = _visitRepository.GetQueryable();

            if (filter.StartDate.HasValue)
            {
                query = query.Where(v => v.CheckIn >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(v => v.CheckIn <= filter.EndDate.Value);
            }

            var totalCount = await query.CountAsync();

            var pagedVisits = await query
                .OrderByDescending(v => v.CheckIn)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (pagedVisits, totalCount);
        }
    }
}
