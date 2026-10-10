using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    public class Visit : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid PersonId { get; set; }
        public Person Person { get; set; } = null!;
        public string? VehiclePlate { get; set; }
        public DateTime? CheckOut { get; set; }
        public Guid? CheckedOutById { get; set; } // quién registró la salida
        public User? CheckedOutBy { get; set; }
        public Guid ResidenceId { get; set; } // a qué propiedad viene
        public Residence Residence { get; set; } = null!;
        public Guid RegisteredById { get; set; } // Guard / User.Id
        public User RegisteredBy { get; set; } = null!;
    }
}
