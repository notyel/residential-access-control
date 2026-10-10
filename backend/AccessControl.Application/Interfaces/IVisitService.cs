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

        /// <param name="ownerId">Si se indica, la visita debe ser a una residencia de ese propietario.</param>
        Task<VisitDto> GetVisitByIdAsync(Guid visitId, Guid? ownerId = null);

        Task<VisitDto> UpdateVisitAsync(Guid visitId, UpdateVisitDto dto);
        Task<VisitDto> CheckoutAsync(Guid visitId, Guid userId);

        /// <param name="ownerId">Si se indica, solo visitas a residencias de ese propietario.</param>
        Task<PaginatedResultDto<VisitDto>> GetVisitsAsync(VisitFilterDto filter, Guid? ownerId = null);
    }
}
