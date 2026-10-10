using AccessControl.Common.DTOs.Validation;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Person
{
    public class UpdatePersonDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        [MaxLength(20)]
        public string DocumentType { get; set; } = null!;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [OptionalEmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        public int PersonType { get; set; }
    }
}
