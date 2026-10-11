using AccessControl.Domain.Common;
using AccessControl.Shared.Enums;
using System;
using System.Collections.Generic;

namespace AccessControl.Domain.Entities
{
    public class Vehicle : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }

        /// <summary>
        /// Placa normalizada (mayúsculas, sin espacios ni guiones), única en el conjunto. Los
        /// vehículos sin placa (una bicicleta) llevan un identificador interno.
        /// </summary>
        public string Plate { get; set; } = null!;
        public bool HasPlate { get; set; } = true;

        /// <summary>Código del catálogo vehicle-type.</summary>
        public string Type { get; set; } = null!;
        public string? Brand { get; set; }
        public string? Color { get; set; }
        public string? Model { get; set; }
        public string? Notes { get; set; }
        public VehicleClass Class { get; set; }

        public ICollection<VehicleAuthorization> Authorizations { get; set; } = new List<VehicleAuthorization>();
    }

    /// <summary>
    /// Vínculo de un vehículo permanente con una unidad y con la persona que responde por él.
    /// Cambiar de unidad o de responsable cierra una y abre otra: el historial se conserva.
    /// </summary>
    public class VehicleAuthorization : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;
        public Guid UnitId { get; set; }
        public Unit Unit { get; set; } = null!;
        public Guid ResponsiblePersonId { get; set; }
        public Person Responsible { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public VehicleAuthorizationStatus Status { get; set; }
        public string? StatusReason { get; set; }
    }

    /// <summary>Novedad de un vehículo: se agrega con fecha y autor, no se sobrescribe.</summary>
    public class VehicleNote : IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? AuthorId { get; set; }
        public User? Author { get; set; }
        public string Text { get; set; } = null!;
    }
}
