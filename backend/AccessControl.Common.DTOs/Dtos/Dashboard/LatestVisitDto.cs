using System;

namespace AccessControl.Common.DTOs.Dashboard
{
    public class LatestVisitDto
    {
        public string? VisitorFullName { get; set; }
        public string? UnitLabel { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime? CheckOut { get; set; }
    }
}
