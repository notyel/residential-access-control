using AccessControl.Common.DTOs.Parking;
using AccessControl.Common.DTOs.Report;
using System;
using System.Collections.Generic;

namespace AccessControl.Common.DTOs.Dashboard
{
    /// <summary>
    /// El panel se arma con bloques; el servidor solo envía los que los permisos del usuario
    /// habilitan (los demás llegan en null). Quien tiene varios roles recibe la unión.
    /// </summary>
    public class DashboardDto
    {
        public int TotalVisitsThisMonth { get; set; }
        public int VisitorsInside { get; set; }
        public int VisitsToday { get; set; }
        public IEnumerable<LatestVisitDto>? LatestVisits { get; set; }

        /// <summary>Portería (access.register).</summary>
        public GateBlockDto? Gate { get; set; }

        /// <summary>Administración y auditoría (reports.read).</summary>
        public ManagementBlockDto? Management { get; set; }

        /// <summary>Propietario o residente (units.read_own).</summary>
        public PortalBlockDto? Portal { get; set; }
    }

    public class GateBlockDto
    {
        public int ExpectedToday { get; set; }
        public int Inside { get; set; }
        public int VehiclesInside { get; set; }
        public AvailabilityRowDto VisitorParking { get; set; } = new();
        public List<IncidentDto> LatestIncidents { get; set; } = new();
    }

    public class DashboardAlertDto
    {
        public DateTime OccurredAt { get; set; }
        /// <summary>rejection o exception.</summary>
        public string Kind { get; set; } = null!;
        public string PersonName { get; set; } = null!;
        public string? UnitLabel { get; set; }
        public string? Reason { get; set; }
    }

    public class ManagementBlockDto
    {
        public int UnitsVacant { get; set; }
        public int UnitsOwnerOccupied { get; set; }
        public int UnitsRented { get; set; }
        public int AccessesToday { get; set; }
        public int AccessesWeek { get; set; }
        public int Inside { get; set; }
        public List<DashboardAlertDto> RecentAlerts { get; set; } = new();
        public List<AvailabilityRowDto> Parking { get; set; } = new();
        public int PendingOccupancies { get; set; }
        public int PendingVehicles { get; set; }
        public int ExpiringLinks { get; set; }
    }

    public class PortalBlockDto
    {
        public List<string> Units { get; set; } = new();
        public int ActiveAuthorizations { get; set; }
        public int Vehicles { get; set; }
    }
}
