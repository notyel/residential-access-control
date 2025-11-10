using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Common;
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

        public VisitService(IRepository<Visit> visitRepository)
        {
            _visitRepository = visitRepository;
        }

        public async Task<VisitDto> RegisterVisitAsync(CreateVisitDto dto, Guid userId)
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
            return MapVisitToDto(visit);
        }

        public async Task<VisitDto?> GetVisitByIdAsync(Guid visitId)
        {
            var visit = await _visitRepository.GetByIdAsync(visitId);
            return visit != null ? MapVisitToDto(visit) : null;
        }

        public async Task<VisitDto?> UpdateVisitAsync(Guid visitId, UpdateVisitDto dto)
        {
            var visit = await _visitRepository.GetByIdAsync(visitId);
            if (visit != null)
            {
                visit.VisitorName = dto.VisitorName;
                visit.VisitorId = dto.VisitorId;
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
            var visit = await _visitRepository.GetByIdAsync(visitId);
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
                .OrderByDescending(v => v.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(v => MapVisitToDto(v))
                .ToListAsync();

            return new PaginatedResultDto<VisitDto> { Items = pagedVisits, TotalCount = totalCount };
        }

        private static VisitDto MapVisitToDto(Visit visit)
        {
            return new VisitDto
            {
                Id = visit.Id,
                VisitorName = visit.VisitorName,
                VisitorId = visit.VisitorId,
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
