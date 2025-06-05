using AccessControl.Domain.Entities;
using AccessControl.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Persistence.Repositories
{
    public class ResidentRepository : IResidentRepository
    {
        private readonly ApplicationDbContext _context;

        public ResidentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Resident>> GetAllAsync()
        {
            return await _context.Residents.ToListAsync();
        }
    }
}
