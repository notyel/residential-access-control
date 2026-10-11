using AccessControl.Domain.Common;
using AccessControl.Shared.Enums;
using System;

namespace AccessControl.Domain.Entities
{
    /// <summary>
    /// Espacio de parqueadero. Guarda lo que el espacio es (tipo, ubicación); si está disponible,
    /// ocupado o reservado se calcula de sus asignaciones. No se borra: se da de baja con IsActive.
    /// </summary>
    public class ParkingSpace : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public string Code { get; set; } = null!;

        /// <summary>Zona, sótano o nivel.</summary>
        public string? Location { get; set; }
        public ParkingSpaceType Type { get; set; }
        public ParkingVehicleKind VehicleKind { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Toda relación espacio–beneficiario–periodo: titular de un privado, asignación temporal,
    /// reserva u ocupación por un vehículo. La base impide que dos incompatibles se crucen.
    /// </summary>
    public class ParkingAssignment : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid SpaceId { get; set; }
        public ParkingSpace Space { get; set; } = null!;
        public ParkingAssignmentClass Class { get; set; }

        public Guid? UnitId { get; set; }
        public Unit? Unit { get; set; }
        public Guid? PersonId { get; set; }
        public Person? Person { get; set; }
        public Guid? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        public Guid? VisitId { get; set; }

        public DateTime StartsAt { get; set; }
        /// <summary>null = sin fin definido (titularidad vigente, vehículo que sigue estacionado).</summary>
        public DateTime? EndsAt { get; set; }
        public ParkingAssignmentStatus Status { get; set; }
        public string? Notes { get; set; }

        /// <summary>Uso que las reglas no permiten (p. ej. un privado ocupado por un vehículo ajeno a su unidad).</summary>
        public bool IsException { get; set; }
        public string? ExceptionReason { get; set; }
    }

    /// <summary>Periodo en que el espacio no se puede usar. Al terminar, vuelve solo a su estado calculado.</summary>
    public class ParkingOutOfService : IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid SpaceId { get; set; }
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public string Reason { get; set; } = null!;
        public Guid? CreatedById { get; set; }
    }

    /// <summary>Novedad de un espacio (mal estacionado, daño), con fecha y autor.</summary>
    public class ParkingNote : IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid SpaceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? AuthorId { get; set; }
        public User? Author { get; set; }
        public string Text { get; set; } = null!;
    }
}
