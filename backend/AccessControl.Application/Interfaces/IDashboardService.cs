using AccessControl.Common.DTOs.Dashboard;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<IEnumerable<LatestVisitDto>> GetLatestVisitsAsync(int count);
        Task<int> GetTotalVisitsThisMonthAsync();
    }
}
