using System;
using System.ComponentModel.DataAnnotations;
using AccessControl.Common.DTOs.Person;

namespace AccessControl.Common.DTOs.Visit
{
    public class CreateVisitDto
    {
        public Guid? PersonId { get; set; }
        public CreatePersonDto? NewPerson { get; set; }

        [MaxLength(15)]
        public string? VehiclePlate { get; set; }

        [Required(ErrorMessage = "La residencia es obligatoria.")]
        public Guid? ResidenceId { get; set; }
    }
}
