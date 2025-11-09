using AccessControl.Domain.Enums;
using System;
using System.Collections.Generic;

namespace AccessControl.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public Role Role { get; set; }
        public ICollection<Residence>? Residences { get; set; } // si es propietario
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
