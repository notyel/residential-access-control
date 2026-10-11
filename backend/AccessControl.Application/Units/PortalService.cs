using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs.Person;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Domain.Common;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccessControl.Application.Units
{
    /// <summary>
    /// Único punto que decide sobre qué unidades puede actuar un usuario del portal: aquellas
    /// donde su persona tiene hoy una titularidad o una ocupación vigente.
    /// </summary>
    public interface IUnitScope
    {
        /// <summary>Unidades de las que la persona del usuario es propietaria hoy.</summary>
        Task<List<Guid>> GetOwnedUnitIdsAsync();

        /// <summary>Unidades en las que la persona del usuario vive hoy.</summary>
        Task<List<Guid>> GetOccupiedUnitIdsAsync();

        /// <summary>Propias u ocupadas.</summary>
        Task<List<Guid>> GetAccessibleUnitIdsAsync();

        /// <summary>Persona enlazada a la cuenta; null si la cuenta no corresponde a una persona del conjunto.</summary>
        Task<Guid?> GetPersonIdAsync();
    }

    public class UnitScope : IUnitScope
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeService _dateTime;
        private Guid? _personId;
        private bool _personLoaded;

        public UnitScope(ApplicationDbContext db, ICurrentUser currentUser, IDateTimeService dateTime)
        {
            _db = db;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Guid?> GetPersonIdAsync()
        {
            if (!_personLoaded)
            {
                _personId = await _db.Users.Where(u => u.Id == _currentUser.UserId).Select(u => u.PersonId).FirstOrDefaultAsync();
                _personLoaded = true;
            }
            return _personId;
        }

        public async Task<List<Guid>> GetOwnedUnitIdsAsync()
        {
            var personId = await GetPersonIdAsync();
            if (personId == null) return new List<Guid>();

            var today = _dateTime.Today;
            return await _db.UnitOwnerships
                .Where(o => o.PersonId == personId && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                .Select(o => o.UnitId).Distinct().ToListAsync();
        }

        public async Task<List<Guid>> GetOccupiedUnitIdsAsync()
        {
            var personId = await GetPersonIdAsync();
            if (personId == null) return new List<Guid>();

            var today = _dateTime.Today;
            return await _db.Occupancies
                .Where(o => o.PersonId == personId && o.Status == OccupancyStatus.Active
                    && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                .Select(o => o.UnitId).Distinct().ToListAsync();
        }

        public async Task<List<Guid>> GetAccessibleUnitIdsAsync()
        {
            return (await GetOwnedUnitIdsAsync()).Union(await GetOccupiedUnitIdsAsync()).ToList();
        }
    }

    public interface IPortalService
    {
        /// <summary>"Mis propiedades" y "Mi residencia": las unidades del usuario con su vínculo.</summary>
        Task<List<MyUnitDto>> GetMyUnitsAsync();

        /// <summary>"Mis residentes": ocupantes vigentes y solicitudes de las unidades de las que es propietario.</summary>
        Task<List<OccupancyDto>> GetMyOccupantsAsync();

        /// <summary>Registra un ocupante en una unidad propia, según la política del conjunto.</summary>
        Task<OccupancyDto> AddOccupantAsync(AddMyOccupantDto dto);

        /// <summary>Actualiza los datos de contacto de un ocupante de una unidad propia.</summary>
        Task<PersonDto> UpdateOccupantContactAsync(Guid occupancyId, UpdateContactDto dto);

        Task<OccupancyDto> EndOccupancyAsync(Guid occupancyId, EndDateDto dto);

        /// <summary>no, approval o direct: lo que la política permite al propietario.</summary>
        Task<string> GetOccupantsPolicyAsync();
    }

    public class PortalService : IPortalService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitScope _scope;
        private readonly ICurrentUser _currentUser;
        private readonly IPolicyService _policies;
        private readonly IOccupancyService _occupancies;
        private readonly IPeopleService _people;
        private readonly IDateTimeService _dateTime;

        public PortalService(
            ApplicationDbContext db,
            IUnitScope scope,
            ICurrentUser currentUser,
            IPolicyService policies,
            IOccupancyService occupancies,
            IPeopleService people,
            IDateTimeService dateTime)
        {
            _db = db;
            _scope = scope;
            _currentUser = currentUser;
            _policies = policies;
            _occupancies = occupancies;
            _people = people;
            _dateTime = dateTime;
        }

        public Task<string> GetOccupantsPolicyAsync() => _policies.GetAsync(ComplexPolicies.OwnerManagesOccupants);

        public async Task<List<MyUnitDto>> GetMyUnitsAsync()
        {
            var today = _dateTime.Today;
            var personId = await _scope.GetPersonIdAsync();
            var owned = await _scope.GetOwnedUnitIdsAsync();
            var occupied = await _scope.GetOccupiedUnitIdsAsync();
            var ids = owned.Union(occupied).ToList();

            var units = await _db.Units.Where(u => ids.Contains(u.Id)).OrderBy(u => u.Label)
                .Select(u => new
                {
                    u.Id,
                    u.Label,
                    u.Type,
                    Occupancies = u.Occupancies
                        .Where(o => o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                        .Select(o => new { o.PersonId, o.Relation, o.StartDate, o.EndDate, Name = o.Person.FirstName + " " + o.Person.LastName })
                        .ToList()
                })
                .ToListAsync();

            return units.Select(u =>
            {
                var mine = u.Occupancies.FirstOrDefault(o => o.PersonId == personId);
                return new MyUnitDto
                {
                    Id = u.Id,
                    Label = u.Label,
                    Type = u.Type,
                    IsOwner = owned.Contains(u.Id),
                    Relation = mine?.Relation,
                    StartDate = mine?.StartDate,
                    EndDate = mine?.EndDate,
                    OccupancyState = u.Occupancies.Any(o => o.Relation == OccupancyRelation.Tenant)
                        ? UnitOccupancyState.Rented
                        : u.Occupancies.Count > 0 ? UnitOccupancyState.OwnerOccupied : UnitOccupancyState.Vacant,
                    // de los convivientes solo se muestran los nombres
                    Occupants = u.Occupancies.Select(o => o.Name).OrderBy(n => n).ToList()
                };
            }).ToList();
        }

        public async Task<List<OccupancyDto>> GetMyOccupantsAsync()
        {
            var owned = await _scope.GetOwnedUnitIdsAsync();
            var today = _dateTime.Today;

            return await _db.Occupancies
                .Where(o => owned.Contains(o.UnitId)
                    && (o.Status == OccupancyStatus.Pending || o.Status == OccupancyStatus.Rejected
                        || (o.Status == OccupancyStatus.Active && (o.EndDate == null || o.EndDate >= today))))
                .OrderBy(o => o.Unit.Label).ThenBy(o => o.Person.FirstName)
                .Select(OccupancyService.ToDto(today))
                .ToListAsync();
        }

        public async Task<OccupancyDto> AddOccupantAsync(AddMyOccupantDto dto)
        {
            var policy = await EnsureCanManageAsync();
            await EnsureOwnsAsync(dto.UnitId!.Value);

            if (dto.Relation == OccupancyRelation.OwnerResident)
            {
                var personId = await _scope.GetPersonIdAsync();
                // el propietario solo puede declararse residente a sí mismo
                return await _occupancies.AddAsync(new AddOccupancyDto
                {
                    UnitId = dto.UnitId, PersonId = personId, Relation = dto.Relation, StartDate = dto.StartDate, EndDate = dto.EndDate
                }, pending: policy == ComplexPolicies.WithApproval);
            }

            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName)
                || string.IsNullOrWhiteSpace(dto.DocumentType) || string.IsNullOrWhiteSpace(dto.DocumentNumber))
            {
                throw new BusinessRuleException("Indique nombre, apellido, tipo y número de documento del ocupante.");
            }

            // se busca primero por documento para no duplicar a la persona
            var existing = (await _people.FindByDocumentAsync(dto.DocumentNumber)).FirstOrDefault();
            var person = existing ?? await _people.CreateAsync(new CreatePersonDto
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DocumentType = dto.DocumentType,
                DocumentNumber = dto.DocumentNumber,
                Phone = dto.Phone,
                Email = dto.Email
            });

            return await _occupancies.AddAsync(new AddOccupancyDto
            {
                UnitId = dto.UnitId,
                PersonId = person.Id,
                Relation = dto.Relation,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                FixedTerm = dto.FixedTerm
            }, pending: policy == ComplexPolicies.WithApproval);
        }

        public async Task<PersonDto> UpdateOccupantContactAsync(Guid occupancyId, UpdateContactDto dto)
        {
            await EnsureCanManageAsync();
            var occupancy = await FindMineAsync(occupancyId);

            var person = await _db.Persons.FirstAsync(p => p.Id == occupancy.PersonId);
            person.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            person.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            await _db.SaveChangesAsync();

            return PeopleService.ToDto(person);
        }

        public async Task<OccupancyDto> EndOccupancyAsync(Guid occupancyId, EndDateDto dto)
        {
            await EnsureCanManageAsync();
            await FindMineAsync(occupancyId);
            return await _occupancies.EndAsync(occupancyId, dto);
        }

        private async Task<string> EnsureCanManageAsync()
        {
            var policy = await GetOccupantsPolicyAsync();
            if (policy == ComplexPolicies.No)
            {
                throw new ForbiddenException("En este conjunto los ocupantes los registra la administración.");
            }
            return policy;
        }

        /// <summary>Una unidad ajena responde igual que una inexistente.</summary>
        private async Task EnsureOwnsAsync(Guid unitId)
        {
            if (_currentUser.HasPermission(Permissions.UnitsManageAny)) return;
            if (!(await _scope.GetOwnedUnitIdsAsync()).Contains(unitId))
            {
                throw new NotFoundException("La unidad no existe.");
            }
        }

        private async Task<Domain.Entities.Occupancy> FindMineAsync(Guid occupancyId)
        {
            var occupancy = await _db.Occupancies.FirstOrDefaultAsync(o => o.Id == occupancyId)
                ?? throw new NotFoundException("La ocupación no existe.");

            if (!_currentUser.HasPermission(Permissions.UnitsManageAny)
                && !(await _scope.GetOwnedUnitIdsAsync()).Contains(occupancy.UnitId))
            {
                throw new NotFoundException("La ocupación no existe.");
            }
            return occupancy;
        }
    }
}
