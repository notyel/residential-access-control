using AccessControl.Domain.Common;
using AccessControl.Shared.Enums;
using System;
using System.Collections.Generic;

namespace AccessControl.Domain.Entities
{
    /// <summary>
    /// Agrupación física del conjunto: torre, bloque, etapa, piso… Es opcional y anidable: un
    /// conjunto de casas puede no tener ninguna y un edificio puede tener torre → piso.
    /// </summary>
    public class Structure : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid? ParentId { get; set; }
        public Structure? Parent { get; set; }

        /// <summary>Código del catálogo structure-type (torre, bloque…).</summary>
        public string Type { get; set; } = null!;
        public string Name { get; set; } = null!;

        public ICollection<Structure> Children { get; set; } = new List<Structure>();
    }

    /// <summary>Unidad inmobiliaria: casa, apartamento, local. Todo lo demás se asocia a una unidad.</summary>
    public class Unit : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }

        /// <summary>Identificador de la unidad, único en el conjunto (lo que se dice en portería).</summary>
        public string Code { get; set; } = null!;

        /// <summary>Código del catálogo unit-type (casa, apartamento, local…).</summary>
        public string Type { get; set; } = null!;

        public Guid? StructureId { get; set; }
        public Structure? Structure { get; set; }

        /// <summary>Texto legible con su estructura: "Torre A · 102". Se recalcula al guardar.</summary>
        public string Label { get; set; } = null!;

        public decimal? Area { get; set; }
        public string? Notes { get; set; }

        public ICollection<UnitOwnership> Ownerships { get; set; } = new List<UnitOwnership>();
        public ICollection<Occupancy> Occupancies { get; set; } = new List<Occupancy>();
    }

    /// <summary>
    /// Titularidad de una persona sobre una unidad durante un periodo. Un cambio de propietario
    /// no edita la fila: cierra la anterior con fecha de fin y crea otra.
    /// </summary>
    public class UnitOwnership : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid UnitId { get; set; }
        public Unit Unit { get; set; } = null!;
        public Guid PersonId { get; set; }
        public Person Person { get; set; } = null!;
        public DateOnly StartDate { get; set; }

        /// <summary>Último día de la titularidad; null = vigente.</summary>
        public DateOnly? EndDate { get; set; }

        public decimal? Percentage { get; set; }
    }

    /// <summary>Quién vive en una unidad, con qué relación y durante qué periodo.</summary>
    public class Occupancy : AuditableEntity, IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public Guid UnitId { get; set; }
        public Unit Unit { get; set; } = null!;
        public Guid PersonId { get; set; }
        public Person Person { get; set; } = null!;
        public OccupancyRelation Relation { get; set; }
        public DateOnly StartDate { get; set; }

        /// <summary>Último día de la ocupación; null = sin fecha de fin.</summary>
        public DateOnly? EndDate { get; set; }

        /// <summary>
        /// Pendiente, vigente, finalizada o rechazada. "Vencida" no se guarda: una ocupación
        /// vigente con EndDate pasada deja de contar sin que nadie la toque.
        /// </summary>
        public OccupancyStatus Status { get; set; } = OccupancyStatus.Active;

        /// <summary>Motivo del rechazo o de la finalización.</summary>
        public string? StatusReason { get; set; }

        /// <summary>Usuario que la solicitó desde el portal, si no la registró la administración.</summary>
        public Guid? RequestedById { get; set; }
    }

    /// <summary>
    /// Valor de un catálogo configurable por conjunto (tipos de unidad, de documento, motivos de
    /// visita…). Mientras un conjunto no guarde los suyos, rigen los valores por defecto del código.
    /// </summary>
    public class CatalogItem : IComplexScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplexId { get; set; }
        public string Catalog { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Label { get; set; } = null!;
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
