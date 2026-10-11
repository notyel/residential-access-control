using AccessControl.API.Infrastructure;
using AccessControl.Application.Reports;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Report;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.API.Controllers
{
    /// <summary>Historial de todos los eventos de acceso: entradas, salidas, rechazos, excepciones y anulaciones.</summary>
    [ApiController]
    [Route("api/access-history")]
    [HasPermission(Permissions.AccessRead, Permissions.AccessReadOwn)]
    public class AccessHistoryController : ControllerBase
    {
        private const int ExportLimit = 5000;

        private readonly IAccessHistoryService _history;
        private readonly IReportService _reports;
        private readonly IPersonalDataPolicy _personalData;

        public AccessHistoryController(IAccessHistoryService history, IReportService reports, IPersonalDataPolicy personalData)
        {
            _history = history;
            _reports = reports;
            _personalData = personalData;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] AccessHistoryFilterDto filter)
        {
            var result = await _history.SearchAsync(filter);
            _personalData.Apply(result.Items);
            return Ok(new ResponseModel<PaginatedResultDto<AccessEventDto>>(true, "Historial consultado.", result));
        }

        // Exporta lo mismo que se ve, con los mismos filtros y el mismo enmascarado de quien exporta
        [HttpGet("export")]
        [HasPermission(Permissions.ReportsExport)]
        public async Task<IActionResult> Export([FromQuery] AccessHistoryFilterDto filter)
        {
            filter.PageNumber = 1;
            filter.PageSize = 200;
            var rows = new List<AccessEventDto>();
            while (rows.Count < ExportLimit)
            {
                var page = await _history.SearchAsync(filter);
                rows.AddRange(page.Items ?? []);
                if (rows.Count >= page.TotalCount || page.Items == null || !page.Items.Any()) break;
                filter.PageNumber++;
            }
            _personalData.Apply(rows);
            await _reports.RecordExportAsync("access-history", new
            {
                filter.StartDate, filter.EndDate, filter.Search, filter.UnitId, filter.Plate, filter.Type, filter.PersonKind,
                filter.OperatorId, filter.AccessPoint, filter.OnlyExceptions, rows = rows.Count
            });

            var csv = Csv.Build(
                new[] { "Fecha y hora (UTC)", "Tipo", "Persona", "Documento", "Clase", "Unidad", "Placa", "Verificación", "Motivo", "Registró", "Anulado" },
                rows.Select(e => new[]
                {
                    e.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss"), TypeName(e.Type), e.PersonName, e.PersonDocument, KindName(e.PersonKind),
                    e.UnitLabel, e.VehiclePlate, e.Verification == VerificationResult.Exception ? "Excepcional" : "", e.Reason,
                    e.OperatorName, e.IsVoided ? "Sí" : ""
                }));
            return File(csv, "text/csv", "historial-de-accesos.csv");
        }

        private static string TypeName(AccessEventType type) => type switch
        {
            AccessEventType.Entry => "Entrada",
            AccessEventType.Exit => "Salida",
            AccessEventType.Rejection => "Rechazo",
            _ => "Anulación"
        };

        private static string KindName(PersonKind kind) => kind switch
        {
            PersonKind.Visitor => "Visitante",
            PersonKind.Resident => "Residente",
            _ => "Propietario"
        };
    }

    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.ReportsRead)]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reports;

        public ReportsController(IReportService reports)
        {
            _reports = reports;
        }

        [HttpGet]
        public IActionResult List()
        {
            return Ok(new ResponseModel<IReadOnlyList<ReportDefinitionDto>>(true, "Reportes disponibles.", _reports.Definitions));
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> Run(string key, [FromQuery] ReportFilterDto filter)
        {
            return Ok(new ResponseModel<ReportDto>(true, "Reporte generado.", await _reports.RunAsync(key, filter)));
        }

        // La exportación queda en la bitácora con los filtros usados
        [HttpGet("{key}/export")]
        [HasPermission(Permissions.ReportsExport)]
        public async Task<IActionResult> Export(string key, [FromQuery] ReportFilterDto filter)
        {
            var report = await _reports.RunAsync(key, filter);
            await _reports.RecordExportAsync(key, new { filter.StartDate, filter.EndDate, rows = report.Rows.Count });
            return File(Csv.Build(report.Columns, report.Rows), "text/csv", $"{key}.csv");
        }
    }

    /// <summary>Novedades del turno: solo se agregan y se consultan.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.IncidentsWrite, Permissions.AccessRead)]
    public class IncidentsController : ControllerBase
    {
        private readonly IIncidentService _incidents;

        public IncidentsController(IIncidentService incidents)
        {
            _incidents = incidents;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] IncidentFilterDto filter)
        {
            return Ok(new ResponseModel<PaginatedResultDto<IncidentDto>>(true, "Novedades consultadas.", await _incidents.SearchAsync(filter)));
        }

        [HttpPost]
        [HasPermission(Permissions.IncidentsWrite)]
        public async Task<IActionResult> Create([FromBody] CreateIncidentDto dto)
        {
            return Ok(new ResponseModel<IncidentDto>(true, "Novedad registrada.", await _incidents.CreateAsync(dto)));
        }
    }

    /// <summary>Estado del servicio para monitoreo. Público a propósito: no revela datos.</summary>
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public HealthController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get()
        {
            bool database;
            try
            {
                database = await _db.Database.CanConnectAsync();
            }
            catch
            {
                database = false;
            }

            var pending = database ? (await _db.Database.GetPendingMigrationsAsync()).Count() : 0;
            var body = new { status = database && pending == 0 ? "ok" : "degraded", api = true, database, pendingMigrations = pending, time = DateTime.UtcNow };
            return database ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
        }
    }
}
