using AccessControl.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessControl.Common.DTOs.Report
{
    public class AccessHistoryFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 200)]
        public int PageSize { get; set; } = 25;

        /// <summary>El rango de fechas es obligatorio: protege el rendimiento de la consulta.</summary>
        [Required(ErrorMessage = "La fecha inicial es obligatoria.")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "La fecha final es obligatoria.")]
        public DateTime? EndDate { get; set; }

        /// <summary>Nombre o documento de la persona.</summary>
        public string? Search { get; set; }
        public Guid? UnitId { get; set; }
        public string? Plate { get; set; }
        public AccessEventType? Type { get; set; }
        public PersonKind? PersonKind { get; set; }
        public Guid? OperatorId { get; set; }
        public string? AccessPoint { get; set; }

        /// <summary>Solo accesos excepcionales.</summary>
        public bool OnlyExceptions { get; set; }
    }

    public class ReportFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ReportDefinitionDto
    {
        public string Key { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        /// <summary>Si el reporte depende del rango de fechas (los de estado actual no).</summary>
        public bool UsesDates { get; set; }
    }

    /// <summary>Reporte tabular: sirve igual para la pantalla y para el CSV.</summary>
    public class ReportDto
    {
        public string Key { get; set; } = null!;
        public string Title { get; set; } = null!;
        public List<string> Columns { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }

    public class IncidentDto
    {
        public Guid Id { get; set; }
        public DateTime OccurredAt { get; set; }
        public string Category { get; set; } = null!;
        public string CategoryLabel { get; set; } = null!;
        public string Text { get; set; } = null!;
        public string? UnitLabel { get; set; }
        public string? PersonName { get; set; }
        public string? VehiclePlate { get; set; }
        public string AuthorName { get; set; } = null!;
    }

    public class CreateIncidentDto
    {
        /// <summary>Código del catálogo incident-category.</summary>
        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [MaxLength(50)]
        public string Category { get; set; } = null!;

        [Required(ErrorMessage = "Describa la novedad.")]
        [MinLength(5, ErrorMessage = "Describa la novedad.")]
        [MaxLength(1000)]
        public string Text { get; set; } = null!;

        public Guid? UnitId { get; set; }
        public Guid? PersonId { get; set; }

        [MaxLength(15)]
        public string? VehiclePlate { get; set; }
    }

    public class IncidentFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 200)]
        public int PageSize { get; set; } = 25;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Category { get; set; }
    }
}
