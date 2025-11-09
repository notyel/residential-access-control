using System;

namespace AccessControl.Shared.Dtos.Visit
{
    public class CreateVisitDto
    {
        public string VisitorName { get; set; } = null!;
        public string VisitorId { get; set; } = null!;
        public string? VehiclePlate { get; set; }
        public Guid ResidenceId { get; set; }
    }
}
