using System;

namespace AccessControl.Shared.Dtos.Visit
{
    public class VisitFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
