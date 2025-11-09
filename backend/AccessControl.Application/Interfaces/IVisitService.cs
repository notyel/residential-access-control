using AccessControl.Domain.Entities;
using AccessControl.Shared.Dtos.Visit;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IVisitService
    {
        Task<Visit> RegisterVisitAsync(CreateVisitDto dto, Guid userId);
        Task<Visit?> GetVisitByIdAsync(Guid visitId);
        Task<Visit?> UpdateVisitAsync(Guid visitId, UpdateVisitDto dto);
        Task<bool> DeleteVisitAsync(Guid visitId);
        Task<Visit?> CheckoutAsync(Guid visitId);
        Task<(IEnumerable<Visit> Visits, int TotalCount)> GetVisitsAsync(VisitFilterDto filter);
    }
}
