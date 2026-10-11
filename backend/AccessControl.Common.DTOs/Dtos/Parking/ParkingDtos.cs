using AccessControl.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Parking
{
    public class ParkingOccupantDto
    {
        public Guid AssignmentId { get; set; }
        public Guid? VehicleId { get; set; }
        public string? Plate { get; set; }
        public string? UnitLabel { get; set; }
        public Guid? VisitId { get; set; }
        public DateTime Since { get; set; }

        /// <summary>Lleva más tiempo que el máximo permitido en un espacio de visitantes.</summary>
        public bool IsOverdue { get; set; }
        public bool IsException { get; set; }
    }

    /// <summary>Reserva o asignación temporal que aparta el espacio.</summary>
    public class ParkingPlanDto
    {
        public Guid AssignmentId { get; set; }
        public ParkingAssignmentClass Class { get; set; }
        public string? UnitLabel { get; set; }
        public string? Plate { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
    }

    public class ParkingSpaceDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string? Location { get; set; }
        public ParkingSpaceType Type { get; set; }
        public ParkingVehicleKind VehicleKind { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Calculado en el momento de la consulta; no se guarda en ninguna parte.</summary>
        public ParkingState State { get; set; }

        // Titular de un espacio privado
        public Guid? HolderAssignmentId { get; set; }
        public Guid? HolderUnitId { get; set; }
        public string? HolderUnitLabel { get; set; }
        public string? HolderPersonName { get; set; }

        public ParkingOccupantDto? Occupant { get; set; }
        public ParkingPlanDto? Plan { get; set; }

        public Guid? OutOfServiceId { get; set; }
        public string? OutOfServiceReason { get; set; }
        public DateTime? OutOfServiceUntil { get; set; }
    }

    public class ParkingAssignmentDto
    {
        public Guid Id { get; set; }
        public ParkingAssignmentClass Class { get; set; }
        public string? UnitLabel { get; set; }
        public string? PersonName { get; set; }
        public string? Plate { get; set; }
        public Guid? VisitId { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public ParkingAssignmentStatus Status { get; set; }
        public string? Notes { get; set; }
        public string? RegisteredBy { get; set; }
        public bool IsException { get; set; }
        public string? ExceptionReason { get; set; }
    }

    public class ParkingOutOfServiceDto
    {
        public Guid Id { get; set; }
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class ParkingNoteDto
    {
        public DateTime CreatedAt { get; set; }
        public string? AuthorName { get; set; }
        public string Text { get; set; } = null!;
    }

    public class ParkingSpaceDetailDto
    {
        public ParkingSpaceDto Space { get; set; } = null!;
        public List<ParkingAssignmentDto> History { get; set; } = new();
        public List<ParkingOutOfServiceDto> OutOfService { get; set; } = new();
        public List<ParkingNoteDto> Notes { get; set; } = new();
    }

    public class SaveParkingSpaceDto
    {
        [Required(ErrorMessage = "El código es obligatorio.")]
        [MaxLength(20)]
        public string Code { get; set; } = null!;

        [MaxLength(100)]
        public string? Location { get; set; }

        [EnumDataType(typeof(ParkingSpaceType), ErrorMessage = "El tipo no es válido.")]
        public ParkingSpaceType Type { get; set; }

        [EnumDataType(typeof(ParkingVehicleKind), ErrorMessage = "El tipo de vehículo no es válido.")]
        public ParkingVehicleKind VehicleKind { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }
    }

    /// <summary>Alta por rango de códigos: prefijo V-, del 1 al 20 → V-01 … V-20.</summary>
    public class BulkParkingSpacesDto
    {
        [MaxLength(10)]
        public string? Prefix { get; set; }

        [Range(0, 9999)]
        public int From { get; set; }

        [Range(0, 9999)]
        public int To { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [EnumDataType(typeof(ParkingSpaceType))]
        public ParkingSpaceType Type { get; set; }

        [EnumDataType(typeof(ParkingVehicleKind))]
        public ParkingVehicleKind VehicleKind { get; set; }
    }

    public class BulkParkingResultDto
    {
        public List<string> Created { get; set; } = new();
        public List<string> Skipped { get; set; } = new();
    }

    public class SetHolderDto
    {
        [Required(ErrorMessage = "La unidad es obligatoria.")]
        public Guid? UnitId { get; set; }

        /// <summary>Titular o asignatario; opcional.</summary>
        public Guid? PersonId { get; set; }
    }

    public class OutOfServiceDto
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio.")]
        [MinLength(3, ErrorMessage = "El motivo es obligatorio.")]
        [MaxLength(300)]
        public string Reason { get; set; } = null!;
    }

    /// <summary>Asignación temporal o reserva: inicio y fin obligatorios.</summary>
    public class PlanParkingDto
    {
        [Required(ErrorMessage = "El espacio es obligatorio.")]
        public Guid? SpaceId { get; set; }

        public Guid? UnitId { get; set; }
        public Guid? VehicleId { get; set; }

        [Required(ErrorMessage = "El inicio es obligatorio.")]
        public DateTime? StartsAt { get; set; }

        [Required(ErrorMessage = "El fin es obligatorio.")]
        public DateTime? EndsAt { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }
    }

    public class OccupyParkingDto
    {
        public Guid? VehicleId { get; set; }

        [MaxLength(15)]
        public string? Plate { get; set; }

        /// <summary>Uso que las reglas no permiten: exige permiso y motivo.</summary>
        public bool IsException { get; set; }

        [MaxLength(300)]
        public string? ExceptionReason { get; set; }
    }

    public class ParkingNoteInputDto
    {
        [Required(ErrorMessage = "Escriba la novedad.")]
        [MaxLength(300)]
        public string Text { get; set; } = null!;
    }

    public class ParkingFilterDto
    {
        public ParkingSpaceType? Type { get; set; }
        public ParkingVehicleKind? VehicleKind { get; set; }
        public ParkingState? State { get; set; }
        public string? Location { get; set; }
        public string? Search { get; set; }
        public bool IncludeInactive { get; set; }

        /// <summary>Periodo futuro para el que se consulta la disponibilidad; sin él, el instante actual.</summary>
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class AvailabilityRowDto
    {
        /// <summary>null en la fila de totales.</summary>
        public ParkingSpaceType? Type { get; set; }
        public int Capacity { get; set; }
        public int Occupied { get; set; }
        public int Reserved { get; set; }
        public int Available { get; set; }
        public int OutOfService { get; set; }
    }

    public class AvailabilityDto
    {
        public List<AvailabilityRowDto> ByType { get; set; } = new();
        public AvailabilityRowDto Total { get; set; } = new();
        public DateTime At { get; set; }
    }
}
