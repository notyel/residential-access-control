using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    public class Menu : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public int Order { get; set; }

        /// <summary>Encabezado bajo el que se agrupa en el menú; null = sin grupo.</summary>
        public string? Group { get; set; }

        /// <summary>
        /// Permiso que habilita la opción. Varios separados por coma = basta con tener uno.
        /// </summary>
        public string Permission { get; set; } = null!;
    }
}
