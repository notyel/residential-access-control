using System;
using System.Collections.Generic;

namespace AccessControl.Domain.Entities
{
    public class Menu
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
    }
}
