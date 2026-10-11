using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.User
{
    // Sin contraseña: el usuario la define con el enlace de invitación que recibe por correo
    public class CreateUserDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [MaxLength(200)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(50)]
        public string? ApartmentNumber { get; set; }

        /// <summary>Roles por id. Se puede combinar con RoleKeys; entre ambos debe haber al menos uno.</summary>
        public List<Guid>? RoleIds { get; set; }

        /// <summary>Roles de sistema por clave: admin, guard, owner, resident, auditor.</summary>
        public List<string>? RoleKeys { get; set; }
    }
}
