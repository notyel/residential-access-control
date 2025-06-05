using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessControl.Domain.Entities
{
    public abstract class UserBase
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string IdentificationNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Resident", "Employee", etc.
        public bool IsActive { get; set; } = true;
    }
}
