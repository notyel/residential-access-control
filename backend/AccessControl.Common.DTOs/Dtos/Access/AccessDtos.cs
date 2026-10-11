using AccessControl.Common.DTOs.Person;
using AccessControl.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Access
{
    // ---------- Autorizaciones ----------

    public class AuthorizationDto
    {
        public Guid Id { get; set; }
        public Guid UnitId { get; set; }
        public string UnitLabel { get; set; } = null!;
        public Guid AuthorizedByPersonId { get; set; }
        public string AuthorizedByName { get; set; } = null!;
        public Guid? VisitorPersonId { get; set; }
        public string VisitorName { get; set; } = null!;
        public string? VisitorDocument { get; set; }
        public string Reason { get; set; } = null!;
        public string? Notes { get; set; }
        public AuthorizationKind Kind { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly? TimeFrom { get; set; }
        public TimeOnly? TimeTo { get; set; }

        /// <summary>Días de la semana de una recurrente: 0 = domingo … 6 = sábado.</summary>
        public List<int> DaysOfWeek { get; set; } = new();

        /// <summary>Active, NotStarted, Expired, Revoked o Consumed. Expired y NotStarted se calculan por fecha.</summary>
        public string State { get; set; } = null!;
        public string? RevokeReason { get; set; }

        /// <summary>Si permite ingresar en este instante; si no, por qué.</summary>
        public bool CanEnterNow { get; set; }
        public string? CannotEnterReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAuthorizationDto
    {
        [Required(ErrorMessage = "La unidad de destino es obligatoria.")]
        public Guid? UnitId { get; set; }

        /// <summary>Quién de la unidad autoriza. Desde el portal se ignora: autoriza quien inicia sesión.</summary>
        public Guid? AuthorizedByPersonId { get; set; }

        public Guid? VisitorPersonId { get; set; }

        [MaxLength(200)]
        public string? VisitorName { get; set; }

        [MaxLength(30)]
        public string? VisitorDocument { get; set; }

        [MaxLength(50)]
        public string? Reason { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }

        [EnumDataType(typeof(AuthorizationKind), ErrorMessage = "La modalidad no es válida.")]
        public AuthorizationKind Kind { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateOnly? StartDate { get; set; }

        /// <summary>Obligatoria salvo en la autorización única (que vale solo el día de inicio).</summary>
        public DateOnly? EndDate { get; set; }

        public TimeOnly? TimeFrom { get; set; }
        public TimeOnly? TimeTo { get; set; }

        /// <summary>0 = domingo … 6 = sábado. Obligatorio en la recurrente.</summary>
        public List<int>? DaysOfWeek { get; set; }
    }

    public class RevokeDto
    {
        [MaxLength(300)]
        public string? Reason { get; set; }
    }

    public class AuthorizationFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 200)]
        public int PageSize { get; set; } = 25;

        /// <summary>Active (vigentes hoy o por empezar), Expired, Revoked o Consumed.</summary>
        public string? State { get; set; }
        public Guid? UnitId { get; set; }
        public string? Search { get; set; }
    }

    // ---------- Portería ----------

    /// <summary>Lo que portería encuentra al buscar por documento, nombre o unidad.</summary>
    public class LookupResultDto
    {
        /// <summary>Autorizaciones que coinciden, con su veredicto en este instante.</summary>
        public List<AuthorizationDto> Authorizations { get; set; } = new();

        /// <summary>Propietarios y ocupantes vigentes que coinciden (para registrar su entrada o salida, o para que confirmen una visita).</summary>
        public List<ResidentMatchDto> Residents { get; set; } = new();

        /// <summary>Personas registradas que coinciden por documento (visitantes anteriores).</summary>
        public List<PersonDto> People { get; set; } = new();
    }

    public class GateContextDto
    {
        /// <summary>not_allowed, resident_confirmation o guard_discretion.</summary>
        public string WithoutAuthorization { get; set; } = null!;
        public bool DocumentRequired { get; set; }
    }

    public class ResidentMatchDto
    {
        public Guid PersonId { get; set; }
        public string Name { get; set; } = null!;
        public Guid UnitId { get; set; }
        public string UnitLabel { get; set; } = null!;
        public PersonKind Kind { get; set; }
    }

    /// <summary>Visitante que se registra en portería: el documento puede faltar si la política lo permite.</summary>
    public class VisitorPersonDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(20)]
        public string? DocumentType { get; set; }

        [MaxLength(30)]
        public string? DocumentNumber { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }
    }

    public class CheckInDto
    {
        [Required(ErrorMessage = "La unidad de destino es obligatoria.")]
        public Guid? UnitId { get; set; }

        public Guid? PersonId { get; set; }
        public VisitorPersonDto? NewPerson { get; set; }

        /// <summary>Código del catálogo visit-reason.</summary>
        [MaxLength(50)]
        public string? Reason { get; set; }

        [MaxLength(200)]
        public string? ReasonNote { get; set; }

        [MaxLength(15)]
        public string? VehiclePlate { get; set; }

        /// <summary>Datos del vehículo de la visita, si la placa aún no está registrada. Tipo: catálogo vehicle-type.</summary>
        [MaxLength(50)]
        public string? VehicleType { get; set; }

        [MaxLength(50)]
        public string? VehicleBrand { get; set; }

        [MaxLength(30)]
        public string? VehicleColor { get; set; }

        /// <summary>Parqueadero en el que se estaciona el vehículo de la visita; se libera solo cuando el vehículo sale.</summary>
        public Guid? ParkingSpaceId { get; set; }

        /// <summary>Autorización previa con la que ingresa.</summary>
        public Guid? AuthorizationId { get; set; }

        /// <summary>Sin autorización previa: ocupante de la unidad que confirmó, y por qué medio (citófono, teléfono, presencial).</summary>
        public Guid? ConfirmedByPersonId { get; set; }

        [MaxLength(30)]
        public string? ConfirmationChannel { get; set; }

        /// <summary>Acceso excepcional: ingreso sin autorización válida. Exige permiso y motivo.</summary>
        public bool IsException { get; set; }

        [MaxLength(300)]
        public string? ExceptionReason { get; set; }

        /// <summary>Quien autorizó el acceso excepcional, si no fue el propio operador.</summary>
        public Guid? ApprovedById { get; set; }

        [MaxLength(50)]
        public string? AccessPoint { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }
    }

    public class CheckOutDto
    {
        /// <summary>Cierre administrativo de una visita que nadie registró al salir; exige motivo.</summary>
        public bool Administrative { get; set; }

        [MaxLength(300)]
        public string? Reason { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }

        /// <summary>El visitante sale sin su vehículo: el vehículo sigue dentro hasta que se registre su salida.</summary>
        public bool WithoutVehicle { get; set; }
    }

    public class VehicleExitDto
    {
        [MaxLength(300)]
        public string? Notes { get; set; }
    }

    public class RejectDto
    {
        public Guid? UnitId { get; set; }
        public Guid? PersonId { get; set; }

        [Required(ErrorMessage = "Indique a quién se rechaza.")]
        [MaxLength(200)]
        public string PersonName { get; set; } = null!;

        [MaxLength(30)]
        public string? PersonDocument { get; set; }

        /// <summary>Código del catálogo rejection-reason.</summary>
        [Required(ErrorMessage = "El motivo del rechazo es obligatorio.")]
        [MaxLength(50)]
        public string ReasonCode { get; set; } = null!;

        [MaxLength(300)]
        public string? Notes { get; set; }

        [MaxLength(15)]
        public string? VehiclePlate { get; set; }
    }

    public class ResidentMovementDto
    {
        [Required(ErrorMessage = "La persona es obligatoria.")]
        public Guid? PersonId { get; set; }

        [Required(ErrorMessage = "La unidad es obligatoria.")]
        public Guid? UnitId { get; set; }

        /// <summary>Entry o Exit.</summary>
        [EnumDataType(typeof(AccessEventType))]
        public AccessEventType Type { get; set; }

        /// <summary>Vehículo permanente en el que entra o sale; también puede indicarse por placa.</summary>
        public Guid? VehicleId { get; set; }

        [MaxLength(15)]
        public string? VehiclePlate { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }

        /// <summary>Parqueadero en el que se estaciona al entrar.</summary>
        public Guid? ParkingSpaceId { get; set; }

        /// <summary>Entrada de un vehículo sin autorización vigente: exige permiso y motivo.</summary>
        public bool IsException { get; set; }

        [MaxLength(300)]
        public string? ExceptionReason { get; set; }
    }

    public class VoidDto
    {
        [Required(ErrorMessage = "El motivo de la anulación es obligatorio.")]
        [MinLength(5, ErrorMessage = "Explique el motivo de la anulación.")]
        [MaxLength(300)]
        public string Reason { get; set; } = null!;
    }

    // ---------- Consultas ----------

    public class VisitDto
    {
        public Guid Id { get; set; }
        public PersonDto Person { get; set; } = null!;
        public Guid UnitId { get; set; }
        public string? UnitLabel { get; set; }
        public string Reason { get; set; } = null!;
        public string? ReasonNote { get; set; }
        public Guid? VehicleId { get; set; }
        public string? VehiclePlate { get; set; }

        /// <summary>El visitante ya salió pero su vehículo sigue dentro.</summary>
        public bool VehicleStillInside { get; set; }

        /// <summary>Parqueadero que ocupa su vehículo mientras está dentro.</summary>
        public string? ParkingSpaceCode { get; set; }
        public VerificationResult Verification { get; set; }
        public Guid? AuthorizationId { get; set; }

        /// <summary>Quién autorizó de antemano o confirmó en portería.</summary>
        public string? AuthorizedByName { get; set; }

        /// <summary>Motivo del acceso excepcional.</summary>
        public string? ExceptionReason { get; set; }

        public Guid EntryEventId { get; set; }
        public DateTime EntryAt { get; set; }
        public string? EntryOperator { get; set; }

        public Guid? ExitEventId { get; set; }
        public DateTime? ExitAt { get; set; }
        public string? ExitOperator { get; set; }
        public bool ExitIsAdministrative { get; set; }

        public bool IsVoided { get; set; }

        /// <summary>Sigue dentro y superó las horas que el conjunto definió para revisarla.</summary>
        public bool NeedsReview { get; set; }
    }

    public enum VisitStatus
    {
        Inside = 0,
        CheckedOut = 1
    }

    public class VisitFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 500)]
        public int PageSize { get; set; } = 10;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public VisitStatus? Status { get; set; }
        public Guid? UnitId { get; set; }

        /// <summary>Nombre o documento del visitante.</summary>
        public string? Search { get; set; }
        public string? Plate { get; set; }
        public VerificationResult? Verification { get; set; }
        public Guid? OperatorId { get; set; }
    }

    public class AccessEventDto
    {
        public Guid Id { get; set; }
        public AccessEventType Type { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid? PersonId { get; set; }
        public string PersonName { get; set; } = null!;
        public string? PersonDocument { get; set; }
        public PersonKind PersonKind { get; set; }
        public Guid? UnitId { get; set; }
        public string? UnitLabel { get; set; }
        public Guid? VisitId { get; set; }
        public Guid? VehicleId { get; set; }
        public string? VehiclePlate { get; set; }
        public Guid OperatorId { get; set; }
        public string OperatorName { get; set; } = null!;
        public string? AccessPoint { get; set; }
        public string? Notes { get; set; }
        public VerificationResult? Verification { get; set; }
        public string? Reason { get; set; }
        public bool IsAdministrative { get; set; }

        /// <summary>En una anulación: el evento anulado.</summary>
        public Guid? VoidsEventId { get; set; }

        /// <summary>Otro evento lo anuló; sigue visible, marcado.</summary>
        public bool IsVoided { get; set; }
    }
}
