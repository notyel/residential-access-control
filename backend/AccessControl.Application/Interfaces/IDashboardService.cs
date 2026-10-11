using AccessControl.Common.DTOs.Dashboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessControl.Application.Interfaces
{
    public interface IDashboardService
    {
        /// <param name="unitScope">Si se indica, las cifras se limitan a esas unidades.</param>
        Task<DashboardDto> GetDashboardAsync(IReadOnlyCollection<Guid>? unitScope = null);
    }
}
