using System;

namespace AccessControl.Shared.Dtos.Visit
{
    public class UpdateVisitDto
    {
        public Guid PersonId { get; set; }
        public string? VehiclePlate { get; set; }
    }
}
