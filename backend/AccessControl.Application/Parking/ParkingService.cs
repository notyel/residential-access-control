using AccessControl.Application.Access;
using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Application.Services;
using AccessControl.Common.DTOs.Parking;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Parking
{
    /// <summary>Reglas de parqueaderos que no dependen de la base: se prueban solas.</summary>
    public static class ParkingRules
    {
        /// <summary>Precedencia del estado: fuera de servicio → ocupado → reservado → disponible.</summary>
        public static ParkingState ComputeState(bool outOfService, bool occupied, bool planned)
        {
            if (outOfService) return ParkingState.OutOfService;
            if (occupied) return ParkingState.Occupied;
            return planned ? ParkingState.Reserved : ParkingState.Available;
        }

        /// <summary>Si un espacio admite ese tipo de vehículo (código del catálogo vehicle-type).</summary>
        public static bool Accepts(ParkingVehicleKind kind, string vehicleType) => kind switch
        {
            ParkingVehicleKind.Any => true,
            ParkingVehicleKind.Car => vehicleType == "automovil",
            ParkingVehicleKind.Motorcycle => vehicleType == "motocicleta",
            _ => false
        };
    }

    public interface IParkingService
    {
        /// <summary>Espacios con su estado calculado. unitScope limita a los privados de esas unidades (portal).</summary>
        Task<List<ParkingSpaceDto>> GetSpacesAsync(ParkingFilterDto filter, IReadOnlyCollection<Guid>? unitScope = null);
        Task<AvailabilityDto> GetAvailabilityAsync(DateTime? from, DateTime? to);

        /// <summary>Espacios de visitantes libres ahora que admiten ese tipo de vehículo: lo que portería puede ofrecer.</summary>
        Task<List<ParkingSpaceDto>> GetFreeVisitorSpacesAsync(string? vehicleType);
        Task<ParkingSpaceDetailDto> GetSpaceAsync(Guid id);

        Task<ParkingSpaceDto> CreateAsync(SaveParkingSpaceDto dto);
        Task<BulkParkingResultDto> CreateBulkAsync(BulkParkingSpacesDto dto);
        Task<ParkingSpaceDto> UpdateAsync(Guid id, SaveParkingSpaceDto dto);
        Task<ParkingSpaceDto> SetActiveAsync(Guid id, bool active);

        /// <summary>Asocia un privado a su unidad titular; si ya tenía, cierra la anterior y conserva el historial.</summary>
        Task<ParkingSpaceDto> SetHolderAsync(Guid id, SetHolderDto dto);
        Task<ParkingSpaceDto> EndHolderAsync(Guid id);

        Task<ParkingSpaceDto> AddOutOfServiceAsync(Guid id, OutOfServiceDto dto);
        Task<ParkingSpaceDto> EndOutOfServiceAsync(Guid outOfServiceId);

        /// <summary>Asignación temporal o reserva de un comunitario.</summary>
        Task<ParkingSpaceDto> PlanAsync(PlanParkingDto dto, ParkingAssignmentClass kind);
        Task<ParkingSpaceDto> CancelAssignmentAsync(Guid assignmentId);

        /// <summary>Ocupa un espacio con un vehículo que ya está dentro (registro manual).</summary>
        Task<ParkingSpaceDto> OccupyAsync(Guid id, OccupyParkingDto dto);
        Task<ParkingSpaceDto> ReleaseAsync(Guid assignmentId);
        Task<ParkingSpaceDto> AddNoteAsync(Guid id, string text);

        // ---- usados por el registro de accesos, dentro de su misma operación (no guardan) ----

        Task AttachOccupancyAsync(Guid spaceId, Vehicle vehicle, Guid? unitId, Guid? visitId, bool isException, string? exceptionReason, DateTime now);

        /// <summary>El vehículo sale: su espacio queda libre sin que nadie tenga que desocuparlo a mano.</summary>
        Task ReleaseVehicleAsync(Guid vehicleId, DateTime now, bool cancelled = false);

        /// <summary>Cancela en todos los conjuntos las reservas que no se usaron dentro del margen. Devuelve cuántas.</summary>
        Task<int> CancelUnusedReservationsAsync();
    }

    public class ParkingService : IParkingService
    {
        public const string OverlapMessage = "El espacio ya está asignado u ocupado en ese periodo, o el vehículo ya ocupa otro espacio.";

        private static readonly ParkingAssignmentClass[] Plans = { ParkingAssignmentClass.Temporary, ParkingAssignmentClass.Reservation };

        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeService _dateTime;
        private readonly IPolicyService _policies;
        private readonly int _reservationGraceMinutes;

        public ParkingService(
            ApplicationDbContext db,
            ICurrentUser currentUser,
            IDateTimeService dateTime,
            IPolicyService policies,
            IConfiguration configuration)
        {
            _db = db;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _policies = policies;
            // margen tras el inicio de una reserva para que el vehículo llegue; pasado, se libera sola
            _reservationGraceMinutes = int.TryParse(configuration["Parking:ReservationGraceMinutes"], out var minutes) && minutes > 0 ? minutes : 60;
        }

        // ---------- Consultas ----------

        public async Task<List<ParkingSpaceDto>> GetSpacesAsync(ParkingFilterDto filter, IReadOnlyCollection<Guid>? unitScope = null)
        {
            var query = _db.ParkingSpaces.AsQueryable();
            if (!filter.IncludeInactive)
            {
                query = query.Where(s => s.IsActive);
            }
            if (filter.Type.HasValue)
            {
                query = query.Where(s => s.Type == filter.Type.Value);
            }
            if (filter.VehicleKind.HasValue)
            {
                // quien busca para un carro también puede usar los que admiten cualquiera
                query = query.Where(s => s.VehicleKind == filter.VehicleKind.Value || s.VehicleKind == ParkingVehicleKind.Any);
            }
            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                var location = $"%{filter.Location.Trim()}%";
                query = query.Where(s => s.Location != null && EF.Functions.ILike(s.Location, location));
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = $"%{filter.Search.Trim()}%";
                query = query.Where(s => EF.Functions.ILike(s.Code, pattern));
            }
            if (unitScope != null)
            {
                var now = _dateTime.UtcNow;
                query = query.Where(s => _db.ParkingAssignments.Any(a => a.SpaceId == s.Id && a.Class == ParkingAssignmentClass.Holder
                    && a.Status == ParkingAssignmentStatus.Active && a.UnitId != null && unitScope.Contains(a.UnitId.Value)
                    && a.StartsAt <= now && (a.EndsAt == null || a.EndsAt > now)));
            }

            var (from, to) = Window(filter.From, filter.To);
            var spaces = await BuildAsync(query, from, to);
            return filter.State.HasValue ? spaces.Where(s => s.State == filter.State.Value).ToList() : spaces;
        }

        public async Task<AvailabilityDto> GetAvailabilityAsync(DateTime? from, DateTime? to)
        {
            var (start, end) = Window(from, to);
            var spaces = await BuildAsync(_db.ParkingSpaces.Where(s => s.IsActive), start, end);

            static AvailabilityRowDto Row(ParkingSpaceType? type, IReadOnlyCollection<ParkingSpaceDto> items) => new()
            {
                Type = type,
                Capacity = items.Count,
                Occupied = items.Count(s => s.State == ParkingState.Occupied),
                Reserved = items.Count(s => s.State == ParkingState.Reserved),
                Available = items.Count(s => s.State == ParkingState.Available),
                OutOfService = items.Count(s => s.State == ParkingState.OutOfService)
            };

            return new AvailabilityDto
            {
                ByType = Enum.GetValues<ParkingSpaceType>().Select(type => Row(type, spaces.Where(s => s.Type == type).ToList())).ToList(),
                Total = Row(null, spaces),
                At = start
            };
        }

        public async Task<List<ParkingSpaceDto>> GetFreeVisitorSpacesAsync(string? vehicleType)
        {
            // solo espacios de visitantes: un privado o un comunitario nunca se ofrecen aquí
            var spaces = await BuildAsync(_db.ParkingSpaces.Where(s => s.IsActive && s.Type == ParkingSpaceType.Visitor), _dateTime.UtcNow, null);
            return spaces
                .Where(s => s.State == ParkingState.Available
                    && (string.IsNullOrWhiteSpace(vehicleType) || ParkingRules.Accepts(s.VehicleKind, vehicleType)))
                .ToList();
        }

        public async Task<ParkingSpaceDetailDto> GetSpaceAsync(Guid id)
        {
            await FindAsync(id);
            var history = await _db.ParkingAssignments
                .Where(a => a.SpaceId == id)
                .OrderByDescending(a => a.StartsAt)
                .Take(100)
                .Select(a => new ParkingAssignmentDto
                {
                    Id = a.Id,
                    Class = a.Class,
                    UnitLabel = a.Unit == null ? null : a.Unit.Label,
                    PersonName = a.Person == null ? null : a.Person.FirstName + " " + a.Person.LastName,
                    Plate = a.Vehicle == null ? null : a.Vehicle.Plate,
                    VisitId = a.VisitId,
                    StartsAt = a.StartsAt,
                    EndsAt = a.EndsAt,
                    Status = a.Status,
                    Notes = a.Notes,
                    RegisteredBy = _db.Users.Where(u => u.Id == a.CreatedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    IsException = a.IsException,
                    ExceptionReason = a.ExceptionReason
                })
                .ToListAsync();

            return new ParkingSpaceDetailDto
            {
                Space = await DtoAsync(id),
                History = history,
                OutOfService = await _db.ParkingOutOfService
                    .Where(o => o.SpaceId == id)
                    .OrderByDescending(o => o.From)
                    .Select(o => new ParkingOutOfServiceDto { Id = o.Id, From = o.From, To = o.To, Reason = o.Reason })
                    .ToListAsync(),
                Notes = await _db.ParkingNotes
                    .Where(n => n.SpaceId == id)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new ParkingNoteDto
                    {
                        CreatedAt = n.CreatedAt,
                        AuthorName = n.Author == null ? null : n.Author.FirstName + " " + n.Author.LastName,
                        Text = n.Text
                    })
                    .ToListAsync()
            };
        }

        // ---------- Espacios ----------

        public async Task<ParkingSpaceDto> CreateAsync(SaveParkingSpaceDto dto)
        {
            var space = new ParkingSpace
            {
                Code = NormalizeCode(dto.Code), Location = Blank.ToNull(dto.Location), Type = dto.Type,
                VehicleKind = dto.VehicleKind, Notes = Blank.ToNull(dto.Notes)
            };
            _db.ParkingSpaces.Add(space);
            await SaveAsync();
            return await DtoAsync(space.Id);
        }

        public async Task<BulkParkingResultDto> CreateBulkAsync(BulkParkingSpacesDto dto)
        {
            if (dto.To < dto.From)
            {
                throw new BusinessRuleException("El rango no es válido: el final es menor que el inicio.");
            }
            if (dto.To - dto.From >= 500)
            {
                throw new BusinessRuleException("Se pueden crear como máximo 500 espacios a la vez.");
            }

            var prefix = (dto.Prefix ?? string.Empty).Trim().ToUpperInvariant();
            var width = Math.Max(2, dto.To.ToString().Length);
            var codes = Enumerable.Range(dto.From, dto.To - dto.From + 1).Select(n => prefix + n.ToString().PadLeft(width, '0')).ToList();
            var existing = await _db.ParkingSpaces.Where(s => codes.Contains(s.Code)).Select(s => s.Code).ToListAsync();

            var result = new BulkParkingResultDto { Skipped = existing };
            foreach (var code in codes.Except(existing))
            {
                _db.ParkingSpaces.Add(new ParkingSpace
                {
                    Code = code, Location = Blank.ToNull(dto.Location), Type = dto.Type, VehicleKind = dto.VehicleKind
                });
                result.Created.Add(code);
            }
            await SaveAsync();
            return result;
        }

        public async Task<ParkingSpaceDto> UpdateAsync(Guid id, SaveParkingSpaceDto dto)
        {
            var space = await FindAsync(id);
            if (space.Type != dto.Type && await HasLiveAssignmentsAsync(id))
            {
                throw new ConflictException("No se puede cambiar el tipo: el espacio tiene asignaciones u ocupaciones vigentes.");
            }

            space.Code = NormalizeCode(dto.Code);
            space.Location = Blank.ToNull(dto.Location);
            space.Type = dto.Type;
            space.VehicleKind = dto.VehicleKind;
            space.Notes = Blank.ToNull(dto.Notes);
            await SaveAsync();
            return await DtoAsync(id);
        }

        public async Task<ParkingSpaceDto> SetActiveAsync(Guid id, bool active)
        {
            var space = await FindAsync(id);
            if (!active && await HasLiveAssignmentsAsync(id))
            {
                throw new ConflictException("No se puede dar de baja: el espacio tiene asignaciones u ocupaciones vigentes.");
            }

            // no se borra: conserva su historial
            space.IsActive = active;
            await _db.SaveChangesAsync();
            return await DtoAsync(id);
        }

        // ---------- Titular de un privado ----------

        public async Task<ParkingSpaceDto> SetHolderAsync(Guid id, SetHolderDto dto)
        {
            var space = await FindAsync(id);
            if (space.Type != ParkingSpaceType.Private)
            {
                throw new BusinessRuleException("Solo los espacios privados se asocian a una unidad.");
            }
            if (!await _db.Units.AnyAsync(u => u.Id == dto.UnitId))
            {
                throw new BusinessRuleException("La unidad indicada no existe.");
            }
            if (dto.PersonId.HasValue && !await _db.Persons.AnyAsync(p => p.Id == dto.PersonId.Value))
            {
                throw new BusinessRuleException("La persona indicada no existe.");
            }

            var now = _dateTime.UtcNow;
            // en una transacción: el espacio nunca queda con dos titulares ni pierde el historial
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var current = await CurrentHolderAsync(id, now);
            if (current != null)
            {
                current.Status = ParkingAssignmentStatus.Released;
                current.EndsAt = now;
                await _db.SaveChangesAsync();
            }

            _db.ParkingAssignments.Add(new ParkingAssignment
            {
                SpaceId = id, Class = ParkingAssignmentClass.Holder, UnitId = dto.UnitId, PersonId = dto.PersonId,
                StartsAt = now, Status = ParkingAssignmentStatus.Active
            });
            await SaveAsync();
            await transaction.CommitAsync();
            return await DtoAsync(id);
        }

        public async Task<ParkingSpaceDto> EndHolderAsync(Guid id)
        {
            await FindAsync(id);
            var now = _dateTime.UtcNow;
            var current = await CurrentHolderAsync(id, now)
                ?? throw new ConflictException("El espacio no está asociado a ninguna unidad.");
            current.Status = ParkingAssignmentStatus.Released;
            current.EndsAt = now;
            await _db.SaveChangesAsync();
            return await DtoAsync(id);
        }

        // ---------- Fuera de servicio ----------

        public async Task<ParkingSpaceDto> AddOutOfServiceAsync(Guid id, OutOfServiceDto dto)
        {
            await FindAsync(id);
            var now = _dateTime.UtcNow;
            var from = dto.From.HasValue ? _dateTime.ToUtc(dto.From.Value) : now;
            DateTime? to = dto.To.HasValue ? _dateTime.ToUtc(dto.To.Value) : null;
            if (to.HasValue && to.Value <= from)
            {
                throw new BusinessRuleException("El fin debe ser posterior al inicio.");
            }
            if (await _db.ParkingAssignments.AnyAsync(a => a.SpaceId == id && a.Class == ParkingAssignmentClass.Occupancy
                    && a.Status == ParkingAssignmentStatus.Active && a.StartsAt <= from && from <= now))
            {
                throw new ConflictException("El espacio está ocupado: registre primero la salida del vehículo.");
            }

            _db.ParkingOutOfService.Add(new ParkingOutOfService
            {
                SpaceId = id, From = from, To = to, Reason = dto.Reason.Trim(), CreatedById = _currentUser.UserId
            });
            await _db.SaveChangesAsync();
            return await DtoAsync(id);
        }

        public async Task<ParkingSpaceDto> EndOutOfServiceAsync(Guid outOfServiceId)
        {
            var period = await _db.ParkingOutOfService.FirstOrDefaultAsync(o => o.Id == outOfServiceId)
                ?? throw new NotFoundException("El periodo fuera de servicio no existe.");
            var now = _dateTime.UtcNow;
            if (period.To.HasValue && period.To.Value <= now)
            {
                throw new ConflictException("Ese periodo ya había terminado.");
            }

            // al terminar, el espacio vuelve solo a su estado calculado
            period.To = period.From > now ? period.From : now;
            await _db.SaveChangesAsync();
            return await DtoAsync(period.SpaceId);
        }

        // ---------- Asignaciones temporales y reservas ----------

        public async Task<ParkingSpaceDto> PlanAsync(PlanParkingDto dto, ParkingAssignmentClass kind)
        {
            var space = await FindAsync(dto.SpaceId!.Value);
            if (!space.IsActive)
            {
                throw new BusinessRuleException("El espacio está dado de baja.");
            }
            if (space.Type != ParkingSpaceType.Community)
            {
                throw new BusinessRuleException("Solo los parqueaderos comunitarios admiten asignaciones temporales y reservas.");
            }
            if (kind == ParkingAssignmentClass.Reservation && !await _policies.GetBoolAsync(ComplexPolicies.CommunityParkingReservations))
            {
                throw new ForbiddenException("Este conjunto no tiene habilitadas las reservas de parqueadero comunitario.");
            }

            var now = _dateTime.UtcNow;
            var start = _dateTime.ToUtc(dto.StartsAt!.Value);
            var end = _dateTime.ToUtc(dto.EndsAt!.Value);
            if (end <= start)
            {
                throw new BusinessRuleException("El fin debe ser posterior al inicio.");
            }
            if (end <= now)
            {
                throw new BusinessRuleException("El periodo ya terminó.");
            }
            if (kind == ParkingAssignmentClass.Reservation && start < now.AddMinutes(-5))
            {
                throw new BusinessRuleException("Una reserva es para una franja futura.");
            }
            if (!dto.UnitId.HasValue && (kind == ParkingAssignmentClass.Reservation || !dto.VehicleId.HasValue))
            {
                throw new BusinessRuleException(kind == ParkingAssignmentClass.Reservation
                    ? "Indique la unidad para la que se reserva."
                    : "Indique la unidad o el vehículo al que se asigna.");
            }
            if (dto.UnitId.HasValue && !await _db.Units.AnyAsync(u => u.Id == dto.UnitId.Value && u.IsActive))
            {
                throw new BusinessRuleException("La unidad indicada no existe o está inactiva.");
            }
            if (dto.VehicleId.HasValue)
            {
                var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId.Value)
                    ?? throw new BusinessRuleException("El vehículo indicado no existe.");
                EnsureKind(space, vehicle);
            }
            if (await _db.ParkingOutOfService.AnyAsync(o => o.SpaceId == space.Id && o.From < end && (o.To == null || o.To > start)))
            {
                throw new ConflictException("El espacio está fuera de servicio en ese periodo.");
            }
            if (start <= now && await _db.ParkingAssignments.AnyAsync(a => a.SpaceId == space.Id
                    && a.Class == ParkingAssignmentClass.Occupancy && a.Status == ParkingAssignmentStatus.Active))
            {
                throw new ConflictException("El espacio está ocupado en este momento.");
            }

            _db.ParkingAssignments.Add(new ParkingAssignment
            {
                SpaceId = space.Id, Class = kind, UnitId = dto.UnitId, VehicleId = dto.VehicleId,
                StartsAt = start, EndsAt = end, Status = ParkingAssignmentStatus.Active, Notes = Blank.ToNull(dto.Notes)
            });
            await SaveAsync();
            return await DtoAsync(space.Id);
        }

        public async Task<ParkingSpaceDto> CancelAssignmentAsync(Guid assignmentId)
        {
            var assignment = await _db.ParkingAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId && Plans.Contains(a.Class))
                ?? throw new NotFoundException("La asignación no existe.");
            if (assignment.Status != ParkingAssignmentStatus.Active)
            {
                throw new ConflictException("La asignación ya no estaba vigente.");
            }

            assignment.Status = ParkingAssignmentStatus.Cancelled;
            await _db.SaveChangesAsync();
            return await DtoAsync(assignment.SpaceId);
        }

        // ---------- Ocupación ----------

        public async Task<ParkingSpaceDto> OccupyAsync(Guid id, OccupyParkingDto dto)
        {
            var plate = AccessService.NormalizePlate(dto.Plate);
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => dto.VehicleId.HasValue ? v.Id == dto.VehicleId.Value : v.Plate == plate)
                ?? throw new BusinessRuleException("El vehículo no está registrado.");

            var visit = await _db.Visits
                .Where(v => v.VehicleId == vehicle.Id && v.ExitEventId == null && v.VoidEventId == null)
                .Select(v => new { v.Id, v.UnitId })
                .FirstOrDefaultAsync();
            await AttachOccupancyAsync(id, vehicle, visit?.UnitId, visit?.Id, dto.IsException, dto.ExceptionReason, _dateTime.UtcNow);
            await SaveAsync();
            return await DtoAsync(id);
        }

        public async Task<ParkingSpaceDto> ReleaseAsync(Guid assignmentId)
        {
            var occupancy = await _db.ParkingAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId && a.Class == ParkingAssignmentClass.Occupancy)
                ?? throw new NotFoundException("La ocupación no existe.");
            if (occupancy.Status != ParkingAssignmentStatus.Active)
            {
                throw new ConflictException("El espacio ya había sido liberado.");
            }

            occupancy.Status = ParkingAssignmentStatus.Released;
            occupancy.EndsAt = _dateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await DtoAsync(occupancy.SpaceId);
        }

        public async Task AttachOccupancyAsync(Guid spaceId, Vehicle vehicle, Guid? unitId, Guid? visitId, bool isException, string? exceptionReason, DateTime now)
        {
            var space = await _db.ParkingSpaces.FirstOrDefaultAsync(s => s.Id == spaceId)
                ?? throw new BusinessRuleException("El parqueadero indicado no existe.");
            if (!space.IsActive)
            {
                throw new BusinessRuleException("El parqueadero está dado de baja.");
            }
            if (await _db.ParkingOutOfService.AnyAsync(o => o.SpaceId == spaceId && o.From <= now && (o.To == null || o.To > now)))
            {
                throw new ConflictException($"El parqueadero {space.Code} está fuera de servicio.");
            }
            EnsureKind(space, vehicle);

            var today = _dateTime.Today;
            // unidades en las que el vehículo tiene autorización vigente
            var vehicleUnits = await _db.VehicleAuthorizations
                .Where(a => a.VehicleId == vehicle.Id && a.Status == VehicleAuthorizationStatus.Active
                    && a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
                .Select(a => a.UnitId)
                .ToListAsync();

            string? violation = null;
            if (space.Type == ParkingSpaceType.Private)
            {
                var holder = await CurrentHolderAsync(spaceId, now);
                if (holder?.UnitId == null || !vehicleUnits.Contains(holder.UnitId.Value))
                {
                    violation = $"El parqueadero {space.Code} es privado y el vehículo no pertenece a su unidad titular.";
                }
            }

            var graceLimit = now.AddMinutes(-_reservationGraceMinutes);
            var plan = await _db.ParkingAssignments.FirstOrDefaultAsync(a => a.SpaceId == spaceId && Plans.Contains(a.Class)
                && a.Status == ParkingAssignmentStatus.Active && a.StartsAt <= now && (a.EndsAt == null || a.EndsAt > now)
                && (a.Class != ParkingAssignmentClass.Reservation || a.StartsAt >= graceLimit));
            if (plan != null)
            {
                var forThisVehicle = plan.VehicleId == vehicle.Id
                    || (plan.UnitId.HasValue && (plan.UnitId == unitId || vehicleUnits.Contains(plan.UnitId.Value)));
                if (!forThisVehicle)
                {
                    violation ??= $"El parqueadero {space.Code} está apartado para otra unidad o vehículo.";
                }
                else if (plan.Class == ParkingAssignmentClass.Reservation)
                {
                    // la reserva se convierte en ocupación: queda como usada
                    plan.Status = ParkingAssignmentStatus.Released;
                    plan.EndsAt = now;
                }
            }

            string? reason = null;
            if (violation != null)
            {
                if (!isException)
                {
                    throw new ConflictException(violation + " Solo puede usarse como acceso excepcional, con motivo.");
                }
                if (!_currentUser.HasPermission(Permissions.AccessException))
                {
                    throw new ForbiddenException("No tiene permiso para registrar accesos excepcionales.");
                }
                reason = (exceptionReason ?? string.Empty).Trim();
                if (reason.Length < AccessService.MinExceptionReasonLength)
                {
                    throw new BusinessRuleException($"Un uso excepcional exige un motivo de al menos {AccessService.MinExceptionReasonLength} caracteres.");
                }
            }

            _db.ParkingAssignments.Add(new ParkingAssignment
            {
                SpaceId = spaceId, Class = ParkingAssignmentClass.Occupancy, Vehicle = vehicle, UnitId = unitId ?? vehicleUnits.Cast<Guid?>().FirstOrDefault(),
                VisitId = visitId, StartsAt = now, Status = ParkingAssignmentStatus.Active,
                IsException = violation != null, ExceptionReason = reason
            });
        }

        public async Task ReleaseVehicleAsync(Guid vehicleId, DateTime now, bool cancelled = false)
        {
            var open = await _db.ParkingAssignments
                .Where(a => a.VehicleId == vehicleId && a.Class == ParkingAssignmentClass.Occupancy && a.Status == ParkingAssignmentStatus.Active)
                .ToListAsync();
            foreach (var occupancy in open)
            {
                occupancy.Status = cancelled ? ParkingAssignmentStatus.Cancelled : ParkingAssignmentStatus.Released;
                occupancy.EndsAt = now > occupancy.StartsAt ? now : occupancy.StartsAt.AddSeconds(1);
            }
        }

        public async Task<ParkingSpaceDto> AddNoteAsync(Guid id, string text)
        {
            await FindAsync(id);
            _db.ParkingNotes.Add(new ParkingNote { SpaceId = id, CreatedAt = _dateTime.UtcNow, AuthorId = _currentUser.UserId, Text = text.Trim() });
            await _db.SaveChangesAsync();
            return await DtoAsync(id);
        }

        public async Task<int> CancelUnusedReservationsAsync()
        {
            var limit = _dateTime.UtcNow.AddMinutes(-_reservationGraceMinutes);
            // una reserva usada ya quedó como "liberada" al ingresar el vehículo; las que siguen vigentes no se usaron
            return await _db.ParkingAssignments.IgnoreQueryFilters()
                .Where(a => a.Class == ParkingAssignmentClass.Reservation && a.Status == ParkingAssignmentStatus.Active && a.StartsAt < limit)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, ParkingAssignmentStatus.Cancelled)
                    .SetProperty(a => a.Notes, "No se usó dentro del margen: liberada automáticamente"));
        }

        // ---------- Internos ----------

        /// <summary>Sin periodo: el instante actual. Con periodo: todo lo que se cruce con él.</summary>
        private (DateTime From, DateTime? To) Window(DateTime? from, DateTime? to)
        {
            if (!from.HasValue) return (_dateTime.UtcNow, null);

            var start = _dateTime.ToUtc(from.Value);
            DateTime? end = to.HasValue ? _dateTime.ToUtc(to.Value) : null;
            if (end.HasValue && end.Value <= start)
            {
                throw new BusinessRuleException("El fin del periodo debe ser posterior al inicio.");
            }
            return (start, end);
        }

        private async Task<List<ParkingSpaceDto>> BuildAsync(IQueryable<ParkingSpace> query, DateTime from, DateTime? to)
        {
            var spaces = await query.OrderBy(s => s.Type).ThenBy(s => s.Code).ToListAsync();
            if (spaces.Count == 0) return new List<ParkingSpaceDto>();

            var ids = spaces.Select(s => s.Id).ToList();
            var now = _dateTime.UtcNow;
            var upper = to ?? from;
            var pointInTime = to == null;

            var assignments = await _db.ParkingAssignments
                .Where(a => ids.Contains(a.SpaceId) && a.Status == ParkingAssignmentStatus.Active
                    && a.StartsAt <= upper && (a.EndsAt == null || a.EndsAt > from))
                .Select(a => new
                {
                    a.Id, a.SpaceId, a.Class, a.UnitId,
                    UnitLabel = a.Unit == null ? null : a.Unit.Label,
                    PersonName = a.Person == null ? null : a.Person.FirstName + " " + a.Person.LastName,
                    a.VehicleId,
                    Plate = a.Vehicle == null ? null : a.Vehicle.Plate,
                    a.VisitId, a.StartsAt, a.EndsAt, a.IsException
                })
                .ToListAsync();
            var outOfService = await _db.ParkingOutOfService
                .Where(o => ids.Contains(o.SpaceId) && o.From <= upper && (o.To == null || o.To > from))
                .ToListAsync();

            var maxVisitorHours = await _policies.GetIntAsync(ComplexPolicies.VisitorParkingMaxHours);
            var graceLimit = now.AddMinutes(-_reservationGraceMinutes);

            return spaces.Select(space =>
            {
                var own = assignments.Where(a => a.SpaceId == space.Id).ToList();
                var holder = own.FirstOrDefault(a => a.Class == ParkingAssignmentClass.Holder);
                var occupancy = own.FirstOrDefault(a => a.Class == ParkingAssignmentClass.Occupancy);
                // una reserva que nadie usó dentro del margen deja de apartar el espacio, aunque el proceso aún no la haya cancelado
                var plan = own.Where(a => Plans.Contains(a.Class)
                        && !(pointInTime && a.Class == ParkingAssignmentClass.Reservation && a.StartsAt < graceLimit))
                    .OrderBy(a => a.StartsAt).FirstOrDefault();
                var broken = outOfService.FirstOrDefault(o => o.SpaceId == space.Id);

                return new ParkingSpaceDto
                {
                    Id = space.Id, Code = space.Code, Location = space.Location, Type = space.Type,
                    VehicleKind = space.VehicleKind, Notes = space.Notes, IsActive = space.IsActive,
                    State = ParkingRules.ComputeState(broken != null, occupancy != null, plan != null),
                    HolderAssignmentId = holder?.Id, HolderUnitId = holder?.UnitId, HolderUnitLabel = holder?.UnitLabel, HolderPersonName = holder?.PersonName,
                    Occupant = occupancy == null ? null : new ParkingOccupantDto
                    {
                        AssignmentId = occupancy.Id, VehicleId = occupancy.VehicleId, Plate = occupancy.Plate, UnitLabel = occupancy.UnitLabel,
                        VisitId = occupancy.VisitId, Since = occupancy.StartsAt, IsException = occupancy.IsException,
                        // excedida: se marca, no se libera sola
                        IsOverdue = space.Type == ParkingSpaceType.Visitor && occupancy.StartsAt.AddHours(maxVisitorHours) < now
                    },
                    Plan = plan == null ? null : new ParkingPlanDto
                    {
                        AssignmentId = plan.Id, Class = plan.Class, UnitLabel = plan.UnitLabel, Plate = plan.Plate,
                        StartsAt = plan.StartsAt, EndsAt = plan.EndsAt
                    },
                    OutOfServiceId = broken?.Id, OutOfServiceReason = broken?.Reason, OutOfServiceUntil = broken?.To
                };
            }).ToList();
        }

        private async Task<ParkingSpaceDto> DtoAsync(Guid id)
        {
            return (await BuildAsync(_db.ParkingSpaces.Where(s => s.Id == id), _dateTime.UtcNow, null)).Single();
        }

        private async Task<ParkingSpace> FindAsync(Guid id)
        {
            return await _db.ParkingSpaces.FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new NotFoundException("El parqueadero no existe.");
        }

        private Task<ParkingAssignment?> CurrentHolderAsync(Guid spaceId, DateTime now)
        {
            return _db.ParkingAssignments.FirstOrDefaultAsync(a => a.SpaceId == spaceId && a.Class == ParkingAssignmentClass.Holder
                && a.Status == ParkingAssignmentStatus.Active && a.StartsAt <= now && (a.EndsAt == null || a.EndsAt > now));
        }

        private Task<bool> HasLiveAssignmentsAsync(Guid spaceId)
        {
            var now = _dateTime.UtcNow;
            return _db.ParkingAssignments.AnyAsync(a => a.SpaceId == spaceId && a.Status == ParkingAssignmentStatus.Active
                && (a.EndsAt == null || a.EndsAt > now));
        }

        private static void EnsureKind(ParkingSpace space, Vehicle vehicle)
        {
            if (!ParkingRules.Accepts(space.VehicleKind, vehicle.Type))
            {
                throw new BusinessRuleException($"El parqueadero {space.Code} no admite ese tipo de vehículo.");
            }
        }

        private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

        private async Task SaveAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
            {
                // la base es quien decide: dos peticiones simultáneas no pueden ganar ambas
                throw new ConflictException(OverlapMessage);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                throw new ConflictException("Ya existe un parqueadero con ese código.");
            }
        }
    }
}
