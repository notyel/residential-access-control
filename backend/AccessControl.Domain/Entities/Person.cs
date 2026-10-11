using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    /// <summary>
    /// Registro único de cualquier persona del conjunto: propietario, ocupante o visitante. Su
    /// papel sale de sus vínculos (titularidades, ocupaciones, visitas), no de un campo.
    /// </summary>
    public class Person : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        /// <summary>Código del catálogo document-type (CC, CE, TI, PA, NIT…).</summary>
        public string DocumentType { get; set; } = null!;
        public string DocumentNumber { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
