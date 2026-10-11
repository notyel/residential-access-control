using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.User
{
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

        /// <summary>Si se envía, reemplaza los roles del usuario; null los deja como están.</summary>
        public List<Guid>? RoleIds { get; set; }
    }

    public class UserFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 500)]
        public int PageSize { get; set; } = 10;

        /// <summary>Texto en nombre, apellido o correo.</summary>
        public string? Search { get; set; }

        /// <summary>Id de un rol o clave de un rol de sistema.</summary>
        public string? Role { get; set; }

        /// <summary>Invited, Active o Disabled.</summary>
        public string? Status { get; set; }
    }
}
