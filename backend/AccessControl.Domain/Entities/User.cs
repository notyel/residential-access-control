using AccessControl.Domain.Common;
using AccessControl.Shared.Enums;
using System;
using System.Collections.Generic;

namespace AccessControl.Domain.Entities
{
    public class User : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ComplexId { get; set; } // null solo para el superadministrador
        public Complex? Complex { get; set; }
        public bool IsSuperAdmin { get; set; }

        /// <summary>
        /// Persona del conjunto a la que corresponde la cuenta, si la tiene. Las credenciales y
        /// los vínculos con unidades son independientes: tener cuenta no da ningún vínculo.
        /// </summary>
        public Guid? PersonId { get; set; }
        public Person? Person { get; set; }
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string ApartmentNumber { get; set; } = null!;

        /// <summary>Vacío mientras el usuario está invitado y no ha definido contraseña.</summary>
        [NotAudited]
        public string PasswordHash { get; set; } = null!;

        public UserStatus Status { get; set; } = UserStatus.Invited;

        /// <summary>
        /// Viaja en el token. Cambia al desactivar, revocar o cambiar la contraseña: los tokens
        /// emitidos antes dejan de servir de inmediato.
        /// </summary>
        [NotAudited]
        public Guid SecurityStamp { get; set; } = Guid.NewGuid();

        /// <summary>Intentos fallidos seguidos de inicio de sesión; vuelve a cero al entrar.</summary>
        public int FailedLoginCount { get; set; }

        /// <summary>Hasta cuándo está bloqueado el inicio de sesión por intentos fallidos.</summary>
        public DateTime? LockedUntil { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    /// <summary>Enlace de un solo uso (invitación o recuperación). Solo se guarda su hash.</summary>
    public class UserToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public UserTokenPurpose Purpose { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
