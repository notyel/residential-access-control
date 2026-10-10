using AccessControl.Domain.Common;
using System;
using System.Collections.Generic;

namespace AccessControl.Application.Services
{
    /// <summary>
    /// Sesión de la petición en curso (una instancia por petición). La API la llena desde el token
    /// antes de llegar a los controladores; las pruebas y tareas internas pueden fijarla a mano.
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; set; }
        public Guid? ComplexId { get; set; }
        public bool IsSuperAdmin { get; set; }
        public string? TimeZoneId { get; set; }
        public IReadOnlySet<string> Permissions { get; set; } = new HashSet<string>();

        // Quien puede sobre todo el conjunto (visits.read) puede también sobre lo propio (visits.read_own)
        public bool HasPermission(string permission) =>
            IsSuperAdmin
            || Permissions.Contains(permission)
            || (permission.EndsWith(OwnSuffix) && Permissions.Contains(permission[..^OwnSuffix.Length]));

        private const string OwnSuffix = "_own";
    }

    public static class AppClaims
    {
        public const string ComplexId = "complex";
        public const string TimeZone = "tz";
        public const string SuperAdmin = "super";
    }
}
