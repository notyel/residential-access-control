using System;
using System.Collections.Generic;

namespace AccessControl.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<User> Users { get; set; } = new List<User>();
    }
}