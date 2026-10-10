using AccessControl.Domain.Common;
using System;
using System.Collections.Generic;

namespace AccessControl.Domain.Entities
{
    /// <summary>Rol de un conjunto. Sus permisos son claves del catálogo (Permissions).</summary>
    public class Role : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        /// <summary>Clave del rol de sistema (admin, guard, owner…); null en los roles creados por el conjunto.</summary>
        public string? SystemKey { get; set; }

        public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
        public string Permission { get; set; } = null!;
    }

    public class UserRole
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }

    /// <summary>
    /// Permisos del catálogo cuyos valores por defecto ya se repartieron a los roles de sistema.
    /// Evita volver a conceder un permiso que un administrador quitó a propósito.
    /// </summary>
    public class AppliedPermissionDefault
    {
        public string Key { get; set; } = null!;
    }
}
