using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    /// <summary>
    /// Novedad del turno de portería. Solo se agregan: no hay operación para editarlas ni borrarlas.
    /// </summary>
    public class Incident : IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public DateTime OccurredAt { get; set; }

        /// <summary>Código del catálogo incident-category.</summary>
        public string Category { get; set; } = null!;
        public string Text { get; set; } = null!;

        public Guid? UnitId { get; set; }
        public Unit? Unit { get; set; }
        public Guid? PersonId { get; set; }
        public Person? Person { get; set; }
        public string? VehiclePlate { get; set; }

        public Guid AuthorId { get; set; }
        public User Author { get; set; } = null!;
    }
}
