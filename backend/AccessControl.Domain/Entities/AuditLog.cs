using System;

namespace AccessControl.Domain.Entities
{
    /// <summary>
    /// Bitácora de lo que hacen los usuarios en la plataforma: quién, qué y cuándo. Solo se
    /// agregan filas; no hay operación para editarlas ni borrarlas.
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Conjunto al que pertenece el hecho; null para acciones de plataforma.</summary>
        public Guid? ComplexId { get; set; }

        public Guid? UserId { get; set; }

        /// <summary>Nombre de quien lo hizo, tal como era en ese momento.</summary>
        public string? UserName { get; set; }

        public DateTime OccurredAt { get; set; }

        /// <summary>created, updated, deleted, o un hecho explícito como auth.login.</summary>
        public string Action { get; set; } = null!;

        public string? EntityType { get; set; }
        public string? EntityId { get; set; }

        /// <summary>JSON con los campos que cambiaron: { campo: { old, new } }. Nunca incluye secretos.</summary>
        public string? Changes { get; set; }

        public string? IpAddress { get; set; }
    }
}
