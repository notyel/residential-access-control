using AccessControl.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IVisitService
    {
        Task<Visit> RegisterVisitAsync(CreateVisitDto dto, Guid userId);
        Task<Visit?> CheckoutAsync(Guid visitId);
    }

    public record CreateVisitDto(string VisitorName, string VisitorDocument, string VehiclePlate, Guid ResidenceId, string Notes);
}
