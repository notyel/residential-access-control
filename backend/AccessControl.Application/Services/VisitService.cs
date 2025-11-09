using AccessControl.Application.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    public class VisitService : IVisitService
    {
        private readonly ApplicationDbContext _db;

        public VisitService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Visit> RegisterVisitAsync(CreateVisitDto dto, Guid userId)
        {
            var visit = new Visit
            {
                VisitorName = dto.VisitorName,
                VisitorDocument = dto.VisitorDocument,
                VehiclePlate = dto.VehiclePlate,
                ResidenceId = dto.ResidenceId,
                Notes = dto.Notes,
                RegisteredById = userId
            };

            _db.Visits.Add(visit);
            await _db.SaveChangesAsync();
            return visit;
        }

        public async Task<Visit?> CheckoutAsync(Guid visitId)
        {
            var visit = await _db.Visits.FindAsync(visitId);
            if (visit != null)
            {
                visit.CheckOut = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return visit;
        }
    }
}
