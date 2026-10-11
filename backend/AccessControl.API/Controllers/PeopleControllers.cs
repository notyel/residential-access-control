using AccessControl.API.Infrastructure;
using AccessControl.Application.Security;
using AccessControl.Application.Units;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Person;
using AccessControl.Common.DTOs.Unit;
using AccessControl.Common.DTOs.User;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.PeopleRead, Permissions.PeopleWrite)]
    public class PersonsController : ControllerBase
    {
        private readonly IPeopleService _people;
        private readonly IPersonalDataPolicy _personalData;
        private readonly AccessControl.Application.Interfaces.IAuditService _audit;
        private readonly AccessControl.Persistence.ApplicationDbContext _db;

        public PersonsController(IPeopleService people, IPersonalDataPolicy personalData,
            AccessControl.Application.Interfaces.IAuditService audit, AccessControl.Persistence.ApplicationDbContext db)
        {
            _people = people;
            _personalData = personalData;
            _audit = audit;
            _db = db;
        }

        // Búsqueda exacta por documento (portería): GET api/persons?documentNumber=123
        [HttpGet]
        public async Task<IActionResult> FindByDocument([FromQuery] string? documentNumber)
        {
            return Ok(new ResponseModel<List<PersonDto>>(true, "Personas consultadas.", await _people.FindByDocumentAsync(documentNumber)));
        }

        // Directorio: GET api/persons/search?search=ana&onlyLinked=true
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] PersonFilterDto filter)
        {
            var result = await _people.SearchAsync(filter);
            _personalData.Apply(result.Items);
            return Ok(new ResponseModel<PaginatedResultDto<PersonListItemDto>>(true, "Personas consultadas.", result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var detail = await _people.GetAsync(id);
            _personalData.Apply(detail.Person);
            if (_personalData.CanViewIdentity)
            {
                // ver el documento completo desde una ficha queda en la bitácora
                _audit.Record("person.identity_viewed", entityType: "Person", entityId: id.ToString());
                await _db.SaveChangesAsync();
            }
            return Ok(new ResponseModel<PersonDetailDto>(true, "Persona consultada.", detail));
        }

        [HttpPost]
        [HasPermission(Permissions.PeopleWrite)]
        public async Task<IActionResult> Create([FromBody] CreatePersonDto dto)
        {
            return Ok(new ResponseModel<PersonDto>(true, "Persona creada.", await _people.CreateAsync(dto)));
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.PeopleWrite)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePersonDto dto)
        {
            if (!_personalData.CanViewIdentity)
            {
                // quien solo ve el documento enmascarado no puede cambiarlo: se conserva el que había
                dto.DocumentNumber = null;
            }
            return Ok(new ResponseModel<PersonDto>(true, "Persona actualizada.", await _people.UpdateAsync(id, dto)));
        }

        // "Dar acceso a la plataforma": crea la cuenta invitada de la persona
        [HttpPost("{id}/grant-access")]
        [HasPermission(Permissions.UsersManage)]
        public async Task<IActionResult> GrantAccess(Guid id, [FromBody] GrantAccessDto dto)
        {
            return Ok(new ResponseModel<UserDto>(true, "Cuenta creada. Se le envió la invitación.", await _people.GrantAccessAsync(id, dto)));
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.OccupancyWrite)]
    public class OccupanciesController : ControllerBase
    {
        private readonly IOccupancyService _occupancies;

        public OccupanciesController(IOccupancyService occupancies)
        {
            _occupancies = occupancies;
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddOccupancyDto dto)
        {
            return Ok(new ResponseModel<OccupancyDto>(true, "Ocupante registrado.", await _occupancies.AddAsync(dto)));
        }

        [HttpPost("{id}/end")]
        public async Task<IActionResult> End(Guid id, [FromBody] EndDateDto dto)
        {
            return Ok(new ResponseModel<OccupancyDto>(true, "Ocupación finalizada.", await _occupancies.EndAsync(id, dto)));
        }

        // Bandeja de solicitudes que hicieron los propietarios desde su portal
        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            return Ok(new ResponseModel<List<OccupancyDto>>(true, "Solicitudes consultadas.", await _occupancies.GetPendingAsync()));
        }

        [HttpPost("{id}/decide")]
        public async Task<IActionResult> Decide(Guid id, [FromBody] DecisionDto dto)
        {
            return Ok(new ResponseModel<OccupancyDto>(true, dto.Approve ? "Solicitud aprobada." : "Solicitud rechazada.", await _occupancies.DecideAsync(id, dto)));
        }
    }

    /// <summary>
    /// Portal de propietarios y residentes. Va aparte de los endpoints administrativos para que
    /// el alcance (solo sus unidades) no sea opcional: aquí todo pasa por IUnitScope.
    /// </summary>
    [ApiController]
    [Route("api/my")]
    [HasPermission(Permissions.UnitsReadOwn)]
    public class MyController : ControllerBase
    {
        private readonly IPortalService _portal;

        public MyController(IPortalService portal)
        {
            _portal = portal;
        }

        [HttpGet("units")]
        public async Task<IActionResult> Units()
        {
            return Ok(new ResponseModel<List<MyUnitDto>>(true, "Unidades consultadas.", await _portal.GetMyUnitsAsync()));
        }

        [HttpGet("occupants")]
        [HasPermission(Permissions.OccupancyWriteOwn)]
        public async Task<IActionResult> Occupants()
        {
            return Ok(new ResponseModel<List<OccupancyDto>>(true, await _portal.GetOccupantsPolicyAsync(), await _portal.GetMyOccupantsAsync()));
        }

        [HttpPost("occupants")]
        [HasPermission(Permissions.OccupancyWriteOwn)]
        public async Task<IActionResult> AddOccupant([FromBody] AddMyOccupantDto dto)
        {
            var occupancy = await _portal.AddOccupantAsync(dto);
            return Ok(new ResponseModel<OccupancyDto>(true,
                occupancy.Status == Shared.Enums.OccupancyStatus.Pending ? "Solicitud enviada a la administración." : "Ocupante registrado.",
                occupancy));
        }

        [HttpPut("occupants/{occupancyId}/contact")]
        [HasPermission(Permissions.OccupancyWriteOwn)]
        public async Task<IActionResult> UpdateContact(Guid occupancyId, [FromBody] UpdateContactDto dto)
        {
            return Ok(new ResponseModel<Common.DTOs.Person.PersonDto>(true, "Datos actualizados.", await _portal.UpdateOccupantContactAsync(occupancyId, dto)));
        }

        [HttpPost("occupants/{occupancyId}/end")]
        [HasPermission(Permissions.OccupancyWriteOwn)]
        public async Task<IActionResult> EndOccupancy(Guid occupancyId, [FromBody] EndDateDto dto)
        {
            return Ok(new ResponseModel<OccupancyDto>(true, "Ocupación finalizada.", await _portal.EndOccupancyAsync(occupancyId, dto)));
        }
    }
}
