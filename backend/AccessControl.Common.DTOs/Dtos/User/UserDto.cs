using AccessControl.Common.DTOs.Role;
using System;
using System.Collections.Generic;

namespace AccessControl.Common.DTOs.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ApartmentNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsSuperAdmin { get; set; }
        public Guid? ComplexId { get; set; }
        public string? ComplexName { get; set; }
        public List<RoleRefDto> Roles { get; set; } = new();

        /// <summary>Permisos efectivos. Solo se llena para el usuario de la sesión (login y auth/me).</summary>
        public List<string>? Permissions { get; set; }
    }
}
