using System;

namespace AccessControl.Common.DTOs.Visit
{
    public class VisitDto
    {
        public Guid Id { get; set; }
        public string? VisitorName { get; set; }
        public string? VisitorId { get; set; }
        public string? VehiclePlate { get; set; }
        public DateTime? CheckOut { get; set; }
        public Guid ResidenceId { get; set; }
        public string? ResidenceIdentifier { get; set; }
        public Guid RegisteredById { get; set; }
        public string? RegisteredByFullName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
