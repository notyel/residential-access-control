using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Policies;
using AccessControl.Application.Units;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Common;
using AccessControl.Domain.Common;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Access
{
    public interface IAuthorizationService
    {
        /// <param name="viaPortal">true = autoriza quien inicia sesión, sobre una unidad con la que tiene vínculo vigente.</param>
        Task<AuthorizationDto> CreateAsync(CreateAuthorizationDto dto, bool viaPortal);

        /// <param name="unitScope">Si se indica, solo autorizaciones de esas unidades.</param>
        Task<PaginatedResultDto<AuthorizationDto>> SearchAsync(AuthorizationFilterDto filter, IReadOnlyCollection<Guid>? unitScope = null);

        Task<AuthorizationDto> RevokeAsync(Guid id, string? reason, IReadOnlyCollection<Guid>? unitScope = null);

        /// <summary>Autorizaciones que aplican hoy y cuyo visitante aún no ha ingresado.</summary>
        Task<List<AuthorizationDto>> GetExpectedTodayAsync(IReadOnlyCollection<Guid>? unitScope = null);

        /// <summary>Carga la autorización y la evalúa en este instante.</summary>
        Task<(VisitAuthorization Authorization, Validity Validity)> EvaluateAsync(Guid id);

        Task<List<AuthorizationDto>> ToDtosAsync(IReadOnlyCollection<VisitAuthorization> authorizations);
    }

    public class AuthorizationService : IAuthorizationService, IOccupancyEndHandler
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeService _dateTime;
        private readonly IPolicyService _policies;
        private readonly IUnitScope _scope;
        private readonly ICatalogService _catalogs;

        public AuthorizationService(
            ApplicationDbContext db,
            ICurrentUser currentUser,
            IDateTimeService dateTime,
            IPolicyService policies,
            IUnitScope scope,
            ICatalogService catalogs)
        {
            _db = db;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _policies = policies;
            _scope = scope;
            _catalogs = catalogs;
        }

        public async Task<AuthorizationDto> CreateAsync(CreateAuthorizationDto dto, bool viaPortal)
        {
            var today = _dateTime.Today;
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == dto.UnitId)
                // desde el portal una unidad ajena o inexistente se ven igual
                ?? throw new NotFoundException("La unidad no existe.");
            if (!unit.IsActive)
            {
                throw new ConflictException("La unidad está inactiva y no admite visitas.");
            }

            var authorizerId = viaPortal ? await _scope.GetPersonIdAsync() : dto.AuthorizedByPersonId;
            if (authorizerId == null)
            {
                throw viaPortal
                    ? new NotFoundException("La unidad no existe.")
                    : new BusinessRuleException("Indique quién de la unidad autoriza la visita.");
            }

            // Toda autorización identifica a quien autoriza, y debe tener vínculo vigente con la unidad
            var isOwner = await _db.UnitOwnerships.AnyAsync(o => o.UnitId == unit.Id && o.PersonId == authorizerId
                && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today));
            var occupancies = await _db.Occupancies
                .Where(o => o.UnitId == unit.Id && o.PersonId == authorizerId && o.Status == OccupancyStatus.Active
                    && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                .ToListAsync();

            if (!isOwner && occupancies.Count == 0)
            {
                throw viaPortal
                    ? new NotFoundException("La unidad no existe.")
                    : new BusinessRuleException("Quien autoriza debe ser propietario u ocupante vigente de la unidad.");
            }
            if (!isOwner && !await _policies.GetBoolAsync(ComplexPolicies.ResidentCanAuthorize))
            {
                throw new ForbiddenException("En este conjunto solo los propietarios y la administración autorizan visitas.");
            }

            var start = dto.StartDate!.Value;
            var end = dto.Kind == AuthorizationKind.Single ? start : dto.EndDate
                ?? throw new BusinessRuleException("Indique hasta cuándo vale la autorización.");
            if (end < start)
            {
                throw new BusinessRuleException("La fecha de fin no puede ser anterior a la de inicio.");
            }
            if (end < today)
            {
                throw new BusinessRuleException("La autorización terminaría en una fecha que ya pasó.");
            }

            var maxDays = await _policies.GetIntAsync(ComplexPolicies.AuthorizationMaxDays);
            if (end.DayNumber - start.DayNumber + 1 > maxDays)
            {
                throw new BusinessRuleException($"Una autorización no puede durar más de {maxDays} días.");
            }

            // No puede valer más allá de lo que dura el vínculo de quien autoriza
            if (!isOwner && occupancies.All(o => o.EndDate != null) && end > occupancies.Max(o => o.EndDate!.Value))
            {
                throw new BusinessRuleException("La autorización no puede terminar después de que termine la ocupación de quien autoriza.");
            }

            var days = AuthorizationRules.ToMask(dto.DaysOfWeek ?? new List<int>());
            if (dto.Kind == AuthorizationKind.Recurring && days == 0)
            {
                throw new BusinessRuleException("Indique los días de la semana de la autorización recurrente.");
            }
            if (dto.TimeFrom.HasValue != dto.TimeTo.HasValue)
            {
                throw new BusinessRuleException("Indique la hora de inicio y la de fin de la franja, o ninguna.");
            }

            var reason = string.IsNullOrWhiteSpace(dto.Reason) ? "social" : dto.Reason.Trim();
            await _catalogs.EnsureValidAsync(Catalogs.VisitReason, reason, "Motivo de la visita");

            string visitorName;
            string? visitorDocument;
            if (dto.VisitorPersonId.HasValue)
            {
                var visitor = await _db.Persons.FirstOrDefaultAsync(p => p.Id == dto.VisitorPersonId.Value)
                    ?? throw new BusinessRuleException("El visitante indicado no existe.");
                visitorName = $"{visitor.FirstName} {visitor.LastName}";
                visitorDocument = visitor.DocumentNumber;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.VisitorName))
                {
                    throw new BusinessRuleException("Indique el nombre del visitante.");
                }
                visitorName = dto.VisitorName.Trim();
                visitorDocument = string.IsNullOrWhiteSpace(dto.VisitorDocument) ? null : dto.VisitorDocument.Trim();
            }

            var authorization = new VisitAuthorization
            {
                UnitId = unit.Id,
                AuthorizedByPersonId = authorizerId.Value,
                VisitorPersonId = dto.VisitorPersonId,
                VisitorName = visitorName,
                VisitorDocument = visitorDocument,
                Reason = reason,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Kind = dto.Kind,
                StartDate = start,
                EndDate = end,
                TimeFrom = dto.TimeFrom,
                TimeTo = dto.TimeTo,
                DaysOfWeek = dto.Kind == AuthorizationKind.Recurring ? days : 0
            };
            _db.VisitAuthorizations.Add(authorization);
            await _db.SaveChangesAsync();

            return (await ToDtosAsync(new[] { await LoadAsync(authorization.Id) })).Single();
        }

        public async Task<PaginatedResultDto<AuthorizationDto>> SearchAsync(AuthorizationFilterDto filter, IReadOnlyCollection<Guid>? unitScope = null)
        {
            var today = _dateTime.Today;
            var query = Query();

            if (unitScope != null)
            {
                query = query.Where(a => unitScope.Contains(a.UnitId));
            }
            if (filter.UnitId.HasValue)
            {
                query = query.Where(a => a.UnitId == filter.UnitId.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = $"%{filter.Search.Trim()}%";
                query = query.Where(a => EF.Functions.ILike(a.VisitorName, pattern)
                    || (a.VisitorDocument != null && EF.Functions.ILike(a.VisitorDocument, pattern))
                    || EF.Functions.ILike(a.Unit.Label, pattern));
            }

            query = (filter.State ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "" => query,
                "active" => query.Where(a => a.Status == AuthorizationStatus.Active && a.EndDate >= today),
                "expired" => query.Where(a => a.Status == AuthorizationStatus.Active && a.EndDate < today),
                "revoked" => query.Where(a => a.Status == AuthorizationStatus.Revoked),
                "consumed" => query.Where(a => a.Status == AuthorizationStatus.Consumed),
                _ => throw new BusinessRuleException("El estado indicado no es válido.")
            };

            var totalCount = await query.CountAsync();
            var page = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<AuthorizationDto>
            {
                Items = await ToDtosAsync(page),
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<AuthorizationDto> RevokeAsync(Guid id, string? reason, IReadOnlyCollection<Guid>? unitScope = null)
        {
            var authorization = await LoadAsync(id);
            if (unitScope != null && !unitScope.Contains(authorization.UnitId))
            {
                throw new NotFoundException("La autorización no existe.");
            }
            if (authorization.Status != AuthorizationStatus.Active)
            {
                throw new ConflictException("La autorización ya no está vigente.");
            }

            Revoke(authorization, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim());
            await _db.SaveChangesAsync();

            return (await ToDtosAsync(new[] { authorization })).Single();
        }

        public async Task<List<AuthorizationDto>> GetExpectedTodayAsync(IReadOnlyCollection<Guid>? unitScope = null)
        {
            var today = _dateTime.Today;
            var (dayStartUtc, dayEndUtc) = _dateTime.GetTodayRangeUtc();

            var query = Query().Where(a => a.Status == AuthorizationStatus.Active && a.StartDate <= today && a.EndDate >= today);
            if (unitScope != null)
            {
                query = query.Where(a => unitScope.Contains(a.UnitId));
            }

            // "Esperado" = aplica hoy y todavía no hay una visita de hoy que la haya usado
            var usedToday = await _db.Visits
                .Where(v => v.AuthorizationId != null && v.VoidEventId == null && v.CreatedAt >= dayStartUtc && v.CreatedAt < dayEndUtc)
                .Select(v => v.AuthorizationId!.Value)
                .ToListAsync();

            var candidates = (await query.OrderBy(a => a.TimeFrom).ThenBy(a => a.VisitorName).ToListAsync())
                .Where(a => AuthorizationRules.CouldApplyOn(a, today) && !usedToday.Contains(a.Id))
                .ToList();

            return await ToDtosAsync(candidates);
        }

        public async Task<(VisitAuthorization Authorization, Validity Validity)> EvaluateAsync(Guid id)
        {
            var authorization = await LoadAsync(id);
            var linked = await LinkedPairsAsync(new[] { authorization });
            var validity = AuthorizationRules.IsValidAt(authorization, _dateTime.LocalNow,
                linked.Contains((authorization.AuthorizedByPersonId, authorization.UnitId)));
            return (authorization, validity);
        }

        public async Task<List<AuthorizationDto>> ToDtosAsync(IReadOnlyCollection<VisitAuthorization> authorizations)
        {
            var now = _dateTime.LocalNow;
            var today = DateOnly.FromDateTime(now);
            var linked = await LinkedPairsAsync(authorizations);

            return authorizations.Select(a =>
            {
                var validity = AuthorizationRules.IsValidAt(a, now, linked.Contains((a.AuthorizedByPersonId, a.UnitId)));
                return new AuthorizationDto
                {
                    Id = a.Id,
                    UnitId = a.UnitId,
                    UnitLabel = a.Unit.Label,
                    AuthorizedByPersonId = a.AuthorizedByPersonId,
                    AuthorizedByName = $"{a.AuthorizedBy.FirstName} {a.AuthorizedBy.LastName}",
                    VisitorPersonId = a.VisitorPersonId,
                    VisitorName = a.VisitorName,
                    VisitorDocument = a.VisitorDocument,
                    Reason = a.Reason,
                    Notes = a.Notes,
                    Kind = a.Kind,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    TimeFrom = a.TimeFrom,
                    TimeTo = a.TimeTo,
                    DaysOfWeek = AuthorizationRules.FromMask(a.DaysOfWeek),
                    State = AuthorizationRules.StateOf(a, today),
                    RevokeReason = a.RevokeReason,
                    CanEnterNow = validity.IsValid,
                    CannotEnterReason = validity.Reason,
                    CreatedAt = a.CreatedAt
                };
            }).ToList();
        }

        /// <summary>
        /// Al terminar la ocupación de alguien se revocan las autorizaciones que emitió para esa
        /// unidad, salvo que conserve otro vínculo vigente con ella (p. ej. sigue siendo propietario).
        /// </summary>
        public async Task OnOccupancyEndedAsync(Occupancy occupancy, DateOnly endDate)
        {
            var stillOwner = await _db.UnitOwnerships.IgnoreQueryFilters().AnyAsync(o =>
                o.UnitId == occupancy.UnitId && o.PersonId == occupancy.PersonId && (o.EndDate == null || o.EndDate > endDate));
            var otherOccupancy = await _db.Occupancies.IgnoreQueryFilters().AnyAsync(o =>
                o.Id != occupancy.Id && o.UnitId == occupancy.UnitId && o.PersonId == occupancy.PersonId
                && o.Status == OccupancyStatus.Active && (o.EndDate == null || o.EndDate > endDate));
            if (stillOwner || otherOccupancy) return;

            var authorizations = await _db.VisitAuthorizations.IgnoreQueryFilters()
                .Where(a => a.UnitId == occupancy.UnitId && a.AuthorizedByPersonId == occupancy.PersonId && a.Status == AuthorizationStatus.Active)
                .ToListAsync();
            foreach (var authorization in authorizations)
            {
                Revoke(authorization, "Vinculación finalizada");
            }
        }

        private void Revoke(VisitAuthorization authorization, string? reason)
        {
            authorization.Status = AuthorizationStatus.Revoked;
            authorization.RevokedAt = _dateTime.UtcNow;
            authorization.RevokedById = _currentUser.UserId;
            authorization.RevokeReason = reason;
        }

        private IQueryable<VisitAuthorization> Query() =>
            _db.VisitAuthorizations.Include(a => a.Unit).Include(a => a.AuthorizedBy);

        private async Task<VisitAuthorization> LoadAsync(Guid id)
        {
            return await Query().FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new NotFoundException("La autorización no existe.");
        }

        /// <summary>Pares (persona que autoriza, unidad) que hoy conservan un vínculo vigente.</summary>
        private async Task<HashSet<(Guid, Guid)>> LinkedPairsAsync(IReadOnlyCollection<VisitAuthorization> authorizations)
        {
            var today = _dateTime.Today;
            var personIds = authorizations.Select(a => a.AuthorizedByPersonId).Distinct().ToList();
            var unitIds = authorizations.Select(a => a.UnitId).Distinct().ToList();

            var owners = await _db.UnitOwnerships
                .Where(o => personIds.Contains(o.PersonId) && unitIds.Contains(o.UnitId)
                    && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                .Select(o => new { o.PersonId, o.UnitId }).ToListAsync();
            var occupants = await _db.Occupancies
                .Where(o => personIds.Contains(o.PersonId) && unitIds.Contains(o.UnitId) && o.Status == OccupancyStatus.Active
                    && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                .Select(o => new { o.PersonId, o.UnitId }).ToListAsync();

            return owners.Concat(occupants).Select(x => (x.PersonId, x.UnitId)).ToHashSet();
        }
    }
}
