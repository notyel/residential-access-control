using AccessControl.Application.Access;
using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Parking;
using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Application.Services;
using AccessControl.Application.Units;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Report;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessControl.Application.Reports
{
    public interface IAccessHistoryService
    {
        /// <summary>
        /// Todos los eventos de acceso, según lo que el rol permite ver: todo el conjunto, solo los
        /// últimos días (portería) o solo las unidades propias durante el vínculo (portal).
        /// </summary>
        Task<PaginatedResultDto<AccessEventDto>> SearchAsync(AccessHistoryFilterDto filter);
    }

    public interface IReportService
    {
        IReadOnlyList<ReportDefinitionDto> Definitions { get; }
        Task<ReportDto> RunAsync(string key, ReportFilterDto filter);

        /// <summary>Deja en la bitácora quién exportó qué y con qué filtros.</summary>
        Task RecordExportAsync(string what, object filters);
    }

    public interface IIncidentService
    {
        Task<PaginatedResultDto<IncidentDto>> SearchAsync(IncidentFilterDto filter);
        Task<IncidentDto> CreateAsync(CreateIncidentDto dto);
    }

    public interface IPrivacyService
    {
        /// <summary>
        /// Anonimiza en todos los conjuntos a los visitantes cuyo último acceso superó el periodo de
        /// retención configurado. Devuelve cuántas personas se anonimizaron.
        /// </summary>
        Task<int> AnonymizeExpiredVisitorsAsync();
    }

    public static class Csv
    {
        /// <summary>CSV con BOM (para que Excel lea los acentos) y separador punto y coma, el habitual en español.</summary>
        public static byte[] Build(IEnumerable<string> columns, IEnumerable<IEnumerable<string?>> rows)
        {
            var text = new StringBuilder();
            text.AppendLine(string.Join(';', columns.Select(Escape)));
            foreach (var row in rows)
            {
                text.AppendLine(string.Join(';', row.Select(Escape)));
            }
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
        }

        private static string Escape(string? value)
        {
            var text = value ?? string.Empty;
            // un valor que empieza por = + - @ se interpretaría como fórmula al abrir el archivo
            if (text.Length > 0 && "=+-@".Contains(text[0]))
            {
                text = "'" + text;
            }
            return text.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
        }
    }

    public class ReportingService : IAccessHistoryService, IReportService, IIncidentService, IPrivacyService
    {
        public const string AccessesByDay = "accesses-by-day";
        public const string VisitsByUnit = "visits-by-unit";
        public const string RejectionsAndExceptions = "rejections-exceptions";
        public const string ParkingOccupancy = "parking-occupancy";
        public const string UnauthorizedVehicles = "unauthorized-vehicles";
        public const string ExpiringLinks = "expiring-links";

        private static readonly IReadOnlyList<ReportDefinitionDto> All = new List<ReportDefinitionDto>
        {
            new() { Key = AccessesByDay, Title = "Accesos por periodo", Description = "Entradas, salidas, rechazos y accesos excepcionales por día.", UsesDates = true },
            new() { Key = VisitsByUnit, Title = "Visitas por unidad", Description = "Cuántas visitas recibió cada unidad y cuántas llegaron con vehículo.", UsesDates = true },
            new() { Key = RejectionsAndExceptions, Title = "Rechazos y accesos excepcionales", Description = "Cada rechazo y cada excepción con su justificación y quién lo registró.", UsesDates = true },
            new() { Key = ParkingOccupancy, Title = "Ocupación de parqueaderos por tipo", Description = "Capacidad, ocupados, reservados, disponibles y fuera de servicio en este momento.", UsesDates = false },
            new() { Key = UnauthorizedVehicles, Title = "Vehículos sin autorización vigente detectados", Description = "Vehículos rechazados o que entraron como acceso excepcional.", UsesDates = true },
            new() { Key = ExpiringLinks, Title = "Vinculaciones que vencen en 30 días", Description = "Ocupaciones y autorizaciones de vehículo con fecha de fin próxima.", UsesDates = false },
        };

        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeService _dateTime;
        private readonly IPolicyService _policies;
        private readonly ICatalogService _catalogs;
        private readonly IUnitScope _scope;
        private readonly IParkingService _parking;
        private readonly IAuditService _audit;
        private readonly int _maxRangeDays;

        public ReportingService(
            ApplicationDbContext db,
            ICurrentUser currentUser,
            IDateTimeService dateTime,
            IPolicyService policies,
            ICatalogService catalogs,
            IUnitScope scope,
            IParkingService parking,
            IAuditService audit,
            IConfiguration configuration)
        {
            _db = db;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _policies = policies;
            _catalogs = catalogs;
            _scope = scope;
            _parking = parking;
            _audit = audit;
            // un rango sin tope permitiría pedir años de eventos en una sola consulta
            _maxRangeDays = int.TryParse(configuration["AccessHistory:MaxRangeDays"], out var days) && days > 0 ? days : 92;
        }

        public IReadOnlyList<ReportDefinitionDto> Definitions => All;

        // ---------- Historial de accesos ----------

        public async Task<PaginatedResultDto<AccessEventDto>> SearchAsync(AccessHistoryFilterDto filter)
        {
            var (start, end) = Range(filter.StartDate, filter.EndDate);
            var query = _db.AccessEvents.AsNoTracking().Where(e => e.OccurredAt >= start && e.OccurredAt <= end);

            if (!_currentUser.HasPermission(Permissions.AccessRead))
            {
                // portal: solo sus unidades y solo desde que empezó su vínculo con cada una
                var links = await CurrentLinksAsync();
                if (links.Count == 0)
                {
                    return Empty(filter);
                }
                query = links
                    .Select(link => query.Where(e => e.UnitId == link.UnitId && e.OccurredAt >= link.Since))
                    .Aggregate((all, next) => all.Concat(next));
            }
            else if (!_currentUser.HasPermission(Permissions.ReportsRead))
            {
                // portería: solo los últimos días que el conjunto definió
                var days = await _policies.GetIntAsync(ComplexPolicies.AccessHistoryDays);
                var limit = _dateTime.UtcNow.AddDays(-days);
                query = query.Where(e => e.OccurredAt >= limit);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = $"%{filter.Search.Trim()}%";
                query = query.Where(e => EF.Functions.ILike(e.PersonName, pattern)
                    || (e.PersonDocument != null && EF.Functions.ILike(e.PersonDocument, pattern)));
            }
            if (filter.UnitId.HasValue)
            {
                query = query.Where(e => e.UnitId == filter.UnitId.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.Plate))
            {
                var plate = AccessService.NormalizePlate(filter.Plate);
                query = query.Where(e => e.VehiclePlate == plate);
            }
            if (filter.Type.HasValue)
            {
                query = query.Where(e => e.Type == filter.Type.Value);
            }
            if (filter.PersonKind.HasValue)
            {
                query = query.Where(e => e.PersonKind == filter.PersonKind.Value);
            }
            if (filter.OperatorId.HasValue)
            {
                query = query.Where(e => e.OperatorId == filter.OperatorId.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.AccessPoint))
            {
                query = query.Where(e => e.AccessPoint == filter.AccessPoint);
            }
            if (filter.OnlyExceptions)
            {
                query = query.Where(e => e.Verification == VerificationResult.Exception);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.OccurredAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(AccessService.EventToDto(_db))
                .ToListAsync();

            return new PaginatedResultDto<AccessEventDto>
            {
                Items = items, TotalCount = totalCount, PageNumber = filter.PageNumber, PageSize = filter.PageSize
            };
        }

        private static PaginatedResultDto<AccessEventDto> Empty(AccessHistoryFilterDto filter) => new()
        {
            Items = new List<AccessEventDto>(), TotalCount = 0, PageNumber = filter.PageNumber, PageSize = filter.PageSize
        };

        /// <summary>Unidades con las que el usuario tiene vínculo hoy, y desde cuándo.</summary>
        private async Task<List<(Guid UnitId, DateTime Since)>> CurrentLinksAsync()
        {
            var personId = await _scope.GetPersonIdAsync();
            if (personId == null) return new List<(Guid, DateTime)>();

            var today = _dateTime.Today;
            var owned = await _db.UnitOwnerships
                .Where(o => o.PersonId == personId && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                .Select(o => new { o.UnitId, o.StartDate }).ToListAsync();
            var occupied = await _db.Occupancies
                .Where(o => o.PersonId == personId && o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                .Select(o => new { o.UnitId, o.StartDate }).ToListAsync();

            return owned.Concat(occupied)
                .GroupBy(l => l.UnitId)
                .Select(g => (g.Key, _dateTime.ToUtc(g.Min(l => l.StartDate).ToDateTime(TimeOnly.MinValue))))
                .ToList();
        }

        // ---------- Reportes ----------

        public async Task<ReportDto> RunAsync(string key, ReportFilterDto filter)
        {
            var definition = All.FirstOrDefault(d => d.Key == key)
                ?? throw new NotFoundException("El reporte no existe.");
            var report = new ReportDto { Key = definition.Key, Title = definition.Title };

            switch (key)
            {
                case AccessesByDay:
                    await AccessesByDayAsync(report, filter);
                    break;
                case VisitsByUnit:
                    await VisitsByUnitAsync(report, filter);
                    break;
                case RejectionsAndExceptions:
                    await RejectionsAndExceptionsAsync(report, filter);
                    break;
                case ParkingOccupancy:
                    await ParkingOccupancyAsync(report);
                    break;
                case UnauthorizedVehicles:
                    await UnauthorizedVehiclesAsync(report, filter);
                    break;
                default:
                    await ExpiringLinksAsync(report);
                    break;
            }
            return report;
        }

        public async Task RecordExportAsync(string what, object filters)
        {
            _audit.Record("report.exported", entityType: "Report", entityId: what, details: filters);
            await _db.SaveChangesAsync();
        }

        /// <summary>Eventos del periodo que cuentan: sin las anulaciones ni lo que fue anulado.</summary>
        private IQueryable<AccessEvent> CountableEvents(DateTime start, DateTime end)
        {
            return _db.AccessEvents.AsNoTracking().Where(e => e.OccurredAt >= start && e.OccurredAt <= end
                && e.Type != AccessEventType.Void && !_db.AccessEvents.Any(v => v.VoidsEventId == e.Id));
        }

        private async Task AccessesByDayAsync(ReportDto report, ReportFilterDto filter)
        {
            var (start, end) = Range(filter.StartDate, filter.EndDate);
            var events = await CountableEvents(start, end)
                .Select(e => new { e.OccurredAt, e.Type, e.Verification })
                .ToListAsync();

            report.Columns = new List<string> { "Fecha", "Entradas", "Salidas", "Rechazos", "Accesos excepcionales" };
            // el día es el del conjunto, no el del servidor
            report.Rows = events
                .GroupBy(e => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(e.OccurredAt, _dateTime.TimeZone)))
                .OrderBy(g => g.Key)
                .Select(g => new List<string>
                {
                    g.Key.ToString("yyyy-MM-dd"),
                    g.Count(e => e.Type == AccessEventType.Entry).ToString(),
                    g.Count(e => e.Type == AccessEventType.Exit).ToString(),
                    g.Count(e => e.Type == AccessEventType.Rejection).ToString(),
                    g.Count(e => e.Verification == VerificationResult.Exception).ToString()
                })
                .ToList();
        }

        private async Task VisitsByUnitAsync(ReportDto report, ReportFilterDto filter)
        {
            var (start, end) = Range(filter.StartDate, filter.EndDate);
            var rows = await _db.Visits.AsNoTracking()
                .Where(v => v.VoidEventId == null && v.EntryEvent.OccurredAt >= start && v.EntryEvent.OccurredAt <= end)
                .GroupBy(v => v.Unit.Label)
                .Select(g => new { Unit = g.Key, Visits = g.Count(), WithVehicle = g.Count(v => v.VehicleId != null), Inside = g.Count(v => v.ExitEventId == null) })
                .OrderByDescending(r => r.Visits).ThenBy(r => r.Unit)
                .ToListAsync();

            report.Columns = new List<string> { "Unidad", "Visitas", "Con vehículo", "Siguen dentro" };
            report.Rows = rows.Select(r => new List<string> { r.Unit, r.Visits.ToString(), r.WithVehicle.ToString(), r.Inside.ToString() }).ToList();
        }

        private async Task RejectionsAndExceptionsAsync(ReportDto report, ReportFilterDto filter)
        {
            var (start, end) = Range(filter.StartDate, filter.EndDate);
            var rows = await CountableEvents(start, end)
                .Where(e => e.Type == AccessEventType.Rejection || e.Verification == VerificationResult.Exception)
                .OrderByDescending(e => e.OccurredAt)
                .Select(e => new
                {
                    e.OccurredAt, e.Type, e.PersonName,
                    Unit = e.Unit == null ? null : e.Unit.Label,
                    e.VehiclePlate, e.Reason,
                    Operator = e.Operator.FirstName + " " + e.Operator.LastName
                })
                .ToListAsync();

            report.Columns = new List<string> { "Fecha y hora", "Tipo", "Persona", "Unidad", "Placa", "Justificación", "Registró" };
            report.Rows = rows.Select(r => new List<string>
            {
                Local(r.OccurredAt), r.Type == AccessEventType.Rejection ? "Rechazo" : "Acceso excepcional",
                r.PersonName, r.Unit ?? "", r.VehiclePlate ?? "", r.Reason ?? "", r.Operator
            }).ToList();
        }

        private async Task ParkingOccupancyAsync(ReportDto report)
        {
            // la misma consulta de la pantalla de disponibilidad: no hay otra lógica en paralelo
            var availability = await _parking.GetAvailabilityAsync(null, null);
            var names = new Dictionary<ParkingSpaceType, string>
            {
                [ParkingSpaceType.Private] = "Privados", [ParkingSpaceType.Community] = "Comunitarios", [ParkingSpaceType.Visitor] = "Visitantes"
            };

            report.Columns = new List<string> { "Tipo", "Capacidad", "Ocupados", "Reservados", "Disponibles", "Fuera de servicio" };
            report.Rows = availability.ByType.Append(availability.Total).Select(r => new List<string>
            {
                r.Type.HasValue ? names[r.Type.Value] : "Total",
                r.Capacity.ToString(), r.Occupied.ToString(), r.Reserved.ToString(), r.Available.ToString(), r.OutOfService.ToString()
            }).ToList();
        }

        private async Task UnauthorizedVehiclesAsync(ReportDto report, ReportFilterDto filter)
        {
            var (start, end) = Range(filter.StartDate, filter.EndDate);
            var rows = await CountableEvents(start, end)
                .Where(e => e.VehiclePlate != null
                    && (e.Type == AccessEventType.Rejection
                        || (e.Type == AccessEventType.Entry && e.Verification == VerificationResult.Exception && e.PersonKind != PersonKind.Visitor)))
                .OrderByDescending(e => e.OccurredAt)
                .Select(e => new
                {
                    e.OccurredAt, e.VehiclePlate, e.Type, e.PersonName,
                    Unit = e.Unit == null ? null : e.Unit.Label, e.Reason
                })
                .ToListAsync();

            report.Columns = new List<string> { "Fecha y hora", "Placa", "Resultado", "Persona", "Unidad", "Motivo" };
            report.Rows = rows.Select(r => new List<string>
            {
                Local(r.OccurredAt), r.VehiclePlate!, r.Type == AccessEventType.Rejection ? "Rechazado" : "Ingresó como excepción",
                r.PersonName, r.Unit ?? "", r.Reason ?? ""
            }).ToList();
        }

        private async Task ExpiringLinksAsync(ReportDto report)
        {
            var today = _dateTime.Today;
            var limit = today.AddDays(30);
            var occupancies = await _db.Occupancies.AsNoTracking()
                .Where(o => o.Status == OccupancyStatus.Active && o.EndDate != null && o.EndDate >= today && o.EndDate <= limit)
                .Select(o => new { Kind = "Ocupación", Unit = o.Unit.Label, Who = o.Person.FirstName + " " + o.Person.LastName, End = o.EndDate!.Value })
                .ToListAsync();
            var vehicles = await _db.VehicleAuthorizations.AsNoTracking()
                .Where(a => a.Status == VehicleAuthorizationStatus.Active && a.EndDate != null && a.EndDate >= today && a.EndDate <= limit)
                .Select(a => new { Kind = "Vehículo", Unit = a.Unit.Label, Who = a.Vehicle.Plate, End = a.EndDate!.Value })
                .ToListAsync();

            report.Columns = new List<string> { "Vinculación", "Unidad", "Persona o placa", "Vence", "Días restantes" };
            report.Rows = occupancies.Concat(vehicles).OrderBy(r => r.End)
                .Select(r => new List<string> { r.Kind, r.Unit, r.Who, r.End.ToString("yyyy-MM-dd"), (r.End.DayNumber - today.DayNumber).ToString() })
                .ToList();
        }

        // ---------- Novedades ----------

        public async Task<PaginatedResultDto<IncidentDto>> SearchAsync(IncidentFilterDto filter)
        {
            var query = _db.Incidents.AsNoTracking();
            if (filter.StartDate.HasValue)
            {
                var start = _dateTime.ToUtc(filter.StartDate.Value);
                query = query.Where(i => i.OccurredAt >= start);
            }
            if (filter.EndDate.HasValue)
            {
                var end = _dateTime.ToUtc(filter.EndDate.Value);
                query = query.Where(i => i.OccurredAt <= end);
            }
            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                query = query.Where(i => i.Category == filter.Category);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(i => i.OccurredAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(ToDto)
                .ToListAsync();
            await LabelAsync(items);

            return new PaginatedResultDto<IncidentDto>
            {
                Items = items, TotalCount = totalCount, PageNumber = filter.PageNumber, PageSize = filter.PageSize
            };
        }

        public async Task<IncidentDto> CreateAsync(CreateIncidentDto dto)
        {
            await _catalogs.EnsureValidAsync(Catalogs.IncidentCategory, dto.Category, "Categoría");
            if (dto.UnitId.HasValue && !await _db.Units.AnyAsync(u => u.Id == dto.UnitId.Value))
            {
                throw new BusinessRuleException("La unidad indicada no existe.");
            }
            if (dto.PersonId.HasValue && !await _db.Persons.AnyAsync(p => p.Id == dto.PersonId.Value))
            {
                throw new BusinessRuleException("La persona indicada no existe.");
            }

            var incident = new Incident
            {
                OccurredAt = _dateTime.UtcNow, Category = dto.Category, Text = dto.Text.Trim(),
                UnitId = dto.UnitId, PersonId = dto.PersonId, VehiclePlate = AccessService.NormalizePlate(dto.VehiclePlate),
                AuthorId = _currentUser.UserId ?? throw new ForbiddenException("Sesión no válida.")
            };
            _db.Incidents.Add(incident);
            await _db.SaveChangesAsync();

            var created = await _db.Incidents.Where(i => i.Id == incident.Id).Select(ToDto).ToListAsync();
            await LabelAsync(created);
            return created.Single();
        }

        private async Task LabelAsync(IEnumerable<IncidentDto> incidents)
        {
            foreach (var incident in incidents)
            {
                incident.CategoryLabel = await _catalogs.LabelAsync(Catalogs.IncidentCategory, incident.Category);
            }
        }

        private static readonly System.Linq.Expressions.Expression<Func<Incident, IncidentDto>> ToDto = i => new IncidentDto
        {
            Id = i.Id,
            OccurredAt = i.OccurredAt,
            Category = i.Category,
            CategoryLabel = i.Category,
            Text = i.Text,
            UnitLabel = i.Unit == null ? null : i.Unit.Label,
            PersonName = i.Person == null ? null : i.Person.FirstName + " " + i.Person.LastName,
            VehiclePlate = i.VehiclePlate,
            AuthorName = i.Author.FirstName + " " + i.Author.LastName
        };

        // ---------- Retención de datos de visitantes ----------

        public async Task<int> AnonymizeExpiredVisitorsAsync()
        {
            var now = _dateTime.UtcNow;
            // cada conjunto decide su plazo; 0 o sin configurar = no se anonimiza
            var retention = await _db.ComplexPolicies.IgnoreQueryFilters()
                .Where(p => p.Key == ComplexPolicies.VisitorRetentionDays)
                .Select(p => new { p.ComplexId, p.Value })
                .ToListAsync();

            var total = 0;
            foreach (var policy in retention)
            {
                if (!int.TryParse(policy.Value, out var days) || days <= 0) continue;
                var limit = now.AddDays(-days);

                // solo quien nunca fue más que visitante: sin cuenta, sin unidad, sin vehículo autorizado
                var people = await _db.Persons.IgnoreQueryFilters()
                    .Where(p => p.ComplexId == policy.ComplexId && p.DocumentType != "ANON"
                        && !_db.Users.IgnoreQueryFilters().Any(u => u.PersonId == p.Id)
                        && !_db.UnitOwnerships.IgnoreQueryFilters().Any(o => o.PersonId == p.Id)
                        && !_db.Occupancies.IgnoreQueryFilters().Any(o => o.PersonId == p.Id)
                        && !_db.VehicleAuthorizations.IgnoreQueryFilters().Any(a => a.ResponsiblePersonId == p.Id)
                        && !_db.VisitAuthorizations.IgnoreQueryFilters().Any(a => a.AuthorizedByPersonId == p.Id || (a.VisitorPersonId == p.Id && a.EndDate >= DateOnly.FromDateTime(now)))
                        && _db.AccessEvents.IgnoreQueryFilters().Any(e => e.PersonId == p.Id)
                        && !_db.AccessEvents.IgnoreQueryFilters().Any(e => e.PersonId == p.Id && e.OccurredAt >= limit))
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach (var id in people)
                {
                    // los eventos de acceso se conservan; la persona deja de ser identificable
                    total += await _db.Persons.IgnoreQueryFilters().Where(p => p.Id == id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.FirstName, "Visitante")
                            .SetProperty(p => p.LastName, "anonimizado")
                            .SetProperty(p => p.DocumentType, "ANON")
                            .SetProperty(p => p.DocumentNumber, "ANON-" + id.ToString("N").Substring(0, 12))
                            .SetProperty(p => p.Phone, (string?)null)
                            .SetProperty(p => p.Email, (string?)null));
                }
            }
            return total;
        }

        // ---------- Internos ----------

        private (DateTime Start, DateTime End) Range(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                throw new BusinessRuleException("Indique la fecha inicial y la fecha final.");
            }

            var start = _dateTime.ToUtc(startDate.Value);
            var end = _dateTime.ToUtc(endDate.Value);
            if (end < start)
            {
                throw new BusinessRuleException("La fecha final no puede ser anterior a la inicial.");
            }
            if ((end - start).TotalDays > _maxRangeDays)
            {
                throw new BusinessRuleException($"El rango de fechas no puede superar {_maxRangeDays} días.");
            }
            return (start, end);
        }

        private string Local(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(utc, _dateTime.TimeZone).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }
}
