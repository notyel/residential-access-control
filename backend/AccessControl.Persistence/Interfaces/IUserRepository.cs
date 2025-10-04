using AccessControl.Domain.Entities;
using System.Threading.Tasks;

namespace AccessControl.Persistence.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateAsync(User user);
        Task<User?> GetByIdAsync(Guid id);
    }
}
