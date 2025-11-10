using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Visit;
using AccessControl.Shared.Dtos.Visit;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IVisitService
    {
        Task<VisitDto> RegisterVisitAsync(CreateVisitDto dto, Guid userId);
        Task<VisitDto?> GetVisitByIdAsync(Guid visitId);
        Task<VisitDto?> UpdateVisitAsync(Guid visitId, UpdateVisitDto dto);
        Task<bool> DeleteVisitAsync(Guid visitId);
        Task<VisitDto?> CheckoutAsync(Guid visitId);
        Task<PaginatedResultDto<VisitDto>> GetVisitsAsync(VisitFilterDto filter);
    }
}
