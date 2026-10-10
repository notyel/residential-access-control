using AccessControl.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Complex
{
    public class ComplexDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public ComplexType Type { get; set; }
        public string? Address { get; set; }
        public string TimeZoneId { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveComplexDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        [EnumDataType(typeof(ComplexType), ErrorMessage = "El tipo de conjunto no es válido.")]
        public ComplexType Type { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "La zona horaria es obligatoria.")]
        [MaxLength(100)]
        public string TimeZoneId { get; set; } = null!;
    }

    public class CreateComplexDto : SaveComplexDto
    {
        // Todo conjunto nace con su primer administrador
        [Required(ErrorMessage = "El correo del administrador es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo del administrador no tiene un formato válido.")]
        [MaxLength(200)]
        public string AdminEmail { get; set; } = null!;

        [Required(ErrorMessage = "El nombre del administrador es obligatorio.")]
        [MaxLength(100)]
        public string AdminFirstName { get; set; } = null!;

        [Required(ErrorMessage = "El apellido del administrador es obligatorio.")]
        [MaxLength(100)]
        public string AdminLastName { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña del administrador es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string AdminPassword { get; set; } = null!;
    }
}
