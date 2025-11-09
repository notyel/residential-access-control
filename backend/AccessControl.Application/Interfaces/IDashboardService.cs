using AccessControl.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<IEnumerable<Visit>> GetLatestVisitsAsync(int count);
        Task<int> GetTotalVisitsThisMonthAsync();
    }
}
