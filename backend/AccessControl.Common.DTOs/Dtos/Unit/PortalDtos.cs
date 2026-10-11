using AccessControl.Common.DTOs.Validation;
using AccessControl.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Unit
{
    /// <summary>Una unidad del usuario del portal, con el vínculo que tiene con ella.</summary>
    public class MyUnitDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsOwner { get; set; }

        /// <summary>Relación con la que vive allí; null si es propietario no residente.</summary>
        public OccupancyRelation? Relation { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public UnitOccupancyState OccupancyState { get; set; }

        /// <summary>Nombres de quienes viven hoy en la unidad.</summary>
        public List<string> Occupants { get; set; } = new();
    }

    public class AddMyOccupantDto
    {
        [Required(ErrorMessage = "La unidad es obligatoria.")]
        public Guid? UnitId { get; set; }

        [EnumDataType(typeof(OccupancyRelation), ErrorMessage = "La relación no es válida.")]
        public OccupancyRelation Relation { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }
        public bool FixedTerm { get; set; }

        // Datos de la persona; no se piden cuando el propietario se registra a sí mismo como residente
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string DocumentType { get; set; } = string.Empty;

        [MaxLength(30)]
        public string DocumentNumber { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [OptionalEmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }
    }

    public class UpdateContactDto
    {
        [MaxLength(30)]
        public string? Phone { get; set; }

        [OptionalEmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }
    }
}
