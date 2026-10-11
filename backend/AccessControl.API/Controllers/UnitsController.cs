using AccessControl.API.Infrastructure;
using AccessControl.Application.Security;
using AccessControl.Application.Units;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.UnitsRead, Permissions.UnitsWrite)]
    public class UnitsController : ControllerBase
    {
        private readonly IUnitsService _units;
        private readonly IOwnershipService _ownerships;
        private readonly ICurrentUser _currentUser;
        private readonly AccessControl.Application.Vehicles.IVehicleService _vehicles;
        private readonly AccessControl.Application.Parking.IParkingService _parking;

        public UnitsController(IUnitsService units, IOwnershipService ownerships, ICurrentUser currentUser,
            AccessControl.Application.Vehicles.IVehicleService vehicles, AccessControl.Application.Parking.IParkingService parking)
        {
            _vehicles = vehicles;
            _parking = parking;
            _units = units;
            _ownerships = ownerships;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] UnitFilterDto filter)
        {
            return Ok(new ResponseModel<PaginatedResultDto<UnitDto>>(true, "Unidades consultadas.", await _units.SearchAsync(filter)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            return Ok(new ResponseModel<UnitDto>(true, "Unidad consultada.", await _units.GetAsync(id)));
        }

        // Ficha de la unidad. Sin people.read (portería) solo trae lo vigente, sin historial
        [HttpGet("{id}/summary")]
        public async Task<IActionResult> Summary(Guid id)
        {
            var summary = await _units.GetSummaryAsync(id, includeHistory: _currentUser.HasPermission(Permissions.PeopleRead));
            summary.Vehicles = await _vehicles.GetForUnitsAsync(new[] { id });
            summary.Parking = await _parking.GetSpacesAsync(
                new AccessControl.Common.DTOs.Parking.ParkingFilterDto { Type = AccessControl.Shared.Enums.ParkingSpaceType.Private }, new[] { id });
            return Ok(new ResponseModel<UnitSummaryDto>(true, "Ficha consultada.", summary));
        }

        [HttpPost]
        [HasPermission(Permissions.UnitsWrite)]
        public async Task<IActionResult> Create([FromBody] SaveUnitDto dto)
        {
            return Ok(new ResponseModel<UnitDto>(true, "Unidad creada.", await _units.CreateAsync(dto)));
        }

        [HttpPost("bulk")]
        [HasPermission(Permissions.UnitsWrite)]
        public async Task<IActionResult> Bulk([FromBody] BulkUnitsDto dto)
        {
            return Ok(new ResponseModel<BulkUnitsResultDto>(true, dto.Preview ? "Vista previa." : "Unidades creadas.", await _units.BulkCreateAsync(dto)));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.UnitsWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveUnitDto dto)
        {
            return Ok(new ResponseModel<UnitDto>(true, "Unidad actualizada.", await _units.UpdateAsync(id, dto)));
        }

        [HttpPost("{id}/activate")]
        [HasPermission(Permissions.UnitsWrite)]
        public async Task<IActionResult> Activate(Guid id)
        {
            return Ok(new ResponseModel<UnitDto>(true, "Unidad activada.", await _units.SetActiveAsync(id, true)));
        }

        [HttpPost("{id}/deactivate")]
        [HasPermission(Permissions.UnitsWrite)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            return Ok(new ResponseModel<UnitDto>(true, "Unidad desactivada.", await _units.SetActiveAsync(id, false)));
        }

        // ---- Propietarios de la unidad ----

        [HttpPost("{id}/owners")]
        [HasPermission(Permissions.OwnershipWrite)]
        public async Task<IActionResult> AddOwner(Guid id, [FromBody] AddOwnershipDto dto)
        {
            return Ok(new ResponseModel<OwnershipDto>(true, "Propietario registrado.", await _ownerships.AddAsync(id, dto)));
        }

        [HttpPost("{id}/owners/transfer")]
        [HasPermission(Permissions.OwnershipWrite)]
        public async Task<IActionResult> Transfer(Guid id, [FromBody] TransferOwnershipDto dto)
        {
            return Ok(new ResponseModel<List<OwnershipDto>>(true, "Cambio de propietario registrado.", await _ownerships.TransferAsync(id, dto)));
        }

        [HttpPost("owners/{ownershipId}/end")]
        [HasPermission(Permissions.OwnershipWrite)]
        public async Task<IActionResult> EndOwnership(Guid ownershipId, [FromBody] EndDateDto dto)
        {
            return Ok(new ResponseModel<OwnershipDto>(true, "Titularidad finalizada.", await _ownerships.EndAsync(ownershipId, dto.EndDate!.Value)));
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.UnitsRead, Permissions.UnitsWrite)]
    public class StructuresController : ControllerBase
    {
        private readonly IUnitsService _units;

        public StructuresController(IUnitsService units)
        {
            _units = units;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(new ResponseModel<List<StructureDto>>(true, "Estructuras consultadas.", await _units.GetStructuresAsync()));
        }

        [HttpPost]
        [HasPermission(Permissions.UnitsWrite)]
        public async Task<IActionResult> Create([FromBody] SaveStructureDto dto)
        {
            return Ok(new ResponseModel<StructureDto>(true, "Estructura creada.", await _units.CreateStructureAsync(dto)));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.UnitsWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveStructureDto dto)
        {
            return Ok(new ResponseModel<StructureDto>(true, "Estructura actualizada.", await _units.UpdateStructureAsync(id, dto)));
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.UnitsWrite)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _units.DeleteStructureAsync(id);
            return Ok(new ResponseModel<string>(true, "Estructura eliminada.", null));
        }
    }

    // Los catálogos los lee cualquier usuario con sesión (llenan listas de formularios); cambiarlos es configuración
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogsController : ControllerBase
    {
        private readonly ICatalogService _catalogs;

        public CatalogsController(ICatalogService catalogs)
        {
            _catalogs = catalogs;
        }

        [HttpGet("{catalog}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Get(string catalog, [FromQuery] bool all = false)
        {
            return Ok(new ResponseModel<List<CatalogItemDto>>(true, "Catálogo consultado.", await _catalogs.GetAsync(catalog, onlyActive: !all)));
        }

        [HttpPut("{catalog}")]
        [HasPermission(Permissions.SettingsManage)]
        public async Task<IActionResult> Replace(string catalog, [FromBody] List<CatalogItemDto> items)
        {
            return Ok(new ResponseModel<List<CatalogItemDto>>(true, "Catálogo actualizado.", await _catalogs.ReplaceAsync(catalog, items)));
        }
    }
}
