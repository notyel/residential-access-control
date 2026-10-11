using AccessControl.API.Infrastructure;
using AccessControl.Application.Policies;
using AccessControl.Application.Security;
using AccessControl.Common.DTOs;
using AccessControl.Common.DTOs.Policy;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [HasPermission(Permissions.SettingsRead, Permissions.SettingsManage)]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _policies;

        public PoliciesController(IPolicyService policies)
        {
            _policies = policies;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(new ResponseModel<List<PolicyDto>>(true, "Políticas consultadas.", await _policies.GetAllAsync()));
        }

        // Cuerpo: { "clave": "valor", ... } solo con las políticas que cambian
        [HttpPut]
        [HasPermission(Permissions.SettingsManage)]
        public async Task<IActionResult> Update([FromBody] Dictionary<string, string> values)
        {
            return Ok(new ResponseModel<List<PolicyDto>>(true, "Políticas actualizadas.", await _policies.UpdateAsync(values)));
        }
    }
}
