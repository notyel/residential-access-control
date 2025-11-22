using System;
using AccessControl.Common.DTOs.Person;

namespace AccessControl.Common.DTOs.Visit
{
    public class CreateVisitDto
    {
        public Guid? PersonId { get; set; }
        public CreatePersonDto? NewPerson { get; set; }
        public string? VehiclePlate { get; set; }
        public Guid ResidenceId { get; set; }
    }
}
