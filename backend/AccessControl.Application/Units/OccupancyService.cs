using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AccessControl.Application.Units
{
    public interface IOccupancyService
    {
        /// <param name="pending">true = queda pendiente de aprobación (solicitud del portal del propietario).</param>
        Task<OccupancyDto> AddAsync(AddOccupancyDto dto, bool pending = false);

        /// <summary>Finaliza la ocupación y revoca lo que dependía de ella. No borra nada.</summary>
        Task<OccupancyDto> EndAsync(Guid occupancyId, EndDateDto dto);

        Task<OccupancyDto> DecideAsync(Guid occupancyId, DecisionDto dto);
        Task<List<OccupancyDto>> GetPendingAsync();

        /// <summary>
        /// Aplica a las ocupaciones que vencieron por fecha las mismas revocaciones que el cierre
        /// manual. Lo ejecuta el proceso diario; devuelve cuántas cerró.
        /// </summary>
        Task<int> CloseExpiredAsync();
    }

    /// <summary>
    /// Lo que debe revocarse cuando una persona deja de estar vinculada a una unidad
    /// (autorizaciones de visita, vehículos…). Cada módulo registra el suyo.
    /// </summary>
    public interface IOccupancyEndHandler
    {
        /// <summary>Se llama dentro de la misma operación que cierra la ocupación, antes de guardar.</summary>
        Task OnOccupancyEndedAsync(Occupancy occupancy, DateOnly endDate);
    }

    public class OccupancyService : IOccupancyService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeService _dateTime;
        private readonly IPermissionService _permissions;
        private readonly IEnumerable<IOccupancyEndHandler> _endHandlers;

        public OccupancyService(
            ApplicationDbContext db,
            ICurrentUser currentUser,
            IDateTimeService dateTime,
            IPermissionService permissions,
            IEnumerable<IOccupancyEndHandler> endHandlers)
        {
            _db = db;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _permissions = permissions;
            _endHandlers = endHandlers;
        }

        public async Task<OccupancyDto> AddAsync(AddOccupancyDto dto, bool pending = false)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == dto.UnitId)
                ?? throw new NotFoundException("La unidad no existe.");
            if (!unit.IsActive)
            {
                throw new ConflictException("La unidad está inactiva y no admite nuevos ocupantes.");
            }
            if (!await _db.Persons.AnyAsync(p => p.Id == dto.PersonId))
            {
                throw new BusinessRuleException("La persona indicada no existe.");
            }

            var start = dto.StartDate!.Value;
            if (dto.EndDate.HasValue && dto.EndDate.Value < start)
            {
                throw new BusinessRuleException("La fecha de fin no puede ser anterior a la de inicio.");
            }
            if (dto.Relation == OccupancyRelation.Tenant && dto.FixedTerm && !dto.EndDate.HasValue)
            {
                throw new BusinessRuleException("Un arrendamiento a término fijo exige fecha de fin.");
            }
            if (dto.Relation == OccupancyRelation.OwnerResident)
            {
                // "propietario residente" solo lo es quien tiene la titularidad vigente en esa fecha
                var owns = await _db.UnitOwnerships.AnyAsync(o => o.UnitId == unit.Id && o.PersonId == dto.PersonId
                    && o.StartDate <= start && (o.EndDate == null || o.EndDate >= start));
                if (!owns)
                {
                    throw new BusinessRuleException("Esa persona no es propietaria vigente de la unidad; regístrela con otra relación o agregue primero la titularidad.");
                }
            }

            var end = dto.EndDate;
            var overlaps = await _db.Occupancies.AnyAsync(o => o.UnitId == unit.Id && o.PersonId == dto.PersonId
                && (o.Status == OccupancyStatus.Active || o.Status == OccupancyStatus.Pending)
                && (o.EndDate == null || o.EndDate >= start) && (end == null || o.StartDate <= end));
            if (overlaps)
            {
                throw new ConflictException("Esa persona ya tiene una ocupación en la unidad que se cruza con ese periodo.");
            }

            var occupancy = new Occupancy
            {
                UnitId = unit.Id,
                PersonId = dto.PersonId!.Value,
                Relation = dto.Relation,
                StartDate = start,
                EndDate = end,
                Status = pending ? OccupancyStatus.Pending : OccupancyStatus.Active,
                RequestedById = pending ? _currentUser.UserId : null
            };
            _db.Occupancies.Add(occupancy);
            await SaveAsync();

            return await GetAsync(occupancy.Id);
        }

        public async Task<OccupancyDto> EndAsync(Guid occupancyId, EndDateDto dto)
        {
            var occupancy = await _db.Occupancies.FirstOrDefaultAsync(o => o.Id == occupancyId)
                ?? throw new NotFoundException("La ocupación no existe.");
            if (occupancy.Status != OccupancyStatus.Active)
            {
                throw new ConflictException("Solo se puede finalizar una ocupación vigente.");
            }

            var endDate = dto.EndDate!.Value;
            if (endDate < occupancy.StartDate)
            {
                throw new BusinessRuleException("La fecha de fin no puede ser anterior a la de inicio.");
            }

            await CloseAsync(occupancy, endDate, dto.Reason);

            if (dto.IncludeDependents && occupancy.Relation == OccupancyRelation.Tenant)
            {
                // quienes vivían allí por cuenta del arrendatario salen con él; los propietarios se quedan
                var dependents = await _db.Occupancies
                    .Where(o => o.UnitId == occupancy.UnitId && o.Id != occupancy.Id && o.Status == OccupancyStatus.Active
                        && o.Relation != OccupancyRelation.OwnerResident && o.StartDate <= endDate
                        && (o.EndDate == null || o.EndDate >= endDate))
                    .ToListAsync();
                foreach (var dependent in dependents)
                {
                    await CloseAsync(dependent, endDate, "Finalizó el arrendamiento de la unidad");
                }
            }

            await _db.SaveChangesAsync();
            _permissions.Invalidate();
            return await GetAsync(occupancyId);
        }

        public async Task<OccupancyDto> DecideAsync(Guid occupancyId, DecisionDto dto)
        {
            var occupancy = await _db.Occupancies.FirstOrDefaultAsync(o => o.Id == occupancyId)
                ?? throw new NotFoundException("La solicitud no existe.");
            if (occupancy.Status != OccupancyStatus.Pending)
            {
                throw new ConflictException("La solicitud ya fue resuelta.");
            }
            if (!dto.Approve && string.IsNullOrWhiteSpace(dto.Reason))
            {
                throw new BusinessRuleException("Indique el motivo del rechazo.");
            }

            occupancy.Status = dto.Approve ? OccupancyStatus.Active : OccupancyStatus.Rejected;
            occupancy.StatusReason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim();
            await SaveAsync();
            return await GetAsync(occupancyId);
        }

        public async Task<List<OccupancyDto>> GetPendingAsync()
        {
            return await _db.Occupancies
                .Where(o => o.Status == OccupancyStatus.Pending)
                .OrderBy(o => o.CreatedAt)
                .Select(ToDto(_dateTime.Today))
                .ToListAsync();
        }

        public async Task<int> CloseExpiredAsync()
        {
            var today = _dateTime.Today;
            // Sin filtro de conjunto: el proceso diario recorre todos. "Hoy" usa la zona por defecto;
            // la diferencia de horas entre zonas solo adelanta o atrasa el cierre formal, porque una
            // ocupación vencida ya dejó de contar como vigente por su fecha.
            var expired = await _db.Occupancies.IgnoreQueryFilters()
                .Where(o => o.Status == OccupancyStatus.Active && o.EndDate != null && o.EndDate < today)
                .ToListAsync();

            foreach (var occupancy in expired)
            {
                await CloseAsync(occupancy, occupancy.EndDate!.Value, "Venció el periodo de ocupación");
            }

            await _db.SaveChangesAsync();
            if (expired.Count > 0) _permissions.Invalidate();
            return expired.Count;
        }

        /// <summary>
        /// Único punto que cierra una ocupación: el cierre manual y el automático revocan lo mismo.
        /// </summary>
        private async Task CloseAsync(Occupancy occupancy, DateOnly endDate, string? reason)
        {
            occupancy.EndDate = endDate;
            occupancy.Status = OccupancyStatus.Ended;
            occupancy.StatusReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            foreach (var handler in _endHandlers)
            {
                await handler.OnOccupancyEndedAsync(occupancy, endDate);
            }

            await RemoveResidentRoleIfUnlinkedAsync(occupancy, endDate);
        }

        /// <summary>
        /// Si a la persona no le queda ningún otro vínculo vigente en el conjunto, su cuenta pierde
        /// el rol de residente. La cuenta sigue existiendo: credenciales y vínculos son independientes.
        /// </summary>
        private async Task RemoveResidentRoleIfUnlinkedAsync(Occupancy occupancy, DateOnly endDate)
        {
            var stillLinked = await _db.Occupancies.IgnoreQueryFilters().AnyAsync(o =>
                    o.PersonId == occupancy.PersonId && o.Id != occupancy.Id && o.Status == OccupancyStatus.Active
                    && (o.EndDate == null || o.EndDate > endDate))
                || await _db.UnitOwnerships.IgnoreQueryFilters().AnyAsync(o =>
                    o.PersonId == occupancy.PersonId && (o.EndDate == null || o.EndDate > endDate));
            if (stillLinked) return;

            var residentRoles = await _db.UserRoles.IgnoreQueryFilters()
                .Where(ur => ur.User.PersonId == occupancy.PersonId && ur.Role.SystemKey == SystemRoles.Resident)
                .ToListAsync();
            _db.UserRoles.RemoveRange(residentRoles);
        }

        private async Task SaveAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
            {
                throw new ConflictException("Esa persona ya tiene una ocupación en la unidad que se cruza con ese periodo.");
            }
        }

        private async Task<OccupancyDto> GetAsync(Guid id)
        {
            return await _db.Occupancies.Where(o => o.Id == id).Select(ToDto(_dateTime.Today)).FirstAsync();
        }

        public static Expression<Func<Occupancy, OccupancyDto>> ToDto(DateOnly today) => o => new OccupancyDto
        {
            Id = o.Id,
            UnitId = o.UnitId,
            UnitLabel = o.Unit.Label,
            PersonId = o.PersonId,
            PersonName = o.Person.FirstName + " " + o.Person.LastName,
            Relation = o.Relation,
            StartDate = o.StartDate,
            EndDate = o.EndDate,
            Status = o.Status,
            StatusReason = o.StatusReason,
            IsCurrent = o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)
        };
    }
}
