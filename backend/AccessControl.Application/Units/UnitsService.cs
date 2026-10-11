using AccessControl.Application.Exceptions;
using AccessControl.Application.Interfaces;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Domain.Entities;
using AccessControl.Persistence;
using AccessControl.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AccessControl.Application.Units
{
    public interface IUnitsService
    {
        Task<List<StructureDto>> GetStructuresAsync();
        Task<StructureDto> CreateStructureAsync(SaveStructureDto dto);
        Task<StructureDto> UpdateStructureAsync(Guid id, SaveStructureDto dto);
        Task DeleteStructureAsync(Guid id);

        Task<PaginatedResultDto<UnitDto>> SearchAsync(UnitFilterDto filter);
        Task<UnitDto> GetAsync(Guid id);
        Task<UnitDto> CreateAsync(SaveUnitDto dto);
        Task<UnitDto> UpdateAsync(Guid id, SaveUnitDto dto);
        Task<UnitDto> SetActiveAsync(Guid id, bool isActive);
        Task<BulkUnitsResultDto> BulkCreateAsync(BulkUnitsDto dto);

        /// <summary>Ficha de la unidad. Sin detalle, solo lo vigente (vista de portería).</summary>
        Task<UnitSummaryDto> GetSummaryAsync(Guid id, bool includeHistory);
    }

    public class UnitsService : IUnitsService
    {
        private const string Separator = " · ";

        private readonly ApplicationDbContext _db;
        private readonly ICatalogService _catalogs;
        private readonly IDateTimeService _dateTime;

        public UnitsService(ApplicationDbContext db, ICatalogService catalogs, IDateTimeService dateTime)
        {
            _db = db;
            _catalogs = catalogs;
            _dateTime = dateTime;
        }

        // ---------- Estructuras ----------

        public async Task<List<StructureDto>> GetStructuresAsync()
        {
            var structures = await _db.Structures.AsNoTracking().ToListAsync();
            var unitCounts = await _db.Units.Where(u => u.StructureId != null)
                .GroupBy(u => u.StructureId!.Value)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
            var byId = structures.ToDictionary(s => s.Id);

            return structures
                .Select(s => new StructureDto
                {
                    Id = s.Id,
                    ParentId = s.ParentId,
                    Type = s.Type,
                    Name = s.Name,
                    Path = PathOf(s, byId),
                    IsActive = s.IsActive,
                    UnitCount = unitCounts.GetValueOrDefault(s.Id)
                })
                .OrderBy(s => s.Path)
                .ToList();
        }

        public async Task<StructureDto> CreateStructureAsync(SaveStructureDto dto)
        {
            await ValidateStructureAsync(dto, null);

            var structure = new Structure { ParentId = dto.ParentId, Type = dto.Type.Trim(), Name = dto.Name.Trim() };
            _db.Structures.Add(structure);
            await _db.SaveChangesAsync();

            return (await GetStructuresAsync()).Single(s => s.Id == structure.Id);
        }

        public async Task<StructureDto> UpdateStructureAsync(Guid id, SaveStructureDto dto)
        {
            var structure = await _db.Structures.FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new NotFoundException("La estructura no existe.");
            await ValidateStructureAsync(dto, id);

            structure.ParentId = dto.ParentId;
            structure.Type = dto.Type.Trim();
            structure.Name = dto.Name.Trim();
            await _db.SaveChangesAsync();

            // el nombre forma parte de la etiqueta de las unidades que cuelgan de ella
            await RefreshLabelsAsync();
            return (await GetStructuresAsync()).Single(s => s.Id == id);
        }

        public async Task DeleteStructureAsync(Guid id)
        {
            var structure = await _db.Structures.FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new NotFoundException("La estructura no existe.");

            if (await _db.Structures.AnyAsync(s => s.ParentId == id) || await _db.Units.AnyAsync(u => u.StructureId == id))
            {
                throw new ConflictException("La estructura tiene unidades o estructuras dentro; muévalas antes de eliminarla.");
            }

            _db.Structures.Remove(structure);
            await _db.SaveChangesAsync();
        }

        private async Task ValidateStructureAsync(SaveStructureDto dto, Guid? currentId)
        {
            await _catalogs.EnsureValidAsync(Catalogs.StructureType, dto.Type, "Tipo de estructura");

            if (dto.ParentId.HasValue)
            {
                var all = await _db.Structures.AsNoTracking().ToDictionaryAsync(s => s.Id);
                if (!all.ContainsKey(dto.ParentId.Value))
                {
                    throw new BusinessRuleException("La estructura contenedora no existe.");
                }
                // una estructura no puede quedar dentro de sí misma ni de una de sus descendientes
                for (Guid? cursor = dto.ParentId; cursor.HasValue; cursor = all[cursor.Value].ParentId)
                {
                    if (cursor == currentId)
                    {
                        throw new BusinessRuleException("Una estructura no puede contenerse a sí misma.");
                    }
                }
            }

            var name = dto.Name.Trim().ToLower();
            if (await _db.Structures.AnyAsync(s => s.Id != currentId && s.ParentId == dto.ParentId && s.Name.ToLower() == name))
            {
                throw new ConflictException("Ya existe una estructura con ese nombre en el mismo nivel.");
            }
        }

        private static string PathOf(Structure structure, IReadOnlyDictionary<Guid, Structure> byId)
        {
            var parts = new List<string>();
            for (var cursor = structure; cursor != null; cursor = cursor.ParentId.HasValue ? byId.GetValueOrDefault(cursor.ParentId.Value) : null)
            {
                parts.Insert(0, cursor.Name);
                if (parts.Count > 20) break; // defensa ante un ciclo en los datos
            }
            return string.Join(Separator, parts);
        }

        private async Task<string> LabelForAsync(string code, Guid? structureId)
        {
            if (!structureId.HasValue) return code;

            var byId = await _db.Structures.AsNoTracking().ToDictionaryAsync(s => s.Id);
            return byId.TryGetValue(structureId.Value, out var structure)
                ? PathOf(structure, byId) + Separator + code
                : code;
        }

        private async Task RefreshLabelsAsync()
        {
            var byId = await _db.Structures.AsNoTracking().ToDictionaryAsync(s => s.Id);
            var units = await _db.Units.Where(u => u.StructureId != null).ToListAsync();
            foreach (var unit in units)
            {
                var label = byId.TryGetValue(unit.StructureId!.Value, out var structure)
                    ? PathOf(structure, byId) + Separator + unit.Code
                    : unit.Code;
                if (unit.Label != label) unit.Label = label;
            }
            await _db.SaveChangesAsync();
        }

        // ---------- Unidades ----------

        public async Task<PaginatedResultDto<UnitDto>> SearchAsync(UnitFilterDto filter)
        {
            var today = _dateTime.Today;
            var query = _db.Units.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = $"%{filter.Search.Trim()}%";
                query = query.Where(u => EF.Functions.ILike(u.Label, pattern)
                    || u.Ownerships.Any(o => o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)
                        && EF.Functions.ILike(o.Person.FirstName + " " + o.Person.LastName, pattern)));
            }
            if (filter.StructureId.HasValue)
            {
                query = query.Where(u => u.StructureId == filter.StructureId.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                query = query.Where(u => u.Type == filter.Type.Trim());
            }
            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }
            if (filter.State.HasValue)
            {
                query = filter.State.Value switch
                {
                    UnitOccupancyState.Rented => query.Where(u => u.Occupancies.Any(o => o.Status == OccupancyStatus.Active && o.StartDate <= today
                        && (o.EndDate == null || o.EndDate >= today) && o.Relation == OccupancyRelation.Tenant)),
                    UnitOccupancyState.OwnerOccupied => query.Where(u =>
                        u.Occupancies.Any(o => o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                        && !u.Occupancies.Any(o => o.Status == OccupancyStatus.Active && o.StartDate <= today
                            && (o.EndDate == null || o.EndDate >= today) && o.Relation == OccupancyRelation.Tenant)),
                    _ => query.Where(u => !u.Occupancies.Any(o => o.Status == OccupancyStatus.Active && o.StartDate <= today
                        && (o.EndDate == null || o.EndDate >= today)))
                };
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.Label)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(ToDto(today))
                .ToListAsync();

            return new PaginatedResultDto<UnitDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<UnitDto> GetAsync(Guid id)
        {
            return await _db.Units.Where(u => u.Id == id).Select(ToDto(_dateTime.Today)).FirstOrDefaultAsync()
                ?? throw new NotFoundException("La unidad no existe.");
        }

        public async Task<UnitDto> CreateAsync(SaveUnitDto dto)
        {
            await ValidateUnitAsync(dto, null);

            var unit = new Unit
            {
                Code = dto.Code.Trim(),
                Type = dto.Type.Trim(),
                StructureId = dto.StructureId,
                Label = await LabelForAsync(dto.Code.Trim(), dto.StructureId),
                Area = dto.Area,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
            };
            _db.Units.Add(unit);
            await _db.SaveChangesAsync();

            return await GetAsync(unit.Id);
        }

        public async Task<UnitDto> UpdateAsync(Guid id, SaveUnitDto dto)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new NotFoundException("La unidad no existe.");
            await ValidateUnitAsync(dto, id);

            unit.Code = dto.Code.Trim();
            unit.Type = dto.Type.Trim();
            unit.StructureId = dto.StructureId;
            unit.Label = await LabelForAsync(unit.Code, unit.StructureId);
            unit.Area = dto.Area;
            unit.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
            await _db.SaveChangesAsync();

            return await GetAsync(id);
        }

        public async Task<UnitDto> SetActiveAsync(Guid id, bool isActive)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new NotFoundException("La unidad no existe.");

            // Una unidad no se borra: inactiva conserva su historial y deja de admitir vínculos nuevos
            unit.IsActive = isActive;
            await _db.SaveChangesAsync();
            return await GetAsync(id);
        }

        public async Task<BulkUnitsResultDto> BulkCreateAsync(BulkUnitsDto dto)
        {
            if (dto.FloorTo < dto.FloorFrom)
            {
                throw new BusinessRuleException("El piso final no puede ser menor que el inicial.");
            }
            var total = (dto.FloorTo - dto.FloorFrom + 1) * dto.UnitsPerFloor;
            if (total > 1000)
            {
                throw new BusinessRuleException("No se pueden crear más de 1000 unidades de una vez.");
            }

            await _catalogs.EnsureValidAsync(Catalogs.UnitType, dto.Type, "Tipo de unidad");
            if (dto.StructureId.HasValue && !await _db.Structures.AnyAsync(s => s.Id == dto.StructureId.Value))
            {
                throw new BusinessRuleException("La estructura indicada no existe.");
            }

            var prefix = (dto.Prefix ?? string.Empty).Trim();
            var codes = new List<string>();
            for (var floor = dto.FloorFrom; floor <= dto.FloorTo; floor++)
            {
                for (var number = 1; number <= dto.UnitsPerFloor; number++)
                {
                    codes.Add($"{prefix}{floor}{number:00}");
                }
            }

            var existing = (await _db.Units.Where(u => codes.Contains(u.Code)).Select(u => u.Code).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = new BulkUnitsResultDto
            {
                Created = codes.Where(c => !existing.Contains(c)).ToList(),
                Skipped = codes.Where(existing.Contains).ToList()
            };

            if (!dto.Preview && result.Created.Count > 0)
            {
                var labelPrefix = await LabelForAsync(string.Empty, dto.StructureId);
                _db.Units.AddRange(result.Created.Select(code => new Unit
                {
                    Code = code,
                    Type = dto.Type.Trim(),
                    StructureId = dto.StructureId,
                    Label = labelPrefix + code
                }));
                await _db.SaveChangesAsync();
            }

            return result;
        }

        private async Task ValidateUnitAsync(SaveUnitDto dto, Guid? currentId)
        {
            await _catalogs.EnsureValidAsync(Catalogs.UnitType, dto.Type, "Tipo de unidad");

            if (dto.StructureId.HasValue && !await _db.Structures.AnyAsync(s => s.Id == dto.StructureId.Value))
            {
                throw new BusinessRuleException("La estructura indicada no existe.");
            }

            var code = dto.Code.Trim().ToLower();
            if (await _db.Units.AnyAsync(u => u.Id != currentId && u.Code.ToLower() == code))
            {
                throw new ConflictException("Ya existe una unidad con ese código en el conjunto.");
            }
        }

        // ---------- Ficha ----------

        public async Task<UnitSummaryDto> GetSummaryAsync(Guid id, bool includeHistory)
        {
            var today = _dateTime.Today;
            var unit = await GetAsync(id);

            var ownerships = await _db.UnitOwnerships.Where(o => o.UnitId == id)
                .OrderByDescending(o => o.StartDate)
                .Select(OwnershipService.ToDto(today))
                .ToListAsync();
            var occupancies = await _db.Occupancies.Where(o => o.UnitId == id)
                .OrderByDescending(o => o.StartDate)
                .Select(OccupancyService.ToDto(today))
                .ToListAsync();

            return new UnitSummaryDto
            {
                Unit = unit,
                Owners = ownerships.Where(o => o.IsCurrent).ToList(),
                Occupants = occupancies.Where(o => o.IsCurrent).ToList(),
                PendingOccupancies = includeHistory ? occupancies.Where(o => o.Status == OccupancyStatus.Pending).ToList() : new(),
                PastOwners = includeHistory ? ownerships.Where(o => !o.IsCurrent).ToList() : null,
                PastOccupants = includeHistory
                    ? occupancies.Where(o => !o.IsCurrent && o.Status != OccupancyStatus.Pending).ToList()
                    : null
            };
        }

        // ---------- Proyecciones ----------

        private static Expression<Func<Unit, UnitDto>> ToDto(DateOnly today) => u => new UnitDto
        {
            Id = u.Id,
            Code = u.Code,
            Type = u.Type,
            StructureId = u.StructureId,
            Label = u.Label,
            Area = u.Area,
            Notes = u.Notes,
            IsActive = u.IsActive,
            // el estado se calcula de las ocupaciones vigentes; no hay un campo que alguien deba mantener
            OccupancyState = u.Occupancies.Any(o => o.Status == OccupancyStatus.Active && o.StartDate <= today
                    && (o.EndDate == null || o.EndDate >= today) && o.Relation == OccupancyRelation.Tenant)
                ? UnitOccupancyState.Rented
                : u.Occupancies.Any(o => o.Status == OccupancyStatus.Active && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                    ? UnitOccupancyState.OwnerOccupied
                    : UnitOccupancyState.Vacant,
            Owners = u.Ownerships
                .Where(o => o.StartDate <= today && (o.EndDate == null || o.EndDate >= today))
                .OrderBy(o => o.Person.FirstName)
                .Select(o => o.Person.FirstName + " " + o.Person.LastName)
                .ToList()
        };
    }
}
