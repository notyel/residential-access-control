using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    public class Visit : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string VisitorName { get; set; } = null!;
        public string VisitorId { get; set; } = null!;
        public string? VehiclePlate { get; set; }
        public DateTime? CheckOut { get; set; }
        public Guid ResidenceId { get; set; } // a qué propiedad viene
        public Residence Residence { get; set; } = null!;
        public Guid RegisteredById { get; set; } // Guard / User.Id
        public User RegisteredBy { get; set; } = null!;
    }
}
