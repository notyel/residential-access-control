using AccessControl.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResidentsController : ControllerBase
    {
        private readonly IResidentRepository _residentRepository;

        public ResidentsController(IResidentRepository residentRepository)
        {
            _residentRepository = residentRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var residents = await _residentRepository.GetAllAsync();
            return Ok(residents);
        }
    }
}
