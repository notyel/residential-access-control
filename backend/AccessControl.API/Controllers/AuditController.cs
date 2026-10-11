using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Audit;
using AccessControl.Common.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    // Solo consulta: la bitácora no tiene operaciones para editar ni borrar
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.AuditRead)]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] AuditFilterDto filter)
        {
            var result = await _auditService.SearchAsync(filter);
            return Ok(new ResponseModel<PaginatedResultDto<AuditLogDto>>(true, "Bitácora consultada.", result));
        }
    }
}
