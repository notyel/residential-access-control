using AccessControl.Domain.Common;
using AccessControl.Shared.Enums;
using System;

namespace AccessControl.Domain.Entities
{
    /// <summary>Conjunto residencial, edificio o propiedad con portería: el cliente al que pertenecen los datos.</summary>
    public class Complex : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public ComplexType Type { get; set; }
        public string? Address { get; set; }
        public string TimeZoneId { get; set; } = "America/Bogota";
    }
}
