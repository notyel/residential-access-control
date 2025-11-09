using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    public class Residence : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Identifier { get; set; } = null!; // ej: Torre A - Apt 102
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;
    }
}
