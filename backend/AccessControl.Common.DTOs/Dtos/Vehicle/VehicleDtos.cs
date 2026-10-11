using AccessControl.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Vehicles
{
    public class VehicleDto
    {
        public Guid Id { get; set; }
        public string Plate { get; set; } = null!;
        public bool HasPlate { get; set; }
        public string Type { get; set; } = null!;
        public string? Brand { get; set; }
        public string? Color { get; set; }
        public string? Model { get; set; }
        public string? Notes { get; set; }
        public VehicleClass Class { get; set; }

        // Autorización en curso (pendiente, vigente o suspendida) o, si no hay, la última que tuvo
        public Guid? AuthorizationId { get; set; }
        public Guid? UnitId { get; set; }
        public string? UnitLabel { get; set; }
        public Guid? ResponsiblePersonId { get; set; }
        public string? ResponsibleName { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public VehicleAuthorizationStatus? Status { get; set; }
        public string? StatusReason { get; set; }

        /// <summary>Vigente hoy: aprobada, no suspendida y con hoy dentro de sus fechas.</summary>
        public bool IsAuthorizedToday { get; set; }
    }

    public class VehicleAuthorizationDto
    {
        public Guid Id { get; set; }
        public Guid UnitId { get; set; }
        public string UnitLabel { get; set; } = null!;
        public Guid ResponsiblePersonId { get; set; }
        public string ResponsibleName { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public VehicleAuthorizationStatus Status { get; set; }
        public string? StatusReason { get; set; }
    }

    public class VehicleNoteDto
    {
        public DateTime CreatedAt { get; set; }
        public string? AuthorName { get; set; }
        public string Text { get; set; } = null!;
    }

    public class VehicleDetailDto
    {
        public VehicleDto Vehicle { get; set; } = null!;
        public List<VehicleAuthorizationDto> History { get; set; } = new();
        public List<VehicleNoteDto> Notes { get; set; } = new();
        public bool IsInside { get; set; }
    }

    public class VehicleDataDto
    {
        [MaxLength(15)]
        public string? Plate { get; set; }

        /// <summary>Código del catálogo vehicle-type.</summary>
        [Required(ErrorMessage = "El tipo de vehículo es obligatorio.")]
        [MaxLength(50)]
        public string Type { get; set; } = null!;

        [MaxLength(50)]
        public string? Brand { get; set; }

        [MaxLength(30)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Model { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }
    }

    public class CreateVehicleDto : VehicleDataDto
    {
        [Required(ErrorMessage = "La unidad es obligatoria.")]
        public Guid? UnitId { get; set; }

        /// <summary>Persona de la unidad que responde por el vehículo. Desde el portal es quien inicia sesión.</summary>
        public Guid? ResponsiblePersonId { get; set; }

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }

    public class ReassignVehicleDto
    {
        [Required(ErrorMessage = "La unidad es obligatoria.")]
        public Guid? UnitId { get; set; }

        [Required(ErrorMessage = "El responsable es obligatorio.")]
        public Guid? ResponsiblePersonId { get; set; }

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }

    public class VehicleReasonDto
    {
        [MaxLength(300)]
        public string? Reason { get; set; }
    }

    public class VehicleNoteInputDto
    {
        [Required(ErrorMessage = "Escriba la novedad.")]
        [MaxLength(300)]
        public string Text { get; set; } = null!;
    }

    public class VehicleDecisionDto
    {
        public bool Approve { get; set; }

        [MaxLength(300)]
        public string? Reason { get; set; }
    }

    public class VehicleFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 200)]
        public int PageSize { get; set; } = 25;

        /// <summary>Placa (en cualquier formato), marca o responsable.</summary>
        public string? Search { get; set; }
        public Guid? UnitId { get; set; }
        public string? Type { get; set; }
        public VehicleClass? Class { get; set; }
        public VehicleAuthorizationStatus? Status { get; set; }
    }

    public class DriverDto
    {
        public Guid PersonId { get; set; }
        public string Name { get; set; } = null!;
    }

    /// <summary>Respuesta de portería al verificar una placa.</summary>
    public class PlateCheckDto
    {
        public string Plate { get; set; } = null!;
        public PlateCheckResult Result { get; set; }
        public string Message { get; set; } = null!;
        public VehicleDto? Vehicle { get; set; }

        /// <summary>Según los eventos de acceso: su último movimiento fue una entrada.</summary>
        public bool IsInside { get; set; }

        /// <summary>Personas con vínculo vigente en la unidad del vehículo: quién puede conducirlo.</summary>
        public List<DriverDto> Drivers { get; set; } = new();

        // Visita en curso con este vehículo
        public Guid? VisitId { get; set; }
        public string? VisitorName { get; set; }
        public string? VisitUnitLabel { get; set; }
    }

    public class VehicleInsideDto
    {
        public Guid VehicleId { get; set; }
        public string Plate { get; set; } = null!;
        public string Type { get; set; } = null!;
        public VehicleClass Class { get; set; }
        public string? UnitLabel { get; set; }
        public string PersonName { get; set; } = null!;
        public DateTime Since { get; set; }

        /// <summary>Visita con la que entró; si esa visita ya salió, el vehículo quedó dentro sin su conductor.</summary>
        public Guid? VisitId { get; set; }
        public bool VisitorLeft { get; set; }
    }
}
