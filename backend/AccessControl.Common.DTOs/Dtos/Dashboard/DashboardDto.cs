using System.Collections.Generic;

namespace AccessControl.Common.DTOs.Dashboard
{
    public class DashboardDto
    {
        public int TotalVisitsThisMonth { get; set; }
        public IEnumerable<LatestVisitDto>? LatestVisits { get; set; }
    }
}
