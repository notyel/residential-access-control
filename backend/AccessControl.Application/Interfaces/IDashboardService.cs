using AccessControl.Common.DTOs.Dashboard;
using System;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IDashboardService
    {
        /// <param name="ownerId">Si se indica, las cifras se limitan a las residencias de ese propietario.</param>
        Task<DashboardDto> GetDashboardAsync(Guid? ownerId = null);
    }
}
