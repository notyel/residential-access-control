using System;

namespace AccessControl.Domain.Common
{
    /// <summary>
    /// Quién hace la petición y en qué conjunto trabaja. Lo llena la API a partir del token;
    /// fuera de una petición (migraciones, tareas en segundo plano) todo es null.
    /// </summary>
    public interface ICurrentUser
    {
        Guid? UserId { get; }

        /// <summary>
        /// Conjunto sobre el que se filtran y se guardan los datos. Null = sin conjunto: las
        /// consultas de entidades con conjunto no devuelven nada.
        /// </summary>
        Guid? ComplexId { get; }

        /// <summary>Administra la plataforma; no pertenece a un conjunto y elige en cuál trabajar.</summary>
        bool IsSuperAdmin { get; }

        /// <summary>Zona horaria del conjunto actual (ID IANA o Windows), si se conoce.</summary>
        string? TimeZoneId { get; }

        /// <summary>Permiso efectivo (unión de los roles activos del usuario). El superadministrador los tiene todos.</summary>
        bool HasPermission(string permission);
    }

    /// <summary>Entidad que pertenece a un conjunto. El DbContext la filtra y la marca automáticamente.</summary>
    public interface IComplexScoped
    {
        Guid ComplexId { get; set; }
    }
}
