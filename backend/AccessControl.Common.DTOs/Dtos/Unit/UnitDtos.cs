using AccessControl.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Unit
{
    public class CatalogItemDto
    {
        [Required(ErrorMessage = "El código es obligatorio.")]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        public string Label { get; set; } = null!;

        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class StructureDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Type { get; set; } = null!;
        public string Name { get; set; } = null!;

        /// <summary>Nombre con sus ancestros: "Torre A · Piso 3".</summary>
        public string Path { get; set; } = null!;
        public bool IsActive { get; set; }
        public int UnitCount { get; set; }
    }

    public class SaveStructureDto
    {
        public Guid? ParentId { get; set; }

        [Required(ErrorMessage = "El tipo es obligatorio.")]
        [MaxLength(50)]
        public string Type { get; set; } = null!;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
    }

    public class UnitDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Type { get; set; } = null!;
        public Guid? StructureId { get; set; }
        public string Label { get; set; } = null!;
        public decimal? Area { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Calculado de las ocupaciones vigentes.</summary>
        public UnitOccupancyState OccupancyState { get; set; }

        /// <summary>Nombres de los propietarios vigentes.</summary>
        public List<string> Owners { get; set; } = new();
    }

    public class SaveUnitDto
    {
        [Required(ErrorMessage = "El código de la unidad es obligatorio.")]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "El tipo de unidad es obligatorio.")]
        [MaxLength(50)]
        public string Type { get; set; } = null!;

        public Guid? StructureId { get; set; }

        [Range(0, 100000, ErrorMessage = "El área no es válida.")]
        public decimal? Area { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>Crea unidades por rango: pisos × unidades por piso. El código es prefijo + piso + número (01, 02…).</summary>
    public class BulkUnitsDto
    {
        [Required(ErrorMessage = "El tipo de unidad es obligatorio.")]
        public string Type { get; set; } = null!;

        public Guid? StructureId { get; set; }

        [MaxLength(20)]
        public string? Prefix { get; set; }

        [Range(0, 200, ErrorMessage = "El piso inicial no es válido.")]
        public int FloorFrom { get; set; }

        [Range(0, 200, ErrorMessage = "El piso final no es válido.")]
        public int FloorTo { get; set; }

        [Range(1, 99, ErrorMessage = "Las unidades por piso deben estar entre 1 y 99.")]
        public int UnitsPerFloor { get; set; }

        /// <summary>true = solo muestra qué se crearía, sin guardar.</summary>
        public bool Preview { get; set; }
    }

    public class BulkUnitsResultDto
    {
        /// <summary>Códigos que se crean (o se crearían, en vista previa).</summary>
        public List<string> Created { get; set; } = new();

        /// <summary>Códigos que ya existían en el conjunto y se omiten.</summary>
        public List<string> Skipped { get; set; } = new();
    }

    public class UnitFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 500)]
        public int PageSize { get; set; } = 10;

        /// <summary>Texto en el código o la etiqueta.</summary>
        public string? Search { get; set; }
        public Guid? StructureId { get; set; }
        public string? Type { get; set; }
        public bool? IsActive { get; set; }
        public UnitOccupancyState? State { get; set; }
    }

    public class OwnershipDto
    {
        public Guid Id { get; set; }
        public Guid UnitId { get; set; }
        public string UnitLabel { get; set; } = null!;
        public Guid PersonId { get; set; }
        public string PersonName { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public decimal? Percentage { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class AddOwnershipDto
    {
        [Required(ErrorMessage = "La persona es obligatoria.")]
        public Guid? PersonId { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateOnly? StartDate { get; set; }

        [Range(0.01, 100, ErrorMessage = "El porcentaje debe estar entre 0,01 y 100.")]
        public decimal? Percentage { get; set; }
    }

    /// <summary>Cambio de propietario: cierra las titularidades vigentes el día anterior y abre las nuevas.</summary>
    public class TransferOwnershipDto
    {
        [Required(ErrorMessage = "Indique al menos un nuevo propietario.")]
        [MinLength(1, ErrorMessage = "Indique al menos un nuevo propietario.")]
        public List<Guid> PersonIds { get; set; } = new();

        [Required(ErrorMessage = "La fecha del cambio es obligatoria.")]
        public DateOnly? Date { get; set; }
    }

    public class EndDateDto
    {
        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        public DateOnly? EndDate { get; set; }

        [MaxLength(300)]
        public string? Reason { get; set; }

        /// <summary>Al finalizar un arrendamiento: finalizar también a los demás ocupantes no propietarios de la unidad.</summary>
        public bool IncludeDependents { get; set; }
    }

    public class OccupancyDto
    {
        public Guid Id { get; set; }
        public Guid UnitId { get; set; }
        public string UnitLabel { get; set; } = null!;
        public Guid PersonId { get; set; }
        public string PersonName { get; set; } = null!;
        public OccupancyRelation Relation { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public OccupancyStatus Status { get; set; }
        public string? StatusReason { get; set; }

        /// <summary>Vigente hoy: aprobada y con hoy dentro de su periodo.</summary>
        public bool IsCurrent { get; set; }
    }

    public class AddOccupancyDto
    {
        [Required(ErrorMessage = "La unidad es obligatoria.")]
        public Guid? UnitId { get; set; }

        [Required(ErrorMessage = "La persona es obligatoria.")]
        public Guid? PersonId { get; set; }

        [EnumDataType(typeof(OccupancyRelation), ErrorMessage = "La relación no es válida.")]
        public OccupancyRelation Relation { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        /// <summary>Arrendamiento a término fijo: exige fecha de fin.</summary>
        public bool FixedTerm { get; set; }
    }

    public class DecisionDto
    {
        public bool Approve { get; set; }

        [MaxLength(300)]
        public string? Reason { get; set; }
    }

    /// <summary>Ficha de unidad: estado actual arriba, historia abajo.</summary>
    public class UnitSummaryDto
    {
        public UnitDto Unit { get; set; } = null!;
        public List<OwnershipDto> Owners { get; set; } = new();
        public List<OccupancyDto> Occupants { get; set; } = new();
        public List<OccupancyDto> PendingOccupancies { get; set; } = new();

        /// <summary>Titularidades y ocupaciones que ya no están vigentes; null si quien consulta no puede verlas.</summary>
        public List<OwnershipDto>? PastOwners { get; set; }
        public List<OccupancyDto>? PastOccupants { get; set; }

        /// <summary>Vehículos con autorización en curso en la unidad.</summary>
        public List<AccessControl.Common.DTOs.Vehicles.VehicleDto> Vehicles { get; set; } = new();

        /// <summary>Parqueaderos privados de los que la unidad es titular.</summary>
        public List<AccessControl.Common.DTOs.Parking.ParkingSpaceDto> Parking { get; set; } = new();
    }
}
