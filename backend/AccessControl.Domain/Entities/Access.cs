using AccessControl.Domain.Common;
using AccessControl.Shared.Enums;
using System;

namespace AccessControl.Domain.Entities
{
    /// <summary>
    /// Hecho de acceso: una entrada, una salida, un rechazo o una anulación. Solo se agregan; ni el
    /// API ni la base permiten modificarlos o borrarlos. Un error se corrige con un evento de
    /// anulación que apunta al original, que sigue visible.
    /// </summary>
    public class AccessEvent : IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public AccessEventType Type { get; set; }
        public DateTime OccurredAt { get; set; }

        public Guid? PersonId { get; set; }
        public Person? Person { get; set; }

        /// <summary>
        /// Nombre y documento tal como eran en el momento del acceso: el historial no cambia si
        /// después se corrigen los datos de la persona.
        /// </summary>
        public string PersonName { get; set; } = null!;
        public string? PersonDocument { get; set; }
        public PersonKind PersonKind { get; set; }

        public Guid? UnitId { get; set; }
        public Unit? Unit { get; set; }

        /// <summary>Estancia a la que pertenece el evento, cuando es de un visitante.</summary>
        public Guid? VisitId { get; set; }

        public Guid? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        /// <summary>Placa tal como se registró en el acceso.</summary>
        public string? VehiclePlate { get; set; }

        /// <summary>Usuario que registró el evento en portería.</summary>
        public Guid OperatorId { get; set; }
        public User Operator { get; set; } = null!;

        /// <summary>Código del catálogo access-point.</summary>
        public string? AccessPoint { get; set; }

        public string? Notes { get; set; }

        /// <summary>Cómo se verificó el ingreso (solo en entradas de visitantes).</summary>
        public VerificationResult? Verification { get; set; }

        /// <summary>Motivo del rechazo, del acceso excepcional, de la salida administrativa o de la anulación.</summary>
        public string? Reason { get; set; }

        /// <summary>Quien autorizó un acceso excepcional, si no fue el propio operador.</summary>
        public Guid? ApprovedById { get; set; }

        /// <summary>Ocupante de la unidad que confirmó la visita, y por qué medio.</summary>
        public Guid? ConfirmedByPersonId { get; set; }
        public string? ConfirmationChannel { get; set; }

        /// <summary>Salida que no corresponde a una hora real de salida (cierre administrativo).</summary>
        public bool IsAdministrative { get; set; }

        /// <summary>En un evento de anulación: el evento que anula.</summary>
        public Guid? VoidsEventId { get; set; }
    }

    /// <summary>
    /// Estancia de un visitante: agrupa su entrada, su salida, la autorización usada y el vehículo.
    /// "Está dentro" = tiene entrada y no tiene salida; no hay un campo de estado.
    /// </summary>
    public class Visit : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid PersonId { get; set; }
        public Person Person { get; set; } = null!;
        public Guid UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        /// <summary>Código del catálogo visit-reason.</summary>
        public string Reason { get; set; } = "otro";
        public string? ReasonNote { get; set; }

        /// <summary>Vehículo con el que llegó; su entrada y su salida son eventos de acceso.</summary>
        public Guid? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public VerificationResult Verification { get; set; }
        public Guid? AuthorizationId { get; set; }
        public VisitAuthorization? Authorization { get; set; }

        public Guid EntryEventId { get; set; }
        public AccessEvent EntryEvent { get; set; } = null!;
        public Guid? ExitEventId { get; set; }
        public AccessEvent? ExitEvent { get; set; }

        /// <summary>Evento que anuló la entrada, si se registró por error.</summary>
        public Guid? VoidEventId { get; set; }

        public Guid RegisteredById { get; set; }
        public User RegisteredBy { get; set; } = null!;
    }

    /// <summary>
    /// Autorización previa de visita. No se edita: se revoca y se crea otra, para que el historial
    /// conserve lo que estaba autorizado en cada momento.
    /// </summary>
    public class VisitAuthorization : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }

        public Guid UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        /// <summary>Propietario u ocupante de la unidad que autoriza.</summary>
        public Guid AuthorizedByPersonId { get; set; }
        public Person AuthorizedBy { get; set; } = null!;

        /// <summary>Visitante ya registrado; si no, se identifica por nombre y documento.</summary>
        public Guid? VisitorPersonId { get; set; }
        public Person? VisitorPerson { get; set; }
        public string VisitorName { get; set; } = null!;
        public string? VisitorDocument { get; set; }

        public string Reason { get; set; } = "social";
        public string? Notes { get; set; }

        public AuthorizationKind Kind { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        /// <summary>Franja horaria en la que vale; null = todo el día. Puede cruzar la medianoche.</summary>
        public TimeOnly? TimeFrom { get; set; }
        public TimeOnly? TimeTo { get; set; }

        /// <summary>Días de la semana (recurrente): bit 0 = domingo … bit 6 = sábado.</summary>
        public int DaysOfWeek { get; set; }

        /// <summary>Vigente, revocada o consumida. "Vencida" se calcula por fecha.</summary>
        public AuthorizationStatus Status { get; set; } = AuthorizationStatus.Active;

        public DateTime? RevokedAt { get; set; }
        public Guid? RevokedById { get; set; }
        public string? RevokeReason { get; set; }
        public DateTime? ConsumedAt { get; set; }
    }
}
