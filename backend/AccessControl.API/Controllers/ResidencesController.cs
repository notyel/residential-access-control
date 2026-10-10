using AccessControl.API.Infrastructure;
using AccessControl.Application.Interfaces;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Common;
using AccessControl.Common.DTOs.Residence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.ResidencesRead)]
    public class ResidencesController : ControllerBase
    {
        private readonly IResidencesService _residencesService;

        public ResidencesController(IResidencesService residencesService)
        {
            _residencesService = residencesService;
        }

        // GET: api/residences?search=torre&pageNumber=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _residencesService.SearchAsync(search, pageNumber, pageSize);
            return Ok(new ResponseModel<PaginatedResultDto<ResidenceDto>>(true, "Residencias consultadas.", result));
        }
    }
}
