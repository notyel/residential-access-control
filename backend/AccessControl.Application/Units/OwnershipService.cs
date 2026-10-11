using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Policies;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AccessControl.Application.Units
{
    public interface IOwnershipService
    {
        Task<OwnershipDto> AddAsync(Guid unitId, AddOwnershipDto dto);

        /// <summary>Fija la fecha de fin de una titularidad; no la borra.</summary>
        Task<OwnershipDto> EndAsync(Guid ownershipId, DateOnly endDate);

        /// <summary>Cambio de propietario: cierra las vigentes el día anterior y crea las nuevas.</summary>
        Task<List<OwnershipDto>> TransferAsync(Guid unitId, TransferOwnershipDto dto);
    }

    public class OwnershipService : IOwnershipService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPolicyService _policies;
        private readonly IDateTimeService _dateTime;

        public OwnershipService(ApplicationDbContext db, IPolicyService policies, IDateTimeService dateTime)
        {
            _db = db;
            _policies = policies;
            _dateTime = dateTime;
        }

        public async Task<OwnershipDto> AddAsync(Guid unitId, AddOwnershipDto dto)
        {
            var unit = await FindActiveUnitAsync(unitId);
            await EnsurePersonAsync(dto.PersonId!.Value);

            var start = dto.StartDate!.Value;
            var overlapping = await _db.UnitOwnerships
                .Where(o => o.UnitId == unitId && (o.EndDate == null || o.EndDate >= start))
                .ToListAsync();

            if (overlapping.Any(o => o.PersonId == dto.PersonId))
            {
                throw new ConflictException("Esa persona ya es propietaria de la unidad en ese periodo.");
            }
            if (overlapping.Count > 0 && !await _policies.GetBoolAsync(ComplexPolicies.MultipleOwners))
            {
                throw new ConflictException(
                    "El conjunto admite un solo propietario por unidad. Use \"Cambiar propietario\" para reemplazar al actual.");
            }

            var ownership = new UnitOwnership
            {
                UnitId = unit.Id,
                PersonId = dto.PersonId.Value,
                StartDate = start,
                Percentage = dto.Percentage
            };
            _db.UnitOwnerships.Add(ownership);
            await SaveAsync();

            return await GetAsync(ownership.Id);
        }

        public async Task<OwnershipDto> EndAsync(Guid ownershipId, DateOnly endDate)
        {
            var ownership = await _db.UnitOwnerships.FirstOrDefaultAsync(o => o.Id == ownershipId)
                ?? throw new NotFoundException("La titularidad no existe.");

            if (endDate < ownership.StartDate)
            {
                throw new BusinessRuleException("La fecha de fin no puede ser anterior a la de inicio.");
            }

            ownership.EndDate = endDate;
            await SaveAsync();
            return await GetAsync(ownershipId);
        }

        public async Task<List<OwnershipDto>> TransferAsync(Guid unitId, TransferOwnershipDto dto)
        {
            var unit = await FindActiveUnitAsync(unitId);
            var date = dto.Date!.Value;
            var newOwners = dto.PersonIds.Distinct().ToList();

            if (newOwners.Count > 1 && !await _policies.GetBoolAsync(ComplexPolicies.MultipleOwners))
            {
                throw new ConflictException("El conjunto admite un solo propietario por unidad.");
            }
            foreach (var personId in newOwners)
            {
                await EnsurePersonAsync(personId);
            }

            var current = await _db.UnitOwnerships
                .Where(o => o.UnitId == unitId && (o.EndDate == null || o.EndDate >= date))
                .ToListAsync();

            foreach (var ownership in current)
            {
                if (ownership.StartDate >= date)
                {
                    throw new ConflictException("Hay una titularidad que empieza en esa fecha o después; revise la fecha del cambio.");
                }
                // nunca se cambia la persona de una titularidad: se cierra y se abre otra
                ownership.EndDate = date.AddDays(-1);
            }

            var created = newOwners
                .Select(personId => new UnitOwnership { UnitId = unit.Id, PersonId = personId, StartDate = date })
                .ToList();
            _db.UnitOwnerships.AddRange(created);
            await SaveAsync();

            var ids = created.Select(c => c.Id).ToList();
            return await _db.UnitOwnerships.Where(o => ids.Contains(o.Id)).Select(ToDto(_dateTime.Today)).ToListAsync();
        }

        private async Task<Unit> FindActiveUnitAsync(Guid unitId)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == unitId)
                ?? throw new NotFoundException("La unidad no existe.");
            if (!unit.IsActive)
            {
                throw new ConflictException("La unidad está inactiva y no admite nuevos propietarios.");
            }
            return unit;
        }

        private async Task EnsurePersonAsync(Guid personId)
        {
            if (!await _db.Persons.AnyAsync(p => p.Id == personId))
            {
                throw new BusinessRuleException("La persona indicada no existe.");
            }
        }

        // La restricción de exclusión de la base impide el solapamiento aunque lleguen dos peticiones a la vez
        private async Task SaveAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
            {
                throw new ConflictException("Esa persona ya es propietaria de la unidad en ese periodo.");
            }
        }

        private async Task<OwnershipDto> GetAsync(Guid id)
        {
            return await _db.UnitOwnerships.Where(o => o.Id == id).Select(ToDto(_dateTime.Today)).FirstAsync();
        }

        public static Expression<Func<UnitOwnership, OwnershipDto>> ToDto(DateOnly today) => o => new OwnershipDto
        {
            Id = o.Id,
            UnitId = o.UnitId,
            UnitLabel = o.Unit.Label,
            PersonId = o.PersonId,
            PersonName = o.Person.FirstName + " " + o.Person.LastName,
            StartDate = o.StartDate,
            EndDate = o.EndDate,
            Percentage = o.Percentage,
            IsCurrent = o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)
        };
    }
}
