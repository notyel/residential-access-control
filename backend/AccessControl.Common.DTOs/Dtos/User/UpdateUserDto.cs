using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.User
{
    // No incluye contraseña ni rol: no se cambian desde la edición de datos
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;


        [MaxLength(50)]
        public string? ApartmentNumber { get; set; }
    }
}
