using System;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Audit
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = null!;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }

        /// <summary>JSON: { campo: { old, new } } en cambios de entidades; datos libres en hechos explícitos.</summary>
        public string? Changes { get; set; }

        public string? IpAddress { get; set; }
    }

    public class AuditFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 200)]
        public int PageSize { get; set; } = 25;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? UserId { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Action { get; set; }
    }
}
