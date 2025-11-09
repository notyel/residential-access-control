using AccessControl.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AccessControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _visitService;
        public VisitsController(IVisitService visitService) => _visitService = visitService;

        [HttpPost]
    [Authorize(Policy = "GuardOnly")]
        public async Task<IActionResult> RegisterVisit([FromBody] CreateVisitDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var visit = await _visitService.RegisterVisitAsync(dto, userId);
            return Ok(visit);
        }

        [HttpPost("{id}/checkout")]
        [Authorize(Policy = "GuardOnly")]
        public async Task<IActionResult> Checkout(Guid id)
        {
            var v = await _visitService.CheckoutAsync(id);
            if (v == null) return NotFound();
            return Ok(v);
        }
    }
}
