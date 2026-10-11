using AccessControl.Application.Access;
using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Application.Services;
using AccessControl.Application.Units;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Vehicles;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Vehicles
{
    public interface IVehicleService
    {
        Task<PaginatedResultDto<VehicleDto>> SearchAsync(VehicleFilterDto filter);
        Task<VehicleDetailDto> GetAsync(Guid id);

        /// <summary>Vehículos con autorización en curso en esas unidades (ficha de unidad, portal).</summary>
        Task<List<VehicleDto>> GetForUnitsAsync(IReadOnlyCollection<Guid> unitIds);

        /// <summary>Registra un vehículo permanente con su autorización. Desde el portal aplica la política del conjunto.</summary>
        Task<VehicleDto> CreateAsync(CreateVehicleDto dto, bool viaPortal);
        Task<VehicleDto> UpdateAsync(Guid id, VehicleDataDto dto);

        /// <summary>Pasa el vehículo a otra unidad o responsable: cierra la autorización anterior y abre otra.</summary>
        Task<VehicleDto> ReassignAsync(Guid id, ReassignVehicleDto dto);

        Task<VehicleDto> SuspendAsync(Guid id, string? reason);
        Task<VehicleDto> ReactivateAsync(Guid id);
        Task<VehicleDto> RevokeAsync(Guid id, string? reason, IReadOnlyCollection<Guid>? unitScope = null);
        Task<VehicleDto> AddNoteAsync(Guid id, string text);

        /// <summary>Solicitudes hechas desde el portal que esperan decisión.</summary>
        Task<List<VehicleDto>> GetPendingAsync();
        Task<VehicleDto> DecideAsync(Guid id, bool approve, string? reason);

        /// <summary>no, approval o direct: lo que la política permite a propietarios y residentes.</summary>
        Task<string> GetPortalPolicyAsync();

        // ---- portería ----

        Task<PlateCheckDto> CheckPlateAsync(string? plate);
        Task<List<VehicleInsideDto>> GetInsideAsync();
        Task<bool> IsInsideAsync(Guid vehicleId);

        /// <summary>Si el vehículo puede entrar hoy como vehículo de esa unidad.</summary>
        Task<bool> IsAuthorizedAsync(Guid vehicleId, Guid unitId);

        /// <summary>
        /// Vehículo con el que llega una visita: se reutiliza si la placa ya existe; si no, se crea
        /// como vehículo visitante. No guarda: lo hace quien registra el ingreso, en la misma operación.
        /// </summary>
        Task<Vehicle> ResolveForVisitAsync(string plate, string? type, string? brand, string? color);
    }

    public class VehicleService : IVehicleService, IOccupancyEndHandler
    {
        private const string DefaultType = "automovil";
        // tipos que pueden no tener placa
        private static readonly string[] PlateOptionalTypes = { "bicicleta", "otro" };

        private static readonly VehicleAuthorizationStatus[] Open =
        {
            VehicleAuthorizationStatus.Pending, VehicleAuthorizationStatus.Active, VehicleAuthorizationStatus.Suspended
        };

        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeService _dateTime;
        private readonly IPolicyService _policies;
        private readonly ICatalogService _catalogs;
        private readonly IUnitScope _scope;

        public VehicleService(
            ApplicationDbContext db,
            ICurrentUser currentUser,
            IDateTimeService dateTime,
            IPolicyService policies,
            ICatalogService catalogs,
            IUnitScope scope)
        {
            _db = db;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _policies = policies;
            _catalogs = catalogs;
            _scope = scope;
        }

        public Task<string> GetPortalPolicyAsync() => _policies.GetAsync(ComplexPolicies.ResidentRegistersVehicles);

        // ---------- Consultas ----------

        public async Task<PaginatedResultDto<VehicleDto>> SearchAsync(VehicleFilterDto filter)
        {
            var query = _db.Vehicles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var text = filter.Search.Trim();
                var plate = AccessService.NormalizePlate(text) ?? text;
                var pattern = $"%{text}%";
                query = query.Where(v => v.Plate.Contains(plate)
                    || (v.Brand != null && EF.Functions.ILike(v.Brand, pattern))
                    || v.Authorizations.Any(a => Open.Contains(a.Status)
                        && EF.Functions.ILike(a.Responsible.FirstName + " " + a.Responsible.LastName, pattern)));
            }
            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                query = query.Where(v => v.Type == filter.Type);
            }
            if (filter.Class.HasValue)
            {
                query = query.Where(v => v.Class == filter.Class.Value);
            }
            if (filter.UnitId.HasValue)
            {
                query = query.Where(v => v.Authorizations.Any(a => a.UnitId == filter.UnitId.Value && Open.Contains(a.Status)));
            }
            if (filter.Status.HasValue)
            {
                query = Open.Contains(filter.Status.Value)
                    ? query.Where(v => v.Authorizations.Any(a => a.Status == filter.Status.Value))
                    // revocado, finalizado o rechazado: que no tenga otra en curso
                    : query.Where(v => v.Authorizations.Any(a => a.Status == filter.Status.Value)
                        && !v.Authorizations.Any(a => Open.Contains(a.Status)));
            }

            var totalCount = await query.CountAsync();
            var vehicles = await query
                .OrderBy(v => v.Plate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<VehicleDto>
            {
                Items = await ToDtosAsync(vehicles),
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<VehicleDetailDto> GetAsync(Guid id)
        {
            var vehicle = await FindAsync(id);
            var history = await _db.VehicleAuthorizations
                .Where(a => a.VehicleId == id)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new VehicleAuthorizationDto
                {
                    Id = a.Id,
                    UnitId = a.UnitId,
                    UnitLabel = a.Unit.Label,
                    ResponsiblePersonId = a.ResponsiblePersonId,
                    ResponsibleName = a.Responsible.FirstName + " " + a.Responsible.LastName,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    Status = a.Status,
                    StatusReason = a.StatusReason
                })
                .ToListAsync();
            var notes = await _db.VehicleNotes
                .Where(n => n.VehicleId == id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new VehicleNoteDto
                {
                    CreatedAt = n.CreatedAt,
                    AuthorName = n.Author == null ? null : n.Author.FirstName + " " + n.Author.LastName,
                    Text = n.Text
                })
                .ToListAsync();

            return new VehicleDetailDto
            {
                Vehicle = (await ToDtosAsync(new[] { vehicle })).Single(),
                History = history,
                Notes = notes,
                IsInside = await IsInsideAsync(id)
            };
        }

        public async Task<List<VehicleDto>> GetForUnitsAsync(IReadOnlyCollection<Guid> unitIds)
        {
            if (unitIds.Count == 0) return new List<VehicleDto>();

            var vehicles = await _db.Vehicles
                .Where(v => v.Authorizations.Any(a => unitIds.Contains(a.UnitId) && Open.Contains(a.Status)))
                .OrderBy(v => v.Plate)
                .ToListAsync();
            return await ToDtosAsync(vehicles);
        }

        public async Task<List<VehicleDto>> GetPendingAsync()
        {
            var vehicles = await _db.Vehicles
                .Where(v => v.Authorizations.Any(a => a.Status == VehicleAuthorizationStatus.Pending))
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();
            return await ToDtosAsync(vehicles);
        }

        // ---------- Altas y cambios ----------

        public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto, bool viaPortal)
        {
            var status = VehicleAuthorizationStatus.Active;
            var responsibleId = dto.ResponsiblePersonId;

            if (viaPortal)
            {
                var policy = await GetPortalPolicyAsync();
                if (policy == ComplexPolicies.No)
                {
                    throw new ForbiddenException("En este conjunto los vehículos los registra la administración.");
                }
                if (!(await _scope.GetAccessibleUnitIdsAsync()).Contains(dto.UnitId!.Value))
                {
                    // una unidad ajena responde igual que una inexistente
                    throw new NotFoundException("La unidad no existe.");
                }
                responsibleId = await _scope.GetPersonIdAsync();
                status = policy == ComplexPolicies.WithApproval ? VehicleAuthorizationStatus.Pending : VehicleAuthorizationStatus.Active;
            }
            if (!responsibleId.HasValue)
            {
                throw new BusinessRuleException("Indique la persona de la unidad que responde por el vehículo.");
            }

            var type = dto.Type.Trim();
            await _catalogs.EnsureValidAsync(Catalogs.VehicleType, type, "Tipo de vehículo");
            var plate = AccessService.NormalizePlate(dto.Plate);
            if (plate == null && !PlateOptionalTypes.Contains(type))
            {
                throw new BusinessRuleException("La placa es obligatoria para este tipo de vehículo.");
            }

            var vehicle = plate == null ? null : await _db.Vehicles.FirstOrDefaultAsync(v => v.Plate == plate);
            if (vehicle != null && await _db.VehicleAuthorizations.AnyAsync(a => a.VehicleId == vehicle.Id && Open.Contains(a.Status)))
            {
                throw new ConflictException("Ya existe un vehículo registrado con esa placa.");
            }

            if (vehicle == null)
            {
                vehicle = new Vehicle
                {
                    // sin placa: identificador interno, para poder buscarlo y registrarle movimientos
                    Plate = plate ?? "SP" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                    HasPlate = plate != null
                };
                _db.Vehicles.Add(vehicle);
            }
            // si la placa ya había entrado con una visita, o tuvo una autorización que terminó, se reutiliza
            vehicle.Class = VehicleClass.Permanent;
            Apply(vehicle, dto, type);

            _db.VehicleAuthorizations.Add(await NewAuthorizationAsync(vehicle, dto.UnitId!.Value, responsibleId.Value, dto.StartDate, dto.EndDate, status));
            await SaveAsync();

            return await DtoAsync(vehicle.Id);
        }

        public async Task<VehicleDto> UpdateAsync(Guid id, VehicleDataDto dto)
        {
            var vehicle = await FindAsync(id);
            var type = dto.Type.Trim();
            await _catalogs.EnsureValidAsync(Catalogs.VehicleType, type, "Tipo de vehículo");

            var plate = AccessService.NormalizePlate(dto.Plate);
            if (plate != null && plate != vehicle.Plate)
            {
                if (await _db.Vehicles.AnyAsync(v => v.Plate == plate && v.Id != id))
                {
                    throw new ConflictException("Ya existe un vehículo registrado con esa placa.");
                }
                Note(vehicle.Id, $"Placa corregida: {vehicle.Plate} → {plate}");
                vehicle.Plate = plate;
                vehicle.HasPlate = true;
            }
            else if (plate == null && vehicle.HasPlate && !PlateOptionalTypes.Contains(type))
            {
                throw new BusinessRuleException("La placa es obligatoria para este tipo de vehículo.");
            }

            if (vehicle.Color != Blank.ToNull(dto.Color) && vehicle.Color != null)
            {
                Note(vehicle.Id, $"Color: {vehicle.Color} → {Blank.ToNull(dto.Color) ?? "sin dato"}");
            }
            Apply(vehicle, dto, type);
            await SaveAsync();

            return await DtoAsync(id);
        }

        public async Task<VehicleDto> ReassignAsync(Guid id, ReassignVehicleDto dto)
        {
            var vehicle = await FindAsync(id);
            var current = await OpenAuthorizationAsync(id);
            var today = _dateTime.Today;

            // en una transacción: nunca queda el vehículo con dos autorizaciones ni sin ninguna
            await using var transaction = await _db.Database.BeginTransactionAsync();
            if (current != null)
            {
                current.Status = VehicleAuthorizationStatus.Ended;
                current.EndDate = today;
                current.StatusReason = "Reasignado a otra unidad o responsable";
                await _db.SaveChangesAsync();
            }

            vehicle.Class = VehicleClass.Permanent;
            _db.VehicleAuthorizations.Add(await NewAuthorizationAsync(
                vehicle, dto.UnitId!.Value, dto.ResponsiblePersonId!.Value, dto.StartDate, dto.EndDate, VehicleAuthorizationStatus.Active));
            await SaveAsync();
            await transaction.CommitAsync();

            return await DtoAsync(id);
        }

        public async Task<VehicleDto> SuspendAsync(Guid id, string? reason)
        {
            var authorization = await RequireOpenAsync(id);
            if (authorization.Status != VehicleAuthorizationStatus.Active)
            {
                throw new ConflictException("Solo se suspende una autorización vigente.");
            }
            var text = RequireReason(reason, "la suspensión");

            authorization.Status = VehicleAuthorizationStatus.Suspended;
            authorization.StatusReason = text;
            Note(id, $"Suspendido: {text}");
            await _db.SaveChangesAsync();
            return await DtoAsync(id);
        }

        public async Task<VehicleDto> ReactivateAsync(Guid id)
        {
            var authorization = await RequireOpenAsync(id);
            if (authorization.Status != VehicleAuthorizationStatus.Suspended)
            {
                throw new ConflictException("La autorización no está suspendida.");
            }

            authorization.Status = VehicleAuthorizationStatus.Active;
            authorization.StatusReason = null;
            Note(id, "Suspensión levantada");
            await _db.SaveChangesAsync();
            return await DtoAsync(id);
        }

        public async Task<VehicleDto> RevokeAsync(Guid id, string? reason, IReadOnlyCollection<Guid>? unitScope = null)
        {
            var authorization = await OpenAuthorizationAsync(id);
            if (authorization == null || (unitScope != null && !unitScope.Contains(authorization.UnitId)))
            {
                // desde el portal, un vehículo ajeno responde igual que uno inexistente
                throw unitScope != null
                    ? new NotFoundException("El vehículo no existe.")
                    : new ConflictException("El vehículo no tiene una autorización en curso.");
            }
            var text = unitScope != null ? Blank.ToNull(reason) ?? "Retirado por el residente" : RequireReason(reason, "la revocación");

            Revoke(authorization, text);
            await _db.SaveChangesAsync();
            return await DtoAsync(id);
        }

        public async Task<VehicleDto> AddNoteAsync(Guid id, string text)
        {
            await FindAsync(id);
            Note(id, text.Trim());
            await _db.SaveChangesAsync();
            return await DtoAsync(id);
        }

        public async Task<VehicleDto> DecideAsync(Guid id, bool approve, string? reason)
        {
            var authorization = await RequireOpenAsync(id);
            if (authorization.Status != VehicleAuthorizationStatus.Pending)
            {
                throw new ConflictException("La solicitud ya fue resuelta.");
            }

            if (approve)
            {
                await EnsureUnitLimitAsync(authorization.UnitId, authorization.Id);
                authorization.Status = VehicleAuthorizationStatus.Active;
            }
            else
            {
                authorization.Status = VehicleAuthorizationStatus.Rejected;
                authorization.StatusReason = RequireReason(reason, "el rechazo");
                authorization.EndDate = _dateTime.Today;
            }
            await _db.SaveChangesAsync();
            return await DtoAsync(id);
        }

        /// <summary>Quien deja de estar vinculado a la unidad pierde la autorización de sus vehículos en ella.</summary>
        public async Task OnOccupancyEndedAsync(Occupancy occupancy, DateOnly endDate)
        {
            var stillOwner = await _db.UnitOwnerships.IgnoreQueryFilters().AnyAsync(o =>
                o.UnitId == occupancy.UnitId && o.PersonId == occupancy.PersonId && (o.EndDate == null || o.EndDate > endDate));
            var otherOccupancy = await _db.Occupancies.IgnoreQueryFilters().AnyAsync(o =>
                o.Id != occupancy.Id && o.UnitId == occupancy.UnitId && o.PersonId == occupancy.PersonId
                && o.Status == OccupancyStatus.Active && (o.EndDate == null || o.EndDate > endDate));
            if (stillOwner || otherOccupancy) return;

            var authorizations = await _db.VehicleAuthorizations.IgnoreQueryFilters()
                .Where(a => a.UnitId == occupancy.UnitId && a.ResponsiblePersonId == occupancy.PersonId && Open.Contains(a.Status))
                .ToListAsync();
            foreach (var authorization in authorizations)
            {
                Revoke(authorization, "Vinculación del responsable finalizada", occupancy.ComplexId);
            }
        }

        // ---------- Portería ----------

        public async Task<PlateCheckDto> CheckPlateAsync(string? plate)
        {
            var normalized = AccessService.NormalizePlate(plate)
                ?? throw new BusinessRuleException("Escriba la placa.");
            var result = new PlateCheckDto { Plate = normalized };

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Plate == normalized);
            if (vehicle == null)
            {
                result.Result = PlateCheckResult.Unknown;
                result.Message = "Vehículo no registrado.";
                return result;
            }

            result.Vehicle = (await ToDtosAsync(new[] { vehicle })).Single();
            result.IsInside = await IsInsideAsync(vehicle.Id);

            if (result.Vehicle.AuthorizationId.HasValue && vehicle.Class == VehicleClass.Permanent)
            {
                var dto = result.Vehicle;
                result.Result = dto.IsAuthorizedToday ? PlateCheckResult.Authorized : PlateCheckResult.NotValid;
                result.Message = dto.IsAuthorizedToday
                    ? $"Autorizado · {dto.UnitLabel}"
                    : dto.Status switch
                    {
                        VehicleAuthorizationStatus.Suspended => "Autorización suspendida.",
                        VehicleAuthorizationStatus.Pending => "Solicitud pendiente de aprobación.",
                        VehicleAuthorizationStatus.Active => "Autorización vencida o aún no vigente.",
                        VehicleAuthorizationStatus.Rejected => "La solicitud fue rechazada.",
                        _ => "Autorización revocada o finalizada."
                    };

                var today = _dateTime.Today;
                var unitId = dto.UnitId!.Value;
                var owners = await _db.UnitOwnerships
                    .Where(o => o.UnitId == unitId && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                    .Select(o => new DriverDto { PersonId = o.PersonId, Name = o.Person.FirstName + " " + o.Person.LastName })
                    .ToListAsync();
                var occupants = await _db.Occupancies
                    .Where(o => o.UnitId == unitId && o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                    .Select(o => new DriverDto { PersonId = o.PersonId, Name = o.Person.FirstName + " " + o.Person.LastName })
                    .ToListAsync();
                result.Drivers = owners.Concat(occupants)
                    .GroupBy(d => d.PersonId).Select(g => g.First())
                    // el responsable primero: es quien suele conducirlo
                    .OrderByDescending(d => d.PersonId == dto.ResponsiblePersonId).ThenBy(d => d.Name)
                    .ToList();
                return result;
            }

            var visit = await _db.Visits
                .Where(v => v.VehicleId == vehicle.Id && v.ExitEventId == null && v.VoidEventId == null)
                .Select(v => new { v.Id, Name = v.Person.FirstName + " " + v.Person.LastName, v.Unit.Label })
                .FirstOrDefaultAsync();
            if (visit != null)
            {
                result.Result = PlateCheckResult.VisitInProgress;
                result.Message = $"Vehículo de una visita en curso · {visit.Label}";
                result.VisitId = visit.Id;
                result.VisitorName = visit.Name;
                result.VisitUnitLabel = visit.Label;
                return result;
            }

            result.Result = PlateCheckResult.Unknown;
            result.Message = result.IsInside
                ? "Vehículo de visitante que quedó dentro sin una visita en curso."
                : "Sin autorización: solo ha ingresado antes como vehículo de visitante.";
            return result;
        }

        public async Task<List<VehicleInsideDto>> GetInsideAsync()
        {
            return await OpenEntries(_db)
                .OrderBy(e => e.OccurredAt)
                .Select(e => new VehicleInsideDto
                {
                    VehicleId = e.VehicleId!.Value,
                    Plate = e.VehiclePlate!,
                    Type = _db.Vehicles.Where(v => v.Id == e.VehicleId).Select(v => v.Type).First(),
                    Class = _db.Vehicles.Where(v => v.Id == e.VehicleId).Select(v => v.Class).First(),
                    UnitLabel = e.Unit == null ? null : e.Unit.Label,
                    PersonName = e.PersonName,
                    Since = e.OccurredAt,
                    VisitId = e.VisitId,
                    VisitorLeft = e.VisitId != null && _db.Visits.Any(v => v.Id == e.VisitId && v.ExitEventId != null)
                })
                .ToListAsync();
        }

        public Task<bool> IsInsideAsync(Guid vehicleId) => OpenEntries(_db).AnyAsync(e => e.VehicleId == vehicleId);

        public async Task<bool> IsAuthorizedAsync(Guid vehicleId, Guid unitId)
        {
            var today = _dateTime.Today;
            return await _db.VehicleAuthorizations.AnyAsync(a => a.VehicleId == vehicleId && a.UnitId == unitId
                && a.Status == VehicleAuthorizationStatus.Active && a.StartDate <= today && (a.EndDate == null || a.EndDate >= today));
        }

        public async Task<Vehicle> ResolveForVisitAsync(string plate, string? type, string? brand, string? color)
        {
            var existing = await _db.Vehicles.FirstOrDefaultAsync(v => v.Plate == plate);
            if (existing != null)
            {
                return existing;
            }

            var code = string.IsNullOrWhiteSpace(type) ? DefaultType : type.Trim();
            await _catalogs.EnsureValidAsync(Catalogs.VehicleType, code, "Tipo de vehículo");
            var vehicle = new Vehicle
            {
                Plate = plate, Type = code, Class = VehicleClass.Visitor,
                Brand = Blank.ToNull(brand), Color = Blank.ToNull(color)
            };
            _db.Vehicles.Add(vehicle);
            return vehicle;
        }

        /// <summary>
        /// Entradas de vehículo que siguen abiertas: sin anular y sin una salida posterior. "Dentro" se
        /// calcula siempre de los eventos; no hay ningún campo de estado que mantener.
        /// </summary>
        internal static IQueryable<AccessEvent> OpenEntries(ApplicationDbContext db)
        {
            return db.AccessEvents.Where(e => e.VehicleId != null && e.Type == AccessEventType.Entry
                && !db.AccessEvents.Any(v => v.VoidsEventId == e.Id)
                && !db.AccessEvents.Any(l => l.VehicleId == e.VehicleId && l.OccurredAt > e.OccurredAt
                    && (l.Type == AccessEventType.Exit || l.Type == AccessEventType.Entry)
                    && !db.AccessEvents.Any(v => v.VoidsEventId == l.Id)));
        }

        // ---------- Internos ----------

        private async Task<Vehicle> FindAsync(Guid id)
        {
            return await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id)
                ?? throw new NotFoundException("El vehículo no existe.");
        }

        private Task<VehicleAuthorization?> OpenAuthorizationAsync(Guid vehicleId)
        {
            return _db.VehicleAuthorizations.FirstOrDefaultAsync(a => a.VehicleId == vehicleId && Open.Contains(a.Status));
        }

        private async Task<VehicleAuthorization> RequireOpenAsync(Guid vehicleId)
        {
            await FindAsync(vehicleId);
            return await OpenAuthorizationAsync(vehicleId)
                ?? throw new ConflictException("El vehículo no tiene una autorización en curso.");
        }

        private async Task<VehicleAuthorization> NewAuthorizationAsync(
            Vehicle vehicle, Guid unitId, Guid responsibleId, DateOnly? start, DateOnly? end, VehicleAuthorizationStatus status)
        {
            var today = _dateTime.Today;
            if (!await _db.Units.AnyAsync(u => u.Id == unitId && u.IsActive))
            {
                throw new BusinessRuleException("La unidad indicada no existe o está inactiva.");
            }

            var linked = await _db.UnitOwnerships.AnyAsync(o => o.UnitId == unitId && o.PersonId == responsibleId
                             && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                         || await _db.Occupancies.AnyAsync(o => o.UnitId == unitId && o.PersonId == responsibleId
                             && o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today));
            if (!linked)
            {
                throw new BusinessRuleException("El responsable debe ser propietario u ocupante vigente de la unidad.");
            }

            var startDate = start ?? today;
            if (end.HasValue && end.Value < startDate)
            {
                throw new BusinessRuleException("La fecha de fin no puede ser anterior a la de inicio.");
            }
            await EnsureUnitLimitAsync(unitId, null);

            return new VehicleAuthorization
            {
                Vehicle = vehicle, UnitId = unitId, ResponsiblePersonId = responsibleId,
                StartDate = startDate, EndDate = end, Status = status
            };
        }

        private async Task EnsureUnitLimitAsync(Guid unitId, Guid? exceptAuthorizationId)
        {
            var max = await _policies.GetIntAsync(ComplexPolicies.MaxVehiclesPerUnit);
            if (max <= 0) return;

            var today = _dateTime.Today;
            // las solicitudes pendientes no ocupan cupo hasta que se aprueban
            var count = await _db.VehicleAuthorizations.CountAsync(a => a.UnitId == unitId && a.Id != exceptAuthorizationId
                && (a.Status == VehicleAuthorizationStatus.Active || a.Status == VehicleAuthorizationStatus.Suspended)
                && (a.EndDate == null || a.EndDate >= today));
            if (count >= max)
            {
                throw new BusinessRuleException($"La unidad ya tiene el máximo de vehículos permitido ({max}).");
            }
        }

        private void Revoke(VehicleAuthorization authorization, string reason, Guid? complexId = null)
        {
            authorization.Status = VehicleAuthorizationStatus.Revoked;
            authorization.StatusReason = reason;
            authorization.EndDate = _dateTime.Today;
            Note(authorization.VehicleId, $"Autorización revocada: {reason}", complexId);
        }

        private void Note(Guid vehicleId, string text, Guid? complexId = null)
        {
            var note = new VehicleNote { VehicleId = vehicleId, CreatedAt = _dateTime.UtcNow, AuthorId = _currentUser.UserId, Text = text };
            if (complexId.HasValue)
            {
                note.ComplexId = complexId.Value;
            }
            _db.VehicleNotes.Add(note);
        }

        private static void Apply(Vehicle vehicle, VehicleDataDto dto, string type)
        {
            vehicle.Type = type;
            vehicle.Brand = Blank.ToNull(dto.Brand);
            vehicle.Color = Blank.ToNull(dto.Color);
            vehicle.Model = Blank.ToNull(dto.Model);
            vehicle.Notes = Blank.ToNull(dto.Notes);
        }

        private static string RequireReason(string? reason, string action)
        {
            var text = (reason ?? string.Empty).Trim();
            if (text.Length < 5)
            {
                throw new BusinessRuleException($"Indique el motivo de {action}.");
            }
            return text;
        }

        private async Task SaveAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // dos altas simultáneas de la misma placa, o dos autorizaciones en curso del mismo vehículo
                throw new ConflictException("Ya existe un vehículo registrado con esa placa.");
            }
        }

        private async Task<VehicleDto> DtoAsync(Guid id)
        {
            return (await ToDtosAsync(new[] { await FindAsync(id) })).Single();
        }

        private async Task<List<VehicleDto>> ToDtosAsync(IReadOnlyCollection<Vehicle> vehicles)
        {
            var ids = vehicles.Select(v => v.Id).ToList();
            var today = _dateTime.Today;
            var authorizations = await _db.VehicleAuthorizations
                .Where(a => ids.Contains(a.VehicleId))
                .Select(a => new
                {
                    a.Id, a.VehicleId, a.UnitId, UnitLabel = a.Unit.Label, a.ResponsiblePersonId,
                    Responsible = a.Responsible.FirstName + " " + a.Responsible.LastName,
                    a.StartDate, a.EndDate, a.Status, a.StatusReason, a.CreatedAt
                })
                .ToListAsync();

            return vehicles.Select(v =>
            {
                // la que está en curso; si no hay, la más reciente, para saber qué pasó con él
                var current = authorizations.Where(a => a.VehicleId == v.Id)
                    .OrderByDescending(a => Open.Contains(a.Status)).ThenByDescending(a => a.CreatedAt)
                    .FirstOrDefault();
                return new VehicleDto
                {
                    Id = v.Id, Plate = v.Plate, HasPlate = v.HasPlate, Type = v.Type, Brand = v.Brand, Color = v.Color,
                    Model = v.Model, Notes = v.Notes, Class = v.Class,
                    AuthorizationId = current?.Id, UnitId = current?.UnitId, UnitLabel = current?.UnitLabel,
                    ResponsiblePersonId = current?.ResponsiblePersonId, ResponsibleName = current?.Responsible,
                    StartDate = current?.StartDate, EndDate = current?.EndDate, Status = current?.Status, StatusReason = current?.StatusReason,
                    IsAuthorizedToday = current != null && current.Status == VehicleAuthorizationStatus.Active
                        && current.StartDate <= today && (current.EndDate == null || current.EndDate >= today)
                };
            }).ToList();
        }
    }
}
