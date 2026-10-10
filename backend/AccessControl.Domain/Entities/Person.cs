using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    public class Person : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string DocumentType { get; set; } = null!;
        public string DocumentNumber { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int PersonType { get; set; }
    }
}
