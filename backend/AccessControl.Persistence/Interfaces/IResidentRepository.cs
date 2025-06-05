using AccessControl.Domain.Entities;

namespace AccessControl.Persistence.Interfaces
{
    public interface IResidentRepository
    {
        Task<List<Resident>> GetAllAsync();
    }
}
