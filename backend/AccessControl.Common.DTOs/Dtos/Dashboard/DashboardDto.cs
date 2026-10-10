using System.Collections.Generic;

namespace AccessControl.Common.DTOs.Dashboard
{
    public class DashboardDto
    {
        public int TotalVisitsThisMonth { get; set; }
        public int VisitorsInside { get; set; }
        public int VisitsToday { get; set; }
        public IEnumerable<LatestVisitDto>? LatestVisits { get; set; }
    }
}
