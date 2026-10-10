using System;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Shared.Dtos.Visit
{
    public enum VisitStatus
    {
        Inside = 0,
        CheckedOut = 1
    }

    public class VisitFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 500)]
        public int PageSize { get; set; } = 10;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public VisitStatus? Status { get; set; }
    }
}
