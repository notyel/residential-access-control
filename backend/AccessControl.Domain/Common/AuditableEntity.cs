using System;

namespace AccessControl.Domain.Common
{
    public abstract class AuditableEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Usuario que creó el registro; null si lo creó el sistema (sembrado, migraciones).</summary>
        public Guid? CreatedById { get; set; }
        public Guid? UpdatedById { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>Campo que nunca se copia a la bitácora (contraseñas, sellos, tokens).</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotAuditedAttribute : Attribute
    {
    }
}
