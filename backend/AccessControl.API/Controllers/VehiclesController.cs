using AccessControl.API.Infrastructure;
using AccessControl.Application.Security;
using AccessControl.Application.Units;
using AccessControl.Application.Vehicles;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.VehiclesRead, Permissions.VehiclesWrite)]
    public class VehiclesController : ControllerBase
    {
        private readonly IVehicleService _vehicles;

        public VehiclesController(IVehicleService vehicles)
        {
            _vehicles = vehicles;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] VehicleFilterDto filter)
        {
            return Ok(new ResponseModel<PaginatedResultDto<VehicleDto>>(true, "Vehículos consultados.", await _vehicles.SearchAsync(filter)));
        }

        // Verificación de portería: registrado o no, unidad, responsable y si puede entrar hoy
        [HttpGet("by-plate/{plate}")]
        [HasPermission(Permissions.VehiclesRead, Permissions.AccessRegister)]
        public async Task<IActionResult> ByPlate(string plate)
        {
            return Ok(new ResponseModel<PlateCheckDto>(true, "Placa verificada.", await _vehicles.CheckPlateAsync(plate)));
        }

        // Vehículos dentro del conjunto, calculado de los eventos de acceso
        [HttpGet("inside")]
        [HasPermission(Permissions.VehiclesRead, Permissions.AccessRegister)]
        public async Task<IActionResult> Inside()
        {
            return Ok(new ResponseModel<List<VehicleInsideDto>>(true, "Vehículos dentro del conjunto.", await _vehicles.GetInsideAsync()));
        }

        // Solicitudes hechas desde el portal
        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            return Ok(new ResponseModel<List<VehicleDto>>(true, "Solicitudes consultadas.", await _vehicles.GetPendingAsync()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            return Ok(new ResponseModel<VehicleDetailDto>(true, "Vehículo consultado.", await _vehicles.GetAsync(id)));
        }

        [HttpPost]
        [HasPermission(Permissions.VehiclesWrite)]
        public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
        {
            return Ok(new ResponseModel<VehicleDto>(true, "Vehículo registrado.", await _vehicles.CreateAsync(dto, viaPortal: false)));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.VehiclesWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] VehicleDataDto dto)
        {
            return Ok(new ResponseModel<VehicleDto>(true, "Vehículo actualizado.", await _vehicles.UpdateAsync(id, dto)));
        }

        // Cambio de unidad o de responsable: cierra la autorización anterior y abre otra
        [HttpPost("{id}/reassign")]
        [HasPermission(Permissions.VehiclesWrite)]
        public async Task<IActionResult> Reassign(Guid id, [FromBody] ReassignVehicleDto dto)
        {
            return Ok(new ResponseModel<VehicleDto>(true, "Vehículo reasignado.", await _vehicles.ReassignAsync(id, dto)));
        }

        [HttpPost("{id}/suspend")]
        [HasPermission(Permissions.VehiclesWrite)]
        public async Task<IActionResult> Suspend(Guid id, [FromBody] VehicleReasonDto dto)
        {
            return Ok(new ResponseModel<VehicleDto>(true, "Autorización suspendida.", await _vehicles.SuspendAsync(id, dto.Reason)));
        }

        [HttpPost("{id}/reactivate")]
        [HasPermission(Permissions.VehiclesWrite)]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            return Ok(new ResponseModel<VehicleDto>(true, "Autorización reactivada.", await _vehicles.ReactivateAsync(id)));
        }

        [HttpPost("{id}/revoke")]
        [HasPermission(Permissions.VehiclesWrite)]
        public async Task<IActionResult> Revoke(Guid id, [FromBody] VehicleReasonDto dto)
        {
            return Ok(new ResponseModel<VehicleDto>(true, "Autorización revocada.", await _vehicles.RevokeAsync(id, dto.Reason)));
        }

        [HttpPost("{id}/decide")]
        [HasPermission(Permissions.VehiclesWrite)]
        public async Task<IActionResult> Decide(Guid id, [FromBody] VehicleDecisionDto dto)
        {
            return Ok(new ResponseModel<VehicleDto>(true, dto.Approve ? "Solicitud aprobada." : "Solicitud rechazada.",
                await _vehicles.DecideAsync(id, dto.Approve, dto.Reason)));
        }

        // Novedad del vehículo: se agrega, no reemplaza a las anteriores. También la registra portería
        [HttpPost("{id}/notes")]
        [HasPermission(Permissions.VehiclesWrite, Permissions.AccessRegister)]
        public async Task<IActionResult> AddNote(Guid id, [FromBody] VehicleNoteInputDto dto)
        {
            return Ok(new ResponseModel<VehicleDto>(true, "Novedad registrada.", await _vehicles.AddNoteAsync(id, dto.Text)));
        }
    }

    /// <summary>"Mis vehículos": solo sobre unidades con vínculo vigente y según la política del conjunto.</summary>
    [ApiController]
    [Route("api/my/vehicles")]
    [HasPermission(Permissions.VehiclesWriteOwn)]
    public class MyVehiclesController : ControllerBase
    {
        private readonly IVehicleService _vehicles;
        private readonly IUnitScope _scope;

        public MyVehiclesController(IVehicleService vehicles, IUnitScope scope)
        {
            _vehicles = vehicles;
            _scope = scope;
        }

        // El mensaje lleva la política (no, approval o direct) para que la pantalla ofrezca o no el registro
        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            var vehicles = await _vehicles.GetForUnitsAsync(await _scope.GetAccessibleUnitIdsAsync());
            return Ok(new ResponseModel<List<VehicleDto>>(true, await _vehicles.GetPortalPolicyAsync(), vehicles));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
        {
            var vehicle = await _vehicles.CreateAsync(dto, viaPortal: true);
            var message = vehicle.Status == AccessControl.Shared.Enums.VehicleAuthorizationStatus.Pending
                ? "Solicitud enviada. La administración debe aprobarla."
                : "Vehículo registrado.";
            return Ok(new ResponseModel<VehicleDto>(true, message, vehicle));
        }

        [HttpPost("{id}/revoke")]
        public async Task<IActionResult> Revoke(Guid id, [FromBody] VehicleReasonDto? dto)
        {
            var vehicle = await _vehicles.RevokeAsync(id, dto?.Reason, await _scope.GetAccessibleUnitIdsAsync());
            return Ok(new ResponseModel<VehicleDto>(true, "Vehículo retirado.", vehicle));
        }
    }
}
