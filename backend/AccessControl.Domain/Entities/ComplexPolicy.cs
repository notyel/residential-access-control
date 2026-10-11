using AccessControl.Domain.Common;
using System;

namespace AccessControl.Domain.Entities
{
    /// <summary>
    /// Valor de una política que un conjunto cambió respecto al valor por defecto del catálogo.
    /// </summary>
    public class ComplexPolicy : IComplexScoped
    {
        public Guid ComplexId { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
