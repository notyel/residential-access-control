using AccessControl.Shared.Enums;
using System;

namespace AccessControl.Domain.Entities
{
    public class RoleMenu
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Role Role { get; set; }
        public Guid MenuId { get; set; }
        public Menu Menu { get; set; } = null!;
    }
}
