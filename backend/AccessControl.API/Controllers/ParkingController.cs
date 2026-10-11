using AccessControl.API.Infrastructure;
using AccessControl.Application.Parking;
using AccessControl.Application.Security;
using AccessControl.Application.Units;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Parking;
using AccessControl.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.ParkingRead, Permissions.ParkingWrite)]
    public class ParkingController : ControllerBase
    {
        private readonly IParkingService _parking;

        public ParkingController(IParkingService parking)
        {
            _parking = parking;
        }

        // Espacios con su estado calculado; con from/to, la disponibilidad para un periodo futuro
        [HttpGet("spaces")]
        public async Task<IActionResult> Spaces([FromQuery] ParkingFilterDto filter)
        {
            return Ok(new ResponseModel<List<ParkingSpaceDto>>(true, "Parqueaderos consultados.", await _parking.GetSpacesAsync(filter)));
        }

        // Resumen por tipo y total: capacidad, ocupados, reservados, disponibles y fuera de servicio
        [HttpGet("availability")]
        [HasPermission(Permissions.ParkingRead, Permissions.AccessRegister)]
        public async Task<IActionResult> Availability([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(new ResponseModel<AvailabilityDto>(true, "Disponibilidad calculada.", await _parking.GetAvailabilityAsync(from, to)));
        }

        // Lo que portería puede ofrecer a una visita: solo espacios de visitantes libres
        [HttpGet("visitor-spaces")]
        [HasPermission(Permissions.ParkingRead, Permissions.AccessRegister)]
        public async Task<IActionResult> VisitorSpaces([FromQuery] string? vehicleType)
        {
            return Ok(new ResponseModel<List<ParkingSpaceDto>>(true, "Espacios disponibles.", await _parking.GetFreeVisitorSpacesAsync(vehicleType)));
        }

        [HttpGet("spaces/{id}")]
        public async Task<IActionResult> Space(Guid id)
        {
            return Ok(new ResponseModel<ParkingSpaceDetailDto>(true, "Parqueadero consultado.", await _parking.GetSpaceAsync(id)));
        }

        [HttpPost("spaces")]
        [HasPermission(Permissions.ParkingWrite)]
        public async Task<IActionResult> Create([FromBody] SaveParkingSpaceDto dto)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Parqueadero creado.", await _parking.CreateAsync(dto)));
        }

        [HttpPost("spaces/bulk")]
        [HasPermission(Permissions.ParkingWrite)]
        public async Task<IActionResult> CreateBulk([FromBody] BulkParkingSpacesDto dto)
        {
            var result = await _parking.CreateBulkAsync(dto);
            return Ok(new ResponseModel<BulkParkingResultDto>(true, $"{result.Created.Count} parqueaderos creados.", result));
        }

        [HttpPut("spaces/{id}")]
        [HasPermission(Permissions.ParkingWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveParkingSpaceDto dto)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Parqueadero actualizado.", await _parking.UpdateAsync(id, dto)));
        }

        // Un espacio no se borra: se da de baja y conserva su historial
        [HttpPost("spaces/{id}/deactivate")]
        [HasPermission(Permissions.ParkingWrite)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Parqueadero dado de baja.", await _parking.SetActiveAsync(id, false)));
        }

        [HttpPost("spaces/{id}/activate")]
        [HasPermission(Permissions.ParkingWrite)]
        public async Task<IActionResult> Activate(Guid id)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Parqueadero reactivado.", await _parking.SetActiveAsync(id, true)));
        }

        [HttpPost("spaces/{id}/holder")]
        [HasPermission(Permissions.ParkingWrite)]
        public async Task<IActionResult> SetHolder(Guid id, [FromBody] SetHolderDto dto)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Parqueadero asociado a la unidad.", await _parking.SetHolderAsync(id, dto)));
        }

        [HttpPost("spaces/{id}/holder/end")]
        [HasPermission(Permissions.ParkingWrite)]
        public async Task<IActionResult> EndHolder(Guid id)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Asociación finalizada.", await _parking.EndHolderAsync(id)));
        }

        // Fuera de servicio: el único estado que se registra a mano, como un periodo
        [HttpPost("spaces/{id}/out-of-service")]
        [HasPermission(Permissions.ParkingWrite)]
        public async Task<IActionResult> OutOfService(Guid id, [FromBody] OutOfServiceDto dto)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Parqueadero fuera de servicio.", await _parking.AddOutOfServiceAsync(id, dto)));
        }

        [HttpPost("out-of-service/{id}/end")]
        [HasPermission(Permissions.ParkingWrite)]
        public async Task<IActionResult> EndOutOfService(Guid id)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "El parqueadero vuelve a estar en servicio.", await _parking.EndOutOfServiceAsync(id)));
        }

        [HttpPost("assignments")]
        [HasPermission(Permissions.ParkingAssign)]
        public async Task<IActionResult> Assign([FromBody] PlanParkingDto dto)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Asignación registrada.", await _parking.PlanAsync(dto, ParkingAssignmentClass.Temporary)));
        }

        [HttpPost("reservations")]
        [HasPermission(Permissions.ParkingReserve)]
        public async Task<IActionResult> Reserve([FromBody] PlanParkingDto dto)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Reserva registrada.", await _parking.PlanAsync(dto, ParkingAssignmentClass.Reservation)));
        }

        [HttpPost("assignments/{id}/cancel")]
        [HasPermission(Permissions.ParkingAssign, Permissions.ParkingReserve)]
        public async Task<IActionResult> Cancel(Guid id)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Asignación cancelada.", await _parking.CancelAssignmentAsync(id)));
        }

        // Registro manual de un vehículo que ya está dentro; el ingreso normal lo hace portería al validar
        [HttpPost("spaces/{id}/occupy")]
        [HasPermission(Permissions.ParkingAssign, Permissions.AccessRegister)]
        public async Task<IActionResult> Occupy(Guid id, [FromBody] OccupyParkingDto dto)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Parqueadero ocupado.", await _parking.OccupyAsync(id, dto)));
        }

        // Corrección manual; lo normal es que la salida del vehículo libere el espacio
        [HttpPost("occupancies/{id}/release")]
        [HasPermission(Permissions.ParkingAssign, Permissions.AccessRegister)]
        public async Task<IActionResult> Release(Guid id)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Parqueadero liberado.", await _parking.ReleaseAsync(id)));
        }

        [HttpPost("spaces/{id}/notes")]
        [HasPermission(Permissions.ParkingWrite, Permissions.ParkingAssign, Permissions.AccessRegister)]
        public async Task<IActionResult> AddNote(Guid id, [FromBody] ParkingNoteInputDto dto)
        {
            return Ok(new ResponseModel<ParkingSpaceDto>(true, "Novedad registrada.", await _parking.AddNoteAsync(id, dto.Text)));
        }
    }

    /// <summary>"Mis parqueaderos": los privados de las unidades del usuario.</summary>
    [ApiController]
    [Route("api/my/parking")]
    [HasPermission(Permissions.ParkingReadOwn)]
    public class MyParkingController : ControllerBase
    {
        private readonly IParkingService _parking;
        private readonly IUnitScope _scope;

        public MyParkingController(IParkingService parking, IUnitScope scope)
        {
            _parking = parking;
            _scope = scope;
        }

        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            var spaces = await _parking.GetSpacesAsync(new ParkingFilterDto { Type = ParkingSpaceType.Private }, await _scope.GetAccessibleUnitIdsAsync());
            return Ok(new ResponseModel<List<ParkingSpaceDto>>(true, "Parqueaderos consultados.", spaces));
        }
    }
}
