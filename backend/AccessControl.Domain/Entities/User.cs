using AccessControl.Shared.Enums;
using AccessControl.Domain.Common;
using System;
using System.Collections.Generic;

namespace AccessControl.Domain.Entities
{
    public class User : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string ApartmentNumber { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public Role Role { get; set; }
        public ICollection<Residence>? Residences { get; set; } // si es propietario
    }
}
