using AccessControl.API.Infrastructure;
using AccessControl.Application.Access;
using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Application.Units;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Access;
using AccessControl.Common.DTOs.Common;
using AccessControl.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    /// <summary>Operación de portería: validar, registrar ingresos y salidas, rechazar y anular.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.AccessRegister)]
    public class AccessController : ControllerBase
    {
        private readonly IAccessService _access;
        private readonly IAuthorizationService _authorizations;
        private readonly IPolicyService _policies;

        public AccessController(IAccessService access, IAuthorizationService authorizations, IPolicyService policies)
        {
            _access = access;
            _authorizations = authorizations;
            _policies = policies;
        }

        // Reglas del conjunto que la pantalla de portería necesita para pedir los datos correctos.
        // Solo orienta al formulario: quien decide cada ingreso es CheckIn
        [HttpGet("context")]
        public async Task<IActionResult> Context()
        {
            var context = new GateContextDto
            {
                WithoutAuthorization = await _policies.GetAsync(ComplexPolicies.VisitsWithoutAuthorization),
                DocumentRequired = await _policies.GetBoolAsync(ComplexPolicies.VisitorDocumentRequired)
            };
            return Ok(new ResponseModel<GateContextDto>(true, "Reglas de portería.", context));
        }

        // Búsqueda única de portería: documento, nombre o unidad
        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string? term)
        {
            return Ok(new ResponseModel<LookupResultDto>(true, "Búsqueda realizada.", await _access.LookupAsync(term)));
        }

        [HttpGet("expected")]
        public async Task<IActionResult> Expected()
        {
            return Ok(new ResponseModel<List<AuthorizationDto>>(true, "Visitantes esperados.", await _authorizations.GetExpectedTodayAsync()));
        }

        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
        {
            return Ok(new ResponseModel<VisitDto>(true, "Ingreso registrado.", await _access.CheckInAsync(dto)));
        }

        [HttpPost("visits/{visitId}/check-out")]
        public async Task<IActionResult> CheckOut(Guid visitId, [FromBody] CheckOutDto? dto)
        {
            return Ok(new ResponseModel<VisitDto>(true, "Salida registrada.", await _access.CheckOutAsync(visitId, dto ?? new CheckOutDto())));
        }

        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromBody] RejectDto dto)
        {
            return Ok(new ResponseModel<AccessEventDto>(true, "Rechazo registrado.", await _access.RejectAsync(dto)));
        }

        // Entrada o salida de un propietario u ocupante
        [HttpPost("residents")]
        public async Task<IActionResult> Resident([FromBody] ResidentMovementDto dto)
        {
            return Ok(new ResponseModel<AccessEventDto>(true, "Movimiento registrado.", await _access.RegisterResidentAsync(dto)));
        }

        // Salida de un vehículo que quedó dentro sin su conductor
        [HttpPost("vehicles/{vehicleId}/exit")]
        public async Task<IActionResult> VehicleExit(Guid vehicleId, [FromBody] VehicleExitDto? dto)
        {
            return Ok(new ResponseModel<AccessEventDto>(true, "Salida del vehículo registrada.", await _access.RegisterVehicleExitAsync(vehicleId, dto?.Notes)));
        }

        // Los eventos no se editan ni se borran: un error se corrige anulándolo
        [HttpPost("events/{eventId}/void")]
        [HasPermission(Permissions.AccessVoid)]
        public async Task<IActionResult> Void(Guid eventId, [FromBody] VoidDto dto)
        {
            return Ok(new ResponseModel<AccessEventDto>(true, "Evento anulado.", await _access.VoidAsync(eventId, dto.Reason)));
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.VisitsRead, Permissions.VisitsReadOwn)]
    public class VisitsController : ControllerBase
    {
        private readonly IAccessService _access;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitScope _unitScope;
        private readonly IPersonalDataPolicy _personalData;

        public VisitsController(IAccessService access, ICurrentUser currentUser, IUnitScope unitScope, IPersonalDataPolicy personalData)
        {
            _personalData = personalData;
            _access = access;
            _currentUser = currentUser;
            _unitScope = unitScope;
        }

        // Con visits.read se ven todas; con visits.read_own, solo las de las unidades con vínculo vigente
        [HttpGet]
        public async Task<IActionResult> GetVisits([FromQuery] VisitFilterDto filter)
        {
            var result = await _access.GetVisitsAsync(filter, await UnitScopeAsync());
            // en listas e historiales el documento sale enmascarado para quien no tiene people.view_identity
            _personalData.Apply(result.Items);
            return Ok(new ResponseModel<PaginatedResultDto<VisitDto>>(true, "Visitas consultadas.", result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVisit(Guid id)
        {
            var visit = _personalData.Apply(await _access.GetVisitAsync(id, await UnitScopeAsync()));
            return Ok(new ResponseModel<VisitDto>(true, "Visita consultada.", visit));
        }

        /// <summary>null = todo el conjunto; una lista = solo esas unidades.</summary>
        private async Task<IReadOnlyCollection<Guid>?> UnitScopeAsync() =>
            _currentUser.HasPermission(Permissions.VisitsRead) ? null : await _unitScope.GetAccessibleUnitIdsAsync();
    }

    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.AuthorizationsRead, Permissions.AuthorizationsWrite)]
    public class AuthorizationsController : ControllerBase
    {
        private readonly IAuthorizationService _authorizations;

        public AuthorizationsController(IAuthorizationService authorizations)
        {
            _authorizations = authorizations;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] AuthorizationFilterDto filter)
        {
            return Ok(new ResponseModel<PaginatedResultDto<AuthorizationDto>>(true, "Autorizaciones consultadas.", await _authorizations.SearchAsync(filter)));
        }

        // La administración autoriza en nombre de una unidad, indicando quién de la unidad lo pidió
        [HttpPost]
        [HasPermission(Permissions.AuthorizationsWrite)]
        public async Task<IActionResult> Create([FromBody] CreateAuthorizationDto dto)
        {
            return Ok(new ResponseModel<AuthorizationDto>(true, "Autorización creada.", await _authorizations.CreateAsync(dto, viaPortal: false)));
        }

        [HttpPost("{id}/revoke")]
        [HasPermission(Permissions.AuthorizationsWrite)]
        public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokeDto? dto)
        {
            return Ok(new ResponseModel<AuthorizationDto>(true, "Autorización revocada.", await _authorizations.RevokeAsync(id, dto?.Reason)));
        }
    }

    /// <summary>Autorizaciones desde el portal: solo sobre unidades con vínculo vigente.</summary>
    [ApiController]
    [Route("api/my/authorizations")]
    [HasPermission(Permissions.AuthorizationsWriteOwn)]
    public class MyAuthorizationsController : ControllerBase
    {
        private readonly IAuthorizationService _authorizations;
        private readonly IUnitScope _unitScope;

        public MyAuthorizationsController(IAuthorizationService authorizations, IUnitScope unitScope)
        {
            _authorizations = authorizations;
            _unitScope = unitScope;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] AuthorizationFilterDto filter)
        {
            var result = await _authorizations.SearchAsync(filter, await _unitScope.GetAccessibleUnitIdsAsync());
            return Ok(new ResponseModel<PaginatedResultDto<AuthorizationDto>>(true, "Autorizaciones consultadas.", result));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAuthorizationDto dto)
        {
            return Ok(new ResponseModel<AuthorizationDto>(true, "Autorización creada.", await _authorizations.CreateAsync(dto, viaPortal: true)));
        }

        // La puede revocar quien la creó u otro ocupante de la misma unidad
        [HttpPost("{id}/revoke")]
        public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokeDto? dto)
        {
            var result = await _authorizations.RevokeAsync(id, dto?.Reason, await _unitScope.GetAccessibleUnitIdsAsync());
            return Ok(new ResponseModel<AuthorizationDto>(true, "Autorización revocada.", result));
        }
    }
}
