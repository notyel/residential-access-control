using AccessControl.Application.Access;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Parking;
using AccessControl.Application.Reports;
using AccessControl.Application.Security;
using AccessControl.Application.Units;
using AccessControl.Application.Vehicles;
using AccessControl.Common.DTOs.Dashboard;
using AccessControl.Common.DTOs.Parking;
using AccessControl.Common.DTOs.Report;
using AccessControl.Domain.Common;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Services
{
    /// <summary>
    /// Arma el panel con los bloques que los permisos del usuario habilitan. Reutiliza las
    /// consultas de visitantes, parqueaderos y reportes: aquí no hay otra lógica en paralelo, y
    /// todas las cifras se calculan en el momento, con el día del conjunto.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private const int LatestVisitsCount = 10;

        private readonly ApplicationDbContext _db;
        private readonly IDateTimeService _dateTime;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitScope _scope;
        private readonly IAuthorizationService _authorizations;
        private readonly IParkingService _parking;
        private readonly IIncidentService _incidents;
        private readonly IReportService _reports;

        public DashboardService(
            ApplicationDbContext db,
            IDateTimeService dateTime,
            ICurrentUser currentUser,
            IUnitScope scope,
            IAuthorizationService authorizations,
            IParkingService parking,
            IIncidentService incidents,
            IReportService reports)
        {
            _db = db;
            _dateTime = dateTime;
            _currentUser = currentUser;
            _scope = scope;
            _authorizations = authorizations;
            _parking = parking;
            _incidents = incidents;
            _reports = reports;
        }

        public async Task<DashboardDto> GetDashboardAsync(IReadOnlyCollection<Guid>? unitScope = null)
        {
            var visits = _db.Visits.AsNoTracking();
            if (unitScope != null)
            {
                visits = visits.Where(v => unitScope.Contains(v.UnitId));
            }

            // los límites del mes y del día siguen la zona horaria del conjunto, comparados en UTC
            var (startUtc, endUtc) = _dateTime.GetCurrentMonthRangeUtc();
            var (todayStartUtc, todayEndUtc) = _dateTime.GetTodayRangeUtc();

            var dashboard = new DashboardDto
            {
                TotalVisitsThisMonth = await visits.CountAsync(v => v.CreatedAt >= startUtc && v.CreatedAt < endUtc),
                VisitorsInside = await visits.CountAsync(v => v.ExitEventId == null && v.VoidEventId == null),
                VisitsToday = await visits.CountAsync(v => v.CreatedAt >= todayStartUtc && v.CreatedAt < todayEndUtc),
                LatestVisits = await visits
                    .OrderByDescending(v => v.CreatedAt)
                    .Take(LatestVisitsCount)
                    .Select(v => new LatestVisitDto
                    {
                        VisitorFullName = v.Person.FirstName + " " + v.Person.LastName,
                        UnitLabel = v.Unit.Label,
                        EntryTime = v.CreatedAt,
                        CheckOut = v.ExitEvent == null ? null : v.ExitEvent.OccurredAt
                    })
                    .ToListAsync()
            };

            if (_currentUser.HasPermission(Permissions.AccessRegister))
            {
                dashboard.Gate = await GateAsync(dashboard.VisitorsInside);
            }
            if (_currentUser.HasPermission(Permissions.ReportsRead))
            {
                dashboard.Management = await ManagementAsync(todayStartUtc, todayEndUtc);
            }
            // "lo propio" solo para quien de verdad es propietario o residente, no para quien administra
            if (_currentUser.HoldsPermission(Permissions.UnitsReadOwn))
            {
                dashboard.Portal = await PortalAsync();
            }
            return dashboard;
        }

        private async Task<GateBlockDto> GateAsync(int inside)
        {
            var availability = await _parking.GetAvailabilityAsync(null, null);
            return new GateBlockDto
            {
                ExpectedToday = (await _authorizations.GetExpectedTodayAsync()).Count,
                Inside = inside,
                VehiclesInside = await VehicleService.OpenEntries(_db).CountAsync(),
                VisitorParking = availability.ByType.First(r => r.Type == ParkingSpaceType.Visitor),
                LatestIncidents = (await _incidents.SearchAsync(new IncidentFilterDto { PageSize = 5 })).Items?.ToList() ?? new List<IncidentDto>()
            };
        }

        private async Task<ManagementBlockDto> ManagementAsync(DateTime todayStartUtc, DateTime todayEndUtc)
        {
            var today = _dateTime.Today;
            var weekStartUtc = todayStartUtc.AddDays(-6);

            // estado de ocupación de cada unidad activa, calculado de sus ocupaciones vigentes
            var units = await _db.Units.AsNoTracking().Where(u => u.IsActive)
                .Select(u => new
                {
                    OwnerLives = u.Occupancies.Any(o => o.Status == OccupancyStatus.Active && o.Relation == OccupancyRelation.OwnerResident
                        && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)),
                    Occupied = u.Occupancies.Any(o => o.Status == OccupancyStatus.Active
                        && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                })
                .ToListAsync();

            var entries = _db.AccessEvents.AsNoTracking().Where(e => e.Type == AccessEventType.Entry
                && !_db.AccessEvents.Any(v => v.VoidsEventId == e.Id));
            var alerts = await _db.AccessEvents.AsNoTracking()
                .Where(e => (e.Type == AccessEventType.Rejection || e.Verification == VerificationResult.Exception)
                    && !_db.AccessEvents.Any(v => v.VoidsEventId == e.Id))
                .OrderByDescending(e => e.OccurredAt)
                .Take(5)
                .Select(e => new DashboardAlertDto
                {
                    OccurredAt = e.OccurredAt,
                    Kind = e.Type == AccessEventType.Rejection ? "rejection" : "exception",
                    PersonName = e.PersonName,
                    UnitLabel = e.Unit == null ? null : e.Unit.Label,
                    Reason = e.Reason
                })
                .ToListAsync();

            var availability = await _parking.GetAvailabilityAsync(null, null);
            var expiring = await _reports.RunAsync(ReportingService.ExpiringLinks, new ReportFilterDto());

            return new ManagementBlockDto
            {
                UnitsVacant = units.Count(u => !u.Occupied),
                UnitsOwnerOccupied = units.Count(u => u.OwnerLives),
                UnitsRented = units.Count(u => u.Occupied && !u.OwnerLives),
                AccessesToday = await entries.CountAsync(e => e.OccurredAt >= todayStartUtc && e.OccurredAt < todayEndUtc),
                AccessesWeek = await entries.CountAsync(e => e.OccurredAt >= weekStartUtc && e.OccurredAt < todayEndUtc),
                Inside = await _db.Visits.CountAsync(v => v.ExitEventId == null && v.VoidEventId == null),
                RecentAlerts = alerts,
                Parking = availability.ByType,
                PendingOccupancies = await _db.Occupancies.CountAsync(o => o.Status == OccupancyStatus.Pending),
                PendingVehicles = await _db.VehicleAuthorizations.CountAsync(a => a.Status == VehicleAuthorizationStatus.Pending),
                ExpiringLinks = expiring.Rows.Count
            };
        }

        private async Task<PortalBlockDto> PortalAsync()
        {
            var unitIds = await _scope.GetAccessibleUnitIdsAsync();
            var today = _dateTime.Today;
            return new PortalBlockDto
            {
                Units = await _db.Units.Where(u => unitIds.Contains(u.Id)).OrderBy(u => u.Label).Select(u => u.Label).ToListAsync(),
                ActiveAuthorizations = await _db.VisitAuthorizations.CountAsync(a => unitIds.Contains(a.UnitId)
                    && a.Status == AuthorizationStatus.Active && a.EndDate >= today),
                Vehicles = await _db.VehicleAuthorizations.CountAsync(a => unitIds.Contains(a.UnitId)
                    && (a.Status == VehicleAuthorizationStatus.Active || a.Status == VehicleAuthorizationStatus.Pending))
            };
        }
    }
}
