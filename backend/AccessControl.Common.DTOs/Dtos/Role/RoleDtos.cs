using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Role
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? SystemKey { get; set; }
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public List<string> Permissions { get; set; } = new();
        public int UserCount { get; set; }
    }

    public class SaveRoleDto
    {
        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(300)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Indique los permisos del rol.")]
        public List<string> Permissions { get; set; } = new();
    }

    public class PermissionDto
    {
        public string Key { get; set; } = null!;
        public string Module { get; set; } = null!;
        public string Label { get; set; } = null!;
    }

    /// <summary>Referencia corta a un rol, para listarlo dentro de un usuario.</summary>
    public class RoleRefDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? SystemKey { get; set; }
    }
}
